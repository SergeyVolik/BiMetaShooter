using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable; //Replace default Hashtables with Photon hashtables
using Photon.Pun;
using Photon.Realtime;

public class bl_AIMananger : bl_PhotonHelper
{
    private int NumberOfBots = 5;
    private List<bl_AIShooterAgent> AllBots = new List<bl_AIShooterAgent>();
    [HideInInspector] public List<Transform> AllBotsTransforms = new List<Transform>();
    public List<BotsStats> BotsStatistics = new List<BotsStats>();

    private bl_GameManager GameManager;
    [SerializeField] private List<PlayersSlots> Team1PlayersSlots = new List<PlayersSlots>();
    [SerializeField] private List<PlayersSlots> Team2PlayersSlots = new List<PlayersSlots>();

    public bool BotsActive { get; set; }
    private List<string> BotsNames = new List<string>();
    private List<bl_AIShooterAgent> SpawningBots = new List<bl_AIShooterAgent>();

    public delegate void EEvent(List<BotsStats> stats);
    public static EEvent OnMaterStatsReceived;
    public delegate void StatEvent(BotsStats stat);
    public static StatEvent OnBotStatUpdate;
    [HideInInspector]public bool hasMasterInfo = false;
    private bool isMasterAlredyInTeam = false;

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        GameManager = FindObjectOfType<bl_GameManager>();
        BotsNames.AddRange(bl_GameTexts.RandomNames);
        bl_PhotonCallbacks.PlayerPropertiesUpdate += OnPhotonPlayerPropertiesChanged;
        bl_PhotonCallbacks.PlayerEnteredRoom += OnPlayerEnter;
        bl_PhotonCallbacks.MasterClientSwitched += OnMasterClientSwitched;
        if (!PhotonNetwork.IsConnected)
            return;

        BotsActive = (bool)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.WithBotsKey];
        NumberOfBots = PhotonNetwork.CurrentRoom.MaxPlayers;
    }

    /// <summary>
    /// 
    /// </summary>
    private void Start()
    {
        FirstSpawn();
        if(bl_GameData.Instance.lobbyJoinMethod == LobbyJoinMethod.WaitingRoom && PhotonNetwork.IsMasterClient)
        {
            this.InvokeAfter(2, SyncBotsDataToAllOthers);
        }
    }
    
    void FirstSpawn()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SetUpSlots();
            if (BotsActive)
            {
                if (isOneTeamMode)
                {
                    for (int i = 0; i < NumberOfBots; i++)
                    {
                        SpawnBot();
                    }
                }
                else
                {
                    int half = NumberOfBots / 2;
                    for (int i = 0; i < half; i++)
                    {
                        SpawnBot(null, Team.Team1);
                    }
                    for (int i = 0; i < half; i++)
                    {
                        SpawnBot(null, Team.Team2);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnDisable()
    {
        bl_PhotonCallbacks.PlayerPropertiesUpdate -= OnPhotonPlayerPropertiesChanged;
        bl_PhotonCallbacks.PlayerEnteredRoom -= OnPlayerEnter;
        bl_PhotonCallbacks.MasterClientSwitched -= OnMasterClientSwitched;
    }

    void OnPlayerEnter(Player player)
    {
        //cause bots statistics are not sync by Hashtables as player data do we need sync it by RPC
        //so for sync it just one time (after will be update by the local client) we send it when a new player enter (only to the new player)
        if (PhotonNetwork.IsMasterClient && player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            //so first we recollect all the stats from the master client and join it in a string line
            string line = GetCompiledBotsData();
            //and send to the new player so him can have the data and update locally.
            photonView.RPC(nameof(SyncAllBotsStats), player, line);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void SyncBotsDataToAllOthers()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player[] players = PhotonNetwork.PlayerList;
        string line = GetCompiledBotsData();
        //and send to the new player so him can have the data and update locally.
        photonView.RPC(nameof(SyncAllBotsStats), RpcTarget.Others, line);
    }

    public string GetCompiledBotsData()
    {
        //so first we recollect all the stats from the master client and join it in a string line
        string line = string.Empty;
        for (int i = 0; i < BotsStatistics.Count; i++)
        {
            BotsStats b = BotsStatistics[i];
            line += string.Format("{0},{1},{2},{3},{4},{5}|", b.Name, b.Kills, b.Deaths, b.Score, (int)b.Team, b.ViewID);
        }
        return line;
    }

    /// <summary>
    /// 
    /// </summary>
    void SetUpSlots()
    {
        if (!isOneTeamMode)
        {
            int ptp = NumberOfBots / 2;
            for (int i = 0; i < ptp; i++)
            {
                PlayersSlots s = new PlayersSlots();
                s.Bot = string.Empty;
                s.Player = string.Empty;
                Team1PlayersSlots.Add(s);
            }
            for (int i = 0; i < ptp; i++)
            {
                PlayersSlots s = new PlayersSlots();
                s.Bot = string.Empty;
                s.Player = string.Empty;
                Team2PlayersSlots.Add(s);
            }
        }
    }

    public void SpawnBot(bl_AIShooterAgent agent = null, Team _team = Team.None)
    {
        Transform t = GameManager.GetAnSpawnPoint;
        string AiName = bl_GameData.Instance.BotTeam1.name;
        if (agent != null)//if is a already instanced bot
        {
            AiName = (agent.AITeam == Team.Team2) ? bl_GameData.Instance.BotTeam2.name : bl_GameData.Instance.BotTeam1.name;
            if (!isOneTeamMode)//if team mode, spawn bots in the respective team spawn points.
            {
                if (agent.AITeam == Team.None) { Debug.LogError("This bot has not team"); }

                if (CheckPlayerSlot(agent, agent.AITeam))
                {
                    t = GameManager.GetAnTeamSpawnPoint(agent.AITeam, true);
                }
                else
                {
                    int ind = BotsStatistics.FindIndex(x => x.Name == agent.AIName);
                    if (ind != -1 && ind <= BotsStatistics.Count - 1)
                    {                      
                        BotsStatistics.RemoveAt(ind);
                    }
                    return;
                }
            }
        }
        else
        {
            AiName = (_team == Team.Team2) ? bl_GameData.Instance.BotTeam2.name : bl_GameData.Instance.BotTeam1.name;
            if (!isOneTeamMode)//if team mode, spawn bots in the respective team spawn points.
            {
                t = GameManager.GetAnTeamSpawnPoint(_team, true);
            }
        }

        int rbn = Random.Range(0, BotsNames.Count);
        string AIName = agent == null ? "BOT " + BotsNames[rbn] : agent.AIName;
        Team AITeam = agent == null ? _team : agent.AITeam;

        object[] botEssentialData = new object[] { AIName, AITeam };
        //use InstantiateSceneObject to make the bots by controlled by Master Client but not destroy them when MC leave the room.
        GameObject bot = PhotonNetwork.InstantiateSceneObject(AiName, t.position, t.rotation, 0, botEssentialData);

        bl_AIShooterAgent newAgent = bot.GetComponent<bl_AIShooterAgent>();
        if (agent != null)
        {
            newAgent.AIName = agent.AIName;
            newAgent.AITeam = agent.AITeam;
            photonView.RPC("SyncBotStat", RpcTarget.Others, agent.AIName, bot.GetComponent<PhotonView>().ViewID, (byte)3);
        }
        else
        {
            newAgent.AIName = AIName;
            newAgent.AITeam = _team;
            BotsNames.RemoveAt(rbn);
            //insert bot stats
            BotsStats bs = new BotsStats();
            bs.Name = newAgent.AIName;
            bs.Team = _team;
            bs.ViewID = bot.GetComponent<PhotonView>().ViewID;
            BotsStatistics.Add(bs);
            CheckPlayerSlot(newAgent, _team);
        }
        newAgent.Init();

        //Build Player Data
        MFPSPlayer playerData = new MFPSPlayer()
        {
            Name = newAgent.AIName,
            Team = newAgent.AITeam,
            Actor = newAgent.transform,
            AimPosition = newAgent.AimTarget,
            isRealPlayer = false,
            isAlive = true,
        };

        bl_EventHandler.OnRemoteActorChange(newAgent.AIName, playerData, true);
        AllBots.Add(newAgent);
        AllBotsTransforms.Add(newAgent.AimTarget);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    bool CheckPlayerSlot(bl_AIShooterAgent agent, Team team)
    {
        if (!isOneTeamMode)
        {
            if (team == Team.Team2)
            {
                bool already = Team2PlayersSlots.Exists(x => x.Bot == agent.AIName);
                if (already)
                {
                    return true;
                }
                else
                {
                    if (hasSpaceInTeamForBot(Team.Team2))
                    {
                        int index = Team2PlayersSlots.FindIndex(x => x.Player == string.Empty && x.Bot == string.Empty);
                        Team2PlayersSlots[index].Bot = agent.AIName;
                        return true;
                    }
                    else { return false; }
                }
            }
            else if(team == Team.Team1)
            {
                bool already = Team1PlayersSlots.Exists(x => x.Bot == agent.AIName);
                if (already)
                {
                    return true;
                }
                else
                {
                    if (hasSpaceInTeamForBot(Team.Team1))
                    {
                        int index = Team1PlayersSlots.FindIndex(x => x.Player == string.Empty && x.Bot == string.Empty);
                        Team1PlayersSlots[index].Bot = agent.AIName;
                        return true;
                    }
                    else { return false; }
                }
            }
        }
        return true;
    }

    private bool hasSpaceInTeam(Team team)
    {
        if (team == Team.Team2)
        {
            return Team2PlayersSlots.Exists(x => x.Player == string.Empty);
        }
        else
        {
            return Team1PlayersSlots.Exists(x => x.Player == string.Empty);
        }
    }

    private bool hasSpaceInTeamForBot(Team team)
    {
        if (team == Team.Team2)
        {
            return Team2PlayersSlots.Exists(x => x.Player == string.Empty && x.Bot == string.Empty);
        }
        else
        {
            return Team1PlayersSlots.Exists(x => x.Player == string.Empty && x.Bot == string.Empty);
        }
    }

    public void OnBotDeath(bl_AIShooterAgent agent, bl_AIShooterAgent killer)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        AllBots.Remove(agent);
        AllBotsTransforms.Remove(agent.AimTarget);
        for (int i = 0; i < AllBots.Count; i++)
        {
            AllBots[i].CheckTargets();
        }

        SpawningBots.Add(agent);
        Invoke("SpawnPendingBot", bl_GameData.Instance.PlayerRespawnTime);
    }

    void SpawnPendingBot()
    {
        if (SpawningBots[0] != null)
        {
            SpawnBot(SpawningBots[0]);
            Destroy(SpawningBots[0].gameObject);
            SpawningBots.RemoveAt(0);
        }
    }

    public List<Transform> GetOtherBots(Transform bot, Team _team)
    {
        List<Transform> all = new List<Transform>();
        if (isOneTeamMode)
        {
            all.AddRange(AllBotsTransforms);
            for (int i = 0; i < all.Count; i++)
            {
                if (AllBotsTransforms[i] == null) continue;
                if (all[i].transform.root.name.Contains("(die)") || all[i].transform.root == bot.root)
                {
                    all.RemoveAt(i);
                }
            }
        }
        else //if TDM game mode
        {
            for (int i = 0; i < AllBotsTransforms.Count; i++)
            {
                if (AllBotsTransforms[i] == null) continue;

                Transform t = AllBotsTransforms[i].root;
                bl_AIShooterAgent asa = t.GetComponent<bl_AIShooterAgent>();
                if (t.name.Contains("(die)") || asa.isDeath) continue;

                if (asa.AITeam != _team && asa.AITeam != Team.None && t != bot.root)
                {
                    all.Add(AllBotsTransforms[i]);
                }
            }
        }
        if (all.Contains(bot))
        {
            all.Remove(bot);
        }
        return all;
    }

    public void SetBotKill(string killer)
    {
        int index = BotsStatistics.FindIndex(x => x.Name == killer);
        if (index <= -1) return;

        BotsStatistics[index].Kills++;
        BotsStatistics[index].Score += bl_GameData.Instance.ScoreReward.ScorePerKill;
        photonView.RPC("SyncBotStat", RpcTarget.Others, BotsStatistics[index].Name, 0, (byte)0);

        bl_GameManager.Instance.SetPoint(1, GameMode.FFA, Team.All);
        bl_GameManager.Instance.SetPoint(1, GameMode.TDM, BotsStatistics[index].Team);
    }

    public void SetBotDeath(string killed)
    {
        int index = BotsStatistics.FindIndex(x => x.Name == killed);
        if (index <= -1) return;

        BotsStatistics[index].Deaths++;
        photonView.RPC("SyncBotStat", RpcTarget.Others, BotsStatistics[index].Name, 0, (byte)1);
    }

    /// <summary>
    /// 
    /// </summary>
    public void UpdateBotView(bl_AIShooterAgent bot, int viewID)
    {
        if(BotsStatistics.Exists(x => x.Name == bot.AIName))
        {
            BotsStatistics.Find(x => x.Name == bot.AIName).ViewID = viewID;
        }
    }

    public bl_AIShooterAgent GetBot(int viewID)
    {
        foreach (bl_AIShooterAgent agent in AllBots)
        {
            if (agent.photonView.ViewID == viewID)
            {
                return agent;
            }
        }
        return null;
    }

    public void OnPhotonPlayerPropertiesChanged(Player player, Hashtable changedProps)
    {
        if (isOneTeamMode || !BotsActive)
            return;

        if (changedProps.ContainsKey(PropertiesKeys.TeamKey))
        {
           string remplaceBot = string.Empty;
            if ((string)changedProps[PropertiesKeys.TeamKey] == Team.Team2.ToString())
            {
                if (Team2PlayersSlots.Exists(x => x.Player == player.NickName)) return;
                int index = Team2PlayersSlots.FindIndex(x => x.Player == string.Empty);
                if (index != -1)
                {
                    remplaceBot = Team2PlayersSlots[index].Bot;
                    DeleteBot(remplaceBot);
                    Team2PlayersSlots[index].Player = player.NickName;
                    Team2PlayersSlots[index].Bot = string.Empty;
                }
            }
            else if((string)changedProps[PropertiesKeys.TeamKey] == Team.Team1.ToString())
            {
                if (Team1PlayersSlots.Exists(x => x.Player == player.NickName)) return;
                int index = Team1PlayersSlots.FindIndex(x => x.Player == string.Empty);
                if (index != -1)
                {
                    remplaceBot = Team1PlayersSlots[index].Bot;
                    DeleteBot(remplaceBot);
                    Team1PlayersSlots[index].Player = player.NickName;
                    Team1PlayersSlots[index].Bot = string.Empty;
                }
            }
            //remove the bot that the master client replace
            if(player.IsMasterClient && PhotonNetwork.IsMasterClient && !isMasterAlredyInTeam && !string.IsNullOrEmpty(remplaceBot))
            {
                bl_AIShooterAgent bot = AllBots.Find(x => x.AIName == remplaceBot);
                if(bot != null)
                {
                    PhotonView bv = bot.GetComponent<PhotonView>();
                    bot.AIHealth.DestroyBot();//destroy on remote clients
                    AllBots.Remove(bot);
                    AllBotsTransforms.Remove(bot.AimTarget);
                    PhotonNetwork.Destroy(bv.gameObject);
                }
                isMasterAlredyInTeam = true;
            }
        }
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        //if the new master client is the local client
        if (newMasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            SetUpSlots();
            //since bots where not collected on the new master client, lets take them manually
            bl_AIShooterAgent[] allBots = FindObjectsOfType<bl_AIShooterAgent>();
            foreach(var bot in allBots)
            {
                if (bot.isDeath)//if the bot was death when master client leave the game
                {
                    //invoke the spawn in the new client
                    SpawningBots.Add(bot);
                    Invoke("SpawnPendingBot", bl_GameData.Instance.PlayerRespawnTime);
                    continue;
                }
                AllBots.Add(bot);
                AllBotsTransforms.Add(bot.transform);
                bot.Init();
            }
            Debug.Log("Bots data has been build in new Master Client");
        }
    }

    void DeleteBot(string Name)
    {
        if (BotsStatistics.Exists(x => x.Name == Name))
        {
            int bi = BotsStatistics.FindIndex(x => x.Name == Name);
            BotsStatistics.RemoveAt(bi);
            bl_UIReferences.Instance.RemoveUIBot(Name);
            photonView.RPC("SyncBotStat", RpcTarget.Others, Name, 0, (byte)2);
        }
    }

    [PunRPC]
    public void SyncBotStat(string botName, int value, byte cmd)
    {
        BotsStats bs = BotsStatistics.Find(x => x.Name == botName);
        if (bs == null) return;
        if (cmd == 0)//add kill
        {
            bs.Kills++;
            bs.Score += bl_GameData.Instance.ScoreReward.ScorePerKill;
        }
        else if (cmd == 1)//death
        {
            bs.Deaths++;
        }
        else if (cmd == 2)//remove bot
        {
            bl_UIReferences.Instance.RemoveUIBot(bs.Name);
            BotsStatistics.Remove(bs);
        }
        else if (cmd == 3)//update view id
        {
            bs.ViewID = value;
            if (OnBotStatUpdate != null) { OnBotStatUpdate(bs); }
        }
    }

    [PunRPC]
    void SyncAllBotsStats(string stats)
    {
        BotsStatistics.Clear();
        string[] split = stats.Split("|"[0]);
        for (int i = 0; i < split.Length; i++)
        {
            if (string.IsNullOrEmpty(split[i])) continue;
            string[] info = split[i].Split(","[0]);
            BotsStats bs = new BotsStats();
            bs.Name = info[0];
            bs.Kills = int.Parse(info[1]);
            bs.Deaths = int.Parse(info[2]);
            bs.Score = int.Parse(info[3]);
            bs.Team = (Team)int.Parse(info[4]);
            bs.ViewID = int.Parse(info[5]);
            BotsStatistics.Add(bs);
        }
        Debug.Log("Received bot info count: " + BotsStatistics.Count);
        if (OnMaterStatsReceived != null)
        {
            OnMaterStatsReceived(BotsStatistics);
        }
        hasMasterInfo = true;
    }

    public BotsStats GetBotWithMoreKills()
    {
        if(BotsStatistics == null || BotsStatistics.Count <= 0)
        {
            BotsStats bs = new BotsStats()
            {
                Name = "None",
                Kills = 0,
                Team = Team.None,
                Score = 0,
            };
            return bs;
        }
        int high = 0;
        int id = 0;
        for (int i = 0; i < BotsStatistics.Count; i++)
        {
            if (BotsStatistics[i].Kills > high)
            {
                high = BotsStatistics[i].Kills;
                id = i;
            }
        }
        return BotsStatistics[id];
    }

    [System.Serializable]
    public class PlayersSlots
    {
        public string Player;
        public string Bot;
    }

    [System.Serializable]
    public class BotsStats
    {
        public string Name;
        public int Kills;
        public int Deaths;
        public int Score;
        public Team Team;
        public int ViewID;
    }

    private static bl_AIMananger _instance;
    public static bl_AIMananger Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<bl_AIMananger>(); }
            return _instance;
        }
    }
}