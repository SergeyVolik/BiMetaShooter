using UnityEngine;

[ExecuteInEditMode]
public class bl_PlayerIK : bl_MonoBehaviour {

	public Transform Target;
    [Header("UPPER BODY")]
    [Range(0,1)]public float Weight;
    [Range(0,1)]public float Body;
    [Range(0,1)]public float Head;
    [Range(0,1)]public float Eyes;
    [Range(0,1)]public float Clamp;
    [Range(1,20)]public float Lerp = 8;

    public Vector3 HandOffset;
    public Vector3 AimSightPosition = new Vector3(0.02f, 0.19f, 0.02f);

    [Header("FOOT IK")]
    public bool useFootPlacement = true;
    public LayerMask FootLayers;
    [Range(0.01f, 2)] public float FootDownOffset = 1.25f;
    [Range(0.1f, 1)] public float FootHeight = 0.43f;
    [Range(0.1f, 1)] public float PositionWeight = 1;
    [Range(0.1f, 1)] public float RotationWeight = 1;
    [Range(-0.5f, 0.5f)] public float TerrainOffset = 0.13f;
    [Range(-0.5f, 0.5f)] public float Radious = 0.125f;

    private Transform RightFeed;
    private Transform LeftFeed;
    private float leftWeight = 0;
    private float rightWeight = 0;
    private float leftRotationWeight = 0;
    private float rightRotationWeight = 0;

    private Animator animator;
    private Vector3 target;
    private Vector3 CachePosition;
    private Quaternion CacheRotation;

    private float RighHand = 1;
    private float LeftHand = 1;
    private float RightHandPos = 0;
    private Transform HeatTrans;
    private bl_PlayerAnimations PlayerAnimation;

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        animator = GetComponent<Animator>();
        PlayerAnimation = transform.parent.GetComponent<bl_PlayerAnimations>();
        HeatTrans = animator.GetBoneTransform(HumanBodyBones.Head);
        if (useFootPlacement)
        {
            LeftFeed = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            RightFeed = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }
    }

    void OnAnimatorIK(int layer)
    {
        if (Target == null || animator == null)
            return;

        if (layer == 0)
        {
            animator.SetLookAtWeight(Weight, Body, Head, Eyes, Clamp);
            target = Vector3.Slerp(target, Target.position, Time.deltaTime * 8);
            animator.SetLookAtPosition(target);

            if(useFootPlacement && PlayerAnimation.localVelocity.magnitude <= 0.1f)
            {
                FootPlacement();
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
            }
        }
        else if (layer == 1)//upper body layer
        {
            if (LeftHandTarget != null && !PlayerAnimation.isWeaponsBlocked && !PlayerAnimation.stopHandsIK)
            {
                HandsIK();
            }
            else
            {
                ResetWeightIK();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void HandsIK()
    {
        float weight = (inPointMode) ? 1 : 0;
        float lweight = (PlayerSync.FPState != PlayerFPState.Running) ? 1 : 0;
        RighHand = Mathf.Lerp(RighHand, lweight, Time.deltaTime * 5);
        LeftHand = Mathf.Lerp(LeftHand, weight, Time.deltaTime * 5);

        animator.SetIKRotation(AvatarIKGoal.LeftHand, CacheRotation);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, CachePosition);
        if (RighHand > 0)
        {
            Transform arm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Vector3 rhs = Target.position - arm.position;
            Quaternion lookAt = Quaternion.LookRotation(rhs);
            Vector3 v = lookAt.eulerAngles;
            v = v + HandOffset;
            animator.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.Euler(v));
        }

        float rpw = (PlayerSync.FPState == PlayerFPState.Aiming || PlayerSync.FPState == PlayerFPState.FireAiming) ? 0.5f : 0;
        RightHandPos = Mathf.Lerp(RightHandPos, rpw, Time.deltaTime * 7);
        Vector3 hf = HeatTrans.TransformPoint(AimSightPosition);
        animator.SetIKPosition(AvatarIKGoal.RightHand, hf);

        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, RighHand);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, RightHandPos);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, LeftHand);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, LeftHand);
    }

    void FootPlacement()
    {
        RaycastHit lr;
        if (Physics.SphereCast(LeftFeed.position + (Vector3.up * FootHeight), Radious, (Vector3.down * FootHeight), out lr, FootDownOffset * FootHeight, FootLayers))
        {
            Vector3 pos = lr.point;
            pos.y += TerrainOffset;
            Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, lr.normal), lr.normal);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, pos);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, rotation);
            leftWeight = PositionWeight;
            leftRotationWeight = RotationWeight;
        }
        else
        {
            leftWeight = Mathf.Lerp(leftWeight, 0, Time.deltaTime * 4);
            leftRotationWeight = Mathf.Lerp(leftRotationWeight, 0, Time.deltaTime * 4);
        }

        RaycastHit rr;
        if (Physics.SphereCast(RightFeed.position + (Vector3.up * FootHeight), Radious, (Vector3.down * FootHeight), out rr, FootDownOffset * FootHeight, FootLayers))
        {
            Vector3 pos = rr.point;
            pos.y += TerrainOffset;
            Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, rr.normal), rr.normal);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, pos);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, rotation);
            rightWeight = PositionWeight;
            rightRotationWeight = RotationWeight;
        }
        else
        {
            rightWeight = Mathf.Lerp(rightWeight, 0, Time.deltaTime * 4);
            rightRotationWeight = 0;
        }

        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftRotationWeight);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightRotationWeight);
    }

    void ResetWeightIK()
    {
        LeftHand = 0;
        RighHand = 0;
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.0f);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
    }

    private bool inPointMode
    {
        get
        {
            return (PlayerSync.FPState != PlayerFPState.Running && PlayerSync.FPState != PlayerFPState.Reloading);
        }
    }

    private Transform LeftHandTarget
    {
        get
        {
            if (Application.isPlaying)
            {
                if (PlayerSync != null && PlayerSync.CurrenGun != null)
                {
                    return PlayerSync.CurrenGun.LeftHandPosition;
                }
            }
            else
            {
                if (PlayerSync != null && PlayerSync.m_PlayerAnimation != null && PlayerSync.m_PlayerAnimation.EditorSelectedGun)
                {
                    return PlayerSync.m_PlayerAnimation.EditorSelectedGun.LeftHandPosition;
                }
            }
            return null;
        }
    }

    public override  void OnUpdate()
     {
         if (LeftHandTarget == null) return;

         CachePosition = LeftHandTarget.position;
         CacheRotation = LeftHandTarget.rotation;
     }

#if UNITY_EDITOR
    void Update()
    {
        if (LeftHandTarget == null || Application.isPlaying) return;

        CachePosition = LeftHandTarget.position;
        CacheRotation = LeftHandTarget.rotation;
    }
#endif

    private void OnDrawGizmos()
    {
        if(animator == null) { animator = GetComponent<Animator>(); }
        if(HeatTrans == null) { HeatTrans = animator.GetBoneTransform(HumanBodyBones.Head); }
        Gizmos.color = Color.yellow;
        Vector3 hf =  HeatTrans.TransformPoint(AimSightPosition);
        Gizmos.DrawLine(HeatTrans.position, hf);
        Gizmos.DrawSphere(hf, 0.03f);
        
    }

    private bl_PlayerNetwork PSync = null;
    private bl_PlayerNetwork PlayerSync
    {
        get
        {
            if (PSync == null) { PSync = transform.root.GetComponent<bl_PlayerNetwork>(); }
            return PSync;
        }
    }
}