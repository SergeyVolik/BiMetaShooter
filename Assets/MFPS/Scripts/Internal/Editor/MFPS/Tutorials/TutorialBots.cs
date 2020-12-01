using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MFPSEditor;

public class TutorialBots : TutorialWizard
{
    //required//////////////////////////////////////////////////////
    private const string ImagesFolder = "mfps2/editor/bots/";
    private NetworkImages[] ServerImages = new NetworkImages[]
    {
        new NetworkImages{Name = "img-1.jpg", Image = null},
        new NetworkImages{Name = "img-2.jpg", Image = null},
        new NetworkImages{Name = "img-3.jpg", Image = null},
        new NetworkImages{Name = "img-4.jpg", Image = null},
    };
    private Steps[] AllSteps = new Steps[] {
     new Steps { Name = "Replace Model", StepsLenght = 3 },
    };
    //final required////////////////////////////////////////////////

    public override void OnEnable()
    {
        base.OnEnable();
        base.Initizalized(ServerImages, AllSteps, ImagesFolder);
        GUISkin gs = Resources.Load<GUISkin>("content/MFPSEditorSkin") as GUISkin;
        if (gs != null)
        {
            base.SetTextStyle(gs.customStyles[2]);
        }
    }

    public override void WindowArea(int window)
    {
       if(window == 0)
        {
            DrawModel();
        }
    }

    GameObject ModelPrefab = null;
    void DrawModel()
    {
        if (subStep == 0)
        {
            DrawText("In order to replace the human model in one of the bots prefabs, you need a Humanoid rigged model.\n \n" +
                "your model has to be set up in <b>Humanoid</b> rigged in the model import settings:");
            Space(2);
            DrawImage(GetServerImage(0));
            DownArrow();
            DrawText("Then, drag the player model in the empty field bellow and click on <b>Create</b> button");
            Space(2);
            GUILayout.BeginVertical("box");
            ModelPrefab = EditorGUILayout.ObjectField("Human Model", ModelPrefab, typeof(GameObject), true) as GameObject;
            if (ModelPrefab != null)
            {
                Space(4);
                if (DrawButton("Create"))
                {
                    ReplaceBotModel();
                    NextStep();
                }
            }
            GUILayout.EndVertical();
        }else if(subStep == 1)
        {
            DrawText("Ok, now if all work correctly you should see a prefab in the scene hierarchy called <b>AISoldier [NEW]</b> with your human model integrated, that's the bot prefab," +
                "your model has been integrated and setup automatically, even though there is a fix that you have to do manually, the weapons model has been move to the new model right hand transform " +
                "but the position of these could be wrong, so you have to positioned it right.");
            DrawImage(GetServerImage(1));
            DrawText("for it you have to select the weapon parent transform, click in the button bellow to select it automatically.");
            Space(2);
            if(DrawButton("Select bot weapons parent"))
            {
                bl_AIShooterWeapon asw = FindObjectOfType<bl_AIShooterWeapon>();
                Transform wr = asw.m_AIWeapons[0].WeaponObject.transform.parent;
                Selection.activeTransform = wr;
                EditorGUIUtility.PingObject(wr);
            }
            DownArrow();
            DrawText("Now positioned the weapons (moving the selected transform) to simulate that the human models is holding it:");
            DrawImage(GetServerImage(2));
        }else if(subStep == 2)
        {
            DrawText("Good, all is ready, now you have to create a prefab of this or replace one of the current bots prefabs," +
                "for it drag the <b>AISoldier [NEW]</b> from hierarchy to a <b>Resources</b> folder, for default you can drag it to <i>MFPS -> Resources</i>, in this folder you can create a prefab" +
                " or replace one of the default bots prefabs (AISoldier or AISoldier2), in case you create a new prefab you also have to assign this prefab in GameData -> BotTeam1 or BotTeam2.");
            DrawImage(GetServerImage(3));
            DrawText("That's :)");
        }
    }

    void ReplaceBotModel()
    {
        if (ModelPrefab == null) return;
        GameObject model = ModelPrefab;
        if(PrefabUtility.IsPartOfAnyPrefab(ModelPrefab))
        {
            model = PrefabUtility.InstantiatePrefab(ModelPrefab) as GameObject;
#if UNITY_2018_3_OR_NEWER
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
#endif
        }
        model.name += " [NEW]";
        GameObject botPrefab = PrefabUtility.InstantiatePrefab(bl_GameData.Instance.BotTeam1.gameObject) as GameObject;
#if UNITY_2018_3_OR_NEWER
        PrefabUtility.UnpackPrefabInstance(botPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
#endif
        botPrefab.name = "AISoldier [NEW]";
        bl_AIAnimation oldModel = botPrefab.GetComponentInChildren<bl_AIAnimation>();
        oldModel.name += " [OLD]";
        Animator modelAnimator = model.GetComponent<Animator>();
        modelAnimator.applyRootMotion = false;
        modelAnimator.runtimeAnimatorController = oldModel.GetComponent<Animator>().runtimeAnimatorController;
        if (!AutoRagdoller.Build(modelAnimator))
        {
            Debug.LogError("Could not build a ragdoll for this model");
            return;
        }
        botPrefab.GetComponent<bl_AIShooterAgent>().AimTarget = modelAnimator.GetBoneTransform(HumanBodyBones.Spine);
        model.transform.parent = oldModel.transform.parent;
        model.transform.localPosition = oldModel.transform.localPosition;
        model.transform.localRotation = oldModel.transform.localRotation;
        bl_AIAnimation aia = model.AddComponent<bl_AIAnimation>();
        aia.mRigidBody.AddRange(model.transform.GetComponentsInChildren<Rigidbody>());
        Collider[] allColliders = model.transform.GetComponentsInChildren<Collider>();
        for (int i = 0; i < allColliders.Length; i++)
        {
            allColliders[i].gameObject.layer = LayerMask.NameToLayer("Player");
            allColliders[i].gameObject.tag = "AI";
            bl_AIHitBox hbox = allColliders[i].gameObject.AddComponent<bl_AIHitBox>();
            hbox.m_Collider = allColliders[i];
            hbox.AI = botPrefab.GetComponent<bl_AIShooterHealth>();
            hbox.isHead = allColliders[i].name.ToLower().Contains("head");
            aia.HitBoxes.Add(hbox);
        }
        Transform weaponRoot = botPrefab.GetComponent<bl_AIShooterWeapon>().m_AIWeapons[0].WeaponObject.transform.parent;
        Vector3 wrp = weaponRoot.localPosition;
        Quaternion wrr = weaponRoot.localRotation;
        weaponRoot.parent = modelAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        weaponRoot.localRotation = wrr;
        weaponRoot.localPosition = wrp;
        DestroyImmediate(oldModel.gameObject);

        var view = (SceneView)SceneView.sceneViews[0];
        view.LookAt(botPrefab.transform.position);
        EditorGUIUtility.PingObject(botPrefab);
        Selection.activeTransform = botPrefab.transform;
    }

    [MenuItem("MFPS/Tutorials/ Change Bots", false, 501)]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(TutorialBots));
    }
}