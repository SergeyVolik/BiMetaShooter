using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MFPSEditor;

public class AddPlayerTutorial : TutorialWizard
{

    //required//////////////////////////////////////////////////////
    private const string ImagesFolder = "mfps2/editor/player/";
    private NetworkImages[] ServerImages = new NetworkImages[]
    {
        new NetworkImages{Name = "img-1.jpg", Image = null},
        new NetworkImages{Name = "dDHltGGDrAA", Image = null, Type = NetworkImages.ImageType.Youtube},
        new NetworkImages{Name = "img-2.jpg", Image = null},
        new NetworkImages{Name = "img-3.jpg", Image = null},
        new NetworkImages{Name = "img-4.jpg", Image = null},
        new NetworkImages{Name = "img-5.jpg", Image = null},
        new NetworkImages{Name = "img-6.jpg", Image = null},
        new NetworkImages{Name = "img-7.jpg", Image = null},
        new NetworkImages{Name = "img-8.jpg", Image = null},
        new NetworkImages{Name = "https://www.lovattostudio.com/en/wp-content/uploads/2017/03/player-selector-product-cover-925x484.png",Type = NetworkImages.ImageType.Custom},
    };
    private Steps[] AllSteps = new Steps[] {
     new Steps { Name = "3DModel", StepsLenght = 0 },
    new Steps { Name = "Ragdolled", StepsLenght = 3 },
    new Steps { Name = "Player Prefab", StepsLenght = 6 },
    };
    //final required////////////////////////////////////////////////

    private GameObject PlayerInstantiated;
    private GameObject PlayerModel;
    private Animator PlayerAnimator;
    private Avatar PlayerModelAvatar;
    private string LogLine = "";
    private ModelImporter ModelInfo;
    Editor p1editor;
    AssetStoreAffiliate playerAssets;

    public override void OnEnable()
    {
        base.OnEnable();
        base.Initizalized(ServerImages, AllSteps, ImagesFolder);
        GUISkin gs = Resources.Load<GUISkin>("content/MFPSEditorSkin") as GUISkin;
        if (gs != null)
        {
            base.SetTextStyle(gs.customStyles[2]);
        }
        if (playerAssets == null)
        {
            playerAssets = new AssetStoreAffiliate();
            playerAssets.Initialize(this, "https://assetstore.unity.com/linkmaker/embed/list/157287/widget-medium");
            playerAssets.FixedHeight = 350;
        }
    }

    public override void WindowArea(int window)
    {
        if (window == 0)
        {
            DrawModelInfo();
        }
        else if (window == 1)
        {
            DrawRagdolled();
        }
        else if (window == 2)
        {
            DrawPlayerPrefab();
        }
    }

    void DrawModelInfo()
    {
        DrawText("This tutorial will guide you step by step to replace the Player Model of the player prefabs, what you need is:");
        DrawHorizontalColumn("Player Model", "A Humanoid <b>Rigged</b> 3D Model with the standard rigged bones or any rigged that work with the unity re-targeting animator system.");
        DrawText("The Model Import <b>Rig</b> setting has to be set as <b>Humanoid</b> in order to work with retargeting animations, for it select the player model <i>(the model not a prefab)</i> and in the inspector window you will see a toolbar, go to the Rig tab and set the <b>Animation Type</b> as Humanoid, the settings should look like this:");
        DrawImage(GetServerImage(0));
        DownArrow();
        DrawText("<b>Important:</b> your model should have a correct <b>T-Pose skeleton</b> to work correctly with the re-targeting animations, if your character model have a wrong posed skeleton the animations will look weird in the player model, in order to fix the skeleton pose you can follow this video tutorial:");
        DrawYoutubeCover("Adjusting Avatar for correct animation retargeting", GetServerImage(1), "https://www.youtube.com/watch?v=dDHltGGDrAA");
        DownArrow();
        DrawText("Also if don't have a player model and you are looking for one, I'll leave you an Asset Store collection of Soldier models here:\n");
        playerAssets.OnGUI();
    }

    void DrawRagdolled()
    {
        if (subStep == 0)
        {
            HideNextButton = true;
            DrawText("All right, with the model ready it's time to start setting it up. The first thing that you need to do is make a ragdoll of your new player model." +
                " Normally you make a ragdoll manually with a GameObject -> " +
                "3D Object -> Ragdoll -> and then assign every player bone in the wizard window manually, but this tool will make this automatically, you simple need to drag the player model.");
            DownArrow();
            DrawText("Drag here your player model from the <b>Project View</b>");
            PlayerModel = EditorGUILayout.ObjectField("Player Model", PlayerModel, typeof(GameObject), false) as GameObject;
            GUI.enabled = PlayerModel != null;
            if (DrawButton("Continue"))
            {
                AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(PlayerModel));
                if (importer != null)
                {
                    ModelInfo = importer as ModelImporter;
                    if (ModelInfo != null)
                    {
                        if (ModelInfo.animationType != ModelImporterAnimationType.Human)
                        {
                            ModelInfo.animationType = ModelImporterAnimationType.Human;
                            EditorUtility.SetDirty(ModelInfo);
                            ModelInfo.SaveAndReimport();
                        }
                        if (ModelInfo.animationType == ModelImporterAnimationType.Human)
                        {
                            PlayerInstantiated = PrefabUtility.InstantiatePrefab(PlayerModel) as GameObject;
                            UnPackPrefab(PlayerInstantiated);
                            PlayerAnimator = PlayerInstantiated.GetComponent<Animator>();
                            PlayerModelAvatar = PlayerAnimator.avatar;
                            var view = (SceneView)SceneView.sceneViews[0];
                            view.camera.transform.position = PlayerInstantiated.transform.position + ((PlayerInstantiated.transform.forward * 10) + (new Vector3(0, 0.5f, 0)));
                            view.LookAt(PlayerInstantiated.transform.position);
                            EditorGUIUtility.PingObject(PlayerInstantiated);
                            Selection.activeTransform = PlayerInstantiated.transform;
                            subStep++;
                        }
                        else
                        {
                            LogLine = "Your models is not setup as a <b>Humanoid</b> rig, setup it:";
                        }
                    }
                    else
                    {
                        LogLine = "Please Select Imported Model in Project View not prefab or other.";
                    }
                }
                else { LogLine = "Please Select Imported Model in Project View not prefab"; }
            }
            GUI.enabled = true;
            if (!string.IsNullOrEmpty(LogLine))
            {
                GUILayout.Label(LogLine);
                if (LogLine.Contains("Humanoid"))
                {
                    DrawImage(GetServerImage(0));
                }
            }
        }
        else if (subStep == 1)
        {
            HideNextButton = false;
            GUI.enabled = false;
            GUILayout.BeginVertical("box");
            PlayerInstantiated = EditorGUILayout.ObjectField("Player Prefab", PlayerInstantiated, typeof(GameObject), false) as GameObject;
            PlayerModelAvatar = EditorGUILayout.ObjectField("Avatar", PlayerModelAvatar, typeof(Avatar), true) as Avatar;
            SkinnedMeshRenderer smr = null;
            if (PlayerInstantiated != null)
            {
                smr = PlayerInstantiated.GetComponentInChildren<SkinnedMeshRenderer>();
                GUILayout.Label(string.Format("Model Height: <b>{0}</b> | Expected Height: <b>2</b>", smr.bounds.size.y));
                if (ModelInfo != null)
                    GUILayout.Label(string.Format("Model Rig: {0}", ModelInfo.animationType.ToString()));
                GUI.enabled = true;
                if (smr.bounds.size.y < 1.9f)
                {
                    GUILayout.Label("<color=yellow>the size of the model is very small</color>, you want resize it automatically?", EditorStyles.label);
                    if (DrawButton("Yes, Resize automatically"))
                    {
                        Vector3 v = PlayerInstantiated.transform.localScale;
                        float dif = 2f / smr.bounds.size.y;
                        v = v * dif;
                        PlayerInstantiated.transform.localScale = v;
                    }
                }
                else if (smr.bounds.size.y > 2.25f)
                {
                    GUILayout.Label("<color=yellow>the size of the model is very large</color>, you want resize it automatically?", EditorStyles.label);
                    if (DrawButton("Yes, Resize automatically"))
                    {
                        Vector3 v = PlayerInstantiated.transform.localScale;
                        float dif = smr.bounds.size.y / 2;
                        v = v / dif;
                        PlayerInstantiated.transform.localScale = v;
                    }
                }
            }
            GUILayout.EndVertical();
            GUI.enabled = true;
            if (PlayerModelAvatar != null && PlayerAnimator != null)
            {
                DownArrow();
                DrawText("All ready to create the ragdoll, Click in the button below to build it.");
                if (DrawButton("Build Ragdoll"))
                {
                    if (MFPSEditor.AutoRagdoller.Build(PlayerAnimator))
                    {
                        var view = (SceneView)SceneView.sceneViews[0];
                        view.ShowNotification(new GUIContent("Ragdoll Created!"));
                        NextStep();
                    }
                }
            }
            else
            {
                GUILayout.Label("<color=yellow>Hmm... something is happening here, can't get the model avatar.</color>", EditorStyles.label);
            }
        }
        else if (subStep == 2)
        {
            DrawText("Right now your player model <i>(in the scene)</i> should look similar to this:");
            DrawImage(GetServerImage(2));
            DownArrow();
            DrawText("Now, these <b>Box</b> and <b>Capsule</b> Colliders are the player HitBoxes <i>(the colliders that detect when a bullet hit the player)</i>, in some models these colliders may not be place/oriented in the right axes causing a problem which will be that some parts of the player will not be hitteable in game.\n\nSo make sure all the colliders cover the player model by modifying the collider values if is necessary.\n\nIf all seems good, you are ready to go to the next step.");

        }
    }

    void DrawPlayerInstanceButton(GameObject player)
    {
        if (player == null) return;

        if (GUILayout.Button(player.name, GUILayout.Width(150)))
        {
            PlayerModel = PlayerInstantiated;
            PlayerInstantiated = PrefabUtility.InstantiatePrefab(player) as GameObject;
            UnPackPrefab(PlayerInstantiated);
            Selection.activeObject = PlayerInstantiated;
            EditorGUIUtility.PingObject(PlayerInstantiated);
            NextStep();
        }
        GUILayout.Space(5);
    }

    void DrawPlayerPrefab()
    {
        if (subStep == 0)
        {
            DrawText("Okay, now that we have the player model ragdolled, we can add it to a player prefab, for it we would open one of the existing player prefabs.\n\nBelow you will have a list of all your available player prefabs, click on the one that you want to use as reference to replace their model.");
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginVertical();
                {
                    DrawPlayerInstanceButton(bl_GameData.Instance.Player1.gameObject);
                    DrawPlayerInstanceButton(bl_GameData.Instance.Player2.gameObject);
#if PSELECTOR
                    foreach (var p in MFPS.PlayerSelector.bl_PlayerSelectorData.Instance.AllPlayers)
                    {
                        if (p == null || p.Prefab == null) continue;
                        DrawPlayerInstanceButton(p.Prefab);
                    }
#endif
                }
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }
        else if (subStep == 1)
        {
            GUI.enabled = (PlayerInstantiated == null || PlayerModel == null);
            PlayerInstantiated = EditorGUILayout.ObjectField("Player Prefab", PlayerInstantiated, typeof(GameObject), true) as GameObject;
            if (PlayerModel == null)
            {
                GUILayout.Label("<color=yellow>Select the ragdolled player model (from hierarchy)</color>");
            }
            PlayerModel = EditorGUILayout.ObjectField("Player Model", PlayerModel, typeof(GameObject), true) as GameObject;
            GUI.enabled = true;
            if (PlayerModel != null && PlayerInstantiated != null)
            {
                DownArrow();
                DrawText("All good, click in the button below to setup the model in the player prefab.");
                GUILayout.Space(5);
                if (DrawButton("SETUP MODEL"))
                {
                    SetUpModelInPrefab();
                    NextStep();
                }
            }
        }
        else if (subStep == 2)
        {
            string pin = PlayerInstantiated == null ? "MPlayer" : PlayerInstantiated.name;
            DrawText($"If all works as expected, you should see <b>just</b> a log in the console: <b><i>Player model integrated</i></b>.\n\nIf it's so, you also should see inside the player prefab instanced in the scene hierarchy: <b>{pin} -> RemotePlayer -></b> boths models the old one <i>(marked with <b>(DELETE THIS)</b> at the end of the name) </i> and the new one.\n\n<i>The old one is not deleted automatically just in case you see a noticeable difference in the position, scale or rotation between both models, if is this the case you have to manually positione the new model as the old model is, otherwise you can simple delete the old model.</i>");
            DrawImage(GetServerImage(3));
            DownArrow();
            DrawText("Ok, now there is one step that you need to do manually.\n\nThe TPWeapons <i>(The third person weapons)</i> has been moved from the old player model to the new one, but because the models almost always have a different local axis orientation, the TPWeapon will be oriented wrong as well, so you need repositioned/re-oriented them, but don't worry, you don't have to re-oriented every single weapon, just the parent transform of those, the <b>RemoteWeapons</b> object.\n\nSo, this is a example of how the weapons may look after replace the player model:");

            DrawImage(GetServerImage(4));
            DownArrow();
            DrawText("In order to repositioned/re-oriented them, select the <b>RemoteWeapons</b> object which is inside of the player prefab <i>(inside of the right hand of the player model)</i>, or click in the button bellow to try to ping it automatically on the hierarchy window.\n");
            if (DrawButton("Ping RemoteWeapons"))
            {
                if (PlayerInstantiated != null)
                {
                    Transform t = PlayerInstantiated.GetComponent<bl_PlayerNetwork>().NetworkGuns[0].transform.parent;
                    Selection.activeTransform = t;
                    EditorGUIUtility.PingObject(t);
                    if (t != null)
                        NextStep();
                }
            }

        }
        else if (subStep == 3)
        {
            DrawText("Now the RemoteWeapons object should be selected and framed in the hierarchy window, to preview the position of the weapons if you don't have a weapon active/showing select one from inside of the object <i>(RemoteWeapons)</i> and make it visible by enabling the game object, or if you have more than one enabled, disable all of them is just leave one showing to make things more clear.\n\nThen select the <b>RemoteWeapons</b> <i>(not a weapon child)</i> parent again and rotate/move to positioned it simulating that the player is holding it in the right hand, something like this:");
            DrawImage(GetServerImage(5));
            DownArrow();
            DrawText("When you are done, just make sure that if you haven't delete the old model yet, you should now:");
            DrawImage(GetServerImage(6));
        }
        else if (subStep == 4)
        {
            DrawText("Now you need to copy this prefab inside the <b>Resources</b> folder, by dragging it to: MFPS -> Resources. Rename it if you wish.");
            DrawImage(GetServerImage(7));
            DownArrow();
            DrawText("Now you need assign this new player prefab for use by one of the Teams (team 1 or team 2). To do this, go to GameData (in Resources folder too) -> Players section, and in the corresponding field (Team1 or Team2), " +
                "drag the new player prefab.");
            DrawImage(GetServerImage(8));
        }
        else if (subStep == 5)
        {
            DrawText("That's it! You have your new player model integrated!.\n\n Please note: Some models are not fully compatible with the default player animations retargeting, causing " +
                "some of your animations to look awkward. Unfortunately, there is nothing we can do to fix it automatically. To fix it you have two options: Edit the animation or replace with another that you know" +
                " works in your model, check the documentation for more info of how replace animations.");
            GUILayout.Space(7);
            DrawText("Do you want to have multiple player options so a player has more players to choose from?, Check out <b>Player Selector</b> Addon, with which you can add how many player models as you want: ");
            GUILayout.Space(5);
            if (DrawButton("PLAYER SELECTOR"))
            {
                Application.OpenURL("https://www.lovattostudio.com/en/shop/addons/player-selector/");
            }
            DrawImage(GetServerImage(9));
        }
    }

    void UnPackPrefab(GameObject prefab)
    {
#if UNITY_2018_3_OR_NEWER
        if (PrefabUtility.GetPrefabInstanceStatus(prefab) == PrefabInstanceStatus.Connected)
        {
            PrefabUtility.UnpackPrefabInstance(prefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
#endif
    }

    void SetUpModelInPrefab()
    {
        UnPackPrefab(PlayerModel);
        UnPackPrefab(PlayerInstantiated);
        GameObject TempPlayerPrefab = PlayerInstantiated;
        GameObject TempPlayerModel = PlayerModel;

        //change name of prefabs to identify
        PlayerInstantiated.gameObject.name += " [NEW]";
        PlayerModel.name += " [NEW]";

        // get the current player model
        GameObject RemoteChildPlayer = TempPlayerPrefab.GetComponentInChildren<bl_PlayerAnimations>().gameObject;
        GameObject ActualModel = TempPlayerPrefab.GetComponentInChildren<bl_PlayerIK>().gameObject;
        Transform NetGunns = TempPlayerPrefab.GetComponent<bl_PlayerNetwork>().NetworkGuns[0].transform.parent;

        //set the new model to the same position as the current model
        TempPlayerModel.transform.parent = RemoteChildPlayer.transform;
        TempPlayerModel.transform.localPosition = ActualModel.transform.localPosition;
        TempPlayerModel.transform.localRotation = ActualModel.transform.localRotation;

        //add and copy components of actual player model
        bl_PlayerIK ahl = ActualModel.GetComponent<bl_PlayerIK>();
        if (TempPlayerModel.GetComponent<Animator>() == null) { TempPlayerModel.AddComponent<Animator>(); }
        Animator NewAnimator = TempPlayerModel.GetComponent<Animator>();

        if (ahl != null)
        {
            bl_PlayerIK newht = TempPlayerModel.AddComponent<bl_PlayerIK>();
            newht.Target = ahl.Target;
            newht.Body = ahl.Body;
            newht.Weight = ahl.Weight;
            newht.Head = ahl.Head;
            newht.Lerp = ahl.Lerp;
            newht.Eyes = ahl.Eyes;
            newht.Clamp = ahl.Clamp;
            newht.useFootPlacement = ahl.useFootPlacement;
            newht.FootDownOffset = ahl.FootDownOffset;
            newht.FootHeight = ahl.FootHeight;
            newht.FootLayers = ahl.FootLayers;
            newht.PositionWeight = ahl.PositionWeight;
            newht.RotationWeight = ahl.RotationWeight;
            newht.AimSightPosition = ahl.AimSightPosition;
            newht.HandOffset = ahl.HandOffset;
            newht.TerrainOffset = ahl.TerrainOffset;
            newht.Radious = ahl.Radious;

            Animator oldAnimator = ActualModel.GetComponent<Animator>();
            NewAnimator.runtimeAnimatorController = oldAnimator.runtimeAnimatorController;
            NewAnimator.applyRootMotion = oldAnimator.hasRootMotion;
            if (NewAnimator.avatar == null)
            {
                NewAnimator.avatar = oldAnimator.avatar;
                Debug.LogWarning("Your new model doesn't have a avatar, that can cause some problems with the animations, be sure to add it manually.");
            }
        }
        Transform RightHand = NewAnimator.GetBoneTransform(HumanBodyBones.RightHand);

        if (RightHand == null)
        {
            Debug.Log("Can't get right hand from new model, are u sure that is an humanoid rig?");
            return;
        }

        bl_PlayerAnimations pa = TempPlayerPrefab.transform.GetComponentInChildren<bl_PlayerAnimations>();
        bl_BodyPartManager bdm = TempPlayerPrefab.transform.GetComponentInChildren<bl_BodyPartManager>();
        pa.m_animator = NewAnimator;
        bdm.m_Animator = NewAnimator;
        bdm.SetUpHitBoxes();

        if(pa.m_animator != null)
        {
            for (int i = 0; i < 5; i++)
            {
                pa.m_animator.Update(0);
            }
        }

        if (RightHand != null)
        {
            NetGunns.parent = RightHand;
            NetGunns.localPosition = Vector3.zero;
            NetGunns.rotation = RightHand.rotation;
        }
        else
        {
            Debug.Log("Can't find right hand");
        }

        ActualModel.name += " (DELETE THIS)";
        ActualModel.SetActive(false);

        var view = (SceneView)SceneView.sceneViews[0];
        view.LookAt(ActualModel.transform.position);
        view.ShowNotification(new GUIContent("Player Setup"));
        Debug.Log("Player model integrated.");
    }

    private Rigidbody[] GetRigidBodys(Transform t)
    {
        Rigidbody[] R = t.GetComponentsInChildren<Rigidbody>();
        return R;
    }

    private Collider[] GetCollider(Transform t)
    {
        Collider[] R = t.GetComponentsInChildren<Collider>();
        return R;
    }

    [MenuItem("MFPS/Tutorials/Add Player", false, 500)]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AddPlayerTutorial));
    }
}