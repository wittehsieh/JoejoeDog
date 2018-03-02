/* 
 * Author:  Johanan Round
 * Package: Editor Window Fullscreen
 * License: Unity Asset Store EULA (Editor extension asset. Requires 1 license per machine.)
 */

using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using SceneViewState = UnityEditor.SceneView.SceneViewState;
using System.Linq;

namespace EditorWindowFullscreen
{
    class CustomSceneView : SceneView
    {
#pragma warning disable 0649
        public bool toolbarVisible;
#pragma warning restore 0649
        static MethodInfo baseOnGUI;
        static FieldInfo basePos;

        public static bool takeAudioState = true; //If true, takes the audio state of the last active scene view.
        private SceneView tookStateFromSceneView;
        private bool audioInitiallyEnabled;

#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3
        [InitializeOnLoadMethod]
        static void CreateCustomSceneViewIcon()
        {
            //Setup an icon for the CustomSceneView to avoid a missing icon error.
            try
            {
                string iconGUID = AssetDatabase.AssetPathToGUID("Assets/Editor Default Resources/Icons/EditorWindowFullscreen.CustomSceneView.png");
                if (String.IsNullOrEmpty(iconGUID))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Editor Default Resources"))
                        AssetDatabase.CreateFolder("Assets", "Editor Default Resources");
                    if (!AssetDatabase.IsValidFolder("Assets/Editor Default Resources/Icons"))
                        AssetDatabase.CreateFolder("Assets/Editor Default Resources", "Icons");

                    AssetDatabase.CopyAsset("Assets/Editor Window Fullscreen/Icons/CustomSceneView.png", "Assets/Editor Default Resources/Icons/EditorWindowFullscreen.CustomSceneView.png");
                }
            }
            catch (System.Exception e)
            {
                if (EditorFullscreenState.LogNonFatalErrors) Debug.LogException(e);
            }

        }
#endif

        static CustomSceneView()
        {
            baseOnGUI = typeof(SceneView).GetMethod("OnGUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            basePos = typeof(EditorWindow).GetField("m_Pos", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public override void OnEnable()
        {
            var last = SceneView.lastActiveSceneView;

            var logEnabled = loggingEnabled;
            loggingEnabled = false;

            try
            {
                this.SetWindowTitle("Scene", true);
                base.OnEnable();

#if UNITY_5_4_OR_NEWER || UNITY_5_3 || UNITY_5_2 || UNITY_5_1
                var sceneIconContent = EditorGUIUtility.IconContent(typeof(SceneView).ToString());
                if (sceneIconContent != null) 
                    this.titleContent.image = sceneIconContent.image;
#endif
            }
            catch (System.Exception e)
            {
                loggingEnabled = logEnabled;
                if (EditorFullscreenState.LogNonFatalErrors) Debug.LogException(e);
            }
            finally
            {
                loggingEnabled = logEnabled;
            }

            if (last != null)
            {
                //Overwite the contents of the new scene view state with the last scene view state.
                tookStateFromSceneView = last;
                audioInitiallyEnabled = last.m_AudioPlay;
                last.CopyStateTo(this);
            }
        }

        void OnGUI()
        {
            var offsetToolbarHeight = EditorFullscreenState.sceneViewToolbarHeight + 1;
            var pos = this.basePosField;

            if (!toolbarVisible)
            {
                GUI.BeginGroup(new Rect(0, -offsetToolbarHeight, this.position.width, this.position.height + offsetToolbarHeight));

                //Trick the base OnGUI into drawing the Scene View at full size by temporarily increasing the window height.
                pos.height += offsetToolbarHeight;
                this.basePosField = pos;
            }

            try
            {
                baseOnGUI.Invoke(this, null);
            }
            catch (TargetInvocationException e)
            {
                if (EditorFullscreenState.LogNonFatalErrors) Debug.LogException(e);
            }
            catch (ExitGUIException e)
            {
                if (EditorFullscreenState.LogNonFatalErrors) Debug.LogException(e);
            }

            if (!toolbarVisible)
            {
                //Reset the window height
                pos.height -= offsetToolbarHeight;
                this.basePosField = pos;

                GUI.EndGroup();
            }
        }

        new void OnDestroy()
        {
            //If the scene view which had its audio state taken still exists, re-enable the audio if it was originally enabled (Only one SceneView at a time can have audio enabled).
            if (takeAudioState && tookStateFromSceneView != null && audioInitiallyEnabled && this.m_AudioPlay)
            {
                tookStateFromSceneView.m_AudioPlay = audioInitiallyEnabled;
                base.OnDestroy();
                try
                {
                    MethodInfo onFocusMethod = typeof(SceneView).GetMethod("OnFocus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (onFocusMethod != null) onFocusMethod.Invoke(tookStateFromSceneView, null);
                }
                catch (System.Exception e)
                {
                    if (EditorFullscreenState.LogNonFatalErrors) Debug.LogException(e);
                }
            }
            else
            {
                base.OnDestroy();
            }
        }

        private bool loggingEnabled
        {
            get
            {
#if UNITY_5_4_OR_NEWER
                return Debug.unityLogger.logEnabled;
#elif UNITY_5_3
                return Debug.logger.logEnabled;
#else
                return true;
#endif
            }
            set
            {
#if UNITY_5_4_OR_NEWER || UNITY_5_3
                Debug.unityLogger.logEnabled = value;
#else
                //Do nothing
#endif
            }
        }

        private Rect basePosField
        {
            get { return (Rect)basePos.GetValue(this); }
            set { basePos.SetValue(this, value); }
        }

        public static Type GetWindowType()
        {
            return typeof(SceneView);
        }


    }

    public static partial class SceneViewExtensions
    {
        private static SceneViewState GetSceneViewState(this SceneView sceneView)
        {
            SceneViewState state = null;
            try
            {
                FieldInfo sceneViewState = typeof(SceneView).GetField("m_SceneViewState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                state = (SceneViewState)sceneViewState.GetValue(sceneView);
            }
            catch (System.Exception e)
            {
                if (EditorFullscreenState.LogNonFatalErrors) Debug.LogException(e);
            }
            return state;
        }

        private static SceneViewState SetSceneViewState(this SceneView sceneView, SceneViewState newState)
        {
            SceneViewState state = null;
            try
            {
                FieldInfo sceneViewState = typeof(SceneView).GetField("m_SceneViewState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                sceneViewState.SetValue(sceneView, newState);
            }
            catch (System.Exception e)
            {
                if (EditorFullscreenState.LogNonFatalErrors) Debug.LogException(e);
            }
            return state;
        }

        /// <summary>
        /// Copy the state of the SceneView to another SceneView.
        /// </summary>
        /// <param name="from">The SceneView to copy from.</param>
        /// <param name="copyToSceneView">The SceneView to copy to.</param>
        public static void CopyStateTo(this SceneView fromSceneView, SceneView copyToSceneView)
        {
            SceneViewState fromState = GetSceneViewState(fromSceneView);
            SceneViewState fromStateCopy = new SceneViewState(fromState);
            copyToSceneView.SetSceneViewState(fromStateCopy);

            var publicFields = typeof(SceneView).GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Where(field => (field.FieldType.IsValueType || field.FieldType == typeof(string))).ToList();
            var publicProperties = typeof(SceneView).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Where(prop => prop.PropertyType.IsArray == false && (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string)) && prop.GetGetMethod() != null && prop.GetSetMethod() != null).ToList();

            foreach (var field in publicFields)
            {
                if (CustomSceneView.takeAudioState == false && field.Name == "m_AudioPlay") continue;
                field.SetValue(copyToSceneView, field.GetValue(fromSceneView));
            }
            foreach (var prop in publicProperties)
            {
                prop.SetValue(copyToSceneView, prop.GetValue(fromSceneView, null), null);
            }
        }
    }
}