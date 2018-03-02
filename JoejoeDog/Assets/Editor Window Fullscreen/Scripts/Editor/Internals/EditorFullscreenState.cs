/* 
 * Author:  Johanan Round
 * Package: Editor Window Fullscreen
 * License: Unity Asset Store EULA (Editor extension asset. Requires 1 license per machine.)
 */
using System.Xml.Serialization;
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using System.CodeDom;
using System.Linq.Expressions;

namespace EditorWindowFullscreen
{
    /// <summary>
    /// Stores the current fullscreen state, and provides methods for modifying it.
    /// </summary>
    public class EditorFullscreenState
    {
        internal delegate void FullscreenEvent(object window, Type windowType, Vector2 atPosition, bool enteredFullscreen);
        internal static event FullscreenEvent FullscreenEventHandler;

        internal static float borderlessOffsetY;
        internal static float topTabFullHeight = 39; //Initialized as a fallback. It is calculated automatically in SetBorderlessPosition().
        internal static float windowTopPadding = 5; //^^
        internal static bool calculatedBorderlessOffsets;
        internal const float sceneViewToolbarHeight = 17f;

        internal static bool LogNonFatalErrors = false;

        internal static string projectLibraryPath;
        internal static System.Type viewType;
        internal static System.Type hostViewType;
        internal static System.Type dockAreaType;
        internal static System.Type containerWindowType;
        internal static System.Type guiViewType;
        internal static System.Type editorApplicationLayoutType;

        internal static System.Type mainWindowType;
        internal static System.Type sceneViewType;
        internal static System.Type gameViewType;
        internal static System.Type profilerWindowType;
        internal static System.Type projectWindowType;
        internal static System.Type consoleWindowType;
        internal static System.Type inspectorWindowType;
        internal static System.Type sceneHierarchyWindowType;
        internal static System.Type animationWindowType;

        [Serializable]
        public class WindowFullscreenState
        {
            public bool IsFullscreen;
            public bool ShowTopTabs;
            public bool ShowTopToolbar;
            public bool CloseOnExitFullscreen;
            public string WindowName;
            public string WindowTitle;

            [SerializeField]
            public string actualTypeAssemblyQualifiedName; //Must be public for XmlSerializer
            private Type actualType;
            [XmlIgnore]
            public Type ActualType
            {
                get { return actualType; }
                set
                {
                    actualType = value;
                    if (actualType != null)
                        actualTypeAssemblyQualifiedName = actualType.AssemblyQualifiedName;
                    else
                        actualTypeAssemblyQualifiedName = null;
                }
            }
            
            [SerializeField]
            public string windowTypeAssemblyQualifiedName; //Must be public for XmlSerializer
            private Type windowType;
            [XmlIgnore]
            public Type WindowType
            {
                get { return windowType; }
                set
                {
                    windowType = value;
                    if (windowType != null)
                        windowTypeAssemblyQualifiedName = windowType.AssemblyQualifiedName;
                    else
                        windowTypeAssemblyQualifiedName = null;
                }
            }
            [XmlIgnore]
            public EditorWindow EditorWin;
            [XmlIgnore]
            public ScriptableObject containerWindow;
            [XmlIgnore]
            public ScriptableObject originalContainerWindow;
            public Rect PreFullscreenPosition;
            public Vector2 PreFullscreenMinSize;
            public Vector2 PreFullscreenMaxSize;
            public bool PreFullscreenMaximized;
            public CursorLockMode CursorLockModePreShowTopToolbar;
            public Rect ScreenBounds;
            public Vector2 FullscreenAtPosition;
            public Rect ContainerPosition;
            public bool HasFocus;
            public bool CreatedAtGameStart;
            public bool UnfocusedGameViewOnEnteringFullscreen;

            public void SetEditorWin(EditorWindow editorWin)
            {
                this.WindowName = editorWin.name;
                this.WindowTitle = editorWin.GetWindowTitle();
                this.EditorWin = editorWin;
            }
        }

        [Serializable]
        public struct FullscreenState
        {
            public List<WindowFullscreenState> window;

            public void CleanDeletedWindows()
            {
                window.RemoveAll(win => win.EditorWin == null && win.containerWindow == null);
            }
        }

        internal static FullscreenState fullscreenState;
        private const string FullscreenStateFilename = "CurrentEditorFullscreenWindowState.fwst";

        private static List<WindowFullscreenState> queuedStatesToToggleOnLoad = new List<WindowFullscreenState>();
        internal delegate void RunOnLoad();
        private static RunOnLoad RunOnNextLoadMethods;

        public static WindowFullscreenState FindWindowState(EditorWindow editorWin)
        {
            if (editorWin == null)
                return null;
            else
                return FindWindowState(editorWin, editorWin.GetType());
        }
        public static WindowFullscreenState FindWindowState(System.Type windowType)
        {
            return FindWindowState(null, windowType);
        }
        public static WindowFullscreenState FindWindowState(EditorWindow editorWin, System.Type windowType)
        {
            return FindWindowState(editorWin, windowType, null);
        }
        public static WindowFullscreenState FindWindowState(EditorWindow editorWin, System.Type windowType, EditorDisplay editorDisplay)
        {
            WindowFullscreenState winState = null;

            Type actualType = windowType;
            windowType = GetWindowType(actualType);

            try
            {
                if (editorWin != null)
                    winState = fullscreenState.window.Find(state => state.EditorWin == editorWin && (editorDisplay == null || state.EditorWin.GetFullscreenDisplay().Bounds.Equals(editorDisplay.Bounds)));
                else
                    winState = fullscreenState.window.Find(state => (state.EditorWin != null || state.containerWindow != null) && state.WindowType == windowType && (editorDisplay == null || state.EditorWin != null && state.EditorWin.GetFullscreenDisplay() != null && state.EditorWin.GetFullscreenDisplay().Bounds.Equals(editorDisplay.Bounds)));
            }
            catch (System.Exception e)
            {
                if (LogNonFatalErrors)
                {
                    Debug.LogError("Error attempting to find window state.");
                    Debug.LogException(e);
                }
            }

            if (winState == null)
            {
                winState = AddWindowState(editorWin, windowType, actualType);
            }

            return winState;
        }

        
        private static WindowFullscreenState lastWinStateToToggleTopTabs;

        public static WindowFullscreenState FocusedWindowState
        {
            get
            {
                if (EditorWindow.focusedWindow == null)
                    return EditorMainWindow.GetWindowFullscreenState();
                else
                    return FindWindowState(EditorWindow.focusedWindow);
            }
        }

        private static WindowFullscreenState AddWindowState(EditorWindow editorWin, Type windowType, Type actualType)
        {
            var winState = new WindowFullscreenState();

            winState.WindowType = windowType;
            winState.ActualType = actualType;

            if (editorWin != null)
            {
                winState.SetEditorWin(editorWin);
            }
            else if (windowType == mainWindowType)
            {
                winState.WindowName = mainWindowType.Name;
                winState.WindowTitle = "Unity Editor";
                winState.containerWindow = (ScriptableObject)EditorMainWindow.FindContainerWindow();
                winState.originalContainerWindow = winState.containerWindow;
            }

            if (fullscreenState.window == null)
            {
                fullscreenState.window = new List<WindowFullscreenState>();
            }

            fullscreenState.window.Add(winState);
            return winState;
        }


        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            FullscreenEventHandler = null;

            viewType = System.Type.GetType("UnityEditor.View,UnityEditor");
            hostViewType = System.Type.GetType("UnityEditor.HostView,UnityEditor");
            dockAreaType = System.Type.GetType("UnityEditor.DockArea,UnityEditor");
            mainWindowType = System.Type.GetType("UnityEditor.MainWindow,UnityEditor");
            if (mainWindowType == null) mainWindowType = System.Type.GetType("UnityEditor.MainView,UnityEditor");
            containerWindowType = System.Type.GetType("UnityEditor.ContainerWindow,UnityEditor");
            gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            guiViewType = System.Type.GetType("UnityEditor.GUIView,UnityEditor");
            editorApplicationLayoutType = System.Type.GetType("UnityEditor.EditorApplicationLayout,UnityEditor");
            profilerWindowType = System.Type.GetType("UnityEditor.ProfilerWindow,UnityEditor");
            projectWindowType = System.Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
            consoleWindowType = System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
            inspectorWindowType = System.Type.GetType("UnityEditor.InspectorWindow,UnityEditor");
            sceneHierarchyWindowType = System.Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor");
            animationWindowType = System.Type.GetType("UnityEditor.AnimationWindow,UnityEditor");
            sceneViewType = typeof(SceneView);

            projectLibraryPath = Directory.GetCurrentDirectory() + "/Library";

            if (viewType == null || hostViewType == null || dockAreaType == null || mainWindowType == null || containerWindowType == null || gameViewType == null)
            {
                if (LogNonFatalErrors)
                {
                    Debug.LogError("One or more Window/View types could not be found...");
                    Debug.LogError("viewType: " + viewType + " hostViewType: " + hostViewType + " dockAreaType: " + dockAreaType + " mainWindowType: " + mainWindowType + " containerWindowType: " + containerWindowType + " gameViewType: " + gameViewType);
                }
            }

            EditorApplication.update += InitialLoadState;
        }

        internal static bool loadedInitialState = false;
        private static int loadChecks = 0;
        private static double timeAtInit = 0;
        private static void InitialLoadState()
        {
            if (loadedInitialState) FinishInitialLoadState();
            if (timeAtInit == 0) timeAtInit = EditorApplication.timeSinceStartup;

            loadChecks++;

            if (loadChecks < 2000)
            {
                var allWins = Resources.FindObjectsOfTypeAll<EditorWindow>();
                bool gameIsStartingOrStopping = (EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying);
                float minWaitTime = EditorApplication.timeSinceStartup < 20 ? 1 : 0.1f;
                if (allWins.Length < 1 || EditorApplication.isCompiling || gameIsStartingOrStopping || (EditorApplication.timeSinceStartup - timeAtInit < minWaitTime))
                {
                    //Wait for the editor windows to be loaded before attempting to load fullscreen state.
                    return;
                }
            }
            try
            {
                LoadFullscreenState();
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error loading fullscreen state. " + e.Message + "\n" + e.StackTrace);
            }
            FinishInitialLoadState();
        }
        private static void FinishInitialLoadState()
        {
            loadedInitialState = true;
            EditorApplication.update -= InitialLoadState;
        }

        internal static void SaveFullscreenState()
        {
            try
            {
                fullscreenState.CleanDeletedWindows();

                //Update state window container positions and focus before saving
                foreach (var state in fullscreenState.window)
                {
                    if (state.EditorWin != null)
                    {
                        state.ContainerPosition = state.EditorWin.GetContainerPosition();
                        state.HasFocus = state.EditorWin == EditorWindow.focusedWindow;
                    }
                }

                string fullscreenStateData = SerializerUtility.Serialize(fullscreenState);
                File.WriteAllText(Path.Combine(projectLibraryPath, FullscreenStateFilename), fullscreenStateData);
            }
            catch (IOException e)
            {
                Debug.LogException(e);
            }
        }

        internal static void LoadFullscreenState()
        {
            try
            {
                string fullscreenStateData = File.ReadAllText(Path.Combine(projectLibraryPath, FullscreenStateFilename));
                fullscreenState = SerializerUtility.Deserialize<FullscreenState>(fullscreenStateData);
            }
            catch (FileNotFoundException)
            {
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            if (fullscreenState.window == null)
                fullscreenState.window = new List<WindowFullscreenState>();

            var allFullscreenStates = fullscreenState.window.ToArray();
            WindowFullscreenState mainWindowFullscreenState = null;

            //Load types from assembly qualified names
            foreach (var state in allFullscreenStates)
            {
                try
                {
                    state.ActualType = Type.GetType(state.actualTypeAssemblyQualifiedName);
                    state.WindowType = Type.GetType(state.windowTypeAssemblyQualifiedName);
                }
                catch (System.Exception e)
                {
                    if (LogNonFatalErrors) Debug.LogException(e);
                }
            }

            //Re-assign recreated window instances to their fullscreen states
            var allWins = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var unassignedFullscreenWins = new List<EditorWindow>();
            foreach (var win in allWins)
            {
                if (win.GetShowMode() == EditorWindowExtensions.ShowMode.PopupMenu)
                {
                    unassignedFullscreenWins.Add(win);
                }
            }
            foreach (var state in allFullscreenStates)
            {
                if (state.EditorWin != null)
                {
                    unassignedFullscreenWins.Remove(state.EditorWin);
                }
                else if (state.WindowType == mainWindowType)
                {
                    mainWindowFullscreenState = state;
                }
                else if (state.IsFullscreen)
                {
                    foreach (var win in unassignedFullscreenWins)
                    {
                        var containerPosition = win.GetContainerPosition();
                        if (win.GetType() == state.ActualType && containerPosition.x == state.ContainerPosition.x && containerPosition.y == state.ContainerPosition.y)
                        {
                            state.EditorWin = win;
                            unassignedFullscreenWins.Remove(win);
                            break;
                        }
                    }
                }
            }

            loadedInitialState = true;

            //Find the window which was focused
            var focusedWindow = fullscreenState.window.Find(state => state.HasFocus == true);

            //Remake fullscreen windows
            foreach (var state in allFullscreenStates)
            {
                if (state.IsFullscreen)
                {
                    if (state.EditorWin != null)
                    {
                        state.EditorWin.SetFullscreen(true, state.FullscreenAtPosition);
                    }
                    else if (state.WindowType != mainWindowType)
                    {
                        ToggleFullscreen(state.ActualType, true, state.FullscreenAtPosition, state.ShowTopToolbar, state.CreatedAtGameStart);
                    }
                }
            }

            //Recreate the main window fullscreen state
            if (mainWindowFullscreenState != null && mainWindowFullscreenState.IsFullscreen)
            {
                var atPosition = mainWindowFullscreenState.FullscreenAtPosition;
                var showTopToolbar = mainWindowFullscreenState.ShowTopToolbar;
                if (mainWindowFullscreenState.containerWindow == null || mainWindowFullscreenState.originalContainerWindow == null)
                {
                    fullscreenState.window.Remove(mainWindowFullscreenState); //Remove the old fullscreen state because the originalContainer needs to be reset
                }
                EditorMainWindow.SetFullscreen(true, showTopToolbar, atPosition);
            }

            //Remove fullscreen popup windows which don't have a fullscreen state
            foreach (var win in unassignedFullscreenWins)
            {
                if (win != null)
                {
                    if (win.GetContainerWindow() != null)
                    {
                        win.Close();
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(win, true);
                    }
                }
            }
            fullscreenState.CleanDeletedWindows();

            //Bring any fullscreen window which is on top of the main window to the front.
            try
            {
                var windowOverMain = fullscreenState.window.Find(state => state.IsFullscreen && state.EditorWin != null && EditorDisplay.ClosestToPoint(state.FullscreenAtPosition).Bounds == EditorDisplay.ClosestToPoint(EditorMainWindow.position.center).Bounds);
                if (windowOverMain != null)
                {
                    GiveFocusAndBringToFront(windowOverMain.EditorWin);
                }
            }
            catch { }

            //Refocus the window which was previously focused
            if (focusedWindow != null && focusedWindow.EditorWin != null)
            {
                GiveFocusAndBringToFront(focusedWindow.EditorWin);
            }

            //Toggle fullscreen for states which were queued up before load was complete
            foreach (var state in queuedStatesToToggleOnLoad)
            {
                ToggleFullscreen(state.ActualType, state.CloseOnExitFullscreen, state.FullscreenAtPosition, state.ShowTopToolbar, state.CreatedAtGameStart);
            }
            queuedStatesToToggleOnLoad.Clear();
            if (RunOnNextLoadMethods != null)
            {
                RunOnNextLoadMethods.Invoke();
                RunOnNextLoadMethods = null;
            }
        }

        internal static bool RunAfterInitialStateLoaded(RunOnLoad methodToRun)
        {
            if (!loadedInitialState)
            {
                RunOnNextLoadMethods -= methodToRun;
                RunOnNextLoadMethods += methodToRun;
                return true;
            }
            return false;
        }

        private static void GiveFocusAndBringToFront(EditorWindow focusWin)
        {
            var focused = EditorWindow.GetWindow(focusWin.GetType(), false, focusWin.GetWindowTitle(), true);
            if (focused != focusWin && focused.GetWindowTitle() == focusWin.GetWindowTitle())
                focused.Close();
            focusWin.Focus();
        }

        /// <summary>
        /// Get the EditorWindow which currently has the mouse over it.
        /// </summary>
        public static EditorWindow GetMouseOverWindow()
        {
            EditorWindow mouseOverWin = null;
            if (EditorWindow.mouseOverWindow != null) mouseOverWin = EditorWindow.mouseOverWindow;
            else if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.position.Contains(EditorInput.MousePosition))
                mouseOverWin = EditorWindow.focusedWindow;
            else if (EditorFullscreenState.fullscreenState.window != null)
            {
                var allWinStates = EditorFullscreenState.fullscreenState.window.ToArray();
                foreach (var win in allWinStates)
                {
                    if (win.EditorWin != null && win.EditorWin.position.Contains(EditorInput.MousePosition))
                    {
                        mouseOverWin = win.EditorWin;
                        break;
                    }
                }
            }

            return mouseOverWin;
        }

        /// <summary>
        /// Returns the fullscreen opening position of the specified window or window type according to the current options.
        /// </summary>
        private static Vector2 GetOptionsSpecifiedFullscreenOpenAtPosition(EditorWindow editorWin, Type windowType, EditorFullscreenSettings.FullscreenOption fullscreenOptions)
        {
            var openAtPosition = fullscreenOptions.openAtPosition;
            Vector2 atPosition = Vector2.zero;

            switch (openAtPosition)
            {
                case EditorFullscreenSettings.OpenFullscreenAtPosition.AtCurrentWindowPosition:
                    if (editorWin != null)
                    {
                        atPosition = editorWin.GetPointOnWindow();
                    }
                    else
                    {
                        var wins = Resources.FindObjectsOfTypeAll<EditorWindow>();
                        bool foundWin = false;
                        foreach (var win in wins)
                        {
                            if (GetWindowType(windowType) == win.GetWindowType())
                            {
                                atPosition = win.GetContainerPosition().center;
                                foundWin = true;
                                break;
                            }
                        }
                        if (!foundWin)
                            atPosition = EditorMainWindow.position.center;
                    }
                    break;
                case EditorFullscreenSettings.OpenFullscreenAtPosition.AtMousePosition:
                    atPosition = EditorInput.MousePosition;
                    break;
                case EditorFullscreenSettings.OpenFullscreenAtPosition.AtSpecifiedPosition:
                    atPosition = fullscreenOptions.position;
                    break;
                case EditorFullscreenSettings.OpenFullscreenAtPosition.AtSpecifiedPositionAndSize:
                    atPosition = fullscreenOptions.position;
                    break;
                case EditorFullscreenSettings.OpenFullscreenAtPosition.None:
                    break;
            }
            return atPosition;
        }

        /// <summary>
        /// Returns true if a window type is fullscreen on the screen specified by the options for opening that window type.
        /// </summary>
        public static bool WindowTypeIsFullscreenAtOptionsSpecifiedPosition(Type windowType)
        {
            var fullscreenOptions = EditorFullscreenSettings.GetFullscreenOptionsForWindowType(windowType);

            //Main Window
            if (windowType == mainWindowType) return EditorMainWindow.IsFullscreenAtPosition(GetOptionsSpecifiedFullscreenOpenAtPosition(null, windowType, fullscreenOptions));

            //Any Other Editor Window
            return WindowTypeIsFullscreenAtOptionsSpecifiedPosition(windowType, fullscreenOptions);
        }

        /// <summary>
        /// Returns true if a window type is fullscreen on the screen specified by the given options.
        /// </summary>
        public static bool WindowTypeIsFullscreenAtOptionsSpecifiedPosition(Type windowType, EditorFullscreenSettings.FullscreenOption fullscreenOptions)
        {
            var openAtPosition = fullscreenOptions.openAtPosition;
            bool isFullscreen = false;
            switch (openAtPosition)
            {
                case EditorFullscreenSettings.OpenFullscreenAtPosition.AtMousePosition:
                    EditorWindow mouseOverWin = EditorFullscreenState.GetMouseOverWindow();
                    if (mouseOverWin != null && mouseOverWin.GetWindowType() == windowType)
                    {
                        isFullscreen = mouseOverWin.IsFullscreen();
                    }
                    break;
                case EditorFullscreenSettings.OpenFullscreenAtPosition.None:
                    isFullscreen = false;
                    break;
                default:
                    Vector2 openAtPos = GetOptionsSpecifiedFullscreenOpenAtPosition(null, windowType, fullscreenOptions);
                    isFullscreen = WindowTypeIsFullscreenOnScreenAtPosition(windowType, openAtPos);
                    break;
            }
            return isFullscreen;
        }

        /// <summary>
        /// Returns true if a window type is fullscreen on the screen at the specified position.
        /// </summary>
        public static bool WindowTypeIsFullscreenOnScreenAtPosition(Type windowType, Vector2 atPosition)
        {
            bool isFullscreen = false;

            //Main Window
            if (windowType == mainWindowType) return EditorMainWindow.IsFullscreenAtPosition(atPosition);

            var wins = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var editorWin in wins)
            {
                if (editorWin.GetType() == windowType || editorWin.GetWindowType() == windowType)
                {
                    var winState = FindWindowState(editorWin);
                    if (winState.IsFullscreen)
                    {
                        if (editorWin.GetContainerPosition().Contains(atPosition) && editorWin.IsFullscreen())
                        {
                            isFullscreen = true;
                            break;
                        }
                    }
                }
            }
            return isFullscreen;
        }

        /// <summary>
        /// Toggle fullscreen at a position decided according to the current options.
        /// </summary>
        public static bool ToggleFullscreenAtOptionsSpecifiedPosition(Type windowType)
        {
            return ToggleFullscreenAtOptionsSpecifiedPosition(windowType, false);
        }
        /// <summary>
        /// Toggle fullscreen at a position decided according to the current options.
        /// </summary>
        public static bool ToggleFullscreenAtOptionsSpecifiedPosition(Type windowType, bool triggeredOnPlayStateChange)
        {
            return ToggleFullscreenAtOptionsSpecifiedPosition(null, windowType, EditorFullscreenSettings.GetFullscreenOptionsForWindowType(windowType), triggeredOnPlayStateChange);
        }
        /// <summary>
        /// Toggle fullscreen at a position decided according to the current options.
        /// </summary>
        public static bool ToggleFullscreenAtOptionsSpecifiedPosition(EditorWindow editorWin, Type windowType, EditorFullscreenSettings.FullscreenOption fullscreenOptions)
        {
            return ToggleFullscreenAtOptionsSpecifiedPosition(editorWin, windowType, fullscreenOptions, false);
        }
        /// <summary>
        /// Toggle fullscreen at a position decided according to the current options.
        /// </summary>
        public static bool ToggleFullscreenAtOptionsSpecifiedPosition(EditorWindow editorWin, Type windowType, EditorFullscreenSettings.FullscreenOption fullscreenOptions, bool triggeredOnPlayStateChange)
        {
            var openAtPosition = fullscreenOptions.openAtPosition;

            if (openAtPosition == EditorFullscreenSettings.OpenFullscreenAtPosition.AtMousePosition)
                return ToggleFullscreenAtMousePosition(windowType, fullscreenOptions.showToolbarByDefault, triggeredOnPlayStateChange);
            else
            {
                Vector2 atPosition = GetOptionsSpecifiedFullscreenOpenAtPosition(editorWin, windowType, fullscreenOptions);
                return ToggleFullscreen(windowType, atPosition, fullscreenOptions.showToolbarByDefault, triggeredOnPlayStateChange);
            }
        }

        /// <summary>
        /// Toggle fullscreen at the current mouse position for the window with the specified type.
        /// </summary>
        public static bool ToggleFullscreenAtMousePosition(Type windowType)
        {
            return ToggleFullscreenAtMousePosition(windowType, true, false);
        }

        /// <summary>
        /// Toggle fullscreen at the current mouse position for the window with the specified type.
        /// </summary>
        public static bool ToggleFullscreenAtMousePosition(Type windowType, bool showTopToolbar)
        {
            return ToggleFullscreenAtMousePosition(windowType, showTopToolbar, false);
        }

        /// <summary>
        /// Toggle fullscreen at the current mouse position for the window with the specified type.
        /// </summary>
        public static bool ToggleFullscreenAtMousePosition(Type windowType, bool showTopToolbar, bool triggeredOnPlayStateChange)
        {
            EditorWindow mouseOverWin = GetMouseOverWindow();

            if (mouseOverWin != null && (windowType == null || mouseOverWin.GetType() == windowType || mouseOverWin.GetWindowType() == windowType))
            {
                return EditorFullscreenState.ToggleFullscreen(mouseOverWin.GetType(), true, EditorInput.MousePosition, showTopToolbar, triggeredOnPlayStateChange);
            }
            else if (windowType != null)
            {
                return EditorFullscreenState.ToggleFullscreen(windowType, EditorInput.MousePosition, showTopToolbar, triggeredOnPlayStateChange);
            }
            return false;
        }

        /// <summary>
        /// Toggle fullscreen for a window type (Creates a new fullscreen window if none already exists).
        /// </summary>
        /// <param name="windowType">The type of the window to create a fullscreen for.</param>
        /// <returns>True if the window type became fullscreen. False if fullscreen was exited.</returns>
        public static bool ToggleFullscreen(Type windowType)
        {
            var fullscreenState = EditorFullscreenState.FindWindowState(windowType);
            return ToggleFullscreen(fullscreenState, true);
        }

        /// <summary>
        /// Toggle fullscreen for a window type, on the screen at a position.
        /// </summary>
        /// <param name="windowType">The type of the window to create a fullscreen for.</param>
        /// <param name="atPosition">Fullscreen will be toggled on the screen which is at this position.</param>
        /// <returns>True if the window type became fullscreen. False if fullscreen was exited.</returns>
        public static bool ToggleFullscreen(Type windowType, Vector2 atPosition)
        {
            return ToggleFullscreen(windowType, atPosition, true);

        }

        /// <summary>
        /// Toggle fullscreen for a window type, on the screen at a position.
        /// </summary>
        /// <param name="windowType">The type of the window to create a fullscreen for.</param>
        /// <param name="atPosition">Fullscreen will be toggled on the screen which is at this position.</param>
        /// <param name="showTopToolbar">Show the top toolbar by default if opening a fullscreen.</param>
        /// <returns>True if the window type became fullscreen. False if fullscreen was exited.</returns>
        public static bool ToggleFullscreen(Type windowType, Vector2 atPosition, bool showTopToolbar)
        {
            return ToggleFullscreen(windowType, true, atPosition, showTopToolbar, false);
        }

        /// <summary> Toggle fullscreen for a window type, on the screen at a position </summary>
        public static bool ToggleFullscreen(Type windowType, Vector2 atPosition, bool showTopToolbar, bool triggeredOnPlayStateChange)
        {
            return ToggleFullscreen(windowType, true, atPosition, showTopToolbar, triggeredOnPlayStateChange);
        }

        /// <summary> Toggle fullscreen for a window type </summary>
        public static bool ToggleFullscreen(Type windowType, bool createNewWindow, Vector2 atPosition, bool showTopToolbar, bool triggeredOnPlayStateChange)
        {
            var windowState = EditorFullscreenState.FindWindowState(null, windowType, EditorDisplay.ClosestToPoint(atPosition));
            if (showTopToolbar) windowState.ShowTopToolbar = true;
            if (triggeredOnPlayStateChange && !windowState.IsFullscreen) windowState.CreatedAtGameStart = true;
            return ToggleFullscreen(windowState, createNewWindow, atPosition);
        }

        /// <summary> Toggle fullscreen for a window state </summary>
        private static bool ToggleFullscreen(WindowFullscreenState windowState, bool createNewWindow)
        {
            if (windowState.EditorWin != null)
            {
                return ToggleFullscreen(windowState, createNewWindow, windowState.EditorWin.GetFullscreenDisplay().Bounds.center);
            }
            else if (windowState.WindowType == mainWindowType)
            {
                return ToggleFullscreen(windowState, createNewWindow, EditorMainWindow.position.center);
            }
            else
            {
                return ToggleFullscreen(windowState, createNewWindow, EditorDisplay.PrimaryDesktopResolution.center);
            }
        }

        /// <summary> Toggle fullscreen for a window state, on the screen at position </summary>
        private static bool ToggleFullscreen(WindowFullscreenState windowState, bool createNewWindow, Vector2 atPosition)
        {
            if (windowState == null) throw new NullReferenceException("windowState is null. Cannot continue.");

            if (!loadedInitialState)
            {
                windowState.CloseOnExitFullscreen = createNewWindow;
                windowState.FullscreenAtPosition = atPosition;
                queuedStatesToToggleOnLoad.Add(windowState);

                if (LogNonFatalErrors)
                    throw new TypeLoadException("The fullscreen state hasn't been loaded yet. Fullscreen toggle has been queued.");
                else
                    return false;
            }
            if (windowState.WindowType == mainWindowType)
            {
                return EditorMainWindow.ToggleFullscreen(windowState.ShowTopToolbar, atPosition);
            }

            if (windowState.EditorWin == null || (!windowState.IsFullscreen && createNewWindow))
            {
                if (windowState.ActualType == typeof(SceneView)) { windowState.ActualType = typeof(CustomSceneView); } //Always create CustomSceneView for SceneView
                var win = EditorWindowExtensions.CreateWindow(windowState.ActualType);
                windowState.SetEditorWin(win);

                if (windowState.ActualType == typeof(CustomSceneView) || win.GetWindowType() == gameViewType)
                    win.SetWindowTitle(windowState.WindowName, true); //Reset title content on custom Scene and Game views to prevent icon not found error.

                windowState.CloseOnExitFullscreen = true; //Since we are creating a new window, this window should close when fullscreen is exited

                bool setFullscreen = win.ToggleFullscreen(atPosition);
                return setFullscreen;
            }
            else
            {
                return windowState.EditorWin.ToggleFullscreen(atPosition);
            }
        }

        /// <summary> Toggle the toolbar in the active window if it is fullscreen </summary>
        public static bool ToggleToolbarInFullscreen()
        {
            bool setShowTopToolbar = false;
            var fullscreenWinState = FocusedWindowState;

            if (fullscreenWinState == null || !fullscreenWinState.IsFullscreen)
            {
                //If the current EditorWindow state isn't fullscreen, toggle the toolbar of the main window if that is fullscreen.
                fullscreenWinState = EditorMainWindow.GetWindowFullscreenState();
            }

            //If no fullscreen window is focused, toggle the top tab for the last top-tab-toggled fullscreen window.
            if ((fullscreenWinState == null || !fullscreenWinState.IsFullscreen) && lastWinStateToToggleTopTabs != null)
            {
                fullscreenWinState = lastWinStateToToggleTopTabs;
            }

            if (fullscreenWinState != null)
            {
                lastWinStateToToggleTopTabs = fullscreenWinState;
                setShowTopToolbar = !fullscreenWinState.ShowTopToolbar;
                return ToggleToolbarInFullscreen(fullscreenWinState, setShowTopToolbar);
            }
            else
            {
                lastWinStateToToggleTopTabs = null;
                return false;
            }
        }
        /// <summary> Toggle the toolbar for the specified window state, if it is fullscreen </summary>
        public static bool ToggleToolbarInFullscreen(WindowFullscreenState fullscreenWinState, bool showTopToolbar)
        {
            if (fullscreenWinState != null && fullscreenWinState.IsFullscreen)
            {
                if (fullscreenWinState.WindowType == mainWindowType)
                {
                    EditorMainWindow.SetFullscreen(true, showTopToolbar);
                }
                else if (fullscreenWinState.EditorWin != null)
                {
                    fullscreenWinState.EditorWin.SetFullscreen(true, showTopToolbar);
                }
            }
            return showTopToolbar;
        }

        public static Type GetWindowType(Type derivedType)
        {
            Type windowType = null;
            try
            {
                MethodInfo getWindowType = derivedType.GetMethod("GetWindowType", BindingFlags.Public | BindingFlags.Static);
                windowType = (Type)getWindowType.Invoke(null, null);
            }
            catch
            {
                Type baseType = derivedType.BaseType;
                if (baseType != null && (baseType == typeof(SceneView) || baseType == gameViewType))
                {
                    windowType = baseType;
                }
            }

            if (windowType != null)
                return windowType;
            else
                return derivedType;
        }

        internal static void TriggerFullscreenEvent(object window, Type windowType, Vector2 atPosition, bool enteredFullscreen)
        {
            if (FullscreenEventHandler != null)
            {
                FullscreenEventHandler.Invoke(window, windowType, atPosition, enteredFullscreen);
            }
        }
    }
}