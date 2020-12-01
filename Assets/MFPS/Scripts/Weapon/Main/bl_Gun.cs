using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.Serialization;

[RequireComponent(typeof(AudioSource))]
public class bl_Gun : bl_GunBase
{
    public Component WeaponBinding;
    public IWeapon weaponLogic { get { if (WeaponBinding != null) { return WeaponBinding as IWeapon; } else { return null; } } }

    public float CrossHairScale = 8;
    // basic weapon variables all guns have in common
    public bool SoundReloadByAnim = false;
    public AudioClip TakeSound;
    public AudioClip FireSound;
    public AudioClip DryFireSound;
    public AudioClip ReloadSound;
    public AudioClip ReloadSound2 = null;
    public AudioClip ReloadSound3 = null;
    public AudioSource DelaySource = null;
    // Objects, effects and tracers
    public BulletInstanceMethod bulletInstanceMethod = BulletInstanceMethod.Pooled;
    public string BulletName = "bullet";
    public GameObject bulletPrefab;
    public GameObject grenade = null;       // the grenade style round... this can also be used for arrows or similar rounds
    public ParticleSystem muzzleFlash = null;     // the muzzle flash for this weapon
    public Transform muzzlePoint = null;    // the muzzle point of this weapon
    public ParticleSystem shell = null;          // the weapons empty shell particle
    public GameObject impactEffect = null;  // impact effect, used for raycast bullet types 
    public Vector3 AimPosition; //position of gun when is aimed
    private Vector3 DefaultPos;
    private Vector3 CurrentPos;
    private bool CanAim;
    public bool useSmooth = true;
    public float AimSmooth;
    public ShakerPresent shakerPresent;
    [Range(0, 179), FormerlySerializedAs("AimFog")]
    public float aimZoom = 50;
    private float defaultZoom;
    private float currentZoom;
    private float DeafultSmoothSway_;

    public bool CanAuto = true;
    public bool CanSemi = true;
    public bool CanSingle = true;

    //Shotgun Specific Vars
    public int pelletsPerShot = 10;         // number of pellets per round fired for the shotgun
    public float delayForSecondFireSound = 0.45f;

    //Burst Specific Vars
    public int roundsPerBurst = 3;          // number of rounds per burst fire
    public float lagBetweenShots = 0.5f;    // time between each shot in a burst
    private bool isBursting = false;
    //Launcher Specific Vars
    public List<GameObject> OnAmmoLauncher = new List<GameObject>();
    public bool ThrowByAnimation = false;

    public int impactForce = 50;            // how much force applied to a rigid body
    public float bulletSpeed = 200.0f;      // how fast are your bullets
    public bool AutoReload = true;
    public bool m_AllowQuickFire = true;
    public ReloadPer reloadPer = ReloadPer.Bullet;
    public int bulletsPerClip = 50;         // number of bullets in each clip
    public int numberOfClips = 5;           // number of clips you start with
    public int maxNumberOfClips = 10;       // maximum number of clips you can hold
    public float DelayFire = 0.85f;

    public Vector2 spreadMinMax = new Vector2(1, 3);
    public float spreadAimMultiplier = 0.5f;
    public float spreadPerSecond = 0.2f;    // if trigger held down, increase the spread of bullets
    public float spread = 0.0f;             // current spread of the gun
    public float decreaseSpreadPerSec = 0.5f;// amount of accuracy regained per frame when the gun isn't being fired 

    public float AimSwayAmount = 0.01f;
    [HideInInspector] public bool isReloading = false;       // am I in the process of reloading
    // used for tracer rendering
    public float nextFireTime { get; set; }
    // Recoil
    public float RecoilAmount = 5.0f;
    public float RecoilSpeed = 2;
    //cached player components
    public Renderer[] weaponRenders = null;
    public bl_PlayerSettings playerSettings;

    private bool m_enable = true;
    private bl_WeaponBob GunBob;
    private bl_WeaponSway SwayGun = null;
    private bl_Recoil RecoilManager;
    private bl_UCrosshair Crosshair;
#if MFPSM
    private bl_TouchHelper TouchHelper;
    private bl_AutoWeaponFire AutoFire;
#endif
    public bool canBeTakenWhenIsEmpty = true;
    private bool alreadyKnife = false;
    private AudioSource Source;
    public bool BlockAimFoV { get; set; }
    private Camera WeaponCamera;
    private Text FireTypeText;
    private bool inReloadMode = false;
    private AmmunitionType AmmoType = AmmunitionType.Bullets;
    public Camera PlayerCamera { get; private set; }
    private AudioSource FireSource = null;
    private bool CanFire = false;
    private bl_ObjectPooling Pooling;
    private bool isInitialized = false;
    private Transform m_Transform;
    public BulletData BulletSettings = new BulletData();
    public PlayerFPState FPState = PlayerFPState.Idle;
    Vector3 firePosition = Vector3.zero;
    Quaternion fireRotation = Quaternion.identity;
    private Vector2 defaultSpreadRange;
    public int extraDamage = 0;

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        Initialized();
    }

    /// <summary>
    /// 
    /// </summary>
    public void Initialized()
    {
        if (isInitialized) return;

        m_Transform = transform;
        GunBob = gunManager.GetComponent<bl_WeaponBob>();
        SwayGun = m_Transform.root.GetComponentInChildren<bl_WeaponSway>();
        RecoilManager = m_Transform.root.GetComponentInChildren<bl_Recoil>();
        if (playerSettings == null) { playerSettings = transform.root.GetComponent<bl_PlayerSettings>(); }
        Pooling = bl_ObjectPooling.Instance;
        if (FireSource == null) { FireSource = gameObject.AddComponent<AudioSource>(); FireSource.playOnAwake = false; }
        Crosshair = bl_UCrosshair.Instance;
        Source = GetComponent<AudioSource>();
        if (bl_UIReferences.Instance != null)
        {
            FireTypeText = bl_UIReferences.Instance.PlayerUI.FireTypeText;
        }
        PlayerCamera = m_Transform.root.GetComponent<bl_PlayerSettings>().PlayerCamera;
#if MFPSM
         TouchHelper = bl_TouchHelper.Instance;
         AutoFire = FindObjectOfType<bl_AutoWeaponFire>();
#endif
        defaultSpreadRange = spreadMinMax;
        AmmoType = bl_GameData.Instance.AmmoType;
        bl_UCrosshair.Instance.Block = false;
        Setup();
        weaponLogic?.Initialitate(this);
        isInitialized = true;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnEnable()
    {

#if MFPSM
        if (bl_UtilityHelper.isMobile)
        {
            bl_TouchHelper.OnFireClick += OnFire;
            bl_TouchHelper.OnReload += OnReload;
        }
#endif

        if (!isInitialized) return;

        base.OnEnable();
        Source.clip = TakeSound;
        Source.Play();
        CanFire = false;
        UpdateUI();
        if (WeaponAnimation)
        {
            float t = WeaponAnimation.DrawWeapon();
            Invoke("DrawComplete", t);
        }
        else
        {
            DrawComplete();
        }
        bl_EventHandler.OnKitAmmo += this.OnPickUpAmmo;
        bl_EventHandler.OnRoundEnd += this.OnRoundEnd;
        if (Info.Type == GunType.Shotgun)
        {
            Crosshair.Change(2);
        }
        else if (Info.Type == GunType.Knife)
        {
            Crosshair.Change(1);
        }
        else if (Info.Type == GunType.Grenade)
        {
            Crosshair.Change(3);
        }
        else
        {
            Crosshair.Change(0);
        }
        SetFireTypeName();
        playerSettings?.DoSpawnWeaponRenderEffect(weaponRenders);
        OnAmmoLauncher.ForEach(x => { x?.SetActive(bulletsLeft > 0); });
    }

    /// <summary>
    /// 
    /// </summary>
    void DrawComplete()
    {
        CanFire = true;
        CanAim = true;
        if (inReloadMode) { Reload(0.2f); }
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        bl_EventHandler.OnKitAmmo -= this.OnPickUpAmmo;
        bl_EventHandler.OnRoundEnd -= this.OnRoundEnd;
#if MFPSM
        if (bl_UtilityHelper.isMobile)
        {
            bl_TouchHelper.OnFireClick -= OnFire;
            bl_TouchHelper.OnReload -= OnReload;
        }
#endif
        alreadyKnife = false;
        if (PlayerCamera == null) { PlayerCamera = transform.root.GetComponent<bl_PlayerSettings>().PlayerCamera; }
        if (isAiming) { PlayerCamera.fieldOfView = defaultZoom; }
        StopAllCoroutines();
        if (isReloading) { inReloadMode = true; isReloading = false; }
        isAiming = false;
        isFiring = false;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Setup(bool initial = false)
    {
        bulletsLeft = bulletsPerClip; // load gun on startup
        DefaultPos = transform.localPosition;
        if (PlayerCamera == null) { PlayerCamera = transform.root.GetComponent<bl_PlayerSettings>().PlayerCamera; }
        WeaponCamera = PlayerCamera.transform.GetChild(0).GetComponent<Camera>();
        defaultZoom =  WeaponCamera.fieldOfView = bl_RoomMenu.Instance.WeaponCameraFov;
        if (!initial)
        {
            DeafultSmoothSway_ = SwayGun.Smoothness;
            if (AmmoType == AmmunitionType.Bullets)
            {
                numberOfClips = bulletsPerClip * numberOfClips;
            }
        }
        CanAim = true;
        Info = bl_GameData.Instance.GetWeapon(GunID);
        if(Info.Type != GunType.Shotgun && Info.Type != GunType.Sniper) { reloadPer = ReloadPer.Magazine; }
    }

    /// <summary>
    /// check what the player is doing every frame
    /// </summary>
    /// <returns></returns>
    public override void OnUpdate()
    {
        if (!bl_RoomMenu.Instance.isCursorLocked)
        {
            if (WeaponCamera != null) WeaponCamera.fieldOfView = bl_RoomMenu.Instance.WeaponCameraFov;
            return;
        }
        if (!m_enable)
            return;

        InputUpdate();
        Aim();
        SyncState();

        if (isFiring) // if the gun is firing
        {
            spread += (Crosshair.isCrouch) ? spreadPerSecond * 0.5f : spreadPerSecond; // gun is less accurate with the trigger held down
        }
        else
        {
            spread -= decreaseSpreadPerSec; // gun regains accuracy when trigger is released
        }
        spread = Mathf.Clamp(spread, BaseSpread, spreadMinMax.y);
    }


    /// <summary>
    /// All Input events 
    /// </summary>
    void InputUpdate()
    {
        if (bl_GameData.Instance.isChating || !gunManager.isGameStarted) return;

        // Did the user press fire.... and what kind of weapon are they using ?  ===============
        if (bl_UtilityHelper.isMobile)
        {
#if MFPSM
            if (bl_GameData.Instance.AutoWeaponFire && AutoFire != null)
            {
                HandleAutoFire();
            }
            else
            {
                if (Info.Type == GunType.Machinegun && TouchHelper != null)
                {
                    if (TouchHelper.FireDown && m_CanFire)
                    {
                        MachineGunFire();   // fire machine gun                 
                    }
                }
            }
#endif
        }
        else
        {
#if MFPSM
            if (bl_GameData.Instance.AutoWeaponFire && AutoFire != null)
            {
                HandleAutoFire();
            }
#endif
            if (m_CanFire)
            {
                if (FireButtonDown)//is was pressed
                {
                    if (weaponLogic != null) { weaponLogic.OnFireDown(); }
                    else
                    {
                        switch (Info.Type)
                        {
                            case GunType.Shotgun:
                                ShotGun_Fire();
                                break;
                            case GunType.Burst:
                                if (!isBursting) { StartCoroutine(Burst_Fire()); }
                                break;
                            case GunType.Grenade:
                                if (!grenadeFired) { GrenadeFire(); }
                                break;
                            case GunType.Pistol:
                                MachineGunFire();
                                break;
                            case GunType.Sniper:
                                SniperFire();
                                break;
                            case GunType.Knife:
                                if (!alreadyKnife) { Knife_Fire(); }
                                break;
                        }
                    }
                }
                if (FireButton)//if keep pressed
                {
                    if (weaponLogic != null) { weaponLogic.OnFire(); }
                    else
                    {
                        if (Info.Type == GunType.Machinegun)
                        {
                            MachineGunFire();
                        }
                    }
                }
            }
            else
            {
                if (FireButtonDown && bulletsLeft <= 0 && !isReloading)//if try fire and don't have more bullets
                {
                    if (Info.Type != GunType.Knife && DryFireSound != null)
                    {
                        Source.clip = DryFireSound;
                        Source.Play();
                    }
                }
            }
        }

        if (FireButtonDown && isReloading)//if try fire while reloading 
        {
            if (Info.Type == GunType.Sniper || Info.Type == GunType.Shotgun)
            {
                if (bulletsLeft > 0)//and has at least one bullet
                {
                    CancelReloading();
                }
            }
        }

        if (Info.Type != GunType.Knife && Info.Type != GunType.Grenade)
        {
            if (bl_UtilityHelper.isMobile)
            {
#if MFPSM
            isAiming = TouchHelper.isAim && m_CamAim;
#endif
            }
            else
            {
                isAiming = AimButton && m_CamAim;
            }
        }

        if (bl_RoomMenu.Instance.isCursorLocked)
        {
            Crosshair.OnAim(isAiming);
        }
        bool inputReload = Input.GetKeyDown(KeyCode.R);
#if INPUT_MANAGER
        if(bl_Input.isGamePad)
        {
            inputReload = bl_Input.isButtonDown("Reload");
        }
#endif
        if (inputReload && m_CanReload)
        {
            Reload();
        }
        if (Info.Type == GunType.Machinegun || Info.Type == GunType.Burst || Info.Type == GunType.Pistol)
        {
            ChangeTypeFire();
        }
        //used to decrease weapon accuracy as long as the trigger remains down =====================
        if (Info.Type != GunType.Grenade && Info.Type != GunType.Knife)
        {
            if (bl_UtilityHelper.isMobile)
            {
#if MFPSM
                if (!bl_GameData.Instance.AutoWeaponFire)
                {
                    isFiring = (TouchHelper.FireDown && m_CanFire);
                }
#endif
            }
            else
            {
                if (Info.Type == GunType.Machinegun)
                {
                    isFiring = (FireButton && m_CanFire); // fire is down, gun is firing
                }
                else
                {
                    if (FireButtonDown && m_CanFire)
                    {
                        isFiring = true;
                        CancelInvoke("CancelFiring");
                        Invoke("CancelFiring", 0.12f);
                    }
                }
            }
        }
    }

    void CancelFiring() { isFiring = false; }
    void CancelReloading() { WeaponAnimation.CancelReload(); }

    /// <summary>
    /// change the type of gun gust
    /// </summary>
    void ChangeTypeFire()
    {
        bool inp = Input.GetKeyDown(KeyCode.B);
#if INPUT_MANAGER
        inp = ((bl_Input.isButtonDown("FireType")));
#endif 
        if (inp)
        {
            switch (Info.Type)
            {
                case GunType.Machinegun:
                    if (CanSemi)
                    {
                        Info.Type = GunType.Burst;
                    }
                    else if (CanSingle)
                    {
                        Info.Type = GunType.Pistol;
                    }
                    break;
                case GunType.Burst:
                    if (CanSingle)
                    {
                        Info.Type = GunType.Pistol;
                    }
                    else if (CanAuto)
                    {
                        Info.Type = GunType.Machinegun;
                    }
                    break;
                case GunType.Pistol:
                    if (CanAuto)
                    {
                        Info.Type = GunType.Machinegun;
                    }
                    else if (CanSemi)
                    {
                        Info.Type = GunType.Burst;
                    }
                    break;
            }
            SetFireTypeName();
            gunManager.PlaySound(0);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnReload()
    {
        if (m_CanReload)
        {
            Reload();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnFire()
    {
        if (!gunManager.isGameStarted) return;

        if (bulletsLeft <= 0 && !isReloading)
        {
            if (Info.Type != GunType.Knife && DryFireSound != null && !isReloading)
            {
                Source.clip = DryFireSound;
                Source.Play();
            }
        }

        if (isReloading)
        {
            if (Info.Type == GunType.Sniper || Info.Type == GunType.Shotgun)
            {
                if (bulletsLeft > 0)
                {
                    CancelReloading();
                }
            }
        }

        if (!m_CanFire)
            return;

        if (weaponLogic != null) { weaponLogic.OnFireDown(); }
        else
        {
            switch (Info.Type)
            {
                case GunType.Shotgun:
                    ShotGun_Fire();  // fire shotgun
                    break;
                case GunType.Burst:
                    if (!isBursting)
                    {
                        StartCoroutine(Burst_Fire()); // fire off a burst of rounds                   
                    }
                    break;
                case GunType.Grenade:
                    if (!grenadeFired && m_CanFire)
                    {
                        GrenadeFire();
                    }
                    break;
                case GunType.Pistol:
                    MachineGunFire();   // fire Pistol gun                    
                    break;
                case GunType.Sniper:
                    SniperFire();
                    break;
                case GunType.Knife:
                    Knife_Fire();
                    break;
                default:
                    if (Info.Type != GunType.Machinegun)
                    {
                        Debug.LogWarning("Unknown gun type");
                    }
                    break;
            }
        }
    }

    void HandleAutoFire()
    {
#if MFPSM
        bool fireDown = AutoFire.Fire();
        isFiring = fireDown;
        if (fireDown)
        {
            switch (Info.Type)
            {
                case GunType.Shotgun:
                    if (m_CanFire)
                    {
                        ShotGun_Fire();  // fire shotgun
                    }
                    break;
                case GunType.Machinegun:
                    if (m_CanFire)
                    {
                        MachineGunFire();   // fire machine gun                 
                    }
                    break;
                case GunType.Burst:
                    if (m_CanFire && !isBursting)
                    {
                        StartCoroutine(Burst_Fire()); // fire off a burst of rounds                   
                    }
                    break;

                case GunType.Grenade:
                    //grenades should throw manually :)
                    break;
                case GunType.Pistol:
                    if (m_CanFire)
                    {
                        MachineGunFire();   // fire Pistol gun     
                    }
                    break;
                case GunType.Sniper:
                    if (m_CanFire)
                    {
                        SniperFire();
                    }
                    break;
                case GunType.Knife:
                    if (m_CanFire && !alreadyKnife)
                    {
                        Knife_Fire();
                    }
                    break;
                default:
                    Debug.LogWarning("Unknown gun type");
                    break;
            }
        }
#endif
    }

    /// <summary>
    /// Sync Weapon state for Upper animations
    /// </summary>
    void SyncState()
    {
        if (PlayerNetwork == null)
            return;

        if (isFiring && !isReloading)
        {
            FPState = (isAiming) ? PlayerFPState.FireAiming : PlayerFPState.Firing;
        }
        else if (isAiming && !isFiring && !isReloading)
        {
            FPState = PlayerFPState.Aiming;
        }
        else if (isReloading)
        {
            FPState = PlayerFPState.Reloading;
        }
        else if (controller.State == PlayerState.Running && !isReloading && !isFiring && !isAiming)
        {
            FPState = PlayerFPState.Running;
        }
        else
        {
            FPState = PlayerFPState.Idle;
        }
        PlayerNetwork.FPState = FPState;
    }

    /// <summary>
    /// fire the machine gun
    /// </summary>
    void MachineGunFire()
    {
        // If there is more than one bullet between the last and this frame
        float time = Time.time;
        if (time - Info.FireRate > nextFireTime)
            nextFireTime = time - Time.deltaTime;

        // Keep firing until we used up the fire time
        while (nextFireTime < time)
        {
            StartCoroutine(FireOneShot());
            if (WeaponAnimation != null)
            {
                if (isAiming)
                {
                    WeaponAnimation.AimFire();
                }
                else
                {
                    WeaponAnimation.Fire();
                }
            }
            PlayFireAudio();
            bulletsLeft--;
            UpdateUI();
            nextFireTime += Info.FireRate;
            EjectShell();
            Kick();
            Shake();
            if (!isAiming)
            {
                if (muzzleFlash) { muzzleFlash.Play(); }
            }
            //is Auto reload
            if (bulletsLeft <= 0 && numberOfClips > 0 && AutoReload)
            {
                Reload();
            }
        }
    }

    /// <summary>
    /// fire the sniper gun
    /// </summary>
    void SniperFire()
    {
        float time = Time.time;
        if (time - Info.FireRate > nextFireTime)
            nextFireTime = time - Time.deltaTime;

        // Keep firing until we used up the fire time
        while (nextFireTime < time)
        {
            StartCoroutine(FireOneShot());
            if (WeaponAnimation != null)
            {
                WeaponAnimation.Fire();
            }
            StartCoroutine(DelayFireSound());
            bulletsLeft--;
            UpdateUI();
            nextFireTime += Info.FireRate;
            EjectShell();
            Kick();
            gunManager.HeadAnimator.Play("Sniper", 0, 0);
            Shake();
            if (!isAiming)
            {
                if (muzzleFlash) { muzzleFlash.Play(); }
            }
            //is Auto reload
            if (bulletsLeft <= 0 && numberOfClips > 0 && AutoReload)
            {
                Reload(delayForSecondFireSound + 0.2f);
            }
        }
    }

    public void FastKnifeFire(System.Action callBack)
    {
        StartCoroutine(IEFastKnife(callBack));
    }

    IEnumerator IEFastKnife(System.Action callBack)
    {
        float tt = Knife_Fire(true);
        yield return new WaitForSeconds(tt);
        callBack();
        gameObject.SetActive(false);
    }

    public IEnumerator FastGrenadeFire(System.Action callBack)
    {
        float tt = WeaponAnimation.GetFirePlusDrawLenght;
        GrenadeFire(true);
        yield return new WaitForSeconds(tt);
        if (numberOfClips > 0)
        {
            bulletsLeft++;
            numberOfClips--;
            UpdateUI();
        }
        callBack();
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 
    /// </summary>
    private float Knife_Fire(bool quickFire = false)
    {
        // If there is more than one shot  between the last and this frame
        // Reset the nextFireTime
        if (Time.time - Info.FireRate > nextFireTime)
            nextFireTime = Time.time - Time.deltaTime;

        float time = 0;
        // Keep firing until we used up the fire time
        while (nextFireTime < Time.time)
        {
            isFiring = true; // fire is down, gun is firing
            alreadyKnife = true;
            StartCoroutine(KnifeSendFire());

            Vector3 position = PlayerCamera.transform.position;
            Vector3 direction = PlayerCamera.transform.TransformDirection(Vector3.forward);

            RaycastHit hit;
            if (Physics.Raycast(position, direction, out hit, Info.Range))
            {
                if (hit.transform.CompareTag("BodyPart"))
                {
                    if (hit.transform.GetComponent<bl_BodyPart>() != null)
                    {
                        hit.transform.GetComponent<bl_BodyPart>().GetDamage(Info.Damage, PhotonNetwork.NickName, DamageCause.Player, transform.position, GunID);
                    }
                }
                else if (hit.transform.CompareTag("AI"))
                {
                    if (hit.transform.GetComponent<bl_AIShooterHealth>() != null)
                    {
                        hit.transform.GetComponent<bl_AIShooterHealth>().DoDamage(Info.Damage, Info.Name, transform.position, bl_GameManager.LocalPlayerViewID, false, PhotonNetwork.LocalPlayer.GetPlayerTeam(), false,0);
                        bl_ObjectPooling.Instance.Instantiate("blood", hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                    }
                    else if (hit.transform.GetComponent<bl_AIHitBox>() != null)
                    {
                        hit.transform.GetComponent<bl_AIHitBox>().DoDamage(Info.Damage, Info.Name, transform.position, bl_GameManager.LocalPlayerViewID, false, PhotonNetwork.LocalPlayer.GetPlayerTeam());
                        bl_ObjectPooling.Instance.Instantiate("blood", hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                    }
                }
            }

            if (WeaponAnimation != null)
            {
                time = WeaponAnimation.KnifeFire(quickFire);
            }
            PlayerNetwork.IsFire(GunType.Knife, Vector3.zero);
            PlayFireAudio();
            nextFireTime += Info.FireRate;
            Kick();
            Shake();
            Crosshair.OnFire();
            isFiring = false;
        }
        return time;
    }

    /// <summary>
    /// burst shooting
    /// </summary>
    /// <returns></returns>
    IEnumerator Burst_Fire()
    {
        int shotCounter = 0;

        // If there is more than one bullet between the last and this frame
        // Reset the nextFireTime
        if (Time.time - Info.FireRate > nextFireTime)
            nextFireTime = Time.time - Time.deltaTime;

        // Keep firing until we used up the fire time
        while (nextFireTime < Time.time)
        {
            while (shotCounter < roundsPerBurst)
            {
                isBursting = true;
                StartCoroutine(FireOneShot());
                shotCounter++;
                bulletsLeft--; // subtract a bullet 
                Kick();
                EjectShell();
                UpdateUI();
                Shake();
                if (muzzleFlash) { muzzleFlash.Play(); }
                if (WeaponAnimation != null)
                {
                    WeaponAnimation.Fire();
                }
                PlayFireAudio();
                yield return new WaitForSeconds(lagBetweenShots);
                if(bulletsLeft <= 0) { break; }
            }

            nextFireTime += Info.FireRate;
            //is Auto reload
            if (bulletsLeft <= 0 && numberOfClips > 0 && AutoReload)
            {
                Reload();
            }
        }
        isBursting = false;
    }

    /// <summary>
    /// fire the shotgun
    /// </summary>
    void ShotGun_Fire()
    {
        int pelletCounter = 0;  // counter used for pellets per round
        // If there is more than one bullet between the last and this frame
        // Reset the nextFireTime
        if (Time.time - Info.FireRate > nextFireTime)
            nextFireTime = Time.time - Time.deltaTime;

        // Keep firing until we used up the fire time
        while (nextFireTime < Time.time)
        {
            do
            {
                StartCoroutine(FireOneShot());
                pelletCounter++; // add another pellet         
            } while (pelletCounter < pelletsPerShot); // if number of pellets fired is less then pellets per round... fire more pellets

            StartCoroutine(DelayFireSound());
            if (WeaponAnimation != null)
            {
                WeaponAnimation.Fire();
            }
            Shake();
            EjectShell(); // eject 1 shell 
            nextFireTime += Info.FireRate;  // can fire another shot in "fire rate" number of frames
            bulletsLeft--; // subtract a bullet
            UpdateUI();
            Kick();
            if (!isAiming)
            {
                if (muzzleFlash) { muzzleFlash.Play(); }
            }
            //is Auto reload
            if (bulletsLeft <= 0 && numberOfClips > 0 && AutoReload)
            {
                Reload(delayForSecondFireSound + 0.3f);
            }
        }
    }

    /// <summary>
    /// most shotguns have the sound of shooting and then reloading
    /// </summary>
    /// <returns></returns>
    IEnumerator DelayFireSound()
    {
        PlayFireAudio();
        yield return new WaitForSeconds(delayForSecondFireSound);
        if (DelaySource != null)
        {
            DelaySource.clip = ReloadSound3;
            DelaySource.Play();
        }
        else
        {
            Source.clip = ReloadSound3;
            Source.Play();
        }
    }

    void GrenadeFire(bool fastFire = false)
    {
        if (grenadeFired || (Time.time - nextFireTime) <= Info.FireRate)
            return;

        if (!fastFire && bulletsLeft == 0 && numberOfClips > 0)
        {
            Reload(1); // if out of ammo, reload
            return;
        }
        isFiring = true;
        grenadeFired = true;
        if (ThrowByAnimation)
        {
            nextFireTime = Time.time + Info.FireRate;
            WeaponAnimation.FireGrenade(fastFire);
        }
        else { StartCoroutine(ThrowGrenade(fastFire)); }
    }

    /// <summary>
    /// fire your launcher
    /// </summary>
    private bool grenadeFired = false;
    public IEnumerator ThrowGrenade(bool fastFire = false, bool useDelay = true)
    {
        float t = 0;
        if (useDelay)
        {
            nextFireTime = Time.time + Info.FireRate;
            t = WeaponAnimation.FireGrenade(fastFire);
            float d = (fastFire) ? DelayFire + WeaponAnimation.GetDrawLenght : DelayFire;
            yield return new WaitForSeconds(d);
        }
        Vector3 angular = (Random.onUnitSphere * 10f);
        FireOneProjectile(angular); // fire 1 round            
        bulletsLeft--; // subtract a bullet
        UpdateUI();
        Kick();

        PlayerNetwork.IsFireGrenade(spread, muzzlePoint.position, transform.parent.rotation, angular);
        PlayFireAudio();
        isFiring = false;

        //hide the grenade mesh when doesn't have ammo
        if(bulletsLeft <= 0)
        {
            OnAmmoLauncher.ForEach(x =>
           {
               x?.SetActive(false);
           });
        }

        //is Auto reload
        if (!fastFire && AutoReload)
        {
            if (useDelay)
            {
                t = t - DelayFire;
                yield return new WaitForSeconds(t);
            }
            if (bulletsLeft <= 0 && numberOfClips > 0)
            {
                Reload(0);
            }
            else if(bulletsLeft <= 0)
            {
                gunManager?.OnReturnWeapon();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void BuildBulletData()
    {
        BulletSettings.Damage = Damage;
        BulletSettings.ImpactForce = impactForce;
        if (Info.Type == GunType.Sniper && !isAiming)
        {
            BulletSettings.Spread = spread * 3;
            BulletSettings.MaxSpread = spreadMinMax.y * 3;
        }
        else
        {
            BulletSettings.MaxSpread = spreadMinMax.y;
            BulletSettings.Spread = spread;
        }
        BulletSettings.Speed = bulletSpeed;
        BulletSettings.WeaponName = Info.Name;
        BulletSettings.Position = this.transform.root.position;
        BulletSettings.WeaponID = GunID;
        BulletSettings.isNetwork = false;
        BulletSettings.LifeTime = Info.Range;
        BulletSettings.ActorViewID = bl_GameManager.LocalPlayerViewID;
        BulletSettings.MFPSActor = bl_GameManager.Instance.LocalActor;
    }

    /// <summary>
    /// Create and fire a bullet
    /// </summary>
    /// <returns></returns>
    GameObject instancedBullet = null;
    RaycastHit hit;
    IEnumerator FireOneShot()
    {
        // set the gun's info into an array to send to the bullet
        BuildBulletData();
        Vector3 hitPoint = GetBulletPosition(out firePosition, out fireRotation);
        //bullet info is set up in start function
        instancedBullet = Pooling.Instantiate(BulletName, firePosition, fireRotation); // create a bullet
        instancedBullet.GetComponent<bl_Bullet>().SetUp(BulletSettings);// send the gun's info to the bullet
        Crosshair.OnFire();
        PlayerNetwork.IsFire(Info.Type, hitPoint);
        if (Info.Type != GunType.Grenade)
        {
            Source.clip = FireSound;
            Source.spread = Random.Range(1.0f, 1.5f);
            Source.Play();
        }

        if ((bulletsLeft == 0))
        {
            Reload();  // if out of bullets.... reload
            yield break;
        }
    }

    /// <summary>
    /// Create and Fire 1 launcher projectile
    /// </summary>
    /// <returns></returns>
    void FireOneProjectile(Vector3 angular)
    {
        Vector3 position = muzzlePoint.position; // position to spawn rocket / grenade is at the muzzle point of the gun
        BuildBulletData();

        //Instantiate grenade
        GameObject newNoobTube = Instantiate(grenade, position, transform.parent.rotation) as GameObject;
        if (newNoobTube.GetComponent<Rigidbody>() != null)//if grenade have a rigidbody,then apply velocity
        {
            newNoobTube.GetComponent<Rigidbody>().angularVelocity = angular;
        }
        newNoobTube.GetComponent<bl_Projectile>().SetUp(BulletSettings);// send the gun's info to the grenade    
        grenadeFired = false;
    }

    public Vector3 GetBulletPosition(out Vector3 position, out Quaternion rotation)
    {
        Vector3 HitPoint = PlayerCamera.transform.forward * 100;
        rotation = m_Transform.parent.rotation;
        position = PlayerCamera.transform.position;
        if (Physics.Raycast(position, PlayerCamera.transform.forward, out hit))
        {
            if (bl_UtilityHelper.Distance(muzzlePoint.position, hit.point) > 5)
            {
                HitPoint = hit.point;
                rotation = Quaternion.LookRotation(HitPoint - muzzlePoint.position);
                position = muzzlePoint.position;
            }
        }
        return HitPoint;
    }

    /// <summary>
    /// 
    /// </summary>
    void EjectShell()
    {
        shell?.Play();
    }

    /// <summary>
    /// 
    /// </summary>
    public void PlayFireAudio()
    {
        FireSource.clip = FireSound;
        FireSource.spread = Random.Range(1.0f, 1.5f);
        FireSource.pitch = Random.Range(1.0f, 1.075f);
        FireSource.Play();
    }

    /// <summary>
    /// ADS
    /// </summary>
    void Aim()
    {
        if (isAiming && !isReloading)
        {
            CurrentPos = AimPosition; //Place in the center ADS
            currentZoom = aimZoom; //create a zoom camera
            GunBob.Intensitity = 0.01f;
            SwayGun.Smoothness = DeafultSmoothSway_ * 2.5f;
            SwayGun.Amount = AimSwayAmount;
            spreadMinMax = defaultSpreadRange * spreadAimMultiplier;
        }
        else // if not aimed
        {
            CurrentPos = DefaultPos; //return to default gun position       
            currentZoom = defaultZoom; //return to default fog
            GunBob.Intensitity = 1;
            SwayGun.Smoothness = DeafultSmoothSway_;
            SwayGun.ResetSettings();
            spreadMinMax = defaultSpreadRange;
        }
        float delta = Time.deltaTime;
        //apply position
        m_Transform.localPosition = useSmooth ? Vector3.Lerp(m_Transform.localPosition, CurrentPos, delta * AimSmooth) : //with smooth effect
        Vector3.MoveTowards(m_Transform.localPosition, CurrentPos, delta * AimSmooth); // with snap effect
        if (PlayerCamera != null && !BlockAimFoV)
        {
            PlayerCamera.fieldOfView = useSmooth ? Mathf.Lerp(PlayerCamera.fieldOfView, currentZoom + controller.RunFov, delta * (AimSmooth * 3)) :
             Mathf.Lerp(PlayerCamera.fieldOfView, currentZoom + controller.RunFov, delta * AimSmooth);
        }
        GunBob.isAim = isAiming;
    }

    public void SetToAim()
    {
        m_Transform.localPosition = AimPosition;
    }
    /// <summary>
    /// send kick back to mouse look
    /// when is fire
    /// </summary>
    public void Kick()
    {
        RecoilManager.SetRecoil(RecoilAmount, RecoilSpeed);
    }

    /// <summary>
    /// 
    /// </summary>
    void Shake()
    {
        float influence = isAiming ? 0.5f : 1;
        bl_EventHandler.DoPlayerCameraShake(shakerPresent, "fpweapon", influence);
    }

    /// <summary>
    /// 
    /// </summary>
    public void CheckBullets(float delay = 0)
    {
        if (bulletsLeft <= 0 && numberOfClips > 0 && AutoReload)
        {
            Reload(delay);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void Reload(float delay = 0.2f)
    {
        if (isReloading)
            return;

        StartCoroutine(DoReload(delay));
    }

    /// <summary>
    /// start reload weapon
    /// deduct the remaining bullets in the cartridge of a new clip
    /// as this happens, we disable the options: fire, aim and run
    /// </summary>
    /// <returns></returns>
    IEnumerator DoReload(float waitTime = 0.2f)
    {
        isAiming = false;
        CanFire = false;

        if (isReloading)
            yield break; // if already reloading... exit and wait till reload is finished


        yield return new WaitForSeconds(waitTime);

        if (numberOfClips > 0 || inReloadMode)//if have at least one cartridge
        {
            isReloading = true; // we are now reloading
            if (WeaponAnimation != null)
            {
                if (reloadPer == ReloadPer.Bullet)//insert one bullet at a time
                {
                    int t_repeat = bulletsPerClip - bulletsLeft; //get the number of spent bullets
                    int add = (numberOfClips >= t_repeat) ? t_repeat : numberOfClips;
                    WeaponAnimation.SplitReload(Info.ReloadTime, add);
                    yield break;
                }
                else
                {
                    WeaponAnimation.Reload(Info.ReloadTime);
                }
            }
            if (!SoundReloadByAnim)
            {
                StartCoroutine(ReloadSoundIE());
            }
            if (!inReloadMode)// take away a clip
            {
                if (AmmoType == AmmunitionType.Clips)
                {
                    numberOfClips--;
                }
            }
            if (Info.Type == GunType.Grenade) { OnAmmoLauncher.ForEach(x => { x?.SetActive(bulletsLeft > 0); }); }
            yield return new WaitForSeconds(Info.ReloadTime); // wait for set reload time
            if (AmmoType == AmmunitionType.Clips)
            {
                bulletsLeft = bulletsPerClip; // fill up the gun
            }
            else
            {
                int need = bulletsPerClip - bulletsLeft;
                int add = (numberOfClips >= need) ? need : numberOfClips;
                bulletsLeft += add;
                numberOfClips -= add;
            }
        }
        UpdateUI();
        isReloading = false; // done reloading
        CanAim = true;
        CanFire = true;
        inReloadMode = false;
    }

    public void AddBullet(int bullet)
    {
        if (AmmoType == AmmunitionType.Bullets)
        {
            numberOfClips -= bullet;
        }
        bulletsLeft += bullet;
        UpdateUI();
    }

    /// <summary>
    /// 
    /// </summary>
    public void FinishReload()
    {
        if (AmmoType == AmmunitionType.Clips)
        {
            numberOfClips--;
        }
        isReloading = false; // done reloading
        CanAim = true;
        CanFire = true;
        inReloadMode = false;
    }

    public void PlayReloadAudio(int part)
    {
        if (SoundReloadByAnim) return;

        if (part == 0)
        {
            Source.clip = ReloadSound;
            Source.Play();
        }
        else if (part == 1)
        {
            Source.clip = ReloadSound2;
            Source.Play();
        }
        else if (part == 2)
        {
             Source.clip = ReloadSound3;
            Source.Play();
        }
    }

    /// <summary>
    /// use this method to various sounds reload.
    /// if you have only 1 sound, them put only one in inspector
    /// and leave empty other box
    /// </summary>
    /// <returns></returns>
    IEnumerator ReloadSoundIE()
    {
        float t_time = Info.ReloadTime / 3;
        if (ReloadSound != null)
        {
            Source.clip = ReloadSound;
            Source.Play();
            gunManager?.HeadAnimation(1, t_time);
        }
        if (ReloadSound2 != null)
        {
            if (Info.Type == GunType.Shotgun)
            {
                int t_repeat = bulletsPerClip - bulletsLeft;
                for (int i = 0; i < t_repeat; i++)
                {
                    yield return new WaitForSeconds(t_time / t_repeat + 0.025f);
                    Source.clip = ReloadSound2;
                    Source.Play();
                }
            }
            else
            {
                yield return new WaitForSeconds(t_time);
                Source.clip = ReloadSound2;
                Source.Play();
            }
        }
        if (ReloadSound3 != null)
        {
            yield return new WaitForSeconds(t_time);
            Source.clip = ReloadSound3;
            Source.Play();
            gunManager?.HeadAnimation(2, t_time);
        }
        yield return new WaitForSeconds(0.65f);
        gunManager?.HeadAnimation(0, t_time);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator KnifeSendFire()
    {
        yield return new WaitForSeconds(0.5f);
        isFiring = false;
        alreadyKnife = false;
    }

    /// <summary>
    /// When we disable the gun ship called the animation
    /// and disable the basic functions
    /// </summary>
    public float DisableWeapon(bool isFastKill = false)
    {
        CanAim = false;
        if (isReloading) { inReloadMode = true; isReloading = false; }
        CanFire = false;
        gunManager?.HeadAnimation(0, 1);
        if (PlayerCamera == null) { PlayerCamera = m_Transform.root.GetComponent<bl_PlayerSettings>().PlayerCamera; }
        PlayerCamera.fieldOfView = defaultZoom;
        if (!isFastKill) { StopAllCoroutines(); }
        return WeaponAnimation.HideWeapon();
    }


    /// <summary>
    /// 
    /// </summary>
    void SetFireTypeName()
    {
        string n = string.Empty;
        switch (Info.Type)
        {
            case GunType.Machinegun:
                n = bl_GameTexts.FireTypeAuto;
                break;
            case GunType.Burst:
                n = bl_GameTexts.FireTypeSemi;
                break;
            case GunType.Pistol:
            case GunType.Shotgun:
            case GunType.Sniper:
                n = bl_GameTexts.FireTypeSingle;
                break;
            default:
                n = "--";
                break;
        }
        if (FireTypeText != null)
        {
            FireTypeText.text = n;
        }
    }

    /// <summary>
    /// When round is end we can't fire
    /// </summary>
    void OnRoundEnd()
    {
        m_enable = false;
    }

    public void OnPickUpAmmo(int bullets, int t_clips, int projectiles)
    {
        if (Info.Type == GunType.Knife) return;
        if (AmmoType == AmmunitionType.Clips)
        {
            if (numberOfClips < maxNumberOfClips)
            {
                numberOfClips += t_clips;
                if (numberOfClips > maxNumberOfClips)
                {
                    numberOfClips = maxNumberOfClips;
                }
            }
        }
        else
        {
            if (Info.Type == GunType.Grenade)
            {
                numberOfClips += projectiles;
             bl_UIReferences.Instance.AddLeftNotifier(string.Format("+{0} {1}", projectiles.ToString(), Info.Name));
            }
            else
            {
                int total = bullets + (bulletsPerClip * t_clips);
                numberOfClips += total;
                bl_UIReferences.Instance.AddLeftNotifier(string.Format("+{0} {1} Bullets", total, Info.Name));
            }
        }
        UpdateUI();
    }

    /// <summary>
    /// 
    /// </summary>
    public void UpdateUI()
    {
        bl_UIReferences.Instance.PlayerUI.UpdateWeaponState(this);
    }

    private float BaseSpread
    {
        get { return Crosshair.isCrouch ? spreadMinMax.x * 0.5f : spreadMinMax.x; }
    }

    public bool FireButtonDown
    {
        get
        {
#if !INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return bl_Input.isButtonDown("SingleFire");
#endif
        }
    }

    public bool FireButton
    {
        get
        {
#if !INPUT_MANAGER
            return Input.GetMouseButton(0);
#else
            return bl_Input.isButton("Fire");
#endif
        }
    }

    public bool AimButton
    {
        get
        {
#if !INPUT_MANAGER
            return (Input.GetMouseButton(1));
#else
            return bl_Input.isButton("Aim");
#endif       
        }
    }

    public int Damage
    {
        get { return Info.Damage + extraDamage; }
        set { extraDamage = value; }//don't modify the base damage, only the extra value
    }

    private bl_WeaponAnimation _anim;
    public bl_WeaponAnimation WeaponAnimation
    {
        get
        {
            if(_anim == null) { _anim = GetComponentInChildren<bl_WeaponAnimation>(); }
            return _anim;
        }
    }
    private bl_FirstPersonController _controller;
    public bl_FirstPersonController controller
    {
        get
        {
            if (_controller == null) { _controller = transform.root.GetComponent<bl_FirstPersonController>(); }
            return _controller;
        }
    }

    private bl_PlayerNetwork _Sync;
    public bl_PlayerNetwork PlayerNetwork
    {
        get
        {
            if (_Sync == null) { _Sync = transform.root.GetComponent<bl_PlayerNetwork>(); }
            return _Sync;
        }
    }

    public int GetCompactClips { get { return (numberOfClips / bulletsPerClip); } }
    /// <summary>
    /// determine if we are ready to shoot
    /// TIP: if you want to have to shoot when running
    /// just remove "!controller.run" of the condition
    /// </summary>
    public bool m_CanFire
    {
        get
        {
            bool can = false;
            if (bulletsLeft > 0 && CanFire && !isReloading && FireWhileRun)
            {
                can = true;
            }
            return can;
        }
    }

    public bool FireRatePassed { get { return (Time.time - nextFireTime) > Info.FireRate; } }
    public bool AllowQuickFire() => m_AllowQuickFire && bulletsLeft > 0 && FireRatePassed;

    /// <summary>
    /// determine if we can Aim
    /// </summary>
    public bool m_CamAim
    {
        get
        {
            bool can = false;
            if (CanAim && controller.State != PlayerState.Running)
            {
                can = true;
            }
            return can;
        }
    }
    /// <summary>_
    /// determine is we can reload
    /// TIP: if you want to have to shoot when running
    /// just remove "!controller.run" of the condition
    /// </summary>
    bool m_CanReload
    {
        get
        {
            bool can = false;
            if (bulletsLeft < bulletsPerClip && numberOfClips > 0 && controller.State != PlayerState.Running && !isReloading)
            {
                can = true;
            }
            if (Info.Type == GunType.Knife && nextFireTime < Time.time)
            {
                can = false;
            }
            return can;
        }
    }

    bool FireWhileRun
    {
        get
        {
            if (bl_GameData.Instance.CanFireWhileRunning)
            {
                return true;
            }
            if (controller.State != PlayerState.Running)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool CanBeTaken() { if (canBeTakenWhenIsEmpty) return true; else return (bulletsLeft > 0 || numberOfClips > 0); }

    [System.Serializable]
    public enum BulletInstanceMethod
    {
        Pooled,
        Instanced,
    }

    [System.Serializable]
    public enum ReloadPer
    {
        Bullet,
        Magazine,
    }

    /// <summary>
    /// 
    /// </summary>
    private bl_GunManager _gunManager;
    private bl_GunManager gunManager
    {
        get
        {
            if(_gunManager == null) { _gunManager = this.transform.root.GetComponentInChildren<bl_GunManager>(); }
            return _gunManager;
        }
    }
#if UNITY_EDITOR
    public bool _aimRecord = false;
    public Vector3 _defaultPosition = new Vector3(-100, 0, 0);

    private void OnDrawGizmos()
    {
        if(muzzlePoint != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.4f);
            Gizmos.DrawSphere(muzzlePoint.position, 0.022f);
            Gizmos.color = Color.white;
        }
    }
#endif
}