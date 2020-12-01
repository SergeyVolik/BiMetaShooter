//////////////////////////////////////////////////////////////////////////////
// bl_PlayerHealthManager.cs
//
// this contains all the logic of the player health
// This is enabled locally or remotely
//                      Lovatto Studio
/////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable; //Replace default Hashtables with Photon hashtables
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class bl_PlayerHealthManager : bl_MonoBehaviour
{

    [HideInInspector] public bool DamageEnabled = true;
    [Header("Settings")]
    //currentKillStreak Player Health
    [Range(0, 100)] public float health = 100;
    [Range(1, 100)] public float maxHealth = 100;
    [Range(1, 10)] public float StartRegenerateIn = 4f;
    [Range(1, 5)] public float RegenerationSpeed = 3f;
    [Range(1, 10)] public float DeathIconShowTime = 5f;

    [Header("GUI")]
    public Texture2D DeathIcon;
    /// <summary>
    /// Color of UI when player health is low
    /// </summary>
    public Gradient HealthColorGradient;
    private Color CurColor = new Color(0, 0, 0);
    [Header("Shake")]
    public ShakerPresent damageShakerPresent;

    [Header("Effects")]
    public AudioClip[] HitsSound;
    [SerializeField] private AudioClip[] InjuredSounds;

    [Header("References")]
    public GameObject KillCamPrefab;
    public bl_BodyPartManager BodyManager;
    [SerializeField] private GameObject DeathIconPrefab;

    private Text HealthTextUI;
    private Image HealthBar;

    const string FallMethod = "FallDown";
    private CharacterController m_CharacterController;
    private bool dead = false;
    private string lastDamageGiverActor;
    private int ScorePerKill, ScorePerHeatShot;
    private bl_GameData GameData;
    private bl_DamageIndicator Indicator;
    private bl_PlayerNetwork PlayerSync;
    private bool HealthRegeneration = false;
    private float TimeToRegenerate = 4;
    private bl_GunManager GunManager;
    private bool isSuscribed = false;
    private int protecTime = 0;
    private int m_RepetingDamage = 1;
    private DamageData RepetingDamageInfo;
    private CanvasGroup DamageAlpha;
    private float damageAlphaValue = 0;
    private Team thisPlayerTeam = Team.None;

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        if (!PhotonNetwork.IsConnected) return;

        base.Awake();
        m_CharacterController = GetComponent<CharacterController>();
        GameData = bl_GameData.Instance;
        Indicator = GetComponent<bl_DamageIndicator>();
        PlayerSync = GetComponent<bl_PlayerNetwork>();
        GunManager = transform.GetComponentInChildren<bl_GunManager>(true);
        DamageAlpha = bl_UIReferences.Instance.PlayerUI.DamageAlpha;
        HealthRegeneration = GameData.HealthRegeneration;
        protecTime = bl_GameData.Instance.SpawnProtectedTime;
    }

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        if (!isConnected)
            return;

        health = maxHealth;
        thisPlayerTeam = (Team)photonView.InstantiationData[0];
        if (isMine)
        {
            bl_GameManager.isLocalAlive = true;
            gameObject.name = PhotonNetwork.NickName;
            HealthTextUI = bl_UIReferences.Instance.PlayerUI.HealthText;
            HealthBar = bl_UIReferences.Instance.PlayerUI.HealthBar;
            UpdateUI();
        }
        if (protecTime > 0) { InvokeRepeating("OnProtectCount", 1, 1); }
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        if (this.isMine)
        {
            bl_GameManager.LocalPlayerViewID = this.photonView.ViewID;
            bl_EventHandler.onPickUpItem += this.OnPickUp;
            bl_EventHandler.OnRoundEnd += this.OnRoundEnd;
            bl_EventHandler.OnDamage += this.GetDamage;
            bl_PhotonCallbacks.PlayerEnteredRoom += OnPhotonPlayerConnected;
            isSuscribed = true;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        if (isSuscribed)
        {
            bl_EventHandler.onPickUpItem -= this.OnPickUp;
            bl_EventHandler.OnRoundEnd -= this.OnRoundEnd;
            bl_EventHandler.OnDamage -= this.GetDamage;
            bl_PhotonCallbacks.PlayerEnteredRoom -= OnPhotonPlayerConnected;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (isMine)
        {
            if (damageAlphaValue > 0)
            {
                DamageUI();
            }
            else { DamageAlpha.alpha = 0; }
            if (HealthRegeneration)
            {
                RegenerateHealth();
            }
        }
    }

    /// <summary>
    /// Call this to make a new damage to the player
    /// </summary>
    public void GetDamage(DamageData e)
    {
        bool canDamage = true;
        if (!DamageEnabled)
        {
            //Fix: bots can't damage Master Client teammates.
            if (e.MFPSActor != null && e.MFPSActor.Team != thisPlayerTeam) { canDamage = true; }
            else canDamage = false;
        }
        if (!canDamage || isProtectionEnable || bl_GameManager.Instance.GameMatchState == MatchState.Waiting)
        {
            if (!isMine)
            {
                PlayerSync.m_PlayerAnimation.OnGetHit();
            }
            return;
        }
        photonView.RPC("SyncDamage", RpcTarget.AllBuffered, e.Damage, e.From, e.Cause, e.Direction, e.isHeadShot, e.GunID, PhotonNetwork.LocalPlayer);
    }

    /// <summary>
    /// Call this when Player Take Damage From fall impact
    /// </summary>
    public void GetFallDamage(int damage)
    {
        if (isProtectionEnable || bl_GameManager.Instance.GameMatchState == MatchState.Waiting)
            return;

        Vector3 downpos = transform.position - transform.TransformVector(new Vector3(0, 5, 1));
        photonView.RPC("SyncDamage", RpcTarget.AllBuffered, damage, PhotonNetwork.NickName, DamageCause.FallDamage, downpos, false, 103, PhotonNetwork.LocalPlayer);
    }

    /// <summary>
    /// Sync the Health of player
    /// </summary>
    [PunRPC]
    void SyncDamage(int damage, string killer, DamageCause cause, Vector3 m_direction, bool isHeatShot, int weaponID, Player m_sender)
    {
        if (dead || isProtectionEnable)
            return;

        if (DamageEnabled)
        {
            if (health >= 0)
            {
                if (isMine)
                {
                    damageAlphaValue += (damage + ((maxHealth - health) * (Time.deltaTime * Mathf.PI))) * 0.2f;
                    bl_EventHandler.DoPlayerCameraShake(damageShakerPresent, "damage");
                    if (Indicator != null)
                    {
                        Indicator.AttackFrom(m_direction);
                    }
                    TimeToRegenerate = StartRegenerateIn;
                }
                else
                {
                    if (m_sender != null)
                    {
                        if (m_sender.NickName == LocalName && cause != DamageCause.Bot)
                        {
                            bl_UCrosshair.Instance.OnHit();
                        }
                    }
                }
            }
            if (cause != DamageCause.FallDamage && cause != DamageCause.Fire)
            {
                if (HitsSound.Length > 0)//Audio effect of hit
                {
                    AudioSource.PlayClipAtPoint(HitsSound[Random.Range(0, HitsSound.Length)], transform.position, 1.0f);
                }
            }
            else
            {
                AudioSource.PlayClipAtPoint(InjuredSounds[Random.Range(0, InjuredSounds.Length)], transform.position, 1.0f);
            }

            lastDamageGiverActor = killer;
        }
        if (health > 0)
        {
            health -= damage;
            if (!isMine)
            {
                PlayerSync.m_PlayerAnimation.OnGetHit();
            }
            else
            {
                UpdateUI();
            }
        }

        if (health < 1)
        {
            health = 0.0f;
            if (isMine)
            {
                bl_GameManager.isLocalAlive = false;
                bl_EventHandler.PlayerLocalDeathEvent();
            }
            Die(lastDamageGiverActor, isHeatShot, cause, weaponID, m_direction, m_sender);
        }
    }

    /// <summary>
    /// Sync Health when pick up a med kit.
    /// </summary>
    [PunRPC]
    void PickUpHealth(float t_amount)
    {
        this.health = t_amount;
        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }

    /// <summary>
    /// Called This when player Die Logic
    /// </summary>
	void Die(string killer, bool isHeadshot, DamageCause cause, int gunID, Vector3 hitPos, Player sender)
    {
        dead = true;
        m_CharacterController.enabled = false;
        bl_GameManager.Instance.GetMFPSPlayer(gameObject.name).isAlive = false;
        bl_GunInfo gunInfo = bl_GameData.Instance.GetWeapon(gunID);
        bool isExplosion = gunInfo.Type == GunType.Grenade || gunInfo.Type == GunType.Launcher;

        if (!isMine)
        {
            BodyManager.Ragdolled(hitPos, isExplosion);// convert into ragdoll the remote player
        }
        else
        {
            Transform ngr = (bl_GameData.Instance.DropGunOnDeath) ? null : PlayerSync.NetGunsRoot;
            BodyManager.SetLocalRagdoll(hitPos, ngr, m_CharacterController.velocity, isExplosion);
        }
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        string weapon = cause.ToString();
        if (cause == DamageCause.Player || cause == DamageCause.Bot || cause == DamageCause.Explosion)
        {
            weapon = gunInfo.Name;
        }
        //Spawn ragdoll
        if (!isMine)// when player is not ours
        {
            if (lastDamageGiverActor == LocalName)
            {
                AddKill(isHeadshot, weapon, gunID);
            }
            if (!isOneTeamMode)
            {
                if (photonView.Owner.GetPlayerTeam() == PhotonNetwork.LocalPlayer.GetPlayerTeam())
                {
                    GameObject di = bl_ObjectPooling.Instance.Instantiate("deathicon", transform.position, transform.rotation);
                    di.GetComponent<bl_ClampIcon>().SetTempIcon(DeathIcon, DeathIconShowTime, 20);
                }
            }
        }
        else//when is local player
        {
            AddDeath();
            //show kill cam
            BodyManager.gameObject.name = "YOU";
            GameObject kc = Instantiate(KillCamPrefab, transform.position, transform.rotation) as GameObject;
            if (bl_GameData.Instance.killCameraType == bl_KillCam.KillCameraType.OrbitTarget)
            {
                kc.GetComponent<bl_KillCam>().SetTarget(sender, cause, killer, BodyManager.PelvisBone);
            }
            else if (bl_GameData.Instance.killCameraType == bl_KillCam.KillCameraType.ObserveDeath)
            {
                kc.GetComponent<bl_KillCam>().PositionedAndLookAt(transform);
            }
            bl_UIReferences.Instance.OnKillCam(true, killer, gunID);
            BodyManager.KillCameraCache = kc;
#if ELIM
            if(GetGameMode == GameMode.ELIM) { bl_Elimination.Instance.OnLocalDeath(kc.GetComponent<bl_KillCam>()); }
#endif
#if DM
            if (GetGameMode == GameMode.DM) { bl_Demolition.Instance.OnLocalDeath(kc.GetComponent<bl_KillCam>()); }
#endif

            if (killer == LocalName)
            {
#if LOCALIZATION
                if (cause == DamageCause.FallDamage)
                {
                    bl_KillFeed.Instance.SendTeamHighlightMessage(LocalName, bl_Localization.Instance.GetText(20), PhotonNetwork.LocalPlayer.GetPlayerTeam());
                }
                else
                {
                    bl_KillFeed.Instance.SendTeamHighlightMessage(LocalName, bl_Localization.Instance.GetText(19), PhotonNetwork.LocalPlayer.GetPlayerTeam());
                }
#else
                if (cause == DamageCause.FallDamage)
                {
                    bl_KillFeed.Instance.SendTeamHighlightMessage(LocalName, bl_GameTexts.DeathByFall, PhotonNetwork.LocalPlayer.GetPlayerTeam());
                }
                else
                {
                    bl_KillFeed.Instance.SendTeamHighlightMessage(LocalName, bl_GameTexts.CommittedSuicide, PhotonNetwork.LocalPlayer.GetPlayerTeam());
                }
#endif
            }
            if (bl_GameData.Instance.DropGunOnDeath)
            {
                GunManager.ThrwoCurrent(true, PlayerSync.NetGunsRoot.position + transform.forward);
            }
            if (cause == DamageCause.Bot)
            {
                bl_KillFeed.Instance.SendKillMessageEvent(killer, gameObject.name, gunID, PhotonNetwork.LocalPlayer.GetPlayerTeam(), isHeadshot);
                bl_AIMananger.Instance.SetBotKill(killer);
            }
            StartCoroutine(DestroyThis());
        }
    }

    /// <summary>
    /// when we get a new kill, synchronize and add points to the player
    /// </summary>
    public void AddKill(bool isHeadshot, string m_weapon, int gunID)
    {
        //send kill feed kill message
        bl_KillFeed.Instance.SendKillMessageEvent(PhotonNetwork.NickName, gameObject.name, gunID, PhotonNetwork.LocalPlayer.GetPlayerTeam(), isHeadshot);
        //Add a new kill and update information
        PhotonNetwork.LocalPlayer.PostKill(1);//Send a new kill
        //Add xp for score and update
        int score = (isHeadshot) ? GameData.ScoreReward.ScorePerKill + GameData.ScoreReward.ScorePerHeadShot : GameData.ScoreReward.ScorePerKill;

        //show an local notification for the kill
        KillInfo localKillInfo = new KillInfo();
        localKillInfo.Killer = lastDamageGiverActor;
        localKillInfo.Killed = gameObject.name;
        localKillInfo.byHeadShot = isHeadshot;
        localKillInfo.KillMethod = m_weapon;
        bl_EventHandler.FireLocalKillEvent(localKillInfo);

        //Send to update score to player
        PhotonNetwork.LocalPlayer.PostScore(score);

        bl_GameManager.Instance.OnLocalPlayerKill();

#if GR
        if (GetGameMode == GameMode.GR)
        {
            GunRace.GetNextGun();
        }
#endif
    }

    /// <summary>
    /// When Player take a new Death synchronize Die Point
    /// </summary>
    public void AddDeath()
    {
        PhotonNetwork.LocalPlayer.PostDeaths(1);
    }

    /// <summary>
    /// 
    /// </summary>
    public void DoRepetingDamage(int damage, int each, DamageData info = null)
    {
        m_RepetingDamage = damage;
        RepetingDamageInfo = info;
        InvokeRepeating("MakeDamageRepeting", 0, each);
    }

    /// <summary>
    /// 
    /// </summary>
    void MakeDamageRepeting()
    {
        DamageData info = new DamageData();
        info.Damage = m_RepetingDamage;
        if (RepetingDamageInfo != null)
        {
            info = RepetingDamageInfo;
            info.Damage = m_RepetingDamage;
        }
        else
        {
            info.Direction = Vector3.zero;
            info.Cause = DamageCause.Map;
        }
        GetDamage(info);
    }

    /// <summary>
    /// 
    /// </summary>
    public void CancelRepetingDamage()
    {
        CancelInvoke("MakeDamageRepeting");
    }

    /// <summary>
    /// 
    /// </summary>
    void UpdateUI()
    {
        float h = Mathf.Clamp(health, 0, 900);
        float deci = h * 0.01f;
        CurColor = HealthColorGradient.Evaluate(deci);
        if (HealthTextUI != null)
        {
            HealthTextUI.text = string.Concat(Mathf.FloorToInt(health), "<size=12>/", maxHealth, "</size>");
            HealthTextUI.color = CurColor;
        }
        if (HealthBar != null) { HealthBar.fillAmount = deci; HealthBar.color = CurColor; }
    }

    /// <summary>
    /// 
    /// </summary>
    void DamageUI()
    {
        DamageAlpha.alpha = Mathf.Lerp(DamageAlpha.alpha, damageAlphaValue, Time.deltaTime * 6);
        damageAlphaValue -= Time.deltaTime;
    }

    /// <summary>
    /// 
    /// </summary>
    private float nextHealthSend = 0;
    void RegenerateHealth()
    {
        if (health < maxHealth)
        {
            if (TimeToRegenerate <= 0)
            {
                health += Time.deltaTime * RegenerationSpeed;
            }
            else
            {
                TimeToRegenerate -= Time.deltaTime * 1.15f;
            }
            if (Time.time - nextHealthSend > 1)
            {
                nextHealthSend = Time.time + 1;
                photonView.RPC("PickUpHealth", RpcTarget.Others, health);
            }
            UpdateUI();
        }
    }

    /// <summary>
    /// Suicide player
    /// </summary>
    public void Suicide()
    {
        if (isMine && bl_GameManager.isLocalAlive)
        {
            DamageData e = new DamageData();
            e.Damage = 500;
            e.From = base.LocalName;
            e.Direction = transform.position;
            e.isHeadShot = false;
            GetDamage(e);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnProtectCount()
    {
        protecTime--;
        if (isMine)
        {
            bl_UIReferences.Instance.OnSpawnCount(protecTime);
        }
        if (protecTime <= 0)
        {
            CancelInvoke("OnProtectCount");
        }
    }
    private bool isProtectionEnable { get { return (protecTime > 0); } }

    IEnumerator DestroyThis()
    {
        yield return new WaitForSeconds(0.15f);
        PhotonNetwork.Destroy(this.gameObject);
    }

    /// <summary>
    /// This event is called when player pick up a med kit
    /// use PhotonTarget.OthersBuffered to save bandwidth
    /// </summary>
    /// <param name="amount"> amount for sum at current health</param>
    void OnPickUp(int amount)
    {
        if (photonView.IsMine)
        {
            float newHealth = health + amount;
            health = newHealth;
            if (health > maxHealth)
            {
                health = maxHealth;
            }
            UpdateUI();
            photonView.RPC("PickUpHealth", RpcTarget.OthersBuffered, newHealth);
        }
    }

    [PunRPC]
    void RpcSyncHealth(float _h, PhotonMessageInfo info)
    {
        if (info.photonView.ViewID == photonView.ViewID)
        {
            health = _h;
        }
    }

    public void OnPhotonPlayerConnected(Player newPlayer)
    {
        if (photonView.IsMine)
        {
            photonView.RPC("RpcSyncHealth", newPlayer, health);
        }
    }

    /// <summary>
    /// When round is end 
    /// desactive some functions
    /// </summary>
    void OnRoundEnd()
    {
        DamageEnabled = false;
    }

#if GR
    private bl_GunRace _gunRace = null;
    private bl_GunRace GunRace { get { if (_gunRace == null) { _gunRace = FindObjectOfType<bl_GunRace>(); } return _gunRace; } }
#endif
}