//////////////////////////////////////////////////////////////////////////////
// bl_ExplosionDamage.cs
//
// This contain the logic of the explosions
// determines the objectives that are punished,
// and calculates the precise damage
//                       LovattoStudio
//////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

public class bl_ExplosionDamage : bl_PhotonHelper
{

    public ExplosionType m_Type = ExplosionType.Normal;
    /// <summary>
    /// This is assigned auto
    /// </summary>
    public float explosionDamage = 50f;
    /// <summary>
    /// range of the explosion generates damage
    /// </summary>
    public float explosionRadius = 50f;
    /// <summary>
    /// the time fire particles disappear
    /// </summary>
    public float DisappearIn = 3f;
    public ShakerPresent shakerPresent;
    public string shakerKey = "explosion";

    public int WeaponID { get; set; }
    public bool isNetwork { get; set; }
    public bool isFromBot { get; set; }
    public int ActorViewID { get; set; }
    private string WeaponName { get; set; }
    private string BotName;

    private Team AITeam;
    private RaycastHit hitInfo;

    /// <summary>
    /// is not remote take damage
    /// </summary>
    void Start()
    {
        if (!isNetwork)
        {
            DoDamage();
            ApplyShake();
        }
        StartCoroutine(Init());
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetUp(BulletData data, int actorViewID)
    {
        isFromBot = false;
        isNetwork = data.isNetwork;
        WeaponID = data.WeaponID;
        WeaponName = data.WeaponName;
        ActorViewID = actorViewID;
        if(data.Damage > 0)
        {
            explosionDamage = data.Damage;
        }
    }

    public void AISetUp(int viewID, Team team, string botName)
    {
        isFromBot = true;
        ActorViewID = viewID;
        AITeam = team;
        BotName = botName;
    }

    /// <summary>
    /// applying impact damage from the explosion to enemies
    /// </summary>
    private void DoDamage()
    {
        if (m_Type == ExplosionType.Shake && !bl_GameData.Instance.ArriveKitsCauseDamage)
            return;

        List<Player> playersInRange = this.GetPlayersInRange();
        if (playersInRange != null && playersInRange.Count > 0)
        {
            foreach (Player player in playersInRange)
            {
                if (player != null)
                {
                    GameObject p = FindPhotonPlayer(player);
                    if (p != null)
                    {
                        if (!ExplosionCanHitTarget(p.transform.root, new Vector3(0, 0.6f, 0)) && !ExplosionCanHitTarget(p.transform.root, new Vector3(0, 0.15f, 0))) continue;//check if there is an obstacle between player and explosion

                        bl_PlayerHealthManager pdm = p.transform.root.GetComponent<bl_PlayerHealthManager>();

                        DamageData odi = new DamageData();
                        odi.Damage = CalculatePlayerDamage(p.transform, player);
                        odi.Direction = transform.position;
                        odi.From = (isFromBot) ? BotName : PhotonNetwork.LocalPlayer.NickName;
                        odi.isHeadShot = false;
                        odi.Cause = (isFromBot) ? DamageCause.Bot : DamageCause.Explosion;
                        odi.GunID = WeaponID;
                        odi.Actor = PhotonNetwork.LocalPlayer;

                        pdm.GetDamage(odi);
                    }
                    else
                    {
                        Debug.LogError("This Player " + player.NickName + " is not found");
                    }
                }
            }
        }
        Collider[] colls = Physics.OverlapSphere(transform.position, explosionRadius);
        List<string> Hited = new List<string>();
        foreach (Collider c in colls)
        {
            if (c.CompareTag("AI"))
            {
                if (Hited.Contains(c.transform.root.name)) continue;
                if (!ExplosionCanHitTarget(c.transform.root, new Vector3(0, 0.6f, 0)) && !ExplosionCanHitTarget(c.transform.root, new Vector3(0, 0.15f, 0))) continue;

                int damage = CalculatePlayerDamage(c.transform.root, null);
                Hited.Add(c.transform.root.name);
                Team t = (isFromBot) ? AITeam : PhotonNetwork.LocalPlayer.GetPlayerTeam();
                string wp = string.IsNullOrEmpty(WeaponName) ? "Grenade" : WeaponName;
                if (c.GetComponent<bl_AIShooterHealth>() != null && !isNetwork)
                {
                    c.GetComponent<bl_AIShooterHealth>().DoDamage(damage, wp, transform.position, ActorViewID, isFromBot, t, false, 0);
                }
                else if (c.GetComponent<bl_AIHitBox>() != null && !isNetwork)
                {
                    c.GetComponent<bl_AIHitBox>().DoDamage(damage, wp, transform.position, ActorViewID, isFromBot, t);
                }
            }
        }
    }

    /// <summary>
    /// When Explosion is local, and take player hit
    /// Send only shake movement
    /// </summary>
    void ApplyShake()
    {
        if (isMyInRange() == true)
        {
            bl_EventHandler.DoPlayerCameraShake(shakerPresent, "shakerKey");
        }
    }

    /// <summary>
    /// calculate the damage it generates, based on the distance
    /// between the player and the explosion
    /// </summary>
    private int CalculatePlayerDamage(Transform trans, Player p)
    {
        if (p != null)
        {
            if (!isOneTeamMode)
            {
                if (bl_GameData.Instance.SelfGrenadeDamage && p == PhotonNetwork.LocalPlayer)
                {

                }
                else
                {
                    if ((string)p.CustomProperties[PropertiesKeys.TeamKey] == myTeam)
                    {
                        return 0;
                    }
                }
            }
        }
        float distance = bl_UtilityHelper.Distance(transform.position, trans.position);
        return Mathf.Clamp((int)(explosionDamage * ((explosionRadius - distance) / explosionRadius)), 0, (int)explosionDamage);
    }

    private bool ExplosionCanHitTarget(Transform target, Vector3 offset)
    {
        bool result = false;
        Vector3 rhs = transform.position + offset;
        Vector3 normalized = (target.position + offset - rhs).normalized;
        if (Physics.Raycast(rhs, normalized, out hitInfo, explosionRadius))
        {
           if(hitInfo.transform.root.name == target.name) { return true; }
        }
        return result;
    }

    /// <summary>
    /// get players who are within the range of the explosion
    /// </summary>
    /// <returns></returns>
    private List<Player> GetPlayersInRange()
    {
        List<Player> list = new List<Player>();
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            GameObject player = FindPhotonPlayer(p);
            if (player == null)
                return null;

            float distance = bl_UtilityHelper.Distance(transform.position, player.transform.position);
            if (!isOneTeamMode)
            {
                if (isFromBot)
                {
                    if (p.GetPlayerTeam() != AITeam && (distance <= explosionRadius))
                    {
                        list.Add(p);
                    }
                }
                else
                {
                    if (p != PhotonNetwork.LocalPlayer)
                    {
                        if (p.GetPlayerTeam() != PhotonNetwork.LocalPlayer.GetPlayerTeam() && (distance <= explosionRadius))
                        {
                            list.Add(p);
                        }
                    }
                    else
                    {
                        if (bl_GameData.Instance.SelfGrenadeDamage)
                        {
                            if (distance <= explosionRadius)
                            {
                                list.Add(p);
                            }
                        }
                    }
                }
            }
            else
            {
                if (p != PhotonNetwork.LocalPlayer)
                {
                    if (distance <= explosionRadius)
                    {
                        list.Add(p);
                    }
                }
                else
                {
                    if (bl_GameData.Instance.SelfGrenadeDamage)
                    {
                        if (distance <= explosionRadius)
                        {
                            list.Add(p);
                        }
                    }
                }
            }
        }
        return list;
    }
    /// <summary>
    /// Calculate if player local in explosion radius
    /// </summary>
    /// <returns></returns>
    private bool isMyInRange()
    {
        GameObject p = bl_GameManager.Instance.LocalPlayer;

        if (p == null)
        {
            return false;
        }
        if ((bl_UtilityHelper.Distance(this.transform.position, p.transform.position) <= this.explosionRadius))
        {
            return true;
        }
        return false;

    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator Init()
    {
        yield return new WaitForSeconds(DisappearIn / 2);
        Destroy(gameObject);
    }

    [System.Serializable]
    public enum ExplosionType
    {
        Normal,
        Shake,
    }
}