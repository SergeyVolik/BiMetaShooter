using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MFPSEditor.Addons
{
    [CreateAssetMenu(fileName = "MFPS Addon", menuName = "MFPS/Addon Info", order = 300)]
    public class MFPSAddon : ScriptableObject
    {
        public string Name;
        public string Version;
        public string MinMFPSVersion = "1.6";

        [TextArea(4, 10)]
        public string Instructions;
        public string TutorialScript = "";
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public void OnLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                if (MFPSAddonsData.Instance != null)
                {
                    int i = MFPSAddonsData.Instance.Addons.FindIndex(x => x.NiceName == Name);
                    if (i >= 0 && MFPSAddonsData.Instance.Addons[i].Info == null)
                    {
                        MFPSAddonsData.Instance.Addons[i].Info = this;
                        EditorUtility.SetDirty(MFPSAddonsData.Instance);
                        AssetDatabase.SaveAssets();
                       AssetDatabase.Refresh();
                    }
                }
        }
#endif
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(MFPSAddon))]
    public class MFPSAddonsEditor : Editor
    {
        MFPSAddon script;
        private GUIStyle TextStyle = null;
        private GUIStyle TextStyleFlat = null;
        private bool editMode = false;

        private void OnEnable()
        {
            script = (MFPSAddon)target;
            TextStyle = Resources.Load<GUISkin>("content/MFPSEditorSkin").customStyles[3];
            TextStyleFlat = Resources.Load<GUISkin>("content/MFPSEditorSkin").customStyles[1];

            if (MFPSAddonsData.Instance != null)
            {
                int i = MFPSAddonsData.Instance.Addons.FindIndex(x => x.NiceName == script.Name);
                if (i >= 0 && MFPSAddonsData.Instance.Addons[i].Info == null)
                {
                    MFPSAddonsData.Instance.Addons[i].Info = script;
                    EditorUtility.SetDirty(MFPSAddonsData.Instance);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }          
        }

        public override void OnInspectorGUI()
        {
            if (!editMode && !string.IsNullOrEmpty(script.Name))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(string.Format("<size=30>{0}</size>", script.Name.ToUpper()), TextStyle);
                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("", GUILayout.Width(10), GUILayout.Height(25)))
                {
                    editMode = !editMode;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(string.Format("<size=14>VERSION: <b>{0}</b></size>", script.Version), TextStyleFlat);
                GUILayout.Space(10);
                EditorGUILayout.LabelField(string.Format("<size=14>MIN MFPS: <b>{0}</b></size>", script.MinMFPSVersion), TextStyleFlat);
                EditorGUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(script.TutorialScript))
                {
                    if (GUILayout.Button("<color=#FFE300FF>OPEN TUTORIAL</color>", TextStyleFlat))
                    {
                        EditorWindow.GetWindow(System.Type.GetType(string.Format("{0}, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", script.TutorialScript)));
                    }
                }
                GUILayout.Space(20);
                EditorGUILayout.TextArea(script.Instructions, TextStyleFlat);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("", GUILayout.Width(10), GUILayout.Height(25)))
                {
                    editMode = !editMode;
                }
                EditorGUILayout.EndHorizontal();
                DrawDefaultInspector();
            }
            GUILayout.Space(25);
            if (GUILayout.Button("ADDONS MANAGER", EditorStyles.toolbarButton))
            {
                EditorWindow.GetWindow<MFPSAddonsWindow>().OpenAddonPage(script.Name);
            }
        }
    }
#endif
}