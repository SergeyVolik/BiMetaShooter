using UnityEngine;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable; //Replace default Hashtables with Photon hashtables
using Photon.Pun;
using Photon.Realtime;

public class bl_RoomSettings : bl_MonoBehaviour
{
    //Private
    [HideInInspector] public int Team_1_Score = 0;
    [HideInInspector] public int Team_2_Score = 0;
    public bool AutoTeamSelection { get; set; }

    private bl_MatchTimeManager TimeManager;
    [HideInInspector] public GameMode currentGameMode = GameMode.FFA;
    public int GameGoal { get; set; }
    private bl_GameData GameData;

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        if ((!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) && !bl_GameData.Instance.offlineMode)
            return;

        TimeManager = base.GetComponent<bl_MatchTimeManager>();
        GameData = bl_GameData.Instance;
        ResetRoom();
        GetRoomInfo();
    }
 
    /// <summary>
    /// 
    /// </summary>
    public void ResetRoom()
    {
        Hashtable table = new Hashtable();
        //Initialize new properties where the information will stay Room
        if (PhotonNetwork.IsMasterClient)
        {
            table.Add(PropertiesKeys.Team1Score, 0);
            table.Add(PropertiesKeys.Team2Score, 0);
            PhotonNetwork.CurrentRoom.SetCustomProperties(table);
        }
        table.Clear();
        //Initialize new properties where the information will stay Players
        if (bl_GameData.Instance.lobbyJoinMethod == LobbyJoinMethod.DirectToMap)
        {
            table.Add(PropertiesKeys.TeamKey, Team.None.ToString());
        }
        table.Add(PropertiesKeys.KillsKey, 0);
        table.Add(PropertiesKeys.DeathsKey, 0);
        table.Add(PropertiesKeys.ScoreKey, 0);
        table.Add(PropertiesKeys.UserRole, bl_GameData.Instance.RolePrefix);
        PhotonNetwork.LocalPlayer.SetCustomProperties(table);

#if ULSP && LM
        bl_DataBase db = FindObjectOfType<bl_DataBase>();
        int scoreLevel = 0;
        if (db != null)
        {
            scoreLevel = db.LocalUser.Score;
        }
        Hashtable PlayerTotalScore = new Hashtable();
        PlayerTotalScore.Add("TotalScore", scoreLevel);
        PhotonNetwork.LocalPlayer.SetCustomProperties(PlayerTotalScore);
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    void GetRoomInfo()
    {
        currentGameMode = GetGameMode;
        TimeManager.roundStyle = (RoundStyle)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.RoomRoundKey];
        AutoTeamSelection = (bool)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.TeamSelectionKey];
        GameGoal = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.RoomGoal];
        LoadPrefs();

        if (AutoTeamSelection && bl_GameData.Instance.lobbyJoinMethod == LobbyJoinMethod.DirectToMap)
        {
            bl_UIReferences.Instance.AutoTeam(true);
            bl_UIReferences.Instance.ShowMenu(false);
            Invoke("SelectTeamAutomatically", 3);
        }
        else if (bl_GameData.Instance.lobbyJoinMethod == LobbyJoinMethod.WaitingRoom && PhotonNetwork.LocalPlayer.GetPlayerTeam() == Team.None)
        {
            bl_UIReferences.Instance.AutoTeam(true);
            bl_UIReferences.Instance.ShowMenu(false);
            Invoke("SelectTeamAutomatically", 3);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        bl_EventHandler.OnRoundEnd += this.OnRoundEnd;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        bl_EventHandler.OnRoundEnd -= this.OnRoundEnd;
    }


    /// <summary>
    /// Set the player that just join to the room to the team with less players on it automatically
    /// </summary>
    void SelectTeamAutomatically()
    {
        string joinText = isOneTeamMode ? bl_GameTexts.JoinedInMatch : bl_GameTexts.JoinIn;
#if LOCALIZATION
         joinText = isOneTeamMode ? bl_Localization.Instance.GetText(17) : bl_Localization.Instance.GetText(23);
#endif

        int teamDelta = PhotonNetwork.PlayerList.GetPlayersInTeam(Team.Team1).Length;
        int teamRecon = PhotonNetwork.PlayerList.GetPlayersInTeam(Team.Team2).Length;
        Team team = Team.All;
        if (!isOneTeamMode)
        {
            if (teamDelta > teamRecon)
            {
                team = Team.Team2;
            }
            else if (teamDelta < teamRecon)
            {
                team = Team.Team1;
            }
            else if (teamDelta == teamRecon)
            {
                team = Team.Team1;
            }

            string jt = string.Format("{0} {1}", joinText, team);
            bl_KillFeed.Instance.SendTeamHighlightMessage(PhotonNetwork.NickName, jt, team);
        }
        else
        {
            bl_KillFeed.Instance.SendMessageEvent(string.Format("{0} {1}", PhotonNetwork.NickName, joinText));
        }
        bl_RoomMenu.Instance.OnAutoTeam();
        bl_UIReferences.Instance.AutoTeam(false);

        if (GetGameMode.GetGameModeInfo().onRoundStartedSpawn == bl_GameData.GameModesEnabled.OnRoundStartedSpawn.WaitUntilRoundFinish && bl_GameManager.Instance.GameMatchState == MatchState.Playing)
        {
            if (bl_RoomMenu.Instance.onWaitUntilRoundFinish != null) { bl_RoomMenu.Instance.onWaitUntilRoundFinish.Invoke(team); }
            bl_GameManager.Instance.SetLocalPlayerToTeam(team);
            return;
        }

        //spawn player
        bl_GameManager.Instance.SpawnPlayer(team);
    }


    /// <summary>
    /// 
    /// </summary>
    void OnRoundEnd()
    {
        StartCoroutine(DisableUI());        
    }

    /// <summary>
    /// 
    /// </summary>
    public string GetWinnerName2
    {
        get
        {
            if (!isOneTeamMode)
            {
                if (Team_1_Score > Team_2_Score)
                {
                    return GameData.Team1Name;
                }
                else if (Team_1_Score < Team_2_Score)
                {
                    return GameData.Team2Name;
                }
                else
                {
                    return bl_GameTexts.NoOneWonName;
                }
            }
            else
            {

#if GR
                 if(GetGameMode == GameMode.GR)
                {
                   return FindObjectOfType<bl_GunRace>().GetWinnerPlayer.NickName;
                }
#endif
                return "";
            }
        }
    }

    void LoadPrefs()
    {
        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt(PropertiesKeys.Quality, 3));
        AudioListener.volume = PlayerPrefs.GetFloat(PropertiesKeys.Volume, 1);
        int i = PlayerPrefs.GetInt(PropertiesKeys.Aniso, 2);
        if (i == 0)
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        }
        else if (i == 1)
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        }
        else
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        }
    }

    IEnumerator DisableUI()
    {
        yield return new WaitForSeconds(10);
    }

    private static bl_RoomSettings _instance;
    public static bl_RoomSettings Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<bl_RoomSettings>(); }
            return _instance;
        }
    }
}