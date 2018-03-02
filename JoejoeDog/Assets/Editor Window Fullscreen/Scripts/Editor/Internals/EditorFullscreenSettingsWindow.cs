/* 
 * Author:  Johanan Round
 * Package: Editor Window Fullscreen
 * License: Unity Asset Store EULA (Editor extension asset. Requires 1 license per machine.)
 */

using UnityEngine;
using UnityEditor;

namespace EditorWindowFullscreen
{
    public class EditorFullscreenSettingsWindow : EditorWindow
    {
        
        [MenuItem(EditorFullscreenSettings.MENU_ITEM_PATH + "Fullscreen Window Settings... %#F8", false, 0)]
        public static void FullscreenWindowSettings()
        {
            var settingsWin = EditorWindow.GetWindow<EditorFullscreenSettingsWindow>(true, "Editor Window Fullscreen Settings", true);
            try
            {
                //Move the settings window to an offset from the main window
                var mainWinPos = EditorMainWindow.position;
                var newPos = new Rect(settingsWin.position);
                newPos.x = mainWinPos.x + 200;
                newPos.y = mainWinPos.y + 100;

                newPos.width = settingsWin.minSize.x;
                newPos.height = settingsWin.minSize.y;

                settingsWin.position = newPos;
            }
            catch
            {
                Debug.Log("Couldn't get the Main Window position");
            }
        }

        private GUIStyle headerStyle = new GUIStyle();
        private GUIStyle subHeaderStyle = new GUIStyle();
        private GUIStyle smallHeadingStyle = new GUIStyle();
        
        public EditorFullscreenSettings settings;

        private bool allowApplySettings = false;
        private bool hotkeyWasSet = false;
        private bool hotkeysWereChanged = false;

        void OnEnable()
        {
            settings = EditorFullscreenSettings.settings;
            var proHeadingColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            this.minSize = new Vector2(540, 620);

            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? proHeadingColor : new Color(0.25f, 0.25f, 0.25f, 1f);
            headerStyle.margin.top = 10;
            headerStyle.margin.bottom = 5;

            subHeaderStyle.fontSize = 14;
            subHeaderStyle.fontStyle = FontStyle.Bold;
            subHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? proHeadingColor : new Color(0.25f, 0.25f, 0.25f, 1f);

            subHeaderStyle.margin.top = 10;

            smallHeadingStyle.fontStyle = FontStyle.Bold;
            smallHeadingStyle.margin.top = 5;
            smallHeadingStyle.margin.left = 6;
            if (EditorGUIUtility.isProSkin)
              smallHeadingStyle.normal.textColor = proHeadingColor;

            settings.LoadSettings();
            EditorFullscreenState.TriggerFullscreenEvent(this, this.GetType(), Vector2.zero, false); //Notify everyone that the settings window was opened.
        }

        void OnGUI()
        {
            hotkeyWasSet = false;
            var style = new GUIStyle();
            var buttonStyle = new GUIStyle();
            var smallIndent = new GUIStyle();
            style.margin.left = 20;
            style.margin.right = 20;
            smallIndent.margin.left = 3;

            EditorGUILayout.BeginVertical(style);
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 245f;
            GUILayout.Label("Fullscreen Window Settings", headerStyle, new GUILayoutOption[0]);

            //Reset to defaults button
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 10;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.padding = new RectOffset(0, 0, 5, 5);
            buttonStyle.margin.top = 10;
            buttonStyle.margin.right = 24;
            if (GUILayout.Button(new GUIContent("Reset to Defaults", "Reset all settings to their default values."), buttonStyle, new GUILayoutOption[0]))
            {
                EditorFullscreenSettings.ResetToDefaults();
                settings = EditorFullscreenSettings.settings;
                hotkeysWereChanged = true;
                settings.SaveSettings(hotkeysWereChanged);
                allowApplySettings = false;
                hotkeysWereChanged = false;
                Repaint();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(style);

            //Fullscreen Window Hotkeys
            EditorGUIUtility.labelWidth = 165f;
            GUILayout.Label("Fullscreen Window Hotkeys", subHeaderStyle, new GUILayoutOption[0]);
            EditorGUILayout.BeginVertical(smallIndent);
            AddFullscreenOption(ref settings.mainUnityWindow, "Main Unity Window", true, true);
            AddFullscreenOption(ref settings.sceneWindow, "Scene Window", true, true);
            AddFullscreenOption(ref settings.gameWindow, "Game Window", true, true);
            AddFullscreenOption(ref settings.currentlyFocusedWindow, "Currently Focused Window", true, true);
            AddFullscreenOption(ref settings.windowUnderCursor, "Window Under Cursor", true, true);
            EditorGUILayout.EndVertical();

            //Other Hotkeys
            GUILayout.Label("Other Hotkeys", subHeaderStyle, new GUILayoutOption[0]);
            EditorGUILayout.BeginVertical(smallIndent);
            AddFullscreenOption(ref settings.toggleTopToolbar, "Show/Hide Top Toolbar", false, false);
            EditorGUILayout.EndVertical();
            style = new GUIStyle(smallIndent);
            style.margin.top = 8;
            EditorGUILayout.BeginVertical(style);
            AddFullscreenOption(ref settings.closeAllFullscreenWindows, "Close All Fullscreen Windows", false, false);
            EditorGUILayout.EndVertical();


            //Game Window Fullscreen Options
            EditorGUIUtility.labelWidth = 245f;
            GUILayout.Label("Game Window Options", subHeaderStyle, new GUILayoutOption[0]);
            EditorGUILayout.BeginVertical(smallIndent);

            var label = new GUIContent("Start the Game When Entering Fullscreen", "Start the game when entering a fullscreen game window.");
            settings.startGameWhenEnteringFullscreen = EditorGUILayout.Toggle(label, settings.startGameWhenEnteringFullscreen, new GUILayoutOption[0]);

            label = new GUIContent("Stop the Game When Exiting Fullscreen", "Stop the game when exiting fullscreen game window.");
            settings.stopGameWhenExitingFullscreen = (EditorFullscreenSettings.StopGameWhenExitingFullscreen)EditorGUILayout.EnumPopup(label, settings.stopGameWhenExitingFullscreen, new GUILayoutOption[0]);

            var ofsGroup = new GUIStyle();
            ofsGroup.margin.top = 0;
            ofsGroup.margin.bottom = smallHeadingStyle.margin.top;
            EditorGUILayout.BeginVertical(ofsGroup);
            label = new GUIContent("Open Fullscreen On Game Start", "Open a fullscreen game window when the game starts.");
            AddFullscreenOption(ref settings.openFullscreenOnGameStart, label, true, true, false, new[] { 0, 1, 2, 3 });
            EditorGUILayout.EndVertical();

            label = new GUIContent("Close Fullscreen On Game Stop", "Close fullscreen game window/s when the game stops.");
            settings.closeFullscreenOnGameStop = (EditorFullscreenSettings.CloseFullscreenOnGameStop)EditorGUILayout.EnumPopup(label, settings.closeFullscreenOnGameStop, new GUILayoutOption[0]);

            EditorGUILayout.EndVertical();


            if (GUI.changed || hotkeyWasSet)
            {
                if (hotkeyWasSet) hotkeysWereChanged = true;
                allowApplySettings = true;
            }

            EditorGUILayout.BeginVertical();
            EditorGUI.BeginDisabledGroup(!allowApplySettings);
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 15;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.padding = new RectOffset(0, 0, 10, 10);
            buttonStyle.margin.top = 12;
            if (GUILayout.Button(new GUIContent("Apply Settings", "Apply all changes."), buttonStyle, new GUILayoutOption[0]))
            {
                settings.SaveSettings(hotkeysWereChanged);
                allowApplySettings = false;
                hotkeysWereChanged = false;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(true);
            style = new GUIStyle();
            style.fontStyle = FontStyle.Normal;
            style.fontSize = 9;
            style.margin.left = 9;
            style.margin.top = 6;
            if (EditorGUIUtility.isProSkin) style.normal.textColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            
            GUILayout.Label("Current Mouse Position: " + EditorInput.MousePosition, style, new GUILayoutOption[0]);
            EditorGUI.EndDisabledGroup();

            //Set the focused control
            if (Event.current.type == EventType.Repaint)
            {
                focusedFieldName = GUI.GetNameOfFocusedControl();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        void AddFullscreenOption(ref EditorFullscreenSettings.FullscreenOption fullscreenOption, string label, bool showOpenAtOption, bool showToolbarOption)
        {
            AddFullscreenOption(ref fullscreenOption, new GUIContent(label), showOpenAtOption, showToolbarOption, true, new[] { 1, 2, 3 });
        }
        void AddFullscreenOption(ref EditorFullscreenSettings.FullscreenOption fullscreenOption, GUIContent label, bool showOpenAtOption, bool showToolbarOption, bool showHotkey, int[] displayedOpenAtOptions)
        {
            var indent = new GUIStyle();
            if (!showHotkey) indent.margin.left = 10;
            var initLabelWidth = EditorGUIUtility.labelWidth;

            if (!(showOpenAtOption || showToolbarOption))
                EditorGUILayout.BeginHorizontal();

            GUILayout.Label(label, smallHeadingStyle, new GUILayoutOption[0]);

            if (showOpenAtOption || showToolbarOption)
            {
                EditorGUIUtility.labelWidth = initLabelWidth - indent.margin.left;
                EditorGUILayout.BeginVertical(indent);
            }

            if (showOpenAtOption)
            {
                EditorGUILayout.BeginHorizontal();
                fullscreenOption.openAtPosition = (EditorFullscreenSettings.OpenFullscreenAtPosition)AddFilteredEnumPopup(new GUIContent("Enter Fullscreen"), fullscreenOption.openAtPosition, displayedOpenAtOptions);

                if (fullscreenOption.openAtPosition == EditorFullscreenSettings.OpenFullscreenAtPosition.AtSpecifiedPosition)
                {
                    var prevLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 15;
                    EditorGUILayout.BeginHorizontal(new[] { GUILayout.MaxWidth(100f) });
                    fullscreenOption.position.x = EditorGUILayout.IntField(new GUIContent("x", "x position"), (int)fullscreenOption.position.x, new GUILayoutOption[0]);
                    fullscreenOption.position.y = EditorGUILayout.IntField(new GUIContent("y", "y position"), (int)fullscreenOption.position.y, new GUILayoutOption[0]);
                    EditorGUILayout.EndHorizontal();
                    EditorGUIUtility.labelWidth = prevLabelWidth;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (showOpenAtOption || showToolbarOption)
                EditorGUILayout.BeginHorizontal();
            if (showToolbarOption)
                fullscreenOption.showToolbarByDefault = EditorGUILayout.Toggle("Show Toolbar By Default", fullscreenOption.showToolbarByDefault, new GUILayoutOption[0]);

            //Hotkey
            if (showHotkey)
                hotkeyWasSet = hotkeyWasSet || AddHotkeyField("Hotkey", ref fullscreenOption.hotkey, ref fullscreenOption.modifiers);

            EditorGUILayout.EndHorizontal();
            if (showOpenAtOption || showToolbarOption)
            {
                EditorGUILayout.EndVertical();
                EditorGUIUtility.labelWidth = initLabelWidth;
            }
        }

        private int AddFilteredEnumPopup(GUIContent label, System.Enum selectedValue, int[] displayedOptions)
        {
            var selectedVal = System.Convert.ToInt32(selectedValue);
            var visibleOptions = new GUIContent[displayedOptions.Length];
            var optionValues = new int[displayedOptions.Length];

            for (int i = 0; i < displayedOptions.Length; i++)
            {
                optionValues[i] = displayedOptions[i];
                visibleOptions[i] = new GUIContent();
                visibleOptions[i].text = System.Enum.GetName(selectedValue.GetType(), displayedOptions[i]);
                if (visibleOptions[i].text == null) visibleOptions[i].text = "Undefined";
                else
                {
                    visibleOptions[i].text = EditorFullscreenSettings.FormatCamelCaseName(visibleOptions[i].text);
                }
            }

            return EditorGUILayout.IntPopup(label, selectedVal, visibleOptions, optionValues, new GUILayoutOption[0]);
        }

        private string focusedFieldName;
        private bool AddHotkeyField(string label, ref KeyCode hotkey, ref EventModifiers modifiers)
        {
            bool hotkeyWasSet = false;
            var guiLabel = new GUIContent(label);
            var s = new GUIStyle(GUIStyle.none);
            s.alignment = TextAnchor.LowerRight;
            s.fixedHeight = 15f;
            s.margin.left = smallHeadingStyle.margin.left;
            s.normal.textColor = EditorStyles.label.normal.textColor;

            EditorGUILayout.BeginHorizontal(new[] { GUILayout.MaxWidth(230f) });
            GUILayout.Label(guiLabel, s, new GUILayoutOption[0]);
            Rect textFieldRect = GUILayoutUtility.GetRect(guiLabel, GUI.skin.textField, new GUILayoutOption[0]);

            //Give the control a unique name using its label and position
            string controlName = label + textFieldRect;

            //Check for Key Press (Must be done before the TextField is set, because it uses the event)
            if (focusedFieldName == controlName && Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode != KeyCode.None && Event.current.keyCode != KeyCode.LeftControl)
                {
                    hotkey = Event.current.keyCode;
                    modifiers = Event.current.modifiers;
                    hotkeyWasSet = true;
                }
            }

            //Create the GUI Hotkey Field
            string keysDownString = EditorInput.GetKeysDownString(hotkey, modifiers);
            GUI.SetNextControlName(controlName);
            EditorGUI.SelectableLabel(textFieldRect, "", EditorStyles.textField);
            EditorGUI.LabelField(textFieldRect, keysDownString, EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            return hotkeyWasSet;
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

    }
}
