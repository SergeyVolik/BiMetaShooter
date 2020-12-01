using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class bl_AIShooterHealth : bl_PhotonHelper
{

    [Range(10, 500)] public int Health = 100;

    [Header("References")]
    public Texture2D DeathIcon;

    private bl_AIShooterAgent Agent;
    private bl_AIMananger AIManager;
    private int LastActorEnemy = -1;
    private bl_AIAnimation AIAnim;
    private int m_RepetingDamage = 1;
    private DamageData RepetingDamageInfo;
    public int lastHitBoxHitted { get; set; }

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        Agent = GetComponent<bl_AIShooterAgent>();
        AIManager = bl_AIMananger.Instance;
        AIAnim = GetComponentInChildren<bl_AIAnimation>();
    }
    /// <summary>
    /// 
    /// </summary>
    public void DoDamage(int damage, string wn, Vector3 direction, int vi, bool fromBot, Team team, bool ishead, int hitBoxID)
    {
        if (Agent.death)
            return;

        if (GetGameMode == GameMode.TDM)
        {
            if (team == Agent.AITeam) return;
        }

        photonView.RPC("RpcDoDamage", RpcTarget.All, damage, wn, direction, vi, fromBot, ishead, hitBoxID);
    }

    [PunRPC]
    void RpcDoDamage(int damage, string weaponName, Vector3 direction, int viewID, bool fromBot, bool ishead, int hitBoxID)
    {
        if (Agent.death)
            return;

        Health -= damage;
        if (LastActorEnemy != viewID)
        {
            Agent.personal = false;
        }
        lastHitBoxHitted = hitBoxID;
        LastActorEnemy = viewID;

        if (PhotonNetwork.IsMasterClient)
        {
            Agent.OnGetHit(direction);
        }
        if (viewID == bl_GameManager.LocalPlayerViewID)//if was me that make damage
        {
            bl_UCrosshair.Instance.OnHit();
        }

        if (Health > 0)
        {
            Transform t = bl_GameManager.Instance.FindActor(viewID);
            if (t != null && !t.name.Contains("(die)"))
            {
                if (Agent.Target == null)
                {
                    Agent.personal = true;
                    Agent.Target = t;
                }
                else
                {
                    if (t != Agent.Target)
                    {
                        float cd = bl_UtilityHelper.Distance(transform.position, Agent.Target.position);
                        float od = bl_UtilityHelper.Distance(transform.position, t.position);
                        if (od < cd && (cd - od) > 7)
                        {
                            Agent.personal = true;
                            Agent.Target = t;
                        }
                    }
                }
            }
            AIAnim.OnGetHit();
        }
        else
        {
            Die(viewID, fromBot, ishead, weaponName, direction);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void Die(int viewID, bool fromBot, bool ishead, string weaponName, Vector3 direction)
    {
        Agent.death = true;
        Agent.OnDeath();
        gameObject.name += " (die)";
        Agent.AimTarget.name += " (die)";
        Agent.AIWeapon.OnDeath();
        Agent.enabled = false;
        Agent.Agent.isStopped = true;
        GetComponent<bl_NamePlateDrawer>().enabled = false;
        //update the MFPSPlayer data
        MFPSPlayer player = bl_GameManager.Instance.GetMFPSPlayer(Agent.AIName);
        if(player != null)
        player.isAlive = false;

        bl_AIShooterAgent killerBot = null;
        if (viewID == bl_GameManager.LocalPlayerViewID && !fromBot)//if was me that kill this bot
        {
            Team team = PhotonNetwork.LocalPlayer.GetPlayerTeam();
            //send kill feed message
            int gunID = bl_GameData.Instance.GetWeaponID(weaponName);
            if (weaponName.Contains("cmd:") || gunID == -1)
            {
                weaponName = weaponName.Replace("cmd:", "");
                gunID = -(bl_KillFeed.Instance.GetCustomIconIndex(weaponName));
            }
            bl_KillFeed.Instance.SendKillMessageEvent(LocalName, Agent.AIName, gunID, team, ishead);

            //Add a new kill and update information
            PhotonNetwork.LocalPlayer.PostKill(1);//Send a new kill

            int score;
            //If heat shot will give you double experience
            if (ishead)
            {
                bl_GameManager.Instance.Headshots++;
                score = bl_GameData.Instance.ScoreReward.ScorePerKill + bl_GameData.Instance.ScoreReward.ScorePerHeadShot;
            }
            else
            {
                score = bl_GameData.Instance.ScoreReward.ScorePerKill;
            }

            //Send to update score to player
            PhotonNetwork.LocalPlayer.PostScore(score);

            //show an local notification for the kill
            KillInfo localKillInfo = new KillInfo();
            localKillInfo.Killer = PhotonNetwork.LocalPlayer.NickName;
            localKillInfo.Killed = string.IsNullOrEmpty(Agent.AIName) ? gameObject.name.Replace("(die)", "") : Agent.AIName;
            localKillInfo.byHeadShot = ishead;
            localKillInfo.KillMethod = weaponName;
            bl_EventHandler.FireLocalKillEvent(localKillInfo);

            //update team score
            bl_GameManager.Instance.SetPoint(1, GameMode.TDM, team);
        }
        else if (fromBot)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonView p = PhotonView.Find(viewID);
                bl_AIShooterAgent bot = null;
                string killer = "Unknown";
                if (p != null)
                {
                    bot = p.GetComponent<bl_AIShooterAgent>();//killer bot
                    killer = bot.AIName;
                    if (string.IsNullOrEmpty(killer)) { killer = p.gameObject.name.Replace(" (die)", ""); }
                    //update bot stats
                    AIManager.SetBotKill(killer);
                }

                //send kill feed message
                int gunID = bl_GameData.Instance.GetWeaponID(weaponName);
                bl_KillFeed.Instance.SendKillMessageEvent(killer, Agent.AIName, gunID, bot.AITeam, ishead);

                if (bot != null)
                {
                    bot.KillTheTarget();
                    killerBot = bot;
                }
                else
                {
                    Debug.Log("Bot can't be found");
                }
            }
        }//else, (if other player kill this bot) -> do nothing.

        AIManager.SetBotDeath(Agent.AIName);
        if (PhotonNetwork.IsMasterClient)
        {
            if (!isOneTeamMode)
            {
                if (Agent.AITeam == PhotonNetwork.LocalPlayer.GetPlayerTeam())
                {
                    GameObject di = bl_ObjectPooling.Instance.Instantiate("deathicon", transform.position, transform.rotation);
                    di.GetComponent<bl_ClampIcon>().SetTempIcon(DeathIcon, 5, 20);
                }
            }
            AIManager.OnBotDeath(Agent, killerBot);
        }
        var deathData = bl_UtilityHelper.CreatePhotonHashTable();
        deathData.Add("direction", direction);
        if (weaponName.Contains("Grenade"))
        {
            deathData.Add("explosion", true);
        }
        this.photonView.RPC("BotDestroyRpc", RpcTarget.AllBuffered, deathData);//callback is in bl_AIShooterAgent.cs
    }

    public void DestroyBot()
    {
        var deathData = bl_UtilityHelper.CreatePhotonHashTable();
        deathData.Add("instant", true);
        this.photonView.RPC("BotDestroyRpc", RpcTarget.AllBuffered, deathData);//callback is in bl_AIShooterAgent.cs
    }

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
        DoDamage((int)info.Damage, "[Burn]", info.Direction, bl_GameManager.LocalPlayerViewID, false, PhotonNetwork.LocalPlayer.GetPlayerTeam(), false, 0);
    }

    public void CancelRepetingDamage()
    {
        CancelInvoke("MakeDamageRepeting");
    }

    [PunRPC]
    void RpcSync(int _health)
    {
        Health = _health;
    }
}