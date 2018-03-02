/* 
 * Author:  Johanan Round
 * Package: Editor Window Fullscreen
 * License: Unity Asset Store EULA (Editor extension asset. Requires 1 license per machine.)
 */

using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace EditorWindowFullscreen
{
    public sealed class EditorFullscreenSettings
    {
        public const int SETTINGS_VERSION_NUMBER = 102;
        public const string MENU_ITEM_PATH = "Window/Editor Window Fullscreen/";
        public readonly static string settingsSaveVersion = SETTINGS_VERSION_NUMBER + "_" + Application.unityVersion;

        public enum OpenFullscreenAtPosition
        {
            None = 0,
            AtCurrentWindowPosition = 1,
            AtMousePosition = 2,
            AtSpecifiedPosition = 3,
            AtSpecifiedPositionAndSize = 4
        }

        public enum CloseFullscreenOnGameStop
        {
            None = 0,
            FullscreensCreatedAtGameStart = 1,
            AllFullscreenGameWindows = 2
        }

        public enum StopGameWhenExitingFullscreen
        {
            Never = 0,
            WhenAnyFullscreenGameViewIsExited = 1,
            WhenAllFullscreenGameViewsExited = 2
        }

        public static string FormatCamelCaseName(string enumName)
        {
            enumName = System.Text.RegularExpressions.Regex.Replace(enumName, "(?!^)([A-Z])", " $1");
            return enumName;
        }

        [Serializable]
        public struct FullscreenOption
        {
            public KeyCode hotkey;
            public EventModifiers modifiers;
            public OpenFullscreenAtPosition openAtPosition;
            public bool showToolbarByDefault;
            public Vector2 position;

            public FullscreenOption(KeyCode hotkey, EventModifiers modifiers, OpenFullscreenAtPosition openAtPosition, bool showToolbarByDefault, Vector2 position)
            {
                this.hotkey = hotkey;
                this.modifiers = modifiers;
                this.openAtPosition = openAtPosition;
                this.showToolbarByDefault = showToolbarByDefault;
                this.position = position;
            }
        }

        //Fullscreen Window Hotkeys
        public FullscreenOption mainUnityWindow = new FullscreenOption(KeyCode.F8, EventModifiers.None, OpenFullscreenAtPosition.AtMousePosition, true, Vector2.zero);
        public FullscreenOption sceneWindow = new FullscreenOption(KeyCode.F10, EventModifiers.None, OpenFullscreenAtPosition.AtMousePosition, true, Vector2.zero);
        public FullscreenOption gameWindow = new FullscreenOption(KeyCode.F11, EventModifiers.None, OpenFullscreenAtPosition.AtMousePosition, false, Vector2.zero);
        public FullscreenOption currentlyFocusedWindow = new FullscreenOption(KeyCode.F9, EventModifiers.None, OpenFullscreenAtPosition.AtMousePosition, true, Vector2.zero);
        public FullscreenOption windowUnderCursor = new FullscreenOption(KeyCode.F9, EventModifiers.Control, OpenFullscreenAtPosition.AtMousePosition, true, Vector2.zero);

        //Other Hotkeys
        public FullscreenOption toggleTopToolbar = new FullscreenOption(KeyCode.F12, EventModifiers.None, OpenFullscreenAtPosition.None, false, Vector2.zero);
        public FullscreenOption closeAllFullscreenWindows = new FullscreenOption(KeyCode.F8, EventModifiers.Control, OpenFullscreenAtPosition.None, false, Vector2.zero);

        //Game Window Options
        public bool startGameWhenEnteringFullscreen = false;
        public StopGameWhenExitingFullscreen stopGameWhenExitingFullscreen = StopGameWhenExitingFullscreen.Never;
        public FullscreenOption openFullscreenOnGameStart;
        public CloseFullscreenOnGameStop closeFullscreenOnGameStop;

        private static EditorFullscreenSettings _settings = new EditorFullscreenSettings();
        public static EditorFullscreenSettings settings
        {
            get { return _settings; }
        }

        private static string scriptFileSubPath = "";
        private static string scriptFilePath = "";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            try
            {
                scriptFileSubPath = GetMenuItemScriptFileRelativePath();
                if (scriptFileSubPath == null) return;
                scriptFilePath = Application.dataPath + scriptFileSubPath;

                _settings.LoadSettings();

                if (MenuItemScriptNeedsRefresh())
                {
                    _settings.SaveSettings(false);
                    UpdateMenuItems();
                }
            }
            catch (System.Exception e)
            {
                if (EditorFullscreenState.LogNonFatalErrors)
                {
                    Debug.LogError("Settings failed to load.");
                    Debug.LogException(e);
                }
            }
        }

        public void SaveSettings()
        {
            SaveSettings(true);
        }

        public void SaveSettings(bool hotkeysWereChanged)
        {
            _settings = this;
            EditorPrefs.SetString("EditorFullscreenWindowSettings_VER", settingsSaveVersion);
            EditorPrefs.SetString("EditorFullscreenWindowSettings_ALL", SerializerUtility.Serialize(this));
            if (hotkeysWereChanged) UpdateMenuItems();
        }

        public void LoadSettings()
        {
            string settingsData = EditorPrefs.GetString("EditorFullscreenWindowSettings_ALL");
            var loadedSettings = SerializerUtility.Deserialize<EditorFullscreenSettings>(settingsData);
            if (loadedSettings != null) _settings = loadedSettings;
        }

        private EditorFullscreenSettings() { }

        public static void ResetToDefaults()
        {
            _settings = new EditorFullscreenSettings();
        }

        public static FullscreenOption GetFullscreenOptionsForWindowType(Type windowType)
        {
            string windowTypeString = windowType == null ? "null" : windowType.ToString();
            return GetFullscreenOptionsForWindowType(windowTypeString);
        }

        public static FullscreenOption GetFullscreenOptionsForWindowType(string windowType)
        {
            FullscreenOption fullscreenOptions;

            if (windowType == EditorFullscreenState.mainWindowType.ToString())
                fullscreenOptions = settings.mainUnityWindow;
            else if (windowType == typeof(SceneView).ToString() || windowType == typeof(CustomSceneView).ToString())
                fullscreenOptions = settings.sceneWindow;
            else if (windowType == EditorFullscreenState.gameViewType.ToString())
                fullscreenOptions = settings.gameWindow;
            else if (windowType == "CurrentlyFocusedWindow")
                fullscreenOptions = settings.currentlyFocusedWindow;
            else if (windowType == "WindowUnderCursor")
                fullscreenOptions = settings.windowUnderCursor;
            else
                fullscreenOptions = new FullscreenOption();

            return fullscreenOptions;
        }

        private static string GetMenuItemScriptFolderPath()
        {
            var scriptableObj = ScriptableObject.CreateInstance(typeof(EditorFullscreenSettingsWindow));
            var scriptableObjScript = MonoScript.FromScriptableObject(scriptableObj);
            var scriptPath = AssetDatabase.GetAssetPath(scriptableObjScript);
            ScriptableObject.DestroyImmediate(scriptableObj);
            if (string.IsNullOrEmpty(scriptPath)) return null;
            int firstSlash = scriptPath.IndexOf("/");
            int lastSlash = scriptPath.LastIndexOf("/");
            int dirLength = lastSlash - firstSlash;
            scriptPath = scriptPath.Substring(firstSlash, dirLength);
            return scriptPath;
        }

        private static string GetMenuItemScriptFileRelativePath()
        {
            var scriptPath = GetMenuItemScriptFolderPath();
            if (string.IsNullOrEmpty(scriptPath)) return null;
            return scriptPath + "/EditorFullscreenControllerMenuItems.cs";
        }

        private static bool MenuItemScriptNeedsRefresh()
        {
            bool needsRefresh = false;
            if (EditorPrefs.GetString("EditorFullscreenWindowSettings_VER") != settingsSaveVersion) return true;
            var latestGuid = EditorPrefs.GetString("EditorFullscreenWindowSettings_GUID");
            if (File.Exists(scriptFilePath))
            {
                string line = "";
                StreamReader sr = new StreamReader(scriptFilePath);
                if (sr.Peek() != -1) sr.ReadLine();
                if (sr.Peek() != -1) line = sr.ReadLine();

                int guidStart = line.IndexOf("{") + 1;
                int guidEnd = line.LastIndexOf("}") - 1;
                int guidLength = guidEnd - guidStart + 1;
                if (guidStart > 0 && guidLength > 0)
                {
                    needsRefresh = line.Substring(guidStart, guidLength) != latestGuid;
                }
                else
                {
                    needsRefresh = true;
                }
                sr.Close();
            }
            else needsRefresh = true;
            return needsRefresh;
        }

        private static void UpdateMenuItems()
        {
            var settings = EditorFullscreenSettings.settings;
            var guid = Guid.NewGuid().ToString();
            //Create all hotkeys as menu-items
            string script = "//AUTO-GENERATED SCRIPT. ANY MODIFICATIONS WILL BE OVERWRITTEN.\r\n"
                          + "//GUID: {" + guid + "}\r\n"
                          + "using UnityEngine;\r\nusing UnityEditor;\r\n"
                          + "namespace " + typeof(EditorFullscreenSettings).Namespace + " {\r\n    public class EditorFullscreenControllerMenuItems {\r\n";

            script += GetMenuItemScript("Toggle Main Window Fullscreen", "ToggleMainWindowFullscreen", settings.mainUnityWindow, "mainUnityWindow");
            script += GetMenuItemScript("Toggle Scene View Fullscreen", "ToggleSceneViewFullscreen", settings.sceneWindow, "sceneWindow");
            script += GetMenuItemScript("Toggle Game Fullscreen", "ToggleGameViewFullscreen", settings.gameWindow, "gameWindow");
            script += GetMenuItemScript("Toggle Focused Window Fullscreen", "ToggleFocusedWindowFullscreen", settings.currentlyFocusedWindow, "currentlyFocusedWindow");
            script += GetMenuItemScript("Toggle Mouseover Window Fullscreen", "ToggleWindowUnderCursorFullscreen", settings.windowUnderCursor, "windowUnderCursor");
            script += GetMenuItemScript("Show \u2215 Hide Toolbar in Fullscreen", "ToggleTopToolbar", settings.toggleTopToolbar, "toggleTopToolbar");
            script += GetMenuItemScript("Close all Editor Fullscreen Windows", "CloseAllEditorFullscreenWindows", settings.closeAllFullscreenWindows, "closeAllFullscreenWindows");

            script += "    }\r\n}\r\n";
            try
            {
                File.WriteAllText(scriptFilePath, script);
                EditorPrefs.SetString("EditorFullscreenWindowSettings_GUID", guid);
                string assetPath = ("Assets/" + scriptFileSubPath).Replace("//", "/"); //Replace double slashes to support some older versions which have an extra slash.
                AssetDatabase.ImportAsset(assetPath);
            }
            catch (IOException) { Debug.LogError("Write error. Could not write the menu items script to disk: " + scriptFilePath); }
        }

        private static string GetMenuItemScript(string label, string methodName, EditorFullscreenSettings.FullscreenOption fullscreenOptions, string fullscreenHotkey)
        {

            string hotkeyString = " _" + EditorInput.GetKeyMenuItemString(fullscreenOptions.hotkey, fullscreenOptions.modifiers);

#if UNITY_STANDALONE_WIN && (UNITY_5_0 || UNITY_5_1)
            //Disable menu-item hotkeys for Unity pre-5.4 because of strange bugs which occur when they have no modifier. Instead the hotkeys are handled through EditorInput.
            hotkeyString = "";
            label += char.ConvertFromUtf32(9) + EditorInput.GetKeysDownString(fullscreenOptions.hotkey, fullscreenOptions.modifiers);
#endif

            string fullscreenOpsString = "EditorFullscreenSettings.settings." + fullscreenHotkey;
            string methodCall = "EditorFullscreenController.TriggerFullscreenHotkey(" + fullscreenOpsString + ");";

            string script = "        [MenuItem(" + typeof(EditorFullscreenSettings).Name + ".MENU_ITEM_PATH + \"" + label + hotkeyString + "\")]\r\n"
                          + "        public static void " + methodName + "() {" + methodCall + "}\r\n";
            return script;

        }

        private static string GetMenuItemScript(string label, string methodName, EditorFullscreenSettings.FullscreenOption fullscreenOptions)
        {
            string hotkeyString = EditorInput.GetKeyMenuItemString(fullscreenOptions.hotkey, fullscreenOptions.modifiers);
            string script = "        [MenuItem(" + typeof(EditorFullscreenSettings).Name + ".MENU_ITEM_PATH + \"" + label + " _" + hotkeyString + "\")]\r\n"
                          + "        public static void " + methodName + "() {EditorFullscreenController." + methodName + "();}\r\n";
            return script;
        }
    }
}