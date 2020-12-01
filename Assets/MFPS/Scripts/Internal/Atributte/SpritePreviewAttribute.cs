using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MFPSEditor
{
    public class SpritePreviewAttribute : PropertyAttribute
    {
        public SpritePreviewAttribute()
        {
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
    public class SpritePreviewAttributeDrawer : PropertyDrawer
    {
        SpritePreviewAttribute script { get { return ((SpritePreviewAttribute)attribute); } }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue == null)
            {
                EditorGUI.PropertyField(position, property);
            }
            else
            {
                Sprite spr = property.objectReferenceValue as Sprite;
                Rect imgp = position;
                imgp.height = EditorGUIUtility.singleLineHeight * 2;
                imgp.width = imgp.height;
                imgp.x += 20;
                GUI.DrawTexture(imgp, spr.texture, ScaleMode.ScaleAndCrop);
                position.x += imgp.height + 25;
                position.width -= imgp.height + 25;
                position.height = EditorGUIUtility.singleLineHeight;
                position.y += EditorGUIUtility.singleLineHeight * 0.5f;
                EditorGUI.PropertyField(position, property);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight * 2;
            }
        }
    }
#endif
}