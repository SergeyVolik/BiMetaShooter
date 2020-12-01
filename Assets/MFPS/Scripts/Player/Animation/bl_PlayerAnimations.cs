using UnityEngine;

public class bl_PlayerAnimations : bl_MonoBehaviour
{
    [HideInInspector]
    public bool m_Update = true;
    [Header("Animations")]
    public Animator m_animator;
    public bl_FootStepsLibrary FootStepLibrary;

    [HideInInspector]
    public bool grounded = true;
    public Vector3 velocity { get; set; } = Vector3.zero;
    [HideInInspector]
    public Vector3 localVelocity = Vector3.zero;
    [HideInInspector]
    public float movementSpeed;
    [HideInInspector]
    public float lastYRotation;
    public PlayerFPState FPState { get; set; } = PlayerFPState.Idle;
    public PlayerState BodyState { get; set; } = PlayerState.Idle;
    private bool HitType = false;
    private GunType cacheWeaponType = GunType.Machinegun;
    private float vertical;
    private float horizontal;
    private Transform PlayerRoot;
    private float turnSpeed;
    private bool parent = false;
    private float TurnLerp = 0;
    [HideInInspector] public bl_NetworkGun EditorSelectedGun = null;
    //foot steps
    private AudioSource StepSource;
    private float m_StepCycle;
    private float m_NextStep;
    private float m_StepInterval;
    private float m_RunStepInterval;
    private bool useFootSteps = false;
    public bool isWeaponsBlocked { get; set; }
    public bool stopHandsIK { get; set; }
    private RaycastHit footRay;
    private float lerpValueSpeed = 12;
    private float reloadSpeed = 1;
    public bl_NetworkGun CurrentNetworkGun { get; set; }
    private PlayerState lastBodyState = PlayerState.Idle;

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        PlayerRoot = transform.root;

        useFootSteps = bl_GameData.Instance.CalculateNetworkFootSteps;
        if (useFootSteps)
        {
            bl_FirstPersonController fpc = PlayerRoot.GetComponent<bl_FirstPersonController>();
            m_RunStepInterval = fpc.m_RunStepInterval;
            m_StepInterval = fpc.m_StepInterval;
            StepSource = gameObject.AddComponent<AudioSource>();
            StepSource.spatialBlend = 1;
            StepSource.maxDistance = 15;
            StepSource.playOnAwake = false;
        }  
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        bl_AnimatorReloadEvent.OnTPReload += OnTPReload;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        bl_AnimatorReloadEvent.OnTPReload -= OnTPReload;
    }

    /// <summary>
    /// 
    /// </summary>
    void OnTPReload(bool enter, Animator theAnimator, AnimatorStateInfo stateInfo)
    {
        if (theAnimator != m_animator || CurrentNetworkGun == null || CurrentNetworkGun.LocalGun == null) return;

        reloadSpeed = enter ? (stateInfo.length / CurrentNetworkGun.Info.ReloadTime) - 0.1f : 1;
        m_animator.SetFloat("ReloadSpeed", reloadSpeed);
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (!m_Update)
            return;

        ControllerInfo();
        Animate();
        UpperControll();
        if (useFootSteps)
        {
            ProgressStepCycle(movementSpeed);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    void ControllerInfo()
    {
        localVelocity = PlayerRoot.InverseTransformDirection(velocity);
        localVelocity.y = 0;

        vertical = Mathf.Lerp(vertical, localVelocity.z, Time.deltaTime * lerpValueSpeed);
        horizontal = Mathf.Lerp(horizontal, localVelocity.x, Time.deltaTime * lerpValueSpeed);

        parent = !parent;
        if (parent)
        {
            lastYRotation = PlayerRoot.rotation.eulerAngles.y;
        }
        turnSpeed = Mathf.DeltaAngle(lastYRotation, PlayerRoot.rotation.eulerAngles.y);
        TurnLerp = Mathf.Lerp(TurnLerp, turnSpeed, lerpValueSpeed * Time.deltaTime);
        movementSpeed = velocity.sqrMagnitude;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    private float HorizontalAngle(Vector3 direction)
    {
        return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// 
    /// </summary>
    void Animate()
    {
        if (m_animator == null)
            return;

        if(BodyState != lastBodyState)
        {
            if (lastBodyState == PlayerState.Sliding && BodyState != PlayerState.Sliding)
            {
                m_animator.CrossFade("Move", 0.2f, 0);
            }
            if (BodyState == PlayerState.Sliding)
            {
                m_animator.Play("Slide", 0, 0);
            }
            lastBodyState = BodyState;
        }
        m_animator.SetInteger("BodyState", (int)BodyState);
        m_animator.SetFloat("Vertical", vertical);
        m_animator.SetFloat("Horizontal", horizontal);
        m_animator.SetFloat("Speed", movementSpeed);
        m_animator.SetFloat("Turn", TurnLerp);
        m_animator.SetBool("isGround", grounded);
    }

    /// <summary>
    /// 
    /// </summary>
    void UpperControll()
    {
        int _fpState = (int)FPState;
        if(_fpState == 9) { _fpState = 1; }
        m_animator.SetInteger("UpperState", _fpState);
    }

    public void OnWeaponBlock(bool isBlock)
    {
        isWeaponsBlocked = isBlock;
        int id = isBlock ? -1 : (int)cacheWeaponType;
        m_animator.SetInteger("GunType", id);
    }

    public void OnGetHit()
    {
        int r = Random.Range(0, 2);
        string hit = (r == 1) ? "Right Hit" : "Left Hit";
        m_animator.Play(hit, 2, 0);
    }

    #region FootSteps
    /// <summary>
    /// 
    /// </summary>
    private void ProgressStepCycle(float speed)
    {
        if (velocity.sqrMagnitude > 1)
        {
            m_StepCycle += (velocity.magnitude + (speed * ((BodyState == PlayerState.Walking) ? 0.33f : 0.38f))) * Time.deltaTime;
        }

        if (!(m_StepCycle > m_NextStep))
        {
            return;
        }

        if (BodyState == PlayerState.Running)
        {
            m_NextStep = m_StepCycle + m_RunStepInterval;
        }
        else
        {
            m_NextStep = m_StepCycle + m_StepInterval;
        }

        PlayFootStepAudio();
    }

    /// <summary>
    /// 
    /// </summary>
    private void PlayFootStepAudio()
    {
        bool isClimbing = (BodyState == PlayerState.Climbing);
        if ((!grounded && !isClimbing) || BodyState == PlayerState.Sliding)
        {
            return;
        }
        if (!isClimbing)
        {
            string _tag = "none";
            int n = 0;
            if (Physics.Raycast(transform.position, -Vector3.up, out footRay, 10))
            {
                _tag = footRay.transform.tag;
            }

            switch (_tag)
            {
                case "Water":
                    n = Random.Range(1, FootStepLibrary.WatertepSounds.Length);
                    StepSource.clip = FootStepLibrary.WatertepSounds[n];
                    StepSource.PlayOneShot(StepSource.clip);
                    // move picked sound to index 0 so it's not picked next time
                    FootStepLibrary.WatertepSounds[n] = FootStepLibrary.WatertepSounds[0];
                    FootStepLibrary.WatertepSounds[0] = StepSource.clip;
                    break;
                case "Metal":
                    n = Random.Range(1, FootStepLibrary.MetalStepSounds.Length);
                    StepSource.clip = FootStepLibrary.MetalStepSounds[n];
                    StepSource.PlayOneShot(StepSource.clip);
                    // move picked sound to index 0 so it's not picked next time
                    FootStepLibrary.MetalStepSounds[n] = FootStepLibrary.MetalStepSounds[0];
                    FootStepLibrary.MetalStepSounds[0] = StepSource.clip;
                    break;
                default:
                    n = Random.Range(1, FootStepLibrary.m_FootstepSounds.Length);
                    StepSource.clip = FootStepLibrary.m_FootstepSounds[n];
                    StepSource.PlayOneShot(StepSource.clip);
                    // move picked sound to index 0 so it's not picked next time
                    FootStepLibrary.m_FootstepSounds[n] = FootStepLibrary.m_FootstepSounds[0];
                    FootStepLibrary.m_FootstepSounds[0] = StepSource.clip;
                    break;
            }
        }
        else
        {
            int n = Random.Range(1, FootStepLibrary.MetalStepSounds.Length);
            StepSource.clip = FootStepLibrary.MetalStepSounds[n];
            StepSource.PlayOneShot(StepSource.clip);
            // move picked sound to index 0 so it's not picked next time
            FootStepLibrary.MetalStepSounds[n] = FootStepLibrary.MetalStepSounds[0];
            FootStepLibrary.MetalStepSounds[0] = StepSource.clip;
        }
    }
    #endregion

    public void PlayFireAnimation(GunType typ)
    {
        switch (typ)
        {
            case GunType.Knife:
                m_animator.Play("FireKnife", 1, 0);
                break;
            case GunType.Machinegun:
                m_animator.Play("RifleFire", 1, 0);
                break;
            case GunType.Pistol:
                m_animator.Play("PistolFire", 1, 0);
                break;
            case GunType.Launcher:
                m_animator.Play("LauncherFire", 1, 0);
                break;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void HitPlayer()
    {
        if (m_animator != null)
        {
            HitType = !HitType;
            int ht = (HitType) ? 1 : 0;
            m_animator.SetInteger("HitType", ht);
            m_animator.SetTrigger("Hit");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetNetworkWeapon(GunType weaponType, bl_NetworkGun networkGun)
    {
        cacheWeaponType = weaponType;
        CurrentNetworkGun = networkGun;
        m_animator?.SetInteger("GunType", (int)weaponType);
        if (CurrentNetworkGun != null && CurrentNetworkGun.LocalGun != null)
        {
           // reloadSpeed = 
        }
        else { reloadSpeed = 1; }
        stopHandsIK = true;
        CancelInvoke(nameof(ResetHandsIK));
        Invoke(nameof(ResetHandsIK), 0.3f);
    }

    void ResetHandsIK() { stopHandsIK = false; }

    public GunType GetCurretWeaponType() { return cacheWeaponType; }
}