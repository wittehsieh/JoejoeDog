/* 
 * Author:  Johanan Round
 * Package: Editor Window Fullscreen
 * License: Unity Asset Store EULA (Editor extension asset. Requires 1 license per machine.)
 */

using UnityEngine;
using UnityEditor;
using FS = EditorWindowFullscreen.EditorFullscreenState;

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace EditorWindowFullscreen
{
    public static class EditorWindowExtensions
    {

        public enum ShowMode
        {
            NormalWindow,
            PopupMenu,
            Utility,
            NoShadow,
            MainWindow,
            AuxWindow
        }

        public static EditorWindow CreateWindow(Type windowType)
        {
            EditorWindow newEditorWin = (EditorWindow)ScriptableObject.CreateInstance(windowType);
            return newEditorWin;
        }

        /// <summary> Show the EditorWindow with a specified mode </summary>
        public static void ShowWithMode(this EditorWindow editorWindow, ShowMode showMode)
        {
            MethodInfo showWithMode = typeof(EditorWindow).GetMethod("ShowWithMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            showWithMode.Invoke(editorWindow, new object[] { (int)showMode });
        }

        /// <summary> Make the EditorWindow fullscreen. </summary>
        public static void SetFullscreen(this EditorWindow editorWindow)
        {
            editorWindow.SetFullscreen(true);
        }

        /// <summary> Make the EditorWindow fullscreen, or return to how it was. </summary>
        public static void SetFullscreen(this EditorWindow editorWindow, bool setFullscreen)
        {
            editorWindow.SetFullscreen(setFullscreen, editorWindow.GetPointOnWindow());
        }

        /// <summary> Make the EditorWindow fullscreen, or return to how it was. Opens the fullscreen window on the screen at a specified position. </summary>
        public static void SetFullscreen(this EditorWindow editorWindow, bool setFullscreen, Vector2 atPosition)
        {
            Type windowType = editorWindow.GetWindowType();
            var fullscreenState = EditorFullscreenState.FindWindowState(editorWindow);

            CursorLockMode currentCursorLockMode = Cursor.lockState;

            if (setFullscreen == false)
            {
                if (fullscreenState.EditorWin != null)
                {
                    if (fullscreenState.CloseOnExitFullscreen)
                    {
                        //Close the window
                        editorWindow.Close();
                    }
                    else
                    {
                        //Restore the window
                        editorWindow.SetBorderlessPosition(fullscreenState.PreFullscreenPosition);
                        fullscreenState.EditorWin.minSize = fullscreenState.PreFullscreenMinSize;
                        fullscreenState.EditorWin.maxSize = fullscreenState.PreFullscreenMaxSize;
                        fullscreenState.EditorWin.position = fullscreenState.PreFullscreenPosition;

                        if (editorWindow.maximized != fullscreenState.PreFullscreenMaximized)
                            editorWindow.maximized = fullscreenState.PreFullscreenMaximized;
                    }
                }

                if (editorWindow.GetWindowType() == FS.gameViewType)
                    Unsupported.SetAllowCursorLock(false); //Unlock the cursor when exiting game fullscreen

                if (fullscreenState.UnfocusedGameViewOnEnteringFullscreen == true)
                {
                    //Refocus the first docked game view
                    var gameView = GetDockedGameView(editorWindow, false);
                    if (gameView != null) gameView.Focus();
                }

            }
            else
            {
                if (!fullscreenState.IsFullscreen)
                {
                    fullscreenState.PreFullscreenPosition = editorWindow.position;
                    fullscreenState.PreFullscreenPosition.y -= FS.windowTopPadding;
                    fullscreenState.PreFullscreenMinSize = editorWindow.minSize;
                    fullscreenState.PreFullscreenMaxSize = editorWindow.maxSize;
                    fullscreenState.PreFullscreenMaximized = editorWindow.maximized;
                }

                editorWindow.SetWindowTitle("FULLSCREEN_WINDOW_" + editorWindow.GetInstanceID(), true);

                if (!editorWindow.IsFullscreen())
                {
                    editorWindow.maximized = false;

                    if (fullscreenState.ShowTopTabs)
                    {
                        editorWindow.Show();
                    }
                    else
                    {
                        editorWindow.ShowWithMode(ShowMode.PopupMenu);
                        editorWindow.SetSaveToLayout(true);
                    }

                    fullscreenState.FullscreenAtPosition = atPosition;
                    editorWindow.SetBorderlessPosition(new Rect(atPosition.x, atPosition.y, 100, 100));
                }
                else if (fullscreenState.IsFullscreen)
                {
                    //If already fullscreen, resize slightly to make sure the taskbar gets covered (E.g. when loading fullscreen state on startup)
                    var tempBounds = editorWindow.position;
                    tempBounds.yMax -= 1;
                    editorWindow.SetBorderlessPosition(tempBounds);
                }

                fullscreenState.ScreenBounds = editorWindow.MakeFullscreenWindow(!fullscreenState.ShowTopToolbar, atPosition);
                editorWindow.ExitFullscreenForOtherWindowsOnScreen(atPosition);

                fullscreenState.WindowName = editorWindow.name;
                fullscreenState.EditorWin = editorWindow;
                fullscreenState.IsFullscreen = true;

                //Usability improvement for Unity bug where only one visible game window accepts input. (Unfocus docked game views if opening fullscreen view on the same screen.)
                try
                {
                    var gameView = GetDockedGameView(editorWindow, true);
                    if (gameView != null)
                    {
                        bool onSameDisplay = EditorDisplay.ClosestToPoint(gameView.position.center).Bounds.Contains(atPosition);
                        var hostView = gameView.GetHostView();
                        if (onSameDisplay && hostView != null && FS.dockAreaType != null)
                        {
                            FieldInfo m_Panes = FS.dockAreaType.GetField("m_Panes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            var dockAreaPanes = (List<EditorWindow>)m_Panes.GetValue(hostView);
                            foreach (var sibling in dockAreaPanes)
                            {
                                if (sibling.GetType() != FS.gameViewType)
                                {
                                    sibling.Focus(); //Focus the first non-game sibling of the docked game view
                                    fullscreenState.UnfocusedGameViewOnEnteringFullscreen = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    if (FS.LogNonFatalErrors) Debug.LogException(e);
                }

                editorWindow.Focus();

                Cursor.lockState = currentCursorLockMode; //Ensure that the cursor lock mode remains the same when entering fullscreen
            }

            FS.SaveFullscreenState();
            FS.TriggerFullscreenEvent(editorWindow, windowType, atPosition, setFullscreen);
        }

        /// <summary> Set fullscreen with the option to show or hide the top tabs </summary>
        public static void SetFullscreen(this EditorWindow editorWindow, bool setFullscreen, bool showTopToolbar)
        {
            editorWindow.SetFullscreen(setFullscreen, editorWindow.GetPointOnWindow(), showTopToolbar);
        }

        /// <summary> Set fullscreen with the option to show or hide the top tabs </summary>
        public static void SetFullscreen(this EditorWindow editorWindow, bool setFullscreen, Vector2 atPosition, bool showTopToolbar)
        {
            var fullscreenState = EditorFullscreenState.FindWindowState(editorWindow);

            if (editorWindow.GetWindowType() == FS.gameViewType)
            {
                if (showTopToolbar && !fullscreenState.ShowTopToolbar)
                {
                    fullscreenState.CursorLockModePreShowTopToolbar = Cursor.lockState;
                    Cursor.lockState = CursorLockMode.None; //Enable cursor when top tab is enabled
                }
                else if (!showTopToolbar && fullscreenState.ShowTopToolbar)
                {
                    Cursor.lockState = fullscreenState.CursorLockModePreShowTopToolbar; //Reset cursor lock mode when top tab is disabled
                }
            }

            fullscreenState.ShowTopToolbar = showTopToolbar;
            SetFullscreen(fullscreenState.EditorWin, setFullscreen, atPosition);
        }

        /// <summary> Toggle fullscreen for a window. </summary>
        public static bool ToggleFullscreen(this EditorWindow editorWindow)
        {
            return ToggleFullscreen(editorWindow, editorWindow.GetPointOnWindow());
        }

        /// <summary> Toggle fullscreen for a window, on the screen at a specified position. </summary>
        public static bool ToggleFullscreen(this EditorWindow editorWindow, Vector2 atPosition)
        {
            if (editorWindow != null)
            {
                bool setFullScreen = !editorWindow.IsFullscreen(atPosition);
                editorWindow.SetFullscreen(setFullScreen, atPosition);
                return setFullScreen;
            }
            else
            {
                SetFullscreen(null, false);
                return false;
            }
        }

        /// <summary> Make the EditorWindow become a fullscreen window </summary>
        private static Rect MakeFullscreenWindow(this EditorWindow editorWindow)
        {
            return editorWindow.MakeFullscreenWindow(false);
        }

        /// <summary> Make the EditorWindow into a fullscreen window, with the option to show the top tabs </summary>
        private static Rect MakeFullscreenWindow(this EditorWindow editorWindow, bool hideTopToolbar)
        {
            return editorWindow.MakeFullscreenWindow(hideTopToolbar, editorWindow.GetPointOnWindow());
        }

        /// <summary> Make the EditorWindow into a fullscreen window, with the option to show the top tabs. Opens the fullscreen window on the screen at a specified position. </summary>
        private static Rect MakeFullscreenWindow(this EditorWindow editorWindow, bool hideTopToolbar, Vector2 atPosition)
        {
            var winRect = EditorDisplay.ClosestToPoint(atPosition).Bounds;

            if (hideTopToolbar == true)
            {
                /*Move the top tab off the screen*/
                winRect.y -= FS.topTabFullHeight;
                winRect.height += FS.topTabFullHeight;
            }

            editorWindow.SetBorderlessPosition(winRect, hideTopToolbar);

#if UNITY_STANDALONE_WIN
            //Fix positioning bug when monitors have differing scale
            var sysDisplays = SystemDisplay.GetAllDisplays();
            var fullscreenDisp = sysDisplays.ClosestToPoint(atPosition);
            var mainWindowDisp = sysDisplays.WithMainWindow();
            if (fullscreenDisp != mainWindowDisp)
            {
                //Check if there is a scaling difference between the main window display and the fullscreen display.
                float fullscreenDispScale = fullscreenDisp.PixelWidth / fullscreenDisp.Bounds.width;
                float mainWindowDispScale = mainWindowDisp.PixelWidth / mainWindowDisp.Bounds.width;
                if (fullscreenDispScale != mainWindowDispScale)
                {
                    //There is a scaling difference, so adjust the winRect to account for the scaling. (Because the window is positioned based on the scaling of the main window display).
                    float relativeScale = fullscreenDispScale / mainWindowDispScale;
                    winRect.x = winRect.x * relativeScale;
                    winRect.y = winRect.y * relativeScale;
                    winRect.width = winRect.width * relativeScale;
                    winRect.height = winRect.height * relativeScale;
                }
                editorWindow.SetBorderlessPosition(winRect, hideTopToolbar);

                //Call system SetWindowPosition to make sure the window covers the taskbar
                SystemDisplay.MakeWindowCoverTaskBar(editorWindow.GetWindowTitle(), fullscreenDisp);

                //Hide the top toolbar if necessary.
                editorWindow.SetToolbarVisibilityAtPos(winRect, hideTopToolbar, false);
            }
#endif
            return winRect;
        }

        /// <summary> Make the EditorWindow borderless and give it an accurate position and size </summary>
        public static void SetBorderlessPosition(this EditorWindow editorWindow, Rect position)
        {
            SetBorderlessPosition(editorWindow, position, false);
        }

        /// <summary> Make the EditorWindow borderless and give it an accurate position and size. Optionally hide the top toolbar of the window if one exists. </summary>
        public static void SetBorderlessPosition(this EditorWindow editorWindow, Rect position, bool hideTopToolbar)
        {
            object hostView = editorWindow.GetHostView();
            position = editorWindow.SetToolbarVisibilityAtPos(position, hideTopToolbar, true);

            /*Make sure the window is borderless*/
            if (editorWindow.minSize != editorWindow.maxSize)
            {
                editorWindow.position = position;
                editorWindow.minSize = position.size;
                editorWindow.maxSize = position.size;
            }

            editorWindow.position = position;

            if (!FS.calculatedBorderlessOffsets)
            {
                FS.calculatedBorderlessOffsets = true;

                /*Get the Y offset of the window from the position it was assigned.  (Because of strange behaviour of the EditorWindow.position setter)*/
                FS.borderlessOffsetY = 0; //Obsolete

                /*Attempt to find the top tab full height and windowTopPadding. If they can't be found the initial values are used.*/
                try
                {
                    MethodInfo GetBorderSize = FS.hostViewType.GetMethod("GetBorderSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    RectOffset hostViewBorderSize = (RectOffset)GetBorderSize.Invoke(hostView, null);
                    FS.topTabFullHeight = FS.borderlessOffsetY + hostViewBorderSize.top;

                    FieldInfo kTabHeight = null;
                    if (FS.dockAreaType != null) kTabHeight = FS.dockAreaType.GetField("kTabHeight", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    float tabHeight = FS.sceneViewToolbarHeight;
                    if (kTabHeight != null) tabHeight = (float)kTabHeight.GetValue(hostView);
                    FS.windowTopPadding = Mathf.Max(0, FS.borderlessOffsetY - tabHeight);
                }
                catch (System.Exception e)
                {
                    if (FS.LogNonFatalErrors) Debug.LogException(e);
                }
            }

            /*Re-adjust the position of the window, taking into account the Y offset and top padding so that the window ends up where it should be*/
            position.y += FS.borderlessOffsetY - FS.windowTopPadding;
            position.height -= FS.borderlessOffsetY;

            editorWindow.minSize = position.size;
            editorWindow.maxSize = position.size;
            editorWindow.position = position;

            editorWindow.SetToolbarVisibilityAtPos(position, hideTopToolbar, false);
        }

        /// <summary>
        /// For internal use only. Don't use this to show/hide the toolbar. Instead use a SetFullscreen overload.
        /// </summary>
        private static Rect SetToolbarVisibilityAtPos(this EditorWindow editorWindow, Rect position, bool hideTopToolbar, bool useFallbackHide)
        {
            //Set toolbar visibility
            Type windowType = editorWindow.GetWindowType();
            FieldInfo toolbarVisible = editorWindow.GetType().GetField("toolbarVisible");
            object hostView = editorWindow.GetHostView();
            PropertyInfo viewPos = null;
            MethodInfo setPos = null;
            bool offsetHostViewToHideToolbar = false;
            bool fallbackOffsetToolbar = false;
            float toolbarHeight = editorWindow.GetShowMode() == ShowMode.PopupMenu ? FS.sceneViewToolbarHeight : FS.topTabFullHeight;
            if (toolbarVisible != null)
            {
                //Set visibility using the property if the EditorWindow has that option.
                toolbarVisible.SetValue(editorWindow, !hideTopToolbar);
            }
            else if (hideTopToolbar && windowType == typeof(SceneView) || windowType == FS.gameViewType)
            {
                try
                {
                    //Offset the hostview within its container to hide the toolbar
                    viewPos = FS.viewType.GetProperty("position", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    setPos = FS.viewType.GetMethod("SetPosition", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    if (hostView == null || viewPos == null || setPos == null)
                    {
                        if (FS.LogNonFatalErrors) Debug.LogError("One or more properties do not exist which are required to hide the toolbar. Using fallback. hostView: " + hostView + " viewPos: " + viewPos + " setPos: " + setPos);
                        fallbackOffsetToolbar = true;
                    }
                    else
                    {
                        offsetHostViewToHideToolbar = true;
                    }
                }
                catch
                {
                    fallbackOffsetToolbar = true;
                }
                finally
                {
                    if (useFallbackHide)
                    {
                        if (fallbackOffsetToolbar)
                        {
                            //Fallback: Offset the entire window upwards in order to hide the top toolbar on the current screen
                            position.y = -toolbarHeight;
                            position.height += toolbarHeight;
                        }
                    }
                }
            }

            if (!useFallbackHide && hideTopToolbar && offsetHostViewToHideToolbar)
            {
                //Apply the toolbar offset to the Host View, to hide the toolbar
                Rect currentViewPos;
                currentViewPos = (Rect)viewPos.GetValue(hostView, null);
                currentViewPos.y = -toolbarHeight;
                currentViewPos.height += toolbarHeight;
                setPos.Invoke(hostView, new object[] { currentViewPos });
            }

            return position;
        }


        /// <summary> Returns a point on the window no matter if it is currently fullscreen or non-fullscreen.</summary>
        public static Vector2 GetPointOnWindow(this EditorWindow editorWindow)
        {
            Vector2 pointOnWindow;
            var state = FS.FindWindowState(editorWindow);
            if (state != null && state.IsFullscreen)
            {
                //When the window is fullscreen sometimes position returns incorrect co-ordinates, so use the FullscreenAtPosition for fullscreen windows.
                pointOnWindow = state.FullscreenAtPosition;
            }
            else
                pointOnWindow = editorWindow.position.center;
            return pointOnWindow;
        }

        /// <summary> Returns true if the EditorWindow is currently fullscreen on its current screen </summary>
        public static bool IsFullscreen(this EditorWindow editorWindow)
        {
            return IsFullscreen(editorWindow, editorWindow.GetPointOnWindow());
        }

        /// <summary> Returns true if the EditorWindow is currently fullscreen on the screen at a position </summary>
        public static bool IsFullscreen(this EditorWindow editorWindow, Vector2 atPosition)
        {
            var fullscreenState = EditorFullscreenState.FindWindowState(editorWindow);
            return fullscreenState.IsFullscreen && editorWindow.IsFullscreenOnDisplay(EditorDisplay.ClosestToPoint(atPosition));
        }

        /// <summary> Returns true if the EditorWindow is currently fullscreen on the screen at a position </summary>
        public static bool IsFullscreenOnDisplay(this EditorWindow editorWindow, EditorDisplay display)
        {
            Rect containerPosition = editorWindow.GetContainerPosition();
            return containerPosition.Contains(display.Bounds) && display.Bounds.width == containerPosition.width;
        }

        /// <summary> Exit fullscreen for other windows on the screen at the specified position. Returns true if at least one fullscreen was closed. </summary>
        public static bool ExitFullscreenForOtherWindowsOnScreen(this EditorWindow editorWindow, Vector2 screenAtPosition)
        {
            bool closedAFullscreen = false;
            var allWinStates = FS.fullscreenState.window.ToArray();
            EditorDisplay display = EditorDisplay.ClosestToPoint(screenAtPosition);
            foreach (var win in allWinStates)
            {
                if (win.IsFullscreen && win.EditorWin != null && win.EditorWin != editorWindow && win.EditorWin.IsFullscreenOnDisplay(display))
                {
                    win.EditorWin.SetFullscreen(false);
                    closedAFullscreen = true;
                }
            }
            return closedAFullscreen;
        }

        /// <summary> Get the EditorDisplay which currently contains the fullscreen editorWindow </summary>
        internal static EditorDisplay GetFullscreenDisplay(this EditorWindow editorWindow)
        {
            var fullscreenState = EditorFullscreenState.FindWindowState(editorWindow);
            EditorDisplay display = null;
            if (fullscreenState != null)
                display = EditorDisplay.ClosestToPoint(fullscreenState.FullscreenAtPosition);
            return display;
        }

        /// <summary> Get the window type of the editor window. (CustomSceneView and CustomGameView return their base type) </summary>
        public static Type GetWindowType(this EditorWindow editorWindow)
        {
            return FS.GetWindowType(editorWindow.GetType());
        }

        /// <summary> Get the ShowMode of the EditorWindow </summary>
        internal static ShowMode GetShowMode(this EditorWindow editorWindow)
        {
            try
            {
                var containerWindow = GetContainerWindow(editorWindow);
                FieldInfo showMode = FS.containerWindowType.GetField("m_ShowMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (ShowMode)showMode.GetValue(containerWindow);
            }
            catch
            {
                return ShowMode.PopupMenu;
            }
        }

        /// <summary> Get the ContainerWindow which contains the EditorWindow </summary>
        internal static object GetContainerWindow(this EditorWindow editorWindow)
        {
            PropertyInfo window = FS.viewType.GetProperty("window", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            object containerWin = null;
            var hostView = editorWindow.GetHostView();
            if (hostView != null) containerWin = window.GetValue(hostView, null);
            return containerWin;
        }

        /// <summary> Get the window position and size of the Container Window which contains the EditorWindow </summary>
        public static Rect GetContainerPosition(this EditorWindow editorWindow)
        {
            var containerWindow = GetContainerWindow(editorWindow);
            PropertyInfo containerPosition = FS.containerWindowType.GetProperty("position", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (containerWindow == null)
                return editorWindow.position;
            else
                return (Rect)containerPosition.GetValue(containerWindow, null);
        }

        /// <summary> Get the screen point of the relative point in the Container Window of the Editor Window </summary>
        internal static Vector2 ContainerWindowPointToScreenPoint(this EditorWindow editorWindow, Vector2 windowPoint)
        {
            var containerWindow = GetContainerWindow(editorWindow);
            MethodInfo windowToScreenPoint = FS.containerWindowType.GetMethod("WindowToScreenPoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return (Vector2)windowToScreenPoint.Invoke(containerWindow, new object[] { windowPoint });
        }

        /// <summary> Get the HostView which contains the EditorWindow </summary>
        internal static object GetHostView(this EditorWindow editorWindow)
        {
            try
            {
                FieldInfo parent = typeof(EditorWindow).GetField("m_Parent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return parent.GetValue(editorWindow);
            }
            catch
            {
                return null;
            }
        }

        /// <summary> Returns true if the EditorWindow is docked </summary>
        internal static bool IsDocked(this EditorWindow editorWindow)
        {
            try
            {
                PropertyInfo prop = typeof(EditorWindow).GetProperty("docked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (bool)prop.GetValue(editorWindow, null);
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Returns true if the EditorWindow has the focus within its dock </summary>
        internal static bool HasFocusInDock(this EditorWindow editorWindow)
        {
            try
            {
                PropertyInfo prop = typeof(EditorWindow).GetProperty("hasFocus", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (bool)prop.GetValue(editorWindow, null);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the currently docked game view if it exists and is currently focused.
        /// </summary>
        private static EditorWindow GetDockedGameView(EditorWindow excludeWindowFromSearch, bool onlyGetFocusedGameView)
        {
            var gameViews = (EditorWindow[])Resources.FindObjectsOfTypeAll(FS.gameViewType);
            foreach (var win in gameViews)
            {
                if (win != excludeWindowFromSearch)
                {
                    if (!win.GetWindowTitle().Contains("FULLSCREEN") && win.IsDocked() && (!onlyGetFocusedGameView || win.HasFocusInDock()))
                    {
                        return win;
                    }
                }
            }
            return null;
        }

        /// <summary> Make the Editor Window save to the Window Layout </summary>
        internal static void SetSaveToLayout(this EditorWindow editorWindow, bool saveToLayout)
        {
            try
            {
                FieldInfo dontSaveToLayout = FS.containerWindowType.GetField("m_DontSaveToLayout", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                dontSaveToLayout.SetValue(editorWindow.GetContainerWindow(), false);
            }
            catch (System.Exception e)
            {
                if (FS.LogNonFatalErrors) Debug.LogException(e);
            }
        }
        internal static bool GetSaveToLayout(this EditorWindow editorWindow)
        {
            try
            {
                FieldInfo dontSaveToLayout = FS.containerWindowType.GetField("m_DontSaveToLayout", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (bool)dontSaveToLayout.GetValue(editorWindow.GetContainerWindow());
            }
            catch (System.Exception e)
            {
                if (FS.LogNonFatalErrors) Debug.LogException(e);
            }
            return false;
        }

        internal static string GetWindowTitle(this EditorWindow editorWindow)
        {
#if UNITY_5_4_OR_NEWER || UNITY_5_3 || UNITY_5_2 || UNITY_5_1
            return editorWindow.titleContent.text;
#else
            return editorWindow.title;
#endif
        }

        internal static void SetWindowTitle(this EditorWindow editorWindow, string title)
        {
            SetWindowTitle(editorWindow, title, false);
        }

        internal static void SetWindowTitle(this EditorWindow editorWindow, string title, bool resetIcon)
        {
#if UNITY_5_4_OR_NEWER || UNITY_5_3 || UNITY_5_2 || UNITY_5_1
            if (resetIcon)
              editorWindow.titleContent = new GUIContent(title);
            else
              editorWindow.titleContent.text = title;
#else
            editorWindow.title = title;
#endif
        }

    }
}