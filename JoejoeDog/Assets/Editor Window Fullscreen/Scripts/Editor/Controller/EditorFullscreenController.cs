/* 
 * Author:  Johanan Round
 * Package: Editor Window Fullscreen
 * License: Unity Asset Store EULA (Editor extension asset. Requires 1 license per machine.)
 */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace EditorWindowFullscreen
{
    /// <summary>
    /// Purpose: Controls the current fullscreen state.
    /// </summary>
    public class EditorFullscreenController
    {
        private static EditorFullscreenSettings settings
        {
            get { return EditorFullscreenSettings.settings; }
        }
        internal delegate void FullscreenHotkeyEvent(KeyCode keyCode, EventModifiers modifiers, bool setFullscreen);
        internal static event FullscreenHotkeyEvent FullscreenHotkeyEventHandler;

        /******************************************/
        /************ Hotkeyed Methods ************/
        /******************************************/

        /// <summary>
        /// Toggles fullscreen for the main editor window.
        /// </summary>
        ///
        public static bool ToggleMainWindowFullscreen()
        {
            return EditorFullscreenState.ToggleFullscreenAtOptionsSpecifiedPosition(EditorFullscreenState.mainWindowType);
        }

        /// <summary>
        /// Toggles fullscreen for the scene view.
        /// </summary>
        public static bool ToggleSceneViewFullscreen()
        {
            return EditorFullscreenState.ToggleFullscreenAtOptionsSpecifiedPosition(typeof(CustomSceneView));
        }

        /// <summary>
        /// Toggles fullscreen for the game view.
        /// </summary>
        public static void ToggleGameViewFullscreen()
        {
            ToggleGameViewFullscreen(false);
        }
        private static void ToggleGameViewFullscreenPlayStateWasChanged()
        {
            ToggleGameViewFullscreen(true);
        }
        private static bool ToggleGameViewFullscreen(bool triggeredOnPlayStateChange)
        {
            EditorFullscreenSettings.FullscreenOption fullscreenOps;
            if (triggeredOnPlayStateChange) fullscreenOps = EditorFullscreenSettings.settings.openFullscreenOnGameStart;
            else fullscreenOps = EditorFullscreenSettings.GetFullscreenOptionsForWindowType(EditorFullscreenState.gameViewType);
            bool setFullscreen = !EditorFullscreenState.WindowTypeIsFullscreenAtOptionsSpecifiedPosition(EditorFullscreenState.gameViewType, fullscreenOps);

            EditorFullscreenState.RunOnLoad methodToRun;
            if (!triggeredOnPlayStateChange) methodToRun = ToggleGameViewFullscreen;
            else methodToRun = ToggleGameViewFullscreenPlayStateWasChanged;
            if (EditorFullscreenState.RunAfterInitialStateLoaded(methodToRun)) return setFullscreen;

            setFullscreen = EditorFullscreenState.ToggleFullscreenAtOptionsSpecifiedPosition(null, EditorFullscreenState.gameViewType, fullscreenOps, triggeredOnPlayStateChange);
            var focusedWindow = EditorWindow.focusedWindow;
            EditorMainWindow.Focus();
            if (focusedWindow != null) focusedWindow.Focus();

            if (!triggeredOnPlayStateChange)
            {
                bool isPlaying = EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode;
                if (settings.startGameWhenEnteringFullscreen && !isPlaying && setFullscreen)
                {
                    //Enter play mode
                    EditorApplication.ExecuteMenuItem("Edit/Play");
                }
                else if (settings.stopGameWhenExitingFullscreen != EditorFullscreenSettings.StopGameWhenExitingFullscreen.Never && isPlaying && !setFullscreen)
                {
                    if (settings.stopGameWhenExitingFullscreen == EditorFullscreenSettings.StopGameWhenExitingFullscreen.WhenAnyFullscreenGameViewIsExited || !WindowTypeIsFullscreen(EditorFullscreenState.gameViewType))
                    {
                        //Exit play mode
                        EditorApplication.ExecuteMenuItem("Edit/Play");
                    }
                }
            }
            return setFullscreen;
        }

        /// <summary>
        /// Toggles fullscreen for the focused window.
        /// </summary>
        /// <returns>True if the window became fullscreen. False if fullscreen was exited.</returns>
        public static bool ToggleFocusedWindowFullscreen()
        {
            if (EditorWindow.focusedWindow != null)
            {
                return EditorFullscreenState.ToggleFullscreenAtOptionsSpecifiedPosition(EditorWindow.focusedWindow, EditorWindow.focusedWindow.GetType(), EditorFullscreenSettings.settings.currentlyFocusedWindow);
            }
            else return false;
        }

        /// <summary>
        /// Toggle fullscreen for the window under the cursor.
        /// </summary>
        /// <returns>True if the window became fullscreen. False if fullscreen was exited.</returns>
        public static bool ToggleWindowUnderCursorFullscreen()
        {
            if (EditorWindow.mouseOverWindow != null)
            {
                return EditorFullscreenState.ToggleFullscreenAtOptionsSpecifiedPosition(EditorWindow.mouseOverWindow, EditorWindow.mouseOverWindow.GetType(), EditorFullscreenSettings.settings.windowUnderCursor);
            }
            else return false;
        }

        /// <summary>
        /// Toggles the top toolbar for the currently focused fullscreen window. (Only applies to Scene View, Game View, and Main Window, which have top toolbars).
        /// </summary>
        public static void ToggleTopToolbar()
        {
            if (EditorFullscreenState.RunAfterInitialStateLoaded(ToggleTopToolbar)) return;
            EditorFullscreenState.ToggleToolbarInFullscreen();
        }

        /// <summary>
        /// Closes all fullscreen editor windows.
        /// </summary>
        /// <returns>True if at least one fullscreen window was closed.</returns>
        public static bool CloseAllEditorFullscreenWindows()
        {
            bool closedAtLeastOneFullscreen = false;
            try
            {
                var allWinStates = EditorFullscreenState.fullscreenState.window.ToArray();
                foreach (var win in allWinStates)
                {
                    if (win.EditorWin != null && win.WindowType != EditorFullscreenState.mainWindowType)
                    {
                        if (win.IsFullscreen) closedAtLeastOneFullscreen = true;
                        win.EditorWin.SetFullscreen(false);
                    }
                }
            }
            catch { }

            if (EditorMainWindow.IsFullscreen()) closedAtLeastOneFullscreen = true;
            EditorMainWindow.SetFullscreen(false);

            EditorFullscreenState.fullscreenState.CleanDeletedWindows();
            EditorFullscreenState.TriggerFullscreenEvent(null, null, Vector2.zero, closedAtLeastOneFullscreen);
            return closedAtLeastOneFullscreen;
        }

        /**********************************************/
        /************ Other Public Methods ************/
        /**********************************************/

        /// <summary>
        /// Returns true if a window type is fullscreen on any screen.
        /// </summary>
        public static bool WindowTypeIsFullscreen(Type windowType)
        {
            bool foundOneFullscreen = false;
            foreach (var state in EditorFullscreenState.fullscreenState.window)
            {
                if (state.WindowType == windowType && state.IsFullscreen)
                {
                    foundOneFullscreen = true;
                    break;
                }
            }
            return foundOneFullscreen;
        }

        /// <summary>
        /// Returns true if an editor window type is fullscreen on the screen specified by the settings for opening that window type.
        /// </summary>
        public static bool WindowTypeIsFullscreenAtOptionsSpecifiedPosition(Type windowType)
        {
            return EditorFullscreenState.WindowTypeIsFullscreenAtOptionsSpecifiedPosition(windowType);
        }

        /// <summary>
        /// Returns true if an editor window type is fullscreen on the screen at the specified position.
        /// </summary>
        public static bool WindowTypeIsFullscreenOnScreenAtPosition(Type windowType, Vector2 atPosition)
        {
            return EditorFullscreenState.WindowTypeIsFullscreenOnScreenAtPosition(windowType, atPosition);
        }

        /// <summary>
        /// Toggle fullscreen at a position decided according to the current settings for the specified window type.
        /// </summary>
        public static bool ToggleFullscreenAtOptionsSpecifiedPosition(Type windowType)
        {
            return EditorFullscreenState.ToggleFullscreenAtOptionsSpecifiedPosition(windowType);
        }

        /// <summary>
        /// Toggle fullscreen at the current mouse position for the window with the specified type. Shows the toolbar if applicable.
        /// </summary>
        public static bool ToggleFullscreenAtMousePosition(Type windowType)
        {
            return EditorFullscreenState.ToggleFullscreenAtMousePosition(windowType, true);
        }

        /// <summary>
        /// Toggle fullscreen at the current mouse position for the window with the specified type.
        /// </summary>
        public static bool ToggleFullscreenAtMousePosition(Type windowType, bool showTopToolbar)
        {
            return EditorFullscreenState.ToggleFullscreenAtMousePosition(windowType, showTopToolbar);
        }
        /// <summary>
        /// Toggle fullscreen for a window type (Creates a new fullscreen window if none already exists).
        /// </summary>
        /// <param name="windowType">The type of the window to create a fullscreen for.</param>
        /// <returns>True if the window type became fullscreen. False if fullscreen was exited.</returns>
        public static bool ToggleFullscreen(Type windowType)
        {
            return EditorFullscreenState.ToggleFullscreen(windowType);
        }

        /// <summary>
        /// Toggle fullscreen for a window type, on the screen at a position. Shows the toolbar if applicable.
        /// </summary>
        /// <param name="windowType">The type of the window to create a fullscreen for.</param>
        /// <param name="atPosition">Fullscreen will be toggled on the screen which is at this position.</param>
        /// <returns>True if the window type became fullscreen. False if fullscreen was exited.</returns>
        public static bool ToggleFullscreen(Type windowType, Vector2 atPosition)
        {
            return EditorFullscreenState.ToggleFullscreen(windowType, atPosition, true);
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
            return EditorFullscreenState.ToggleFullscreen(windowType, atPosition, showTopToolbar);
        }

        /// <summary>
        /// Toggle fullscreen for the game view, on the screen at a position.
        /// </summary>
        /// <param name="atPosition">Fullscreen will be toggled on the screen which is at this position.</param>
        /// <param name="showTopToolbar">Show the top toolbar by default if opening a fullscreen.</param>
        /// <returns>True if the game view became fullscreen. False if fullscreen was exited.</returns>
        public static bool ToggleGameViewFullscreen(Vector2 atPosition, bool showTopToolbar)
        {
            return ToggleFullscreen(EditorFullscreenState.gameViewType, atPosition, showTopToolbar);
        }

        /// <summary>
        /// Exits the fullscreen game views.
        /// </summary>
        /// <param name="onlyThoseCreatedAtGameStart">If true, only exits the game views which were created when the game was started.</param>
        public static void ExitGameFullscreens(bool onlyThoseCreatedAtGameStart)
        {
            EditorFullscreenState.RunOnLoad methodToRun = ExitGameFullscreensAll;
            if (onlyThoseCreatedAtGameStart) methodToRun = ExitGameFullscreensOnlyThoseCreatedAtGameStart;
            if (EditorFullscreenState.RunAfterInitialStateLoaded(methodToRun)) return;

            var fullscreenGameWindows = new List<EditorWindow>();
            foreach (var state in EditorFullscreenState.fullscreenState.window)
            {
                if (state.EditorWin != null && state.WindowType == EditorFullscreenState.gameViewType && state.EditorWin.IsFullscreen())
                {
                    if (!onlyThoseCreatedAtGameStart || state.CreatedAtGameStart)
                        fullscreenGameWindows.Add(state.EditorWin);
                }
            }
            foreach (var gameWin in fullscreenGameWindows)
            {
                gameWin.SetFullscreen(false);
            }
        }
        private static void ExitGameFullscreensAll()
        {
            ExitGameFullscreens(false);
        }
        private static void ExitGameFullscreensOnlyThoseCreatedAtGameStart()
        {
            ExitGameFullscreens(true);
        }

        /// <summary>
        /// Triggers a Fullscreen Hotkey.
        /// </summary>
        /// <param name="keyCode">The key code of the hotkey to be triggered.</param>
        /// <param name="modifiers">The modifiers of the hotkey to be triggered.</param>
        /// <returns></returns>
        internal static bool TriggerFullscreenHotkey(KeyCode keyCode, EventModifiers modifiers)
        {
            bool setFullscreen = false;
            bool fullscreenHotkeyTriggered = true;
            var settings = EditorFullscreenSettings.settings;
            if (CheckHotkeyTriggered(keyCode, modifiers, settings.closeAllFullscreenWindows)) setFullscreen = CloseAllEditorFullscreenWindows(); //In this case setFullscreen is set to true if at least one fullscreen was closed.
            else if (CheckHotkeyTriggered(keyCode, modifiers, settings.mainUnityWindow)) setFullscreen = ToggleMainWindowFullscreen();
            else if (CheckHotkeyTriggered(keyCode, modifiers, settings.sceneWindow)) setFullscreen = ToggleSceneViewFullscreen();
            else if (CheckHotkeyTriggered(keyCode, modifiers, settings.gameWindow)) setFullscreen = ToggleGameViewFullscreen(false);
            else if (CheckHotkeyTriggered(keyCode, modifiers, settings.currentlyFocusedWindow)) setFullscreen = ToggleFocusedWindowFullscreen();
            else if (CheckHotkeyTriggered(keyCode, modifiers, settings.windowUnderCursor)) setFullscreen = ToggleWindowUnderCursorFullscreen();
            else if (CheckHotkeyTriggered(keyCode, modifiers, settings.toggleTopToolbar)) ToggleTopToolbar();
            else fullscreenHotkeyTriggered = false;

            if (FullscreenHotkeyEventHandler != null && fullscreenHotkeyTriggered)
            {
                FullscreenHotkeyEventHandler.Invoke(keyCode, modifiers, setFullscreen);
            }
            return fullscreenHotkeyTriggered;
        }
        
        /// <summary>
        /// Triggers a Fullscreen Hotkey
        /// </summary>
        /// <param name="fullscreenOption">The fullscreen option to trigger the hotkey of.</param>
        internal static void TriggerFullscreenHotkey(EditorFullscreenSettings.FullscreenOption fullscreenOption)
        {
            TriggerFullscreenHotkey(fullscreenOption.hotkey, fullscreenOption.modifiers);
        }

        /**********************************************/
        /************ Private Methods *****************/
        /**********************************************/
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            FullscreenHotkeyEventHandler = null;
            EditorApplication.playmodeStateChanged += PlayModeStateChanged;

            EditorInput.KeyEventHandler -= EventController;
            EditorInput.KeyEventHandler += EventController;
        }

        private static void EventController()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                var keyCode = Event.current.keyCode;
                var modifiers = Event.current.modifiers;
                TriggerFullscreenHotkey(keyCode, modifiers);
            }
        }

        private static bool CheckHotkeyTriggered(KeyCode keyCode, EventModifiers modifiers, EditorFullscreenSettings.FullscreenOption fullscreenOption)
        {
            var keyString = keyCode.ToKeyString();

            if (keyCode == fullscreenOption.hotkey)
            {
                if (modifiers.MatchesFlag(EventModifiers.FunctionKey) != fullscreenOption.modifiers.MatchesFlag(EventModifiers.FunctionKey) && keyString.Length > 1 && keyString.Substring(0, 1) == "F")
                {
                    //If a function key is pushed with no modifiers, the "FunctionKey" modifier is enabled. In this case, allow a match with no modifiers;
                    int FNum = 0;
                    bool isNumericFkey = int.TryParse(keyString.Substring(1), out FNum);
                    if (isNumericFkey)
                    {
                        modifiers = fullscreenOption.modifiers | EventModifiers.FunctionKey;
                        fullscreenOption.modifiers = fullscreenOption.modifiers | EventModifiers.FunctionKey;
                    }
                }
                if (modifiers == fullscreenOption.modifiers)
                    return true;
            }
            return false;
        }

        private static void PlayModeStateChanged()
        {
            bool startingPlay = !EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode;
            bool stoppedPlay = !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;

            if (startingPlay)
            {
                if (settings.openFullscreenOnGameStart.openAtPosition != EditorFullscreenSettings.OpenFullscreenAtPosition.None && !EditorFullscreenState.WindowTypeIsFullscreenAtOptionsSpecifiedPosition(EditorFullscreenState.gameViewType, settings.openFullscreenOnGameStart))
                    ToggleGameViewFullscreen(true);
            }
            else if (stoppedPlay)
            {
                if (settings.closeFullscreenOnGameStop == EditorFullscreenSettings.CloseFullscreenOnGameStop.AllFullscreenGameWindows)
                {
                    ExitGameFullscreens(false);
                }
                else if (settings.closeFullscreenOnGameStop == EditorFullscreenSettings.CloseFullscreenOnGameStop.FullscreensCreatedAtGameStart)
                {
                    ExitGameFullscreens(true);
                }
            }
        }
    }
}