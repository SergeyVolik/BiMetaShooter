/////////////////////////////////////////////////////////////////////////////////
///////////////////////////bl_GunManager.cs//////////////////////////////////////
/////////////Use this to manage all weapons Player///////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
//////////////////////////////Lovatto Studio/////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class bl_GunManager : bl_MonoBehaviour
{

    [Header("Weapons List")]
    /// <summary>
    /// all the Guns of game
    /// </summary>
    public List<bl_Gun> AllGuns = new List<bl_Gun>();
    /// <summary>
    /// weapons that the player take equipped
    /// </summary>
    [HideInInspector] public List<bl_Gun> PlayerEquip = new List<bl_Gun>() { null, null, null, null };

    [Header("Player Class")]
    public bl_PlayerClassLoadout m_AssaultClass;
    public bl_PlayerClassLoadout m_EngineerClass;
    public bl_PlayerClassLoadout m_ReconClass;
    public bl_PlayerClassLoadout m_SupportClass;

    [Header("Settings")]
    /// <summary>
    /// ID the weapon to take at start
    /// </summary>
    public int currentWeaponIndex = 0;
    /// <summary>
    /// time it takes to switch weapons
    /// </summary>
    public float SwichTime = 1;
    public float PickUpTime = 2.5f;
    public ChangeWeaponStyle changeWeaponStyle = ChangeWeaponStyle.HideAndDraw;

    [HideInInspector] public bl_Gun CurrentGun;
    public bool CanSwich { get; set; }
    [Header("References")]
    public Animator HeadAnimator;
    public Transform TrowPoint = null;
    public AudioClip SwitchFireAudioClip;
    private bl_GunPickUpManager PUM;
    private int PreviousGun = 0;
    private bool isFastFire = false;
    public bool ObservedComponentsFoldoutOpen = false;
    AudioSource ASource;
    public bool isGameStarted { get; set; }
#if GR
    public bool isGunRace { get; set; }
    private bl_GunRace GunRace;
#endif

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        PUM = FindObjectOfType<bl_GunPickUpManager>();
        ASource = GetComponent<AudioSource>();
        isGameStarted = bl_MatchTimeManager.Instance.TimeState == RoomTimeState.Started;
#if GR
        if (transform.root.GetComponent<PhotonView>().IsMine)
        {
            GunRace = FindObjectOfType<bl_GunRace>();
            if (GunRace != null) { GunRace.SetGunManager(this); }
            else { Debug.Log("Gun Race is not integrated in this map, just go to MFPS -> Addons -> Gun Race -> Integrate, with the map scene open)."); }
        }
#endif
        //when player instance select player class select in bl_RoomMenu
        GetClass();
    }

    /// <summary>
    /// 
    /// </summary>
    private void Start()
    {
        //Disable all weapons in children and take the first
        foreach (bl_Gun g in PlayerEquip) { g?.Setup(true); }
        foreach (bl_Gun guns in AllGuns) { guns.gameObject?.SetActive(false); }
        bl_UIReferences.Instance.PlayerUI.LoadoutUI.SetInitLoadout(PlayerEquip);
#if GR
        if (isGunRace)
        {
            PlayerEquip[0] = GunRace.GetGunInfo(AllGuns);
            currentWeaponIndex = 0;
        }
#endif
        TakeWeapon(PlayerEquip[currentWeaponIndex]);
        bl_EventHandler.ChangeWeaponEvent(PlayerEquip[currentWeaponIndex].GunID);

        if (bl_GameManager.Instance.GameMatchState == MatchState.Waiting && !bl_GameManager.Instance.alreadyEnterInGame)
        {
            BlockAllWeapons();
        }
#if LMS
        if (GetGameMode == GameMode.LSM)
        {
            PlayerEquip[currentWeaponIndex].gameObject.SetActive(false);
            for (int i = 0; i < PlayerEquip.Count; i++)
            {
                PlayerEquip[i] = null;
            }
            bl_UCrosshair.Instance.Show(false);
            bl_UCrosshair.Instance.Block = true;
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    void GetClass()
    {
#if CLASS_CUSTOMIZER
        //Get info for class
        bl_RoomMenu.PlayerClass = bl_ClassManager.Instance.m_Class;
        bl_ClassManager.Instance.SetUpClasses(this);
#else
        //when player instance select player class select in bl_RoomMenu
        bl_PlayerClassLoadout pcl = null;
        switch (bl_RoomMenu.PlayerClass)
        {
            case PlayerClass.Assault:
                pcl = m_AssaultClass;
                break;
            case PlayerClass.Recon:
                pcl = m_ReconClass;
                break;
            case PlayerClass.Engineer:
                pcl = m_EngineerClass;
                break;
            case PlayerClass.Support:
                pcl = m_SupportClass;
                break;
        }

        if (pcl == null)
        {
            Debug.LogError($"Player Class Loadout has not been assigned for the class {bl_RoomMenu.PlayerClass.ToString()}");
            return;
        }

        PlayerEquip[0] = GetGunOnListById(pcl.Primary);
        PlayerEquip[1] = GetGunOnListById(pcl.Secondary);
        PlayerEquip[2] = GetGunOnListById(pcl.Letal);
        PlayerEquip[3] = GetGunOnListById(pcl.Perks);
#endif
        for (int i = 0; i < PlayerEquip.Count; i++)
        {
            if (PlayerEquip[i] == null) continue;
            PlayerEquip[i].Initialized();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        bl_EventHandler.OnPickUpGun += this.PickUpGun;
        bl_EventHandler.onMatchStart += OnMatchStart;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        bl_EventHandler.OnPickUpGun -= this.PickUpGun;
        bl_EventHandler.onMatchStart -= OnMatchStart;
    }
    void OnMatchStart() { isGameStarted = true; }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (!bl_RoomMenu.Instance.isCursorLocked)
            return;

        InputControl();
        CurrentGun = PlayerEquip[currentWeaponIndex];
    }

    /// <summary>
    /// 
    /// </summary>
    void InputControl()
    {
        if (!CanSwich || bl_GameData.Instance.isChating)
            return;
#if GR
        if (isGunRace)
            return;
#endif

#if !INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha1) && currentWeaponIndex != 0 && PlayerEquip[0] != null)
        {
            ChangeCurrentWeaponTo(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && currentWeaponIndex != 1 && PlayerEquip[1] != null)
        {
            ChangeCurrentWeaponTo(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && currentWeaponIndex != 2 && PlayerEquip[2] != null)
        {
            ChangeCurrentWeaponTo(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) && currentWeaponIndex != 3 && PlayerEquip[3] != null)
        {
            ChangeCurrentWeaponTo(3);
        }

        //fast fire knife
        if (Input.GetKeyDown(KeyCode.V) && PlayerEquip[3].m_AllowQuickFire && currentWeaponIndex != 3 && !isFastFire && PlayerEquip[3] != null)
        {
          DoFastKnifeShot();
        }

        //fast throw grenade
        if (Input.GetKeyDown(KeyCode.G) && PlayerEquip[2].AllowQuickFire() && currentWeaponIndex != 2 && !isFastFire && PlayerEquip[2] != null)
        {
            DoSingleGrenadeThrow();
        }

#else
        InputManagerControll();
#endif
        if (PlayerEquip.Count <= 0 || PlayerEquip == null)
            return;
        //change gun with Scroll mouse
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            SwitchNext();
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            SwitchPrevious();
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public int SwitchNext()
    {
#if GR
        if (isGunRace)
            return 0;
#endif

        int next = (this.currentWeaponIndex + 1) % this.PlayerEquip.Count;
        ChangeCurrentWeaponTo(next);
        return currentWeaponIndex;
    }

    /// <summary>
    /// 
    /// </summary>
    public int SwitchPrevious()
    {
#if GR
        if (isGunRace)
            return 0;
#endif
        int next = 0;
        if (this.currentWeaponIndex != 0)
        {
            next = (this.currentWeaponIndex - 1) % this.PlayerEquip.Count;
        }
        else
        {
            next = PlayerEquip.Count - 1;
        }
        ChangeCurrentWeaponTo(next);
        return currentWeaponIndex;
    }

    /// <summary>
    /// 
    /// </summary>
    public void DoFastKnifeShot()
    {
        PreviousGun = currentWeaponIndex;
        isFastFire = true;
        currentWeaponIndex = 3; // 3 = knife position in list
        PlayerEquip[PreviousGun].gameObject.SetActive(false);
        PlayerEquip[currentWeaponIndex].gameObject.SetActive(true);
        PlayerEquip[currentWeaponIndex].FastKnifeFire(OnReturnWeapon);
        CanSwich = false;
    }

    /// <summary>
    /// 
    /// </summary>
    public void DoSingleGrenadeThrow()
    {
        PreviousGun = currentWeaponIndex;
        isFastFire = true;
        currentWeaponIndex = 2; // 2 = GRENADE position in list
        PlayerEquip[PreviousGun].gameObject.SetActive(false);
        PlayerEquip[currentWeaponIndex].gameObject.SetActive(true);
        StartCoroutine(PlayerEquip[currentWeaponIndex].FastGrenadeFire(OnReturnWeapon));
        CanSwich = false;
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnReturnWeapon()
    {
        PlayerEquip[currentWeaponIndex].gameObject.SetActive(false);
        currentWeaponIndex = PreviousGun;
        PlayerEquip[currentWeaponIndex].gameObject.SetActive(true);
        CanSwich = true;
        isFastFire = false;
        bl_EventHandler.ChangeWeaponEvent(PlayerEquip[currentWeaponIndex].GunID);
    }

    /// <summary>
    /// 
    /// </summary>
    void TakeWeapon(bl_Gun gun)
    {
        if (gun == null) return;
        gun.gameObject.SetActive(true);
        CanSwich = true;
    }

    /// <summary>
    /// Call to set none weapon in the local player and can't select any weapon
    /// </summary>
    public void BlockAllWeapons()
    {
        foreach (bl_Gun g in PlayerEquip) { g.gameObject.SetActive(false); }
        bl_UCrosshair.Instance.Show(false);
        bl_UCrosshair.Instance.Block = true;
        CanSwich = false;
        PlayerSync.SetWeaponBlocked(true);
    }

    /// <summary>
    /// Make local player can switch weapons again
    /// </summary>
    public void ReleaseWeapons(bool takeFirst)
    {
        CanSwich = true;
        bl_UCrosshair.Instance.Block = false;
        bl_UCrosshair.Instance.Show(true);
        if (takeFirst)
        {
            TakeWeapon(PlayerEquip[0]);
        }
        else
        {
            TakeWeapon(PlayerEquip[currentWeaponIndex]);
        }
        PlayerSync.SetWeaponBlocked(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bl_Gun GetCurrentWeapon()
    {
        if (CurrentGun == null)
        {
            return PlayerEquip[currentWeaponIndex];
        }
        else
        {
            return CurrentGun;
        }
    }

    public int GetCurrentGunID
    {
        get
        {
            if (GetCurrentWeapon() == null) { return -1; }
            return GetCurrentWeapon().GunID;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void ChangeTo(int AllWeaponsIndex)
    {
        StartCoroutine(ChangeGun(currentWeaponIndex, AllGuns[AllWeaponsIndex].gameObject, currentWeaponIndex));
        PlayerEquip[currentWeaponIndex] = AllGuns[AllWeaponsIndex];
    }

    /// <summary>
    /// 
    /// </summary>
    public void ChangeToInstant(int AllWeaponsIndex)
    {
        PlayerEquip[currentWeaponIndex].gameObject.SetActive(false);
        AllGuns[AllWeaponsIndex].gameObject.SetActive(true);
        bl_EventHandler.ChangeWeaponEvent(PlayerEquip[currentWeaponIndex].GunID);
        PlayerEquip[currentWeaponIndex] = AllGuns[AllWeaponsIndex];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nextSlotID"></param>
    public void ChangeCurrentWeaponTo(int nextSlotID)
    {
        if (!CanSwich || bl_GameData.Instance.isChating)
            return;
        if (!PlayerEquip[nextSlotID].CanBeTaken()) return;//if the weapons can't be taken when is empty (of ammo)

        PreviousGun = currentWeaponIndex;
        StartCoroutine(ChangeGun(currentWeaponIndex, PlayerEquip[nextSlotID].gameObject, nextSlotID));
        currentWeaponIndex = nextSlotID;
    }

    /// <summary>
    /// Coroutine to Change of Gun
    /// </summary>
    /// <returns></returns>
    public IEnumerator ChangeGun(int IDfrom, GameObject t_next, int newID)
    {
        CanSwich = false;
        bl_UIReferences.Instance.PlayerUI.LoadoutUI.ChangeWeapon(newID);
        float hideTime = PlayerEquip[IDfrom].DisableWeapon();
        //instantly disable the current weapon and active the next weapon
        if (changeWeaponStyle == ChangeWeaponStyle.CounterStrike)
        {
            PlayerEquip[IDfrom].gameObject.SetActive(false);
        }
        else if (changeWeaponStyle == ChangeWeaponStyle.HideAndDraw)
        {
            HeadAnimator?.Play("SwichtGun", 0, 0);
            //wait a fixed delay before active the next weapon
            yield return new WaitForSeconds(SwichTime);
        }
        else if (changeWeaponStyle == ChangeWeaponStyle.HideCompletelyAndThenDraw)
        {
            HeadAnimator?.Play("SwichtGun", 0, 0);
            //wait until the current weapon hide animation complete before active the next weapon
            yield return new WaitForSeconds(hideTime);
        }
        foreach (bl_Gun guns in AllGuns)
        {
            if (guns.gameObject.activeSelf == true)
            {
                guns.gameObject.SetActive(false);
            }
        }
        TakeWeapon(PlayerEquip[newID]);
        bl_EventHandler.ChangeWeaponEvent(PlayerEquip[newID].GunID);
    }

    /// <summary>
    /// 
    /// </summary>
    public void PickUpGun(GunPickUpData e)
    {
        if (PUM == null)
        {
            Debug.LogError("Need a 'Pick Up Manager' in scene!");
            return;
        }
        //If not already equip
        if (!PlayerEquip.Exists(x => x != null && x.GunID == e.ID))
        {

            int actualID = (PlayerEquip[currentWeaponIndex] == null) ? -1 : PlayerEquip[currentWeaponIndex].GunID;
            int nextID = AllGuns.FindIndex(x => x.GunID == e.ID);
            //Get Info
            int[] info = new int[2];
            int clips = (PlayerEquip[currentWeaponIndex] == null) ? 3 : PlayerEquip[currentWeaponIndex].numberOfClips;
            info[0] = clips;
            info[1] = (PlayerEquip[currentWeaponIndex] == null) ? 30 : PlayerEquip[currentWeaponIndex].bulletsLeft;
            bool replace = true;
            if (PlayerEquip.Exists(x => x == null))
            {
                int nullId = PlayerEquip.FindIndex(x => x == null);
                PlayerEquip[nullId] = AllGuns[nextID];
                replace = false;
            }
            else
            {
                PlayerEquip[currentWeaponIndex] = AllGuns[nextID];
            }
            //Send Info
            AllGuns[nextID].numberOfClips = e.Clips;
            AllGuns[nextID].bulletsLeft = e.Bullets;
            AllGuns[nextID].Setup(true);
            bl_UIReferences.Instance.PlayerUI.LoadoutUI.ReplaceSlot(currentWeaponIndex, AllGuns[nextID]);
            StartCoroutine(PickUpGun((PlayerEquip[currentWeaponIndex].gameObject), AllGuns[nextID], actualID, info, replace));
        }
        else
        {
            foreach (bl_Gun g in PlayerEquip)
            {
                if (g != null && g.GunID == e.ID)
                {
                    g.OnPickUpAmmo(e.Bullets, e.Clips, 1);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public IEnumerator PickUpGun(GameObject t_current, bl_Gun nextGun, int id, int[] info, bool replace)
    {
        CanSwich = false;
        if (HeadAnimator != null)
        {
            HeadAnimator.Play("TakeGun", 0, 0);
        }
        t_current.GetComponent<bl_Gun>().DisableWeapon();
        yield return new WaitForSeconds(PickUpTime);
        foreach (bl_Gun guns in AllGuns)
        {
            if (guns.gameObject.activeSelf == true)
            {
                guns.gameObject.SetActive(false);
            }
        }
        TakeWeapon(nextGun);
        if (replace)
        {
            PUM.TrownGun(id, TrowPoint.position, info, false);
        }
        bl_EventHandler.ChangeWeaponEvent(PlayerEquip[currentWeaponIndex].GunID);
    }

    public void ThrwoCurrent(bool AutoDestroy)
    {
        ThrwoCurrent(AutoDestroy, TrowPoint.position);
    }

    /// <summary>
    /// Throw the current gun
    /// </summary>
    public void ThrwoCurrent(bool AutoDestroy, Vector3 throwPosition)
    {
        if (PlayerEquip[currentWeaponIndex] == null)
            return;

        int actualID = PlayerEquip[currentWeaponIndex].GunID;
        int[] info = new int[2];
        int clips = (bl_GameData.Instance.AmmoType == AmmunitionType.Bullets) ? PlayerEquip[currentWeaponIndex].numberOfClips / PlayerEquip[currentWeaponIndex].bulletsPerClip : PlayerEquip[currentWeaponIndex].numberOfClips;
        info[0] = clips;
        info[1] = PlayerEquip[currentWeaponIndex].bulletsLeft;
        PUM.TrownGun(actualID, throwPosition, info, AutoDestroy);
    }

    public bl_Gun GetGunOnListById(int id)
    {
        bl_Gun gun = null;
        if (AllGuns.Exists(x => x != null && x.GunID == id))
        {
            gun = AllGuns.Find(x => x.GunID == id);
        }
        else
        {
            Debug.LogError("Gun: " + id + " has not been added on this player list.");
        }
        return gun;
    }

#if INPUT_MANAGER
     void InputManagerControll()
    {
        if (!CanSwich) return;

        if (bl_Input.isGamePad)
        {
            if (bl_Input.isButtonDown("NextWeapon"))
            {
                SwitchNext();
            }

            if (bl_Input.isButtonDown("PreviousWeapon"))
            {
                SwitchPrevious();
            }
        }
        else
        {
            if (bl_Input.isButtonDown("Weapon1") && CanSwich && currentWeaponIndex != 0)
            {
               ChangeCurrentWeaponTo(0);
            }
            if (bl_Input.isButtonDown("Weapon2") && CanSwich && currentWeaponIndex != 1)
            {
                ChangeCurrentWeaponTo(1);
            }
            if (bl_Input.isButtonDown("Weapon3") && CanSwich && currentWeaponIndex != 2)
            {
               ChangeCurrentWeaponTo(2);
            }
            if (bl_Input.isButtonDown("Weapon4") && CanSwich && currentWeaponIndex != 3)
            {
               ChangeCurrentWeaponTo(3);
            }
        }

        //fast fire knife
        if (bl_Input.isButtonDown("FastKnife") && CanSwich && currentWeaponIndex != 3 && !isFastFire)
        {
            DoFastKnifeShot();
        }
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    public void HeadAnimation(int state, float speed)
    {
        if (HeadAnimator == null)
            return;

        switch (state)
        {
            case 0:
                HeadAnimator.SetInteger("Reload", 0);
                break;
            case 1:
                HeadAnimator.SetInteger("Reload", 1);
                break;
            case 2:
                HeadAnimator.SetInteger("Reload", 2);
                break;
            case 3:
                HeadAnimator.CrossFade("Insert", 0.2f, 0, 0);
                break;
        }
    }

    public void PlaySound(int id)
    {
        if (ASource == null) return;
        if (id == 0)
        {
            if (SwitchFireAudioClip == null) return;
            ASource.clip = SwitchFireAudioClip;
            ASource.Play();
        }
    }

    private bl_PlayerNetwork _Sync;
    public bl_PlayerNetwork PlayerSync
    {
        get
        {
            if (_Sync == null) { _Sync = transform.root.GetComponent<bl_PlayerNetwork>(); }
            return _Sync;
        }
    }

    [System.Serializable]
    public enum ChangeWeaponStyle
    {
        HideAndDraw,
        CounterStrike,
        HideCompletelyAndThenDraw,
    }
}