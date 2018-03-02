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


namespace EditorWindowFullscreen
{
    /// <summary>
    /// Allows changing properties of the Editor's Main Window
    /// </summary>
    public static class EditorMainWindow
    {
        internal static float topToolbarHeight = 20f;
        internal static System.Type windowLayoutType;

        private static FieldInfo containerMainViewField;
        private static PropertyInfo containerMainView;
        private static PropertyInfo containerPosition;
        private static MethodInfo containerShow;
        private static MethodInfo containerClose;

        #pragma warning disable 0414
        private static MethodInfo setPosition;
        private static MethodInfo setMinMaxSizes;
        private static PropertyInfo windowPosition;
        private static MethodInfo containerSetMinMaxSizes;
        private static MethodInfo containerCloseWin;
        private static MethodInfo containerMoveInFrontOf;
        private static MethodInfo containerMoveBehindOf;
        #pragma warning restore 0414

        static EditorMainWindow()
        {
            windowLayoutType = System.Type.GetType("UnityEditor.WindowLayout,UnityEditor");

            setPosition = FS.mainWindowType.GetMethod("SetPosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Rect) }, null);
            setMinMaxSizes = FS.viewType.GetMethod("SetMinMaxSizes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Vector2), typeof(Vector2) }, null);
            windowPosition = FS.viewType.GetProperty("windowPosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            containerMainViewField = FS.containerWindowType.GetField("m_MainView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (containerMainViewField == null)
                containerMainViewField = FS.containerWindowType.GetField("m_RootView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            containerMainView = FS.containerWindowType.GetProperty("mainView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (containerMainView == null)
                containerMainView = FS.containerWindowType.GetProperty("rootView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            containerPosition = FS.containerWindowType.GetProperty("position", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            containerSetMinMaxSizes = FS.containerWindowType.GetMethod("SetMinMaxSizes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Vector2), typeof(Vector2) }, null);
            containerShow = FS.containerWindowType.GetMethod("Show", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int), typeof(bool), typeof(bool) }, null);
            containerClose = FS.containerWindowType.GetMethod("Close", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new System.Type[] { }, null);
            containerCloseWin = FS.containerWindowType.GetMethod("InternalCloseWindow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new System.Type[] { }, null);
            containerMoveInFrontOf = FS.containerWindowType.GetMethod("MoveInFrontOf", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new System.Type[] { FS.containerWindowType }, null);
            containerMoveBehindOf = FS.containerWindowType.GetMethod("MoveBehindOf", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new System.Type[] { FS.containerWindowType }, null);
        }

        internal static object FindMainWindow()
        {
            try
            {
                MethodInfo findMainWindow = windowLayoutType.GetMethod("FindMainWindow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (findMainWindow == null) findMainWindow = windowLayoutType.GetMethod("FindMainView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (findMainWindow == null) findMainWindow = windowLayoutType.GetMethod("FindRootWindow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (findMainWindow == null) findMainWindow = windowLayoutType.GetMethod("FindRootView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                return findMainWindow.Invoke(null, null);
            }
            catch
            {
                Debug.LogError("Couldn't find the editor Main Window");
                return null;
            }
        }

        internal static object FindContainerWindow()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
                return null;
            else
            {
                try
                {
                    PropertyInfo window = FS.viewType.GetProperty("window", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    return window.GetValue(mainWindow, null);
                }
                catch
                {
                    Debug.LogError("Couldn't find the editor Container Window");
                    return null;
                }
            }
        }

        internal static object FindOriginalContainerWindow()
        {
            var containers = Resources.FindObjectsOfTypeAll(FS.containerWindowType);

            foreach (var container in containers)
            {
                EditorWindowExtensions.ShowMode showMode;
                try
                {
                    PropertyInfo showModeProp = FS.containerWindowType.GetProperty("showMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    showMode = (EditorWindowExtensions.ShowMode)showModeProp.GetValue(container, null);
                    if (showMode == EditorWindowExtensions.ShowMode.MainWindow)
                        return container;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to find the Original Container Window (Error retrieving showMode property) " + e.Message);
                }
            }
            return null;
        }

        internal static IntPtr GetWindowHandle()
        {
            try
            {
                FieldInfo winPtr = FS.containerWindowType.GetField("m_WindowPtr", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                object wPtr = winPtr.GetValue(FindContainerWindow());
                IntPtr ptr = (IntPtr)wPtr.GetType().GetField("m_IntPtr", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(wPtr);
                return ptr;
            }
            catch
            {
                Debug.LogError("Couldn't find the editor main window handle.");
                return IntPtr.Zero;
            }
        }

        public static Rect position
        {
            get
            {
                PropertyInfo position = FS.viewType.GetProperty("screenPosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (Rect)position.GetValue(FindMainWindow(), null);
            }
            set
            {
                containerPosition.SetValue(FindContainerWindow(), value, null);
            }
        }

        public static Vector2 minSize
        {
            get
            {
                PropertyInfo minSize = FS.viewType.GetProperty("minSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (Vector2)minSize.GetValue(FindMainWindow(), null);
            }
        }

        public static Vector2 maxSize
        {
            get
            {
                PropertyInfo maxSize = FS.viewType.GetProperty("maxSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (Vector2)maxSize.GetValue(FindMainWindow(), null);
            }
        }

        public static bool maximized
        {
            get
            {
                //PropertyInfo maximized = FS.containerWindowType.GetProperty("maximized", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                //return (bool)maximized.GetValue(FindContainerWindow(), null);
                var pos = position;
                var displayBounds = EditorDisplay.ClosestToPoint(pos.center).Bounds;
                return pos.x == displayBounds.x && pos.y <= displayBounds.y + 50 && pos.width == displayBounds.width && Mathf.Abs(displayBounds.height - pos.height) <= 200;
            }
        }

        public static void ToggleMaximize()
        {
            MethodInfo toggleMaximize = FS.containerWindowType.GetMethod("ToggleMaximize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new System.Type[] { }, null);
            toggleMaximize.Invoke(FindContainerWindow(), null);
        }

        public static void SetMinMaxSizes(Vector2 minSize, Vector2 maxSize)
        {

            MethodInfo setMinMaxSizes = FS.viewType.GetMethod("SetMinMaxSizes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Vector2), typeof(Vector2) }, null);
            setMinMaxSizes.Invoke(FindMainWindow(), new object[] { minSize, maxSize });
        }

        public static void Focus()
        {
            var win = ScriptableObject.CreateInstance<EditorWindow>();
            win.Show(true);
            win.Focus();
            win.Close();
        }

        public static FS.WindowFullscreenState GetWindowFullscreenState()
        {
            return FS.FindWindowState(null, FS.mainWindowType);
        }

        public static bool IsFullscreen()
        {
            var fullscreenState = GetWindowFullscreenState();
            return fullscreenState.IsFullscreen;
        }

        public static bool IsFullscreenAtPosition(Vector2 checkPosition)
        {
            var currentScreenBounds = EditorDisplay.ClosestToPoint(position.center).Bounds;
            var fullscreenBoundsAtCheckPosition = EditorDisplay.ClosestToPoint(checkPosition).Bounds;
            var fullscreenState = GetWindowFullscreenState();
            return fullscreenState.IsFullscreen && currentScreenBounds == fullscreenBoundsAtCheckPosition;
        }

        public static bool ToggleFullscreen()
        {
            return ToggleFullscreen(true);
        }
        public static bool ToggleFullscreen(bool showTopToolbar)
        {
            return ToggleFullscreen(showTopToolbar, position.center);
        }
        public static bool ToggleFullscreen(bool showTopToolbar, Vector2 fullscreenAtPosition)
        {
            var fullscreenState = GetWindowFullscreenState();

            var currentScreenBounds = EditorDisplay.ClosestToPoint(position.center).Bounds;
            var newFullscreenBounds = EditorDisplay.ClosestToPoint(fullscreenAtPosition).Bounds;

            bool setFullscreen = !fullscreenState.IsFullscreen || currentScreenBounds != newFullscreenBounds;

            if (EditorWindowExtensions.ExitFullscreenForOtherWindowsOnScreen(fullscreenState.EditorWin, fullscreenAtPosition))
            {
                setFullscreen = true;
            }

            SetFullscreen(setFullscreen, showTopToolbar, fullscreenAtPosition);
            return setFullscreen;
        }

        public static void SetFullscreen(bool fullscreen)
        {
            SetFullscreen(fullscreen, true);
        }

        public static void SetFullscreen(bool fullscreen, bool showTopToolbar)
        {
            SetFullscreen(fullscreen, showTopToolbar, position.center);
        }

        public static void SetFullscreen(bool fullscreen, bool showTopToolbar, Vector2 fullscreenAtPosition)
        {
            var originallyFocusedEditorWin = EditorWindow.focusedWindow;
            var originallyFocusedEditorWinType = originallyFocusedEditorWin == null ? null : originallyFocusedEditorWin.GetType();

            var fullscreenState = GetWindowFullscreenState();
            bool wasFullscreen = fullscreenState.IsFullscreen;
            fullscreenState.ShowTopToolbar = showTopToolbar;
            fullscreenState.originalContainerWindow = (ScriptableObject)FindOriginalContainerWindow();
            fullscreenState.containerWindow = (ScriptableObject)FindContainerWindow();
            object mainWindow = FindMainWindow();

            bool inOriginalContainer = fullscreenState.containerWindow == fullscreenState.originalContainerWindow;
            var screenBounds = EditorDisplay.ClosestToPoint(fullscreenAtPosition).Bounds;

            if (fullscreen)
            {
                fullscreenState.ScreenBounds = screenBounds;
                fullscreenState.FullscreenAtPosition = fullscreenAtPosition;

                if (!wasFullscreen)
                {
                    var wasMaximized = maximized;

                    if (wasMaximized)
                        ToggleMaximize();
                    
                    fullscreenState.PreFullscreenPosition = position;
                    fullscreenState.PreFullscreenMinSize = minSize;
                    fullscreenState.PreFullscreenMaxSize = maxSize;
                    fullscreenState.PreFullscreenMaximized = wasMaximized;
                }
            }

            if (fullscreen && !showTopToolbar)
            {
                object fsContainerWindow;
                if (inOriginalContainer)
                    fsContainerWindow = ScriptableObject.CreateInstance(FS.containerWindowType);
                else
                    fsContainerWindow = fullscreenState.containerWindow;

                //Custom toolbar
                //int toolbarHeight = 18;
                //fullscreenState.EditorWin = MainWindowMenu.Create(new Rect(newPos.xMin, newPos.yMin, newPos.width, toolbarHeight));
                //newPos.yMin += toolbarHeight;

                //Put the main view into the fullscreen container window
                containerMainView.SetValue(fsContainerWindow, mainWindow, null);
                inOriginalContainer = false;

                containerPosition.SetValue(fsContainerWindow, screenBounds, null);
                containerShow.Invoke(fsContainerWindow, new object[] { 3, false, true });
                SetMinMaxSizes(screenBounds.size, screenBounds.size);
                containerPosition.SetValue(fsContainerWindow, screenBounds, null);

                MethodInfo displayAllViews = FS.containerWindowType.GetMethod("DisplayAllViews", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new System.Type[] { }, null);
                displayAllViews.Invoke(fsContainerWindow, null);

                fullscreenState.containerWindow = (ScriptableObject)fsContainerWindow;
                fullscreenState.IsFullscreen = true;

            }
            else
            {
                if (fullscreenState.EditorWin != null)
                    fullscreenState.EditorWin.Close();

                if (!inOriginalContainer)
                {
                    //Reset main view back to original container view
                    containerMainViewField.SetValue(fullscreenState.originalContainerWindow, null);
                    containerMainView.SetValue(fullscreenState.originalContainerWindow, mainWindow, null);
                    try
                    {
                        containerMainViewField.SetValue(fullscreenState.containerWindow, null);
                        containerClose.Invoke(fullscreenState.containerWindow, null);
                    }
                    catch (System.Exception e)
                    {
                        if (FS.LogNonFatalErrors) Debug.LogException(e);
                    }
                    fullscreenState.containerWindow = fullscreenState.originalContainerWindow;
                    fullscreenState.IsFullscreen = false;
                    inOriginalContainer = true;
                }

                if (fullscreen)
                {
                    //Set fullscreen with toolbar
                    var newPos = screenBounds;
                    newPos.yMin += topToolbarHeight;

                    position = newPos;
                    SetMinMaxSizes(newPos.size, newPos.size);
                    position = newPos;

                    if (position.x != newPos.x)
                    {
                        //Position didn't set correctly, so must be maximized
                        fullscreenState.PreFullscreenMaximized = true;
                        ToggleMaximize();
                        position = newPos;
                    }

                    fullscreenState.IsFullscreen = true;
                }
            }

            if (!fullscreen && inOriginalContainer && wasFullscreen)
            {
                //Reset position
                position = fullscreenState.PreFullscreenPosition;
                SetMinMaxSizes(fullscreenState.PreFullscreenMinSize, fullscreenState.PreFullscreenMaxSize);
                position = fullscreenState.PreFullscreenPosition;

                PropertyInfo pos = FS.viewType.GetProperty("position", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                pos.SetValue(FindMainWindow(), fullscreenState.PreFullscreenPosition, null);

                fullscreenState.IsFullscreen = false;

                //Save and reload temporary layout, to fix resizable main window
                var fsSaveToLayout = new Dictionary<FS.WindowFullscreenState, bool>();
                var allWinStates = FS.fullscreenState.window.ToArray();
                foreach (var state in allWinStates)
                {
                    if (state.EditorWin != null)
                    {
                        fsSaveToLayout.Add(state, state.EditorWin.GetSaveToLayout());
                        state.EditorWin.SetSaveToLayout(true);
                    }
                }
                FS.SaveFullscreenState();
                WindowLayoutUtility.SaveProjectLayout("PostFullscreenLayout.dwlt");
                foreach (var state in fsSaveToLayout)
                {
                    if (state.Key.EditorWin != null)
                    {
                        state.Key.EditorWin.SetSaveToLayout(state.Value);
                    }
                }
                WindowLayoutUtility.LoadProjectLayout("PostFullscreenLayout.dwlt");
                Focus();

                position = fullscreenState.PreFullscreenPosition; //Reset position
                position = fullscreenState.PreFullscreenPosition;
                if (fullscreenState.PreFullscreenMaximized != maximized)
                    ToggleMaximize();

                FS.LoadFullscreenState();
            }

            FS.SaveFullscreenState();
            FS.TriggerFullscreenEvent(mainWindow, FS.mainWindowType, fullscreenAtPosition, fullscreen);
            if (EditorWindow.focusedWindow == null)
            {
                if (originallyFocusedEditorWin != null)
                    originallyFocusedEditorWin.Focus();
                else if (originallyFocusedEditorWinType != null)
                {
                    EditorWindow.FocusWindowIfItsOpen(originallyFocusedEditorWinType);
                }
            }
        }

        internal static object[] GetAllChildViews()
        {
            try
            {
                var mainWindow = FindMainWindow();
                PropertyInfo allChildren = FS.viewType.GetProperty("allChildren", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (object[])allChildren.GetValue(mainWindow, null);
            }
            catch (System.Exception e)
            {
                if (FS.LogNonFatalErrors) Debug.LogException(e);
            }
            return null;
        }
    }

    internal class MainWindowMenu : EditorWindow
    {
        public static EditorWindow Create(Rect bounds)
        {
            EditorWindow win = (EditorWindow)ScriptableObject.CreateInstance(typeof(MainWindowMenu));
            Debug.Log(win == null);
            win.ShowWithMode((EditorWindowExtensions.ShowMode.PopupMenu));
            win.SetBorderlessPosition(bounds);
            return win;
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            CreatePopupMenu("File");
            CreatePopupMenu("Edit");
            CreatePopupMenu("Assets");
            CreatePopupMenu("GameObject");
            CreatePopupMenu("Component");
            CreatePopupMenu("Window");
            CreatePopupMenu("Help");

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            ;
        }

        private void CreatePopupMenu(string menuName)
        {
            GUIContent content = new GUIContent(menuName);
            Rect buttonPos = GUILayoutUtility.GetRect(content, EditorStyles.toolbarDropDown, null);
            if (GUI.Button(buttonPos, content, EditorStyles.toolbarDropDown))
            {
                EditorUtility.DisplayPopupMenu(buttonPos, menuName, null);
                EditorGUIUtility.ExitGUI();
            }
        }
    }
}