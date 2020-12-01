using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(bl_WeaponMovements))]
public class bl_EditorWeaponMovement : Editor
{
    private bool isRecording = false;
    Vector3 defaultPosition = Vector3.zero;
    Quaternion defaultRotation = Quaternion.identity;
    bl_WeaponMovements script;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        script = (bl_WeaponMovements)target;

        EditorGUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal("box");
        Color c = isRecording ? Color.red : Color.white;
        GUI.color = c;
        if (GUILayout.Button(new GUIContent(" Edit",EditorGUIUtility.IconContent("d_EditCollider").image), EditorStyles.toolbarButton))
        {
            isRecording = !isRecording;
            if (isRecording)
            {
                defaultPosition = script.transform.localPosition;
                defaultRotation = script.transform.localRotation;
                if (script.moveTo != Vector3.zero)
                {
                    script.transform.localPosition = script.moveTo;
                    script.transform.localRotation = Quaternion.Euler(script.rotateTo);
                }
                Tools.current = Tool.Transform;
                ActiveEditorTracker.sharedTracker.isLocked = true;
            }
            else
            {
                script.transform.localPosition = defaultPosition;
                script.transform.localRotation = defaultRotation;
                ActiveEditorTracker.sharedTracker.isLocked = false;
            }
        }
        GUI.color = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Label("On Run weapon position", EditorStyles.helpBox);
        script.moveTo = EditorGUILayout.Vector3Field("Position", script.moveTo);
        script.rotateTo = EditorGUILayout.Vector3Field("Rotation", script.rotateTo);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Get Actual Position", EditorStyles.toolbarButton))
        {
            script.moveTo = script.transform.localPosition;
            script.rotateTo = script.transform.localEulerAngles;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("On Run and Reload weapon position", EditorStyles.helpBox);
        script.moveToReload = EditorGUILayout.Vector3Field("Position", script.moveToReload);
        script.rotateToReload = EditorGUILayout.Vector3Field("Rotation", script.rotateToReload);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Get Actual Position", EditorStyles.toolbarButton))
        {
            script.moveToReload = script.transform.localPosition;
            script.rotateToReload = script.transform.localRotation.eulerAngles;
        }
        if (GUILayout.Button("Copy", EditorStyles.toolbarButton))
        {
            script.moveToReload = script.moveTo;
            script.rotateToReload = script.rotateTo;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        script.InSpeed = EditorGUILayout.Slider("In Speed", script.InSpeed, 1, 25);
        script.OutSpeed = EditorGUILayout.Slider("Out Speed", script.OutSpeed, 1, 25);
        GUILayout.EndVertical();
        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }

    Vector3 CalculateCenter()
    {
        var renderers = script.transform.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
        foreach (var r in renderers)
        {
            if (r.GetComponent<ParticleSystem>() != null) continue;
            if (b.extents == Vector3.zero)
                b = r.bounds;

            b.Encapsulate(r.bounds);
        }
       return  b.center;
    }
}