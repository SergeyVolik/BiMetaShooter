using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using MFPSEditor;
using UnityEditorInternal;

[CustomEditor(typeof(bl_BodyPartManager))]
public class bl_BodyPartManagerEditor : ReorderableArrayInspector
{
    bl_BodyPartManager script;
    private bool m_BodyMaskFoldout = true;
    public static GUIContent BodyMask = new GUIContent("Body", "Define body part damage multiplier");
    float[] multipliers = new float[4] { 1, 1, 1, 1 };

    /// <summary>
    /// 
    /// </summary>
    private void OnEnable()
    {
        script = (bl_BodyPartManager)target;
       // hblist = ReorderableListUtility.CreateAutoLayout(serializedObject.FindProperty("HitBoxs"));
        if(script.HitBoxs.Exists(x => x.Bone == HumanBodyBones.Head)) { multipliers[0] = script.HitBoxs.Find(x => x.Bone == HumanBodyBones.Head).DamageMultiplier; }
        if (script.HitBoxs.Exists(x => x.Bone == HumanBodyBones.LeftUpperArm)) { multipliers[1] = script.HitBoxs.Find(x => x.Bone == HumanBodyBones.LeftUpperArm).DamageMultiplier; }
        if (script.HitBoxs.Exists(x => x.Bone == HumanBodyBones.Spine)) { multipliers[2] = script.HitBoxs.Find(x => x.Bone == HumanBodyBones.Spine).DamageMultiplier; }
        if (script.HitBoxs.Exists(x => x.Bone == HumanBodyBones.LeftUpperLeg)) { multipliers[3] = script.HitBoxs.Find(x => x.Bone == HumanBodyBones.LeftUpperLeg).DamageMultiplier; }
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        serializedObject.Update();
        m_BodyMaskFoldout = EditorGUILayout.Foldout(m_BodyMaskFoldout, BodyMask, true);
        if (m_BodyMaskFoldout)
        {
            GUILayout.BeginHorizontal("box");
            Show(9);
            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            GUILayout.Space(10);
            GUILayout.Label("HEAD DAMAGE MULTIPLIER");
            multipliers[0] = EditorGUILayout.Slider(multipliers[0],1,12);
            GUILayout.Space(30);
            GUILayout.Label("ARMS DAMAGE MULTIPLIER");
            multipliers[1] = EditorGUILayout.Slider(multipliers[1], 1, 12);
            GUILayout.Space(15);
            GUILayout.Label("CHEST DAMAGE MULTIPLIER");
            multipliers[2] = EditorGUILayout.Slider(multipliers[2], 1, 12);
            GUILayout.Space(60);
            GUILayout.Label("LENGS DAMAGE MULTIPLIER");
            multipliers[3] = EditorGUILayout.Slider(multipliers[3], 1, 12);
            GUILayout.Space(20);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.BeginVertical("box");
        //ReorderableListUtility.DoLayoutListWithFoldout(hblist, "HitBoxes");
        DrawPropertiesAll();
        script.PlayerAnimation = EditorGUILayout.ObjectField("Player Animation", script.PlayerAnimation, typeof(bl_PlayerAnimations), true) as bl_PlayerAnimations;
        script.m_Animator = EditorGUILayout.ObjectField("Animator", script.m_Animator, typeof(Animator), true) as Animator;
        script.RightHand = EditorGUILayout.ObjectField("Right Hand", script.RightHand, typeof(Transform), true) as Transform;
        script.PelvisBone = EditorGUILayout.ObjectField("Pelvis Bone", script.PelvisBone, typeof(Transform), true) as Transform;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);
        //EditorGUILayout.PropertyField(rigids, true);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            UpdateMultipliers();
            EditorUtility.SetDirty(target);
        }
    }

    void UpdateMultipliers()
    {
        SetValueToBox(HumanBodyBones.Head, multipliers[0]);
        SetValueToBox(HumanBodyBones.LeftUpperArm, multipliers[1]);
        SetValueToBox(HumanBodyBones.LeftLowerArm, multipliers[1]);
        SetValueToBox(HumanBodyBones.RightUpperArm, multipliers[1]);
        SetValueToBox(HumanBodyBones.RightLowerArm, multipliers[1]);
        SetValueToBox(HumanBodyBones.Spine, multipliers[2]);
        SetValueToBox(HumanBodyBones.LeftUpperLeg, multipliers[3]);
        SetValueToBox(HumanBodyBones.LeftLowerLeg, multipliers[3]);
        SetValueToBox(HumanBodyBones.RightUpperLeg, multipliers[3]);
        SetValueToBox(HumanBodyBones.RightLowerLeg, multipliers[3]);
    }

    void SetValueToBox(HumanBodyBones bone, float value)
    {
        if(script.HitBoxs.Exists(x => x.Bone == bone))
        {
            script.HitBoxs.Find(x => x.Bone == bone).DamageMultiplier = value;
        }
    }

    static class Styles
    {
        public static GUIContent UnityDude = EditorGUIUtility.IconContent("AvatarInspector/BodySIlhouette");
        public static GUIContent PickingTexture = EditorGUIUtility.IconContent("AvatarInspector/BodyPartPicker");

        public static GUIContent[] BodyPart =
        {
                EditorGUIUtility.IconContent("AvatarInspector/MaskEditor_Root"),
                EditorGUIUtility.IconContent("AvatarInspector/Torso"),

                EditorGUIUtility.IconContent("AvatarInspector/Head"),

                EditorGUIUtility.IconContent("AvatarInspector/LeftLeg"),
                EditorGUIUtility.IconContent("AvatarInspector/RightLeg"),

                EditorGUIUtility.IconContent("AvatarInspector/LeftArm"),
                EditorGUIUtility.IconContent("AvatarInspector/RightArm"),

                EditorGUIUtility.IconContent("AvatarInspector/LeftFingers"),
                EditorGUIUtility.IconContent("AvatarInspector/RightFingers"),

                EditorGUIUtility.IconContent("AvatarInspector/LeftFeetIk"),
                EditorGUIUtility.IconContent("AvatarInspector/RightFeetIk"),

                EditorGUIUtility.IconContent("AvatarInspector/LeftFingersIk"),
                EditorGUIUtility.IconContent("AvatarInspector/RightFingersIk"),
            };
    }

    protected static Color[] m_MaskBodyPartPicker =
      {
            new Color(255 / 255.0f,   144 / 255.0f,     0 / 255.0f), // root
            new Color(0 / 255.0f, 174 / 255.0f, 240 / 255.0f), // body
            new Color(171 / 255.0f, 160 / 255.0f,   0 / 255.0f), // head

            new Color(0 / 255.0f, 255 / 255.0f,     255 / 255.0f), // ll
            new Color(247 / 255.0f,   151 / 255.0f, 121 / 255.0f), // rl

            new Color(0 / 255.0f, 255 / 255.0f, 0 / 255.0f), // la
            new Color(86 / 255.0f, 116 / 255.0f, 185 / 255.0f), // ra

            new Color(255 / 255.0f,   255 / 255.0f,     0 / 255.0f), // lh
            new Color(130 / 255.0f,   202 / 255.0f, 156 / 255.0f), // rh

            new Color(82 / 255.0f,    82 / 255.0f,      82 / 255.0f), // lfi
            new Color(255 / 255.0f,   115 / 255.0f,     115 / 255.0f), // rfi
            new Color(159 / 255.0f,   159 / 255.0f,     159 / 255.0f), // lhi
            new Color(202 / 255.0f,   202 / 255.0f, 202 / 255.0f), // rhi

            new Color(101 / 255.0f,   101 / 255.0f, 101 / 255.0f), // hi
        };

    static string sAvatarBodyMaskStr = "AvatarMask";
    static int s_Hint = sAvatarBodyMaskStr.GetHashCode();

    public void Show(int count)
    {
        if (Styles.UnityDude.image)
        {
            Rect rect = GUILayoutUtility.GetRect(Styles.UnityDude, GUIStyle.none, GUILayout.MaxWidth(Styles.UnityDude.image.width));
            //rect.x += (Screen.width - rect.width) / 2;

            Color oldColor = GUI.color;
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            GUI.DrawTexture(rect, Styles.UnityDude.image);

            for (int i = 1; i < count; i++)
            {
                GUI.color = GetPartColor(i);
                if (Styles.BodyPart[i].image)
                {
                    GUI.DrawTexture(rect, Styles.BodyPart[i].image);
                }
              
            }
            GUI.color = oldColor;

           // DoPicking(rect, count);
        }
    }

    private Color GetPartColor(int id)
    {
        switch (id)
        {
            case 2:
                return Color.Lerp(Color.yellow, Color.red, multipliers[0] / 5);
            case 1:
                return Color.Lerp(Color.yellow, Color.red, multipliers[2] / 5);
            case 6:
            case 7:
            case 8:
            case 5:
                return Color.Lerp(Color.yellow, Color.red, multipliers[1] / 5);
            default:
                return Color.Lerp(Color.yellow, Color.red, multipliers[3] / 5);
        }
    }
    protected static void DoPicking(Rect rect, int count)
    {
        if (Styles.PickingTexture.image)
        {
            int id = GUIUtility.GetControlID(s_Hint, FocusType.Passive, rect);
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    {
                        if (rect.Contains(evt.mousePosition))
                        {
                            evt.Use();

                            // Texture coordinate start at 0,0 at bottom, left
                            // Screen coordinate start at 0,0 at top, left
                            // So we need to convert from screen coord to texture coord
                            int top = (int)evt.mousePosition.x - (int)rect.x;
                            int left = Styles.UnityDude.image.height - ((int)evt.mousePosition.y - (int)rect.y);

                            Texture2D pickTexture = Styles.PickingTexture.image as Texture2D;
                            Color color = pickTexture.GetPixel(top, left);

                            bool anyBodyPartPick = false;
                            for (int i = 0; i < count; i++)
                            {
                                if (m_MaskBodyPartPicker[i] == color)
                                {
                                    GUI.changed = true;
                                    //bodyMask.GetArrayElementAtIndex(i).intValue = bodyMask.GetArrayElementAtIndex(i).intValue == 1 ? 0 : 1;
                                    Debug.Log("Pick " + i);
                                    anyBodyPartPick = true;
                                }
                            }

                            if (!anyBodyPartPick)
                            {
                                bool atLeastOneSelected = false;

                                for (int i = 0; i < count && !atLeastOneSelected; i++)
                                {
                                   // atLeastOneSelected = bodyMask.GetArrayElementAtIndex(i).intValue == 1;
                                }

                                for (int i = 0; i < count; i++)
                                {
                                    //bodyMask.GetArrayElementAtIndex(i).intValue = !atLeastOneSelected ? 1 : 0;
                                }
                                GUI.changed = true;
                            }
                        }
                        break;
                    }
            }
        }
    }
}