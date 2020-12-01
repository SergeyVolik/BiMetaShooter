using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Reflection;
namespace MFPSEditor
{
    /// <summary>
    /// This attribute can only be applied to fields because its
    /// associated PropertyDrawer only operates on fields (either
    /// public or tagged with the [SerializeField] attribute) in
    /// the target MonoBehaviour.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class InspectorButtonAttribute : PropertyAttribute
    {
        public static float kDefaultButtonWidth = 80;

        public readonly string MethodName;

        private float _buttonWidth = kDefaultButtonWidth;
        public float ButtonWidth
        {
            get { return _buttonWidth; }
            set { _buttonWidth = value; }
        }

        public InspectorButtonAttribute(string MethodName)
        {
            this.MethodName = MethodName;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(InspectorButtonAttribute))]
    public class InspectorButtonPropertyDrawer : PropertyDrawer
    {
        private MethodInfo _eventMethodInfo = null;

        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            InspectorButtonAttribute inspectorButtonAttribute = (InspectorButtonAttribute)attribute;
            float width = EditorStyles.toolbarButton.CalcSize(new GUIContent(label.text)).x;
            if(width < inspectorButtonAttribute.ButtonWidth) { width = inspectorButtonAttribute.ButtonWidth; }
            Rect buttonRect = new Rect(position.x + (position.width - width) * 0.5f, position.y, width, position.height);
            if (GUI.Button(buttonRect, label.text,EditorStyles.toolbarButton))
            {
                System.Type eventOwnerType = prop.serializedObject.targetObject.GetType();
                string eventName = inspectorButtonAttribute.MethodName;

                if (_eventMethodInfo == null)
                    _eventMethodInfo = eventOwnerType.GetMethod(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (_eventMethodInfo != null)
                    _eventMethodInfo.Invoke(prop.serializedObject.targetObject, null);
                else
                    Debug.LogWarning(string.Format("InspectorButton: Unable to find method {0} in {1}", eventName, eventOwnerType));
            }
        }
    }

   /* [CustomPropertyDrawer(typeof(bl_GameData.SceneInfo))]
    public class SceneFieldPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
        {
            EditorGUI.BeginProperty(_position, GUIContent.none, _property);
            SerializedProperty showName = _property.FindPropertyRelative("ShowName");
            SerializedProperty sceneAsset = _property.FindPropertyRelative("m_Scene");
            SerializedProperty sceneName = _property.FindPropertyRelative("RealSceneName");
            SerializedProperty prw = _property.FindPropertyRelative("Preview");
            Rect r = _position;
            showName.stringValue = EditorGUI.TextField(r, "Show Name", showName.stringValue);
            r.y += 20;
            sceneAsset.objectReferenceValue = EditorGUI.ObjectField(r, "Scene", sceneAsset.objectReferenceValue, typeof(SceneAsset), false);
            EditorGUI.EndProperty();
        }
    }
    */
#endif
}