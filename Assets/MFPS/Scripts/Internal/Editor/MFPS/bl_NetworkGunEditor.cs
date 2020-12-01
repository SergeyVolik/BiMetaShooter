using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using MFPSEditor;

[CustomEditor(typeof(bl_NetworkGun))]
public class bl_NetworkGunEditor : Editor
{

    private bl_GunManager GunManager;
    private bl_NetworkGun script;
    private bl_PlayerNetwork PSync;
    private List<string> FPWeaponsAvailable = new List<string>();
    private bl_Gun[] LocalGuns;
    private int selectLG = 0;

    /// <summary>
    /// 
    /// </summary>
    private void OnEnable()
    {
        script = (bl_NetworkGun)target;
        PSync = script.transform.root.GetComponent<bl_PlayerNetwork>();
        GunManager = script.transform.root.GetComponentInChildren<bl_GunManager>(true);
        if (GunManager != null)
        {
            LocalGuns = GunManager.transform.GetComponentsInChildren<bl_Gun>(true);
            for (int i = 0; i < LocalGuns.Length; i++)
            {
                FPWeaponsAvailable.Add(LocalGuns[i].name);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool allowSceneObjects = !EditorUtility.IsPersistent(script);

        if (script.LocalGun != null)
        {
            script.gameObject.name = bl_GameData.Instance.GetWeapon(script.LocalGun.GunID).Name;
        }

        EditorGUILayout.BeginVertical("box");
        script.LocalGun = EditorGUILayout.ObjectField("Local Weapon", script.LocalGun, typeof(bl_Gun), allowSceneObjects) as bl_Gun;
        EditorGUILayout.EndVertical();

        if (script.LocalGun != null)
        {
            if (script.LocalGun.Info.Type != GunType.Knife)
            {
                EditorGUILayout.BeginVertical("box");

                if (script.LocalGun.Info.Type == GunType.Grenade)
                {
                    script.Bullet = EditorGUILayout.ObjectField("Bullet", script.Bullet, typeof(GameObject), allowSceneObjects) as GameObject;
                }
                if (script.LocalGun.Info.Type != GunType.Grenade)
                {
                    script.MuzzleFlash = EditorGUILayout.ObjectField("MuzzleFlash", script.MuzzleFlash, typeof(ParticleSystem), allowSceneObjects) as ParticleSystem;
                }
                if (script.LocalGun.Info.Type == GunType.Grenade)
                {
                    script.DesactiveOnOffAmmo = EditorGUILayout.ObjectField("Desactive On No Ammo", script.DesactiveOnOffAmmo, typeof(GameObject), allowSceneObjects) as GameObject;
                }
                EditorGUILayout.EndVertical();
            }

            if (script.LocalGun.Info.Type != GunType.Grenade && script.LocalGun.Info.Type != GunType.Knife)
            {
                GUILayout.BeginVertical("box");
                if (script.LeftHandPosition != null)
                {
                    if (GUILayout.Button("Edit Hand Position", EditorStyles.toolbarButton))
                    {
                        OpenIKWindow(script);
                    }
                }
                else
                {
                    if (GUILayout.Button("SetUp Hand IK", EditorStyles.toolbarButton))
                    {

                        GameObject gobject = new GameObject("LeftHandPoint");
                        gobject.transform.parent = script.transform;
                        gobject.transform.position = bl_UtilityHelper.CalculateCenter(script.transform);
                        gobject.transform.localEulerAngles = Vector3.zero;
                        script.LeftHandPosition = gobject.transform;
                        OpenIKWindow(script);
                    }
                }
                if (PSync != null && !PSync.NetworkGuns.Contains(script))
                {
                    if (GUILayout.Button("Enlist TPWeapon", EditorStyles.toolbarButton))
                    {
                        PSync.NetworkGuns.Add(script);
                        EditorUtility.SetDirty(PSync);
                    }
                }
                GUILayout.EndVertical();
            }
            else
            {
                if (PSync != null && !PSync.NetworkGuns.Contains(script))
                {
                    if (GUILayout.Button("Enlist TPWeapon", EditorStyles.toolbarButton))
                    {
                        PSync.NetworkGuns.Add(script);
                        EditorUtility.SetDirty(PSync);
                    }
                }
            }
        }
        else
        {
            if (GunManager != null)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("Select the local weapon of this TPWeapon");
                GUILayout.BeginHorizontal();
                GUILayout.Label("FPWeapon:", GUILayout.Width(100));
                selectLG = EditorGUILayout.Popup(selectLG, FPWeaponsAvailable.ToArray());
                if (GUILayout.Button("Select", EditorStyles.toolbarButton, GUILayout.Width(75)))
                {
                    script.LocalGun = LocalGuns[selectLG];
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            else
            {
                if (GUILayout.Button("Open FPWeapons", EditorStyles.toolbarButton))
                {
                    bl_GunManager gm = script.transform.root.GetComponentInChildren<bl_GunManager>();
                    Selection.activeObject = gm.transform.GetChild(0).gameObject;
                    EditorGUIUtility.PingObject(gm.transform.GetChild(0).gameObject);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    void OpenIKWindow(bl_NetworkGun script)
    {
        AnimatorRunner window = (AnimatorRunner)EditorWindow.GetWindow(typeof(AnimatorRunner));
        window.Show();
        bl_PlayerNetwork pa = script.transform.root.GetComponent<bl_PlayerNetwork>();
        Animator anim = pa.m_PlayerAnimation.m_animator;
        pa.m_PlayerAnimation.EditorSelectedGun = script;
        bl_PlayerIK hm = pa.m_PlayerAnimation.GetComponentInChildren<bl_PlayerIK>(true);
        if (hm != null) { hm.enabled = true; }
        window.SetAnim(anim);
        Selection.activeObject = script.LeftHandPosition.gameObject;
    }
}