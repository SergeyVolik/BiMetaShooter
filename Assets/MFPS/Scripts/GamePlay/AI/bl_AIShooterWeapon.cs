using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class bl_AIShooterWeapon : bl_PhotonHelper
{
    [Header("Settings")]
    [LovattoToogle] public bool ForceFireWhenTargetClose = true;
    [Range(0, 5)] public int Grenades = 3;
    [SerializeField, Range(10, 100)] private float GrenadeSpeed = 50;
    [Range(10, 100)] public float MinumumDistanceForGranades = 20;

    [Header("Weapons")]
    public List<AIWeapon> m_AIWeapons = new List<AIWeapon>();

    [Header("References")]
    [SerializeField] private GameObject Grenade;
    [SerializeField] private Transform GrenadeFirePoint;
    [SerializeField] private AudioSource FireSource;

    private int bullets;
    private bool canFire = true;
    private float attackTime;
    private int FollowingShoots = 0;
    public bool isFiring { get; set; }
    private bl_AIShooterAgent AI;
    private Animator Anim;
    private GameObject bullet;
    private bl_ObjectPooling Pooling;
    private AIWeapon Weapon;
    private int WeaponID = -1;
    private BulletData m_BulletData = new BulletData();
#if UMM
    private bl_MiniMapItem miniMapItem;
#endif
    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        AI = GetComponent<bl_AIShooterAgent>();
        Anim = GetComponentInChildren<Animator>();
        Pooling = bl_ObjectPooling.Instance;
        bl_PhotonCallbacks.PlayerEnteredRoom += OnPhotonPlayerConnected;
#if UMM
        miniMapItem = GetComponent<bl_MiniMapItem>();
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        attackTime = Time.time;
        if (PhotonNetwork.IsMasterClient)
        {
            WeaponID = Random.Range(0, m_AIWeapons.Count);
            photonView.RPC("SyncAIWeapon", RpcTarget.All, WeaponID);
        }
    }

    private void OnDisable()
    {
        bl_PhotonCallbacks.PlayerEnteredRoom -= OnPhotonPlayerConnected;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Fire(FireReason fireReason = FireReason.Normal)
    {
        if (!canFire || AI.ObstacleBetweenTarget)
            return;
        if(ForceFireWhenTargetClose && AI.CachedTargetDistance < 10) { fireReason = FireReason.Forced; }
        if (fireReason == FireReason.Normal)
        {
            if (!AI.playerInFront)
                return;
        }
        if (Weapon == null) return;
        if(AI.Target != null && AI.Target.name.Contains("(die)"))
        {
            AI.KillTheTarget();
            return;
        }
        if (Time.time >= attackTime)
        {
            if (Grenades > 0 && AI.TargetDistance >= MinumumDistanceForGranades && FollowingShoots > 5)
            {
                if ((Random.Range(0, 200) > 175))
                {
                    StartCoroutine(ThrowGrenade(false, Vector3.zero, Vector3.zero));
                    attackTime = Time.time + 3.3f;
                    return;
                }
            }
            Anim.SetInteger("UpperState", 1);
            attackTime = (fireReason == FireReason.OnMove) ? Time.time + Random.Range(Weapon.AttackRate * 2, Weapon.AttackRate * 5) : Time.time + Weapon.AttackRate;
            bullet = Pooling.Instantiate(Weapon.BulletName, Weapon.FirePoint.position, transform.root.rotation);
            bullet.transform.LookAt(AI.TargetPosition);
            //build bullet data
            m_BulletData.Damage = Weapon.Damage;
            m_BulletData.isNetwork = false;
            m_BulletData.Position = transform.position;
            m_BulletData.WeaponID = Weapon.GunID;
            m_BulletData.Spread = 2f;
            m_BulletData.MaxSpread = 3f;
            m_BulletData.Speed = 300;
            m_BulletData.LifeTime = 10;
            m_BulletData.WeaponName = Weapon.Info.Name;
            m_BulletData.ActorViewID = photonView.ViewID;
            m_BulletData.MFPSActor = AI.BotMFPSActor;
            bullet.GetComponent<bl_Bullet>().SetUp(m_BulletData);
            bullet.GetComponent<bl_Bullet>().AISetUp(AI.AIName, photonView.ViewID, AI.AITeam);
            if (FireSource.enabled)
            {
                FireSource.pitch = Random.Range(0.85f, 1.1f);
                FireSource.clip = Weapon.FireAudio;
                FireSource.Play();
            }
            if (Weapon.MuzzleFlash != null) { Weapon.MuzzleFlash.Play(); }
            bullets--;
            FollowingShoots++;
            photonView.RPC("RpcFire", RpcTarget.Others, Weapon.FirePoint.position, AI.TargetPosition);
#if UMM
            ShowMiniMapItem();
#endif
            if (bullets <= 0)
            {
                canFire = false;
                StartCoroutine(Reload());
            }
            else
            {
                if (FollowingShoots > 5)
                {
                    if (Random.Range(0, 15) > 12)
                    {
                        attackTime += Random.Range(0.01f, 5);
                        FollowingShoots = 0;
                    }
                }
            }
            isFiring = true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnDeath()
    {
        StopAllCoroutines();
    }

#if UMM
    void ShowMiniMapItem()
    {
        if (AI.isTeamMate) return;
#if KSA
        if (bl_KillStreakHandler.Instance.activeAUVs > 0) return;
#endif
        if (miniMapItem != null && !miniMapItem.isTeamMateBot())
        {
            CancelInvoke("HideMiniMapItem");
            miniMapItem.ShowItem();
            Invoke("HideMiniMapItem", 0.25f);
        }
    }
    void HideMiniMapItem()
    {
        if (AI.isTeamMate) return;
        if (miniMapItem != null)
        {
            miniMapItem.HideItem();
        }
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    IEnumerator ThrowGrenade(bool network, Vector3 velocity, Vector3 forward)
    {
        Anim.SetInteger("UpperState", 2);
        Anim.Play("FireGrenade", 1, 0);
        attackTime = Time.time + Weapon.AttackRate;
        yield return new WaitForSeconds(0.2f);
        GameObject bullet = Instantiate(Grenade, GrenadeFirePoint.position, transform.root.rotation) as GameObject;

        m_BulletData.Damage = 100;
        m_BulletData.isNetwork = network;
        m_BulletData.Position = transform.position;
        m_BulletData.WeaponID = 3;
        m_BulletData.Spread = 2f;
        m_BulletData.MaxSpread = 3f;
        m_BulletData.Speed = GrenadeSpeed;
        m_BulletData.LifeTime = 5;
        m_BulletData.WeaponName = "Grenade";
        bullet.GetComponent<bl_Projectile>().SetUp(m_BulletData);
        bullet.GetComponent<bl_Projectile>().AISetUp(photonView.ViewID, AI.AITeam, AI.AIName);
        if (!network)
        {
            Rigidbody r = bullet.GetComponent<Rigidbody>();
            velocity = GetVelocity(AI.TargetPosition);
            r.velocity = velocity;
            r.AddRelativeTorque(Vector3.right * -5500.0f);
            forward = AI.TargetPosition - r.transform.position;
            r.transform.forward = forward;
            Grenades--;
            photonView.RPC("FireGrenadeRPC", RpcTarget.Others, velocity, forward);
        }
        else
        {
            Rigidbody r = bullet.GetComponent<Rigidbody>();
            r.velocity = velocity;
            r.AddRelativeTorque(Vector3.right * -5500.0f);
            r.transform.forward = forward;
        }
#if UMM
        ShowMiniMapItem();
#endif
    }

    [PunRPC]
    void FireGrenadeRPC(Vector3 velocity, Vector3 forward)
    {
        StartCoroutine(ThrowGrenade(true, velocity, forward));
    }

    private Vector3 GetVelocity(Vector3 target)
    {
        Vector3 velocity = Vector3.zero;
        Vector3 toTarget = target - transform.position;
        float speed = 15;
        // Set up the terms we need to solve the quadratic equations.
        float gSquared = Physics.gravity.sqrMagnitude;
        float b = speed * speed + Vector3.Dot(toTarget, Physics.gravity);
        float discriminant = b * b - gSquared * toTarget.sqrMagnitude;

        // Check whether the target is reachable at max speed or less.
        if (discriminant < 0)
        {
            velocity = toTarget;
            velocity.y = 0;
            velocity.Normalize();
            velocity.y = 0.7f;

            Debug.DrawRay(transform.position, velocity * 3.0f, Color.blue);

            velocity *= speed;
            return velocity;
        }

        float discRoot = Mathf.Sqrt(discriminant);

        // Highest shot with the given max speed:
        float T_max = Mathf.Sqrt((b + discRoot) * 2f / gSquared);

        float T = 0;
        T = T_max;


        // Convert from time-to-hit to a launch velocity:
        velocity = toTarget / T - Physics.gravity * T / 2f;

        return velocity;
    }

    [PunRPC]
    void RpcFire(Vector3 pos, Vector3 look)
    {
        if (Weapon == null) return;
        Anim.SetInteger("UpperState", 1);
        bullet = Pooling.Instantiate("bullet", pos, Quaternion.identity);
        bullet.transform.LookAt(look);

        m_BulletData.Damage = 0;
        m_BulletData.isNetwork = true;
        m_BulletData.Position = transform.position;
        m_BulletData.WeaponID = Weapon.GunID;
        m_BulletData.Spread = 2f;
        m_BulletData.MaxSpread = 3f;
        m_BulletData.Speed = 200;
        m_BulletData.LifeTime = 10;
        m_BulletData.WeaponName = Weapon.Info.Name;
        m_BulletData.ActorViewID = photonView.ViewID;
        m_BulletData.MFPSActor = AI.BotMFPSActor;
        bullet.GetComponent<bl_Bullet>().SetUp(m_BulletData);
        FireSource.pitch = Random.Range(0.85f, 1.1f);
        FireSource.clip = Weapon.FireAudio;
        FireSource.Play();
        if (Weapon.MuzzleFlash != null) { Weapon.MuzzleFlash.Play(); }
#if UMM
        ShowMiniMapItem();
#endif
    }

    [PunRPC]
    void SyncAIWeapon(int ID)
    {
        WeaponID = ID;
        Weapon = m_AIWeapons[ID];
        foreach (AIWeapon item in m_AIWeapons)
        {
            item.WeaponObject.SetActive(false);
        }
        bullets = Weapon.Bullets;
        Weapon.WeaponObject.SetActive(true);
        Anim.SetInteger("GunType", (int)Weapon.Info.Type);
    }

    IEnumerator Reload()
    {
        photonView.RPC("RpcReload", RpcTarget.Others);
        yield return new WaitForSeconds(0.25f);
        Anim.SetInteger("UpperState", 2);
        yield return StartCoroutine(PlayReloadSound());
        Anim.SetInteger("UpperState", 0);
        bullets = Weapon.Bullets;
        canFire = true;
    }

    [PunRPC]
    IEnumerator RpcReload()
    {
        Anim.SetInteger("UpperState", 2);
        yield return StartCoroutine(PlayReloadSound());
        Anim.SetInteger("UpperState", 0);
    }

    /// <summary>
    /// 
    /// </summary>
    IEnumerator PlayReloadSound()
    {
        for (int i = 0; i < Weapon.ReloadAudio.Length; i++)
        {
            FireSource.clip = Weapon.ReloadAudio[i];
            FireSource.Play();
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void OnPhotonPlayerConnected(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient && WeaponID != -1)
        {
            photonView.RPC("SyncAIWeapon", newPlayer, WeaponID);
        }
    }

    public Transform FirePoint
    {
        get
        {
            if (Weapon != null)
            {
                return Weapon.FirePoint;
            }
            return transform;
        }
    }

    public enum FireReason
    {
        Normal,
        OnMove,
        Forced,
    }

    [System.Serializable]
    public class AIWeapon
    {
        [Header("Info")]
        public string Name;
        [GunID] public int GunID;
        [Range(1, 60)] public int Bullets = 30;
        [Range(0.01f, 2)] public float AttackRate = 3;
        [Range(1, 100)] public float Damage = 20; // The damage AI give
        public string BulletName = "Bullet";
        [Header("References")]
        public GameObject WeaponObject;
        public Transform FirePoint;
        public ParticleSystem MuzzleFlash;
        public AudioClip FireAudio;
        public AudioClip[] ReloadAudio;

        public bl_GunInfo Info
        {
            get
            {
                return bl_GameData.Instance.GetWeapon(GunID);
            }
        }
    }
}