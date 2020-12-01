//////////////////////////////////////////////////////////////////
// bl_BodyPartManager.cs
//
// This script helps us manage our remote player hitboxes
// mind just place it in the root of the remote player
// and executes the last two options of the "ContextMenu" component. 
//                       Lovatto Studio
//////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class bl_BodyPartManager : bl_PhotonHelper
{
    /// <summary>
    /// Change the tag if you use other
    /// </summary>
    public const string HitBoxTag = "BodyPart";
    [Header("Hit Boxes"), Reorderable]
    public List<BodyHitBox> HitBoxs = new List<BodyHitBox>();
    public List<Rigidbody> rigidBodys = new List<Rigidbody>();
    public bool ApplyVelocityToRagdoll = true;
    [Header("References")]
    public bl_PlayerAnimations PlayerAnimation;
    public Animator m_Animator;
    public Transform RightHand;
    public Transform PelvisBone;

    public GameObject KillCameraCache { get; set; }
    private Vector3 Velocity = Vector3.zero;

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        GetRigidBodys();
        if (rigidBodys.Count > 0)
        {
            SetKinematic();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetLocalRagdoll(Vector3 hitPos, Transform netRoot, Vector3 velocity, bool isExplosion)
    {
        gameObject.SetActive(true);
        Velocity = velocity;
        if (RightHand != null && netRoot != null)
        {
            Vector3 RootPos = netRoot.localPosition;
            netRoot.parent = RightHand;
            netRoot.localPosition = RootPos;
        }
        Ragdolled(hitPos, isExplosion, false);

        if (bl_RoomSettings.Instance.currentGameMode.GetGameModeInfo().onPlayerDie == bl_GameData.GameModesEnabled.OnPlayerDie.SpawnAfterRoundFinish)
        {
            bl_UIReferences.Instance.OnKillCam(false);
            Destroy(gameObject, 5);
            return;
        }

        StartCoroutine(RespawnCountdown());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator RespawnCountdown()
    {
        float t = bl_GameData.Instance.PlayerRespawnTime / 3;
        yield return new WaitForSeconds(t * 2);
        if (!bl_RoomMenu.Instance.isFinish)
        {
            StartCoroutine(bl_UIReferences.Instance.FinalFade(true, false));
        }
        yield return new WaitForSeconds(t);
        if (bl_GameManager.Instance.SpawnPlayer(PhotonNetwork.LocalPlayer.GetPlayerTeam()))
        {
            if (KillCameraCache != null) { Destroy(KillCameraCache); }
            bl_UIReferences.Instance.OnKillCam(false);
            Destroy(gameObject);
        }
        else
        {
            bl_EventHandler.onLocalPlayerSpawn += OnLocalSpawn;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnLocalSpawn()
    {
        bl_EventHandler.onLocalPlayerSpawn -= OnLocalSpawn;
        if (KillCameraCache != null) { Destroy(KillCameraCache); }
        bl_UIReferences.Instance.OnKillCam(false);
        Destroy(gameObject);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="b">is Kinematic?</param>
    public void SetKinematic(bool b = true)
    {
        if (rigidBodys == null || rigidBodys.Count <= 0)
            return;

        foreach (Rigidbody r in rigidBodys)
        {
            r.isKinematic = b;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void Ragdolled(Vector3 hitPos, bool isExplosion, bool autoDestroy = true)
    {
        this.transform.parent = null;
        m_Animator.enabled = false;
        foreach (Rigidbody r in rigidBodys)
        {
            r.isKinematic = false;
            r.useGravity = true;
            if (ApplyVelocityToRagdoll)
            {
                r.velocity = autoDestroy ? PlayerAnimation.velocity : Velocity;
            }
            if (isExplosion)
            {
                r.AddExplosionForce(875, hitPos, 7);
            }
        }
        Destroy(PlayerAnimation);
        if (autoDestroy)
            Destroy(gameObject, 10);
    }
    /// <summary>
    /// 
    /// </summary>
    public void IgnorePlayerCollider()
    {
        GameObject p = FindPlayerRoot(bl_GameManager.LocalPlayerViewID);
        if (p == null) return;

        Collider Player = p.GetComponent<Collider>();
        if (Player != null)
        {
            for (int i = 0; i < HitBoxs.Count; i++)
            {
                if (HitBoxs[i].collider != null)
                {
                    Physics.IgnoreCollision(HitBoxs[i].collider, Player);
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void DisableLocalRagdoll()
    {
        for (int i = 0; i < HitBoxs.Count; i++)
        {
            HitBoxs[i].collider.enabled = false;
        }
        foreach (var item in rigidBodys)
        {
            item.isKinematic = true;
        }
    }

    public BodyHitBox GetHitBox(int identifier) { return HitBoxs[identifier]; }

    [ContextMenu("Setup")]
    public void SetUpHitBoxes()
    {
        if (m_Animator == null || m_Animator.avatar == null) return;

        HitBoxs.Clear();
        List<Collider> colliders = new List<Collider>();
        colliders.AddRange(transform.GetComponentsInChildren<Collider>());

        if (colliders.Count > 0)
        {
            CreateHitBox(HumanBodyBones.LeftUpperLeg);
            CreateHitBox(HumanBodyBones.LeftLowerLeg);
            CreateHitBox(HumanBodyBones.RightUpperLeg);
            CreateHitBox(HumanBodyBones.RightLowerLeg);
            CreateHitBox(HumanBodyBones.Spine);
            CreateHitBox(HumanBodyBones.LeftUpperArm);
            CreateHitBox(HumanBodyBones.LeftLowerArm);
            CreateHitBox(HumanBodyBones.Head);
            CreateHitBox(HumanBodyBones.RightUpperArm);
            CreateHitBox(HumanBodyBones.RightLowerArm);
        }
        else
        {
            Debug.LogWarning("This player has not been ragdolled yet.");
        }
        GetRigidBodys();
        GetRequireBones();
    }

    void CreateHitBox(HumanBodyBones bone)
    {
        BodyHitBox box = new BodyHitBox();
        Collider col = m_Animator.GetBoneTransform(bone).GetComponent<Collider>();
        if (col == null) { Debug.LogWarning("The bone: " + bone.ToString() + " doesn't have a collider."); return; }

        col.gameObject.layer = LayerMask.NameToLayer("Player");
        col.gameObject.tag = HitBoxTag;
        box.Name = col.name;
        box.Bone = bone;
        box.collider = col;
        if (bone == HumanBodyBones.Head) { box.DamageMultiplier = 10; }

        HitBoxs.Add(box);
        bl_BodyPart bp = col.GetComponent<bl_BodyPart>();
        if (bp == null) { bp = col.gameObject.AddComponent<bl_BodyPart>(); }

        bp.HitBoxIdentifier = HitBoxs.Count - 1;
        bp.BodyManager = this;
        bp.HealtScript = transform.root.GetComponent<bl_PlayerHealthManager>();
    }

    void GetRigidBodys()
    {
        rigidBodys.Clear();
        rigidBodys.AddRange(transform.GetComponentsInChildren<Rigidbody>());
    }

    public void GetRequireBones()
    {
        if (m_Animator == null || m_Animator.avatar == null) return;
        RightHand = m_Animator.GetBoneTransform(HumanBodyBones.RightHand);
        PelvisBone = m_Animator.GetBoneTransform(HumanBodyBones.Hips);
    }
}