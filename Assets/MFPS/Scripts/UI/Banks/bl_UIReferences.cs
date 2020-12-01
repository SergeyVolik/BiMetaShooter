using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class bl_UIReferences : bl_PhotonHelper,IInRoomCallbacks
{

    public RoomMenuState State = RoomMenuState.Init;

    [Header("ScoreBoard")]
    public Transform Team1Panel;
    public Transform Team2Panel;
    public Transform TeamFFAPanel;
    [SerializeField] private GameObject PlayerScoreboardPrefab;
    public Graphic[] Team1UI;
    public Graphic[] Team2UI;

    [Header("References")]
    [SerializeField] private Text SpawnProtectionText;
    public GameObject TDMScoreboardsRoot;
    public GameObject FFAScoreboardRoot;
    public GameObject ScoreboardUI;
    public GameObject JoinBusttons;
    public GameObject OptionsUI;
    public GameObject FinalUI;
    [SerializeField] private GameObject LocalKillPrefab;
    [SerializeField] private GameObject KillfeedPrefab;
    [SerializeField] private Transform LocalKillPanel;
    [SerializeField] private Transform KillfeedPanel;
    [SerializeField] private Transform LeftNotifierPanel;
    [SerializeField] private RectTransform ScoreBoardPopUp;
    [SerializeField] private GameObject LeftNotifierPrefab;
    [SerializeField] private GameObject FFAJoinButton;
    [SerializeField] private GameObject BottonMenu;
    [SerializeField] private GameObject TopMenu;
    public Animator ClassButtonUI;
    [SerializeField] private GameObject SuicideButton;
    [SerializeField] private GameObject AutoTeamUI;
    [SerializeField] private GameObject SpectatorUI;
    [SerializeField] private GameObject SpectatorButton;
    [SerializeField] private GameObject BlackScreen;
    [SerializeField] private GameObject PingUI;
    [SerializeField] private GameObject FrameRateUI;
    [SerializeField] private GameObject LeaveComfirmUI;
    public GameObject ButtonsClassPlay = null;
    public GameObject JumpLadder;
    [SerializeField] private GameObject ChangeTeamButton;

    public GameObject ChatInputField;
    [SerializeField] private Button[] ClassButtons;
    [SerializeField] private GameObject[] TDMJoinButton;
    public Text ChatText;
    [SerializeField] private Text FinalUIText;
    [SerializeField] private Text FinalCountText;
    [SerializeField] private Text FinalWinnerText;
    [SerializeField] private Text RoomNameText;
    [SerializeField] private Text GameModeText;
    [SerializeField] private Text MaxPlayerText;
    [SerializeField] private Text MaxKillsText;
    [SerializeField] private Text WaitingPlayersText;
    [SerializeField] private Text RespawnCountText;  
    public Text AFKText;
    public Text Team1NameText, Team2NameText;
    [SerializeField] private Text SpectatorsCountText;
    [SerializeField] private Dropdown QualityDropdown;
    [SerializeField] private Dropdown AnisoDropdown;
    [SerializeField] private Slider VolumeSlider;
    [SerializeField] private Slider SensitivitySlider;
    [SerializeField] private Slider SensitivityAimSlider;
    [SerializeField] private Slider FovSlider;
    [SerializeField] private Toggle IMVToggle;
    [SerializeField] private Toggle IMHToggle;
    [SerializeField] private Toggle FrameRateToggle;
    [SerializeField] private Toggle MotionBlurToogle;
    public Toggle MuteVoiceToggle;
    public Toggle PushToTalkToggle;
    [SerializeField] private CanvasGroup FinalFadeAlpha;
    [SerializeField] private bl_LocalKillUI LocalKillIndividual;
    [SerializeField] private bl_KillCamUI KillCamUI;

    private List<bl_PlayerScoreboardUI> cachePlayerScoreboard = new List<bl_PlayerScoreboardUI>();
    private List<bl_PlayerScoreboardUI> cachePlayerScoreboardSorted = new List<bl_PlayerScoreboardUI>();
    private List<bl_PlayerScoreboardUI> cachePlayerScoreboardSorted2 = new List<bl_PlayerScoreboardUI>();
    private bl_RoomMenu RoomMenu;
    private bl_GameManager GameManager;
    private bool ChrAberration = true;
    private bool Antialiasing = true;
    private bool Bloom = true;
    private bool inTeam = false;
    private bool ssao = true;
    private int MaxRoomPing = 2000;
    private bool startKickingByPing = false;
    private int ChangeTeamTimes = 0;
    private bool botsScoreInstance = false;
    private bl_GunPickUp CacheGunPickUp = null;
    public Player PlayerPopUp { get; set; }
    private bl_AIMananger AIManager;
#if LOCALIZATION
    private int[] LocaleTextIDs = new int[] { 126, 22, 38, 32, 33, 34, 127, 13, 12,31 };
    private string[] LocaleStrings;
#endif
    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            return;

        PhotonNetwork.AddCallbackTarget(this);
        RoomMenu = FindObjectOfType<bl_RoomMenu>();
        GameManager = FindObjectOfType<bl_GameManager>();

        GetRoomInfo();
    }

    /// <summary>
    /// 
    /// </summary>
    void OnEnable()
    {
        RoomMenu = FindObjectOfType<bl_RoomMenu>();
        AIManager = FindObjectOfType<bl_AIMananger>();
#if LOCALIZATION
        LocaleStrings = bl_Localization.Instance.GetTextArray(LocaleTextIDs);
#endif
        InvokeRepeating("UpdateScoreboard", 1, 1);
        SetUpUI();
        if (MaxRoomPing > 0)
        {
            InvokeRepeating("CheckPing", 5, 5);
        }
        bl_EventHandler.onLocalPlayerSpawn += OnPlayerSpawn;
        bl_EventHandler.onPickUpItem += OnPicUpMedKit;
        bl_EventHandler.onLocalPlayerDeath += OnLocalPlayerDeath;
        bl_EventHandler.OnRoundEnd += OnRoundFinish;
        bl_PhotonCallbacks.LeftRoom += OnLeftRoom;
    }

    /// <summary>
    /// 
    /// </summary>
    void SetUpUI()
    {
        StartCoroutine(FinalFade(false));
        ScoreboardUI.SetActive(true);
        OptionsUI.SetActive(false);
        SpectatorUI.SetActive(false);
        BlackScreen.SetActive(false);
        ChangeTeamButton.SetActive(false);
        PlayerUI.SpeakerIcon.SetActive(false);
        ChatInputField.SetActive(false);
        ButtonsClassPlay.SetActive(true);
        ScoreBoardPopUp.gameObject.SetActive(false);
        SetUpJoinButtons();

        if (RoomMenu.isPlaying)
        {
            SuicideButton.SetActive(true);
            ClassButtonUI.gameObject.SetActive(false);
        }
        else
        {
            TopMenu.SetActive(false);
            SuicideButton.SetActive(false);
            ClassButtonUI.gameObject.SetActive(true);
            LoadSettings();
        }
        if (!bl_GameData.Instance.UseVoiceChat || Application.platform == RuntimePlatform.WebGLPlayer)
        {
            MuteVoiceToggle.gameObject.SetActive(false);
            PushToTalkToggle.gameObject.SetActive(false);
        }

        Team1NameText.text = bl_GameData.Instance.Team1Name.ToUpper();
        Team2NameText.text = bl_GameData.Instance.Team2Name.ToUpper();
        if (PhotonNetwork.IsConnected)
        {
            int MaxKills = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.RoomGoal];
#if LOCALIZATION
            MaxKillsText.text = string.Format("{0} {1}", MaxKills, LocaleStrings[0]);
#else
            MaxKillsText.text = string.Format("{0} KILLS", MaxKills);
#endif
        }
#if LMS
        if (GetGameMode == GameMode.LSM)
        {
            ShowMenu(false);
            ClassButtonUI.gameObject.SetActive(false);
        }
#endif
        if (bl_GameData.Instance.lobbyJoinMethod == LobbyJoinMethod.WaitingRoom)
        {
            SetUpJoinButtons(true);
            SpectatorButton.SetActive(false);
            ShowMenu(false);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void SetUpJoinButtons(bool forced = false)
    {
        if (!inTeam && !bl_RoomSettings.Instance.AutoTeamSelection || forced)
        {
            if (isOneTeamMode)
            {
                foreach (GameObject g in TDMJoinButton) { g.SetActive(false); }
                TDMScoreboardsRoot.SetActive(false);
                FFAScoreboardRoot.SetActive(true);
            }
            else
            {
                FFAJoinButton.SetActive(false);
                TDMScoreboardsRoot.SetActive(true);
                FFAScoreboardRoot.SetActive(false);
            }
            JoinBusttons.SetActive(true);
        }
        else { JoinBusttons.SetActive(false); }
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnDisable()
    {
        CancelInvoke("UpdateScoreboard");
        StopAllCoroutines();
        bl_EventHandler.onLocalPlayerSpawn -= OnPlayerSpawn;
        bl_EventHandler.onPickUpItem -= OnPicUpMedKit;
        bl_EventHandler.onLocalPlayerDeath -= OnLocalPlayerDeath;
        bl_EventHandler.OnRoundEnd -= OnRoundFinish;
        bl_PhotonCallbacks.LeftRoom -= OnLeftRoom;
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    void OnLocalPlayerDeath()
    {
        PlayerUI.PickUpUI.SetActive(false);
        JumpLadder.SetActive(false);
    }

    /// <summary>
    /// 
    /// </summary>
    void OnRoundFinish()
    {
        ScoreboardUI.SetActive(true);
        StopAllCoroutines();
        FinalFadeAlpha.alpha = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public void UpdateScoreboard()
    {
        int spectators = 0;
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].GetPlayerTeam() != Team.None)
            {
                if (ExistPlayerOnList(players[i]))
                {
                    if (GetPlayerScoreboardUI(players[i]) != null)
                    {
                        GetPlayerScoreboardUI(players[i]).Refresh();
                    }
                }
                else
                {
                    GameObject newUIPS = Instantiate(PlayerScoreboardPrefab) as GameObject;
                    newUIPS.GetComponent<bl_PlayerScoreboardUI>().Init(players[i], this);
                    Transform tp = null;
                    if (!isOneTeamMode)
                    {
                        tp = ((string)players[i].CustomProperties[PropertiesKeys.TeamKey] == Team.Team1.ToString()) ? Team1Panel : Team2Panel;
                    }
                    else
                    {
                        tp = TeamFFAPanel;
                    }
                    newUIPS.transform.SetParent(tp, false);
                    cachePlayerScoreboard.Add(newUIPS.GetComponent<bl_PlayerScoreboardUI>());
                }
            }
            else { spectators++; }
        }
        UpdateBotScoreboard();
#if LOCALIZATION
        SpectatorsCountText.text = string.Format(bl_Localization.Instance.GetTextPlural(122), spectators);
#else
        SpectatorsCountText.text = string.Format(bl_GameTexts.Spectators, spectators);
#endif
        SortScoreboard();
    }

    /// <summary>
    /// 
    /// </summary>
    void UpdateBotScoreboard()
    {
        if (AIManager == null || !AIManager.BotsActive || AIManager.BotsStatistics.Count <= 0) return;

        for (int i = 0; i < AIManager.BotsStatistics.Count; i++)
        {
            bl_AIMananger.BotsStats stat = AIManager.BotsStatistics[i];
            if (botsScoreInstance)
            {
                if (GetBotScoreboardUI(stat) != null)
                {
                    GetBotScoreboardUI(stat).InitBot();
                }
            }
            else
            {
                GameObject newUIPS = Instantiate(PlayerScoreboardPrefab) as GameObject;
                newUIPS.GetComponent<bl_PlayerScoreboardUI>().Init(null, this, stat);
                Transform tp = null;
                if (!isOneTeamMode)
                {
                    tp = stat.Team == Team.Team1 ? Team1Panel : Team2Panel;
                }
                else
                {
                    tp = TeamFFAPanel;
                }
                newUIPS.transform.SetParent(tp, false);
                cachePlayerScoreboard.Add(newUIPS.GetComponent<bl_PlayerScoreboardUI>());
            }
        }
        botsScoreInstance = true;
    }

    /// <summary>
    /// 
    /// </summary>
    void SortScoreboard()
    {
        if (isOneTeamMode)
        {
            cachePlayerScoreboardSorted.Clear();
            cachePlayerScoreboardSorted.AddRange(cachePlayerScoreboard.ToArray());
            cachePlayerScoreboardSorted = cachePlayerScoreboardSorted.OrderBy(x => x.GetScore()).ToList();

            for (int i = 0; i < cachePlayerScoreboardSorted.Count; i++)
            {
                if (cachePlayerScoreboardSorted[i] == null) return;
                cachePlayerScoreboardSorted[i].transform.SetSiblingIndex((cachePlayerScoreboardSorted.Count - 1) - i);
            }
        }
        else
        {
            cachePlayerScoreboardSorted.Clear();
            cachePlayerScoreboardSorted2.Clear();
            for (int i = 0; i < cachePlayerScoreboard.Count; i++)
            {
                if (cachePlayerScoreboard[i].GetTeam() == Team.Team1)
                {
                    cachePlayerScoreboardSorted.Add(cachePlayerScoreboard[i]);
                }
                else if (cachePlayerScoreboard[i].GetTeam() == Team.Team2)
                {
                    cachePlayerScoreboardSorted2.Add(cachePlayerScoreboard[i]);
                }
            }
            cachePlayerScoreboardSorted = cachePlayerScoreboardSorted.OrderBy(x => x.GetScore()).ToList();
            cachePlayerScoreboardSorted2 = cachePlayerScoreboardSorted2.OrderBy(x => x.GetScore()).ToList();
            for (int i = 0; i < cachePlayerScoreboardSorted.Count; i++)
            {
                if (cachePlayerScoreboardSorted[i] == null) return;
                cachePlayerScoreboardSorted[i].transform.SetSiblingIndex((cachePlayerScoreboardSorted.Count - 1) - i);
            }
            for (int i = 0; i < cachePlayerScoreboardSorted2.Count; i++)
            {
                if (cachePlayerScoreboardSorted2[i] == null) return;
                cachePlayerScoreboardSorted2[i].transform.SetSiblingIndex((cachePlayerScoreboardSorted2.Count - 1) - i);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void CheckPing()
    {
        int ping = PhotonNetwork.GetPing();
        if(ping >= MaxRoomPing)
        {
            PingUI.SetActive(true);
            if (!startKickingByPing) { Invoke("StartPingKick", 11); startKickingByPing = true; }
        }
        else
        {
            PingUI.SetActive(false);
            if (startKickingByPing) { CancelInvoke("StartPingKick");  startKickingByPing = false; }
        }
    }


    /// <summary>
    /// 
    /// </summary>
    void GetRoomInfo()
    {
        GameMode mode = GetGameMode;
        RoomNameText.text = PhotonNetwork.CurrentRoom.Name.ToUpper();
        GameModeText.text = mode.GetName().ToUpper();
        int vs = (!isOneTeamMode) ? PhotonNetwork.CurrentRoom.MaxPlayers / 2 : PhotonNetwork.CurrentRoom.MaxPlayers - 1;
        MaxPlayerText.text = (!isOneTeamMode) ? string.Format("{0} VS {1}", vs, vs) : string.Format("1 VS {0}", vs);
        MaxRoomPing = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.MaxPing];

        foreach(Graphic g in Team1UI) { if (g == null) continue; g.color = bl_GameData.Instance.Team1Color; }
        foreach (Graphic g in Team2UI) { if (g == null) continue; g.color = bl_GameData.Instance.Team2Color; }
    }

    /// <summary>
    /// Global notification (on right corner) when kill someone
    /// </summary>
    public void SetKillFeed(KillFeed feed)
    {
        GameObject newkillfeed = Instantiate(KillfeedPrefab) as GameObject;
        newkillfeed.GetComponent<bl_KillFeedUI>().Init(feed);
        newkillfeed.transform.SetParent(KillfeedPanel, false);
        newkillfeed.transform.SetAsFirstSibling();
    }

    /// <summary>
    /// Local notification when kill someone
    /// </summary>
    public void SetLocalKillFeed(KillInfo info, bl_KillFeed.LocalKillDisplay display)
    {
        if (display == bl_KillFeed.LocalKillDisplay.Multiple)
        {
            if (info.byHeadShot)
            {
                GameObject newkillfeedh = Instantiate(LocalKillPrefab) as GameObject;
                newkillfeedh.GetComponent<bl_LocalKillUI>().InitMultiple(info, true);
                newkillfeedh.transform.SetParent(LocalKillPanel, false);
                newkillfeedh.transform.SetAsFirstSibling();
            }
            GameObject newkillfeed = Instantiate(LocalKillPrefab) as GameObject;
            newkillfeed.GetComponent<bl_LocalKillUI>().InitMultiple(info,false);
            newkillfeed.transform.SetParent(LocalKillPanel, false);
            newkillfeed.transform.SetAsFirstSibling();
        }
        else
        {
            LocalKillIndividual.InitIndividual(info);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public void ShowMenu(bool active)
    {
        ScoreboardUI.SetActive(active);
        BottonMenu.SetActive(active);
        TopMenu.SetActive(active);
        ButtonsClassPlay.SetActive(active);
        JoinBusttons.SetActive(false);
        if (bl_GameData.Instance.CanChangeTeam && !isOneTeamMode && ChangeTeamTimes <= bl_GameData.Instance.MaxChangeTeamTimes && !bl_RoomSettings.Instance.AutoTeamSelection && bl_MatchTimeManager.Instance.TimeState == RoomTimeState.Started)
        {
#if BDGM
            if(GetGameMode != GameMode.SND)
            {
                ChangeTeamButton.SetActive(true);
            }
#else
            ChangeTeamButton.SetActive(true);
#endif
        }
        if (active)
        {
            if (RoomMenu.isPlaying)
            {
                SuicideButton.SetActive(true);
            }
            else
            {
                SuicideButton.SetActive(false);
            }
            InvokeRepeating("UpdateScoreboard", 0, 1);
        }
        else
        {
            OptionsUI.SetActive(false);
            ScoreBoardPopUp.gameObject.SetActive(false);
            CancelInvoke("UpdateScoreboard");
#if PSELECTOR
            if (bl_PlayerSelector.Instance != null)
            {
                bl_PlayerSelector.Instance.isChangeOfTeam = false;
            }
#endif
        }
#if INPUT_MANAGER
        if (bl_Input.isGamePad)
        {
            MFPS.InputManager.bl_GamePadPointer.Instance?.SetActive(active);
        }
#endif
        State = (active) ? RoomMenuState.Full : RoomMenuState.Hidde;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Resume()
    {
        bl_UtilityHelper.LockCursor(true);
        ShowMenu(false);
        State = RoomMenuState.Hidde;
        bl_UCrosshair.Instance.Show(true);
        ScoreBoardPopUp.gameObject.SetActive(false);
    }

    /// <summary>
    /// 
    /// </summary>
    public void ActiveOptions()
    {
        ScoreboardUI.SetActive(false);
        OptionsUI.SetActive(true);
        State = RoomMenuState.Options;
        ScoreBoardPopUp.gameObject.SetActive(false);
    }

    /// <summary>
    /// 
    /// </summary>
    public void ActiveScoreboard()
    {
        ScoreboardUI.SetActive(true);
        OptionsUI.SetActive(false);
        State = RoomMenuState.Scoreboard;
    }

    /// <summary>
    /// 
    /// </summary>
    public void AutoTeam(bool v)
    {
        AutoTeamUI.SetActive(v);
        if (!v)
        {
            JoinBusttons.SetActive(false);
            ScoreboardUI.SetActive(false);
            BottonMenu.SetActive(false);
            ClassButtonUI.gameObject.SetActive(false);
            SpectatorUI.SetActive(false);
            SpectatorButton.SetActive(false);
            inTeam = true;
            State = RoomMenuState.Hidde;
            CancelInvoke("UpdateScoreboard");
        }
        else
        {
            if (PhotonNetwork.OfflineMode)
            {
                AutoTeamUI.GetComponentInChildren<Text>().text = bl_GameTexts.StartingOfflineRoom;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void LeftRoom(bool quit)
    {
        if (!quit)
        {
            bl_UtilityHelper.LockCursor(false);
            LeaveComfirmUI.SetActive(true);
        }
        else
        {
#if ULSP
            if (bl_DataBase.Instance != null)
            {
                Player p = PhotonNetwork.LocalPlayer;
                bl_DataBase.Instance.SaveData(p.GetPlayerScore(), p.GetKills(), p.GetDeaths());
                bl_DataBase.Instance.StopAndSaveTime();
            }
#endif
            BlackScreen.SetActive(true);
            LeaveComfirmUI.SetActive(false);
            PhotonNetwork.LeaveRoom();
        }
    }

    public void CancelLeave()
    {
        bl_UtilityHelper.LockCursor(true);
        LeaveComfirmUI.SetActive(false);
    }

    public void OnChangeClass(PlayerClass Class)
    {
        foreach (Button b in ClassButtons) { b.interactable = true; }
        ClassButtons[(int)Class].interactable = false;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Suicide()
    {
        RoomMenu.Suicide();
        bl_UtilityHelper.LockCursor(false);
        ShowMenu(false);
    }

    public void OnKillCam(bool active, string killer = "", int gunID = 0)
    {
        if (active)
        {
            KillCamUI.Show(killer, gunID);
            StartCoroutine(RespawnCountDown());
        }
        KillCamUI.gameObject.SetActive(active);
    }

    IEnumerator RespawnCountDown()
    {
        float d = 0;
        float rt = bl_GameData.Instance.PlayerRespawnTime;
        while (d < 1)
        {
            d += Time.deltaTime / rt;
#if LOCALIZATION
            RespawnCountText.text = string.Format(LocaleStrings[2], Mathf.FloorToInt(rt * (1 - d)));
#else
            RespawnCountText.text = string.Format(bl_GameTexts.RespawnIn, Mathf.FloorToInt( rt * (1 - d)));
#endif
            yield return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetFinalText(string winner)
    {
        FinalUI.SetActive(true);
#if LOCALIZATION
        FinalUIText.text = (bl_MatchTimeManager.Instance.roundStyle == RoundStyle.OneMacht) ? LocaleStrings[3] : LocaleStrings[4];
        FinalWinnerText.text = string.Format("{0} {1}", winner, LocaleStrings[5]).ToUpper();
#else
        FinalUIText.text = (bl_MatchTimeManager.Instance.roundStyle == RoundStyle.OneMacht) ? bl_GameTexts.FinalOneMatch : bl_GameTexts.FinalRounds;
        FinalWinnerText.text = string.Format("{0} {1}", winner, bl_GameTexts.FinalWinner).ToUpper();
#endif
    }

    public void SetCountDown(int count) { count = Mathf.Clamp(count, 0, int.MaxValue); FinalCountText.text = count.ToString(); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    public void JoinTeam(int id)
    {
#if ELIM
        if(GetGameMode == GameMode.ELIM)
        {
            if (bl_GameManager.Instance.GameMatchState == MatchState.Playing)
            {
                bl_Elimination.Instance.WaitForRoundFinish();
            }
        }
#endif
        RoomMenu.JoinTeam(id);
        JoinBusttons.SetActive(false);
        ScoreboardUI.SetActive(false);
        BottonMenu.SetActive(false);
        ClassButtonUI.gameObject.SetActive(false);
        inTeam = true;
        State = RoomMenuState.Hidde;
        AutoTeamUI.SetActive(false);
        SpectatorUI.SetActive(false);
        SpectatorButton.SetActive(false);
        RoomMenu.SpectatorMode = false;
        TopMenu.SetActive(false);
        CancelInvoke("UpdateScoreboard");
        if (bl_GameManager.Joined) { ChangeTeamTimes++; }
        if (bl_GameData.Instance.CanChangeTeam && !isOneTeamMode && ChangeTeamTimes <= bl_GameData.Instance.MaxChangeTeamTimes && bl_MatchTimeManager.Instance.TimeState == RoomTimeState.Started)
        {
            ChangeTeamButton.SetActive(true);
        }
    }

    public void ActiveChangeTeam()
    {
#if PSELECTOR
        if (bl_PlayerSelector.Instance != null)
        {
            bl_PlayerSelector.Instance.isChangeOfTeam = true;
        }
#endif
        FFAJoinButton.SetActive(false);
        FFAScoreboardRoot.SetActive(false);
        JoinBusttons.SetActive(true);
        ChangeTeamButton.SetActive(false);
        TopMenu.SetActive(true);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="active"></param>
    public void ShowScoreboard(bool active)
    {
        ScoreboardUI.SetActive(active);
        State = (active) ? RoomMenuState.Scoreboard : RoomMenuState.Hidde;
        if (active) { InvokeRepeating("UpdateScoreboard", 1, 1); } else { CancelInvoke("UpdateScoreboard"); }
    }

    public void OnSpawnCount(int count)
    {
#if LOCALIZATION
        SpawnProtectionText.text = string.Format(LocaleStrings[6].ToUpper(), count);
#else
        SpawnProtectionText.text = string.Format("SPAWN PROTECTION DISABLE IN: <b>{0}</b>", count);
#endif
        SpawnProtectionText.gameObject.SetActive(count > 0);
    }

    /// <summary>
    /// 
    /// </summary>
    public void SpectatorMode(bool active)
    {
        RoomMenu.OnSpectator(active);
        SpectatorUI.SetActive(active);
        JoinBusttons.SetActive(!active);
        ScoreboardUI.SetActive(!active);
        BottonMenu.SetActive(!active);
        ClassButtonUI.gameObject.SetActive(!active);
    }

    public void OpenScoreboardPopUp(bool active, Player player = null)
    {
        PlayerPopUp = player;
        if (active)
        {
            ScoreBoardPopUp.gameObject.SetActive(true);
            ScoreBoardPopUp.position = Input.mousePosition;
        }
        else
        {
            ScoreBoardPopUp.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    void OnPlayerSpawn()
    {
       if(KillCamUI != null) KillCamUI.gameObject.SetActive(false);
#if INPUT_MANAGER
        if (bl_Input.isGamePad)
        {
            MFPS.InputManager.bl_GamePadPointer.Instance?.SetActive(false);
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t_amount"></param>
    void OnPicUpMedKit(int Amount)
    {
        AddLeftNotifier(string.Format("+{0} Health", Amount));
    }

    void OnLeftNotifier(string _Text)
    {
        AddLeftNotifier(_Text);
    }   

    public void SetChat(InputField field)
    {
        RoomMenu.GetComponent<bl_ChatRoom>().SetChat(field.text);
        field.text = string.Empty;
    }

    public void SetPickUp(bool show, int id = 0, bl_GunPickUp gun = null, bool equiped  = false)
    {
        if (show)
        {
            bl_GunInfo info = bl_GameData.Instance.GetWeapon(id);
#if LOCALIZATION
            string t = (equiped) ? string.Format(LocaleStrings[7], info.Name) : string.Format(LocaleStrings[8], bl_GameData.Instance.GetWeapon(id).Name);
#else
            string t = (equiped) ? string.Format(bl_GameTexts.PickUpWeaponEquipped, info.Name) : string.Format(bl_GameTexts.PickUpWeapon, bl_GameData.Instance.GetWeapon(id).Name);
#endif
            PlayerUI.PickUpText.text = t.ToUpper();
            PlayerUI.PickUpUI.SetActive(true);
            if(PlayerUI.PickUpIconUI != null)
            {
                PlayerUI.PickUpIconUI.transform.GetChild(0).GetComponent<Image>().sprite = info.GunIcon;
                PlayerUI.PickUpIconUI.SetActive(info.GunIcon != null);
            }
        }
        else { PlayerUI.PickUpUI.SetActive(false); }
        CacheGunPickUp = gun;
    }

    public void OnPickUpClicked()
    {
        if (CacheGunPickUp != null)
        {
            CacheGunPickUp.Pickup();
        }
    }

    public void AddLeftNotifier(string text)
    {
        GameObject nn = Instantiate(LeftNotifierPrefab) as GameObject;
        nn.GetComponent<bl_UILeftNotifier>().SetInfo(text.ToUpper(), 7);
        nn.transform.SetParent(LeftNotifierPanel, false);
    }

    public void SetWaitingPlayersText(string text = "", bool show = false)
    {
        WaitingPlayersText.text = text;
        WaitingPlayersText.transform.parent.gameObject.SetActive(show);
    }

    public void OnChangeQuality(int id)
    {
        QualitySettings.SetQualityLevel(id, true);
        PlayerPrefs.SetInt(PropertiesKeys.Quality, id);
    }

    public void OnVolumeChannge(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(PropertiesKeys.Volume, v);
    }

    public void OnSensitivityChannge(float v)
    {
        RoomMenu.m_sensitive = v;
        PlayerPrefs.SetFloat(PropertiesKeys.Sensitivity, v);
    }

    public void OnAimSensitivityChannge(float v)
    {
        RoomMenu.SensitivityAim = v;
        PlayerPrefs.SetFloat(PropertiesKeys.SensitivityAim, v);
    }

    public void OnGunFovChannge(float v)
    {
        RoomMenu.WeaponCameraFov = (int)v;
        PlayerPrefs.SetInt(PropertiesKeys.WeaponFov, (int)v);
    }

    public void OnCrhAb(bool ab)
    {
        ChrAberration = ab;
        SendEffects();
    }

    public void SSAO(bool ab)
    {
        ssao = ab;
        SendEffects();
    }

    public void OnAntialiasing(bool ab)
    {
        Antialiasing = ab;
        SendEffects();
    }

    public void OnBloom(bool ab)
    {
        Bloom = ab;
        SendEffects();
    }
    public void SetMotionBlur(bool v) { PlayerPrefs.SetInt(PropertiesKeys.MotionBlur, v ? 1 : 0); useMotionBlur = v; SendEffects(); }
    public void SetMuteVoiceChat(bool b)
    {
        MuteVoiceToggle.isOn = b;
        if (GameManager != null && GameManager.LocalPlayer != null)
        {
            GameManager.LocalPlayer.GetComponent<bl_PlayerVoice>().OnMuteChange(b);
        }
        PlayerPrefs.SetInt(PropertiesKeys.MuteVoiceChat, b ? 1 : 0);
    }

    public void SetPushToTalk(bool b)
    {
        PushToTalkToggle.isOn = b;
        if (GameManager != null && GameManager.LocalPlayer != null)
        {
            GameManager.LocalPlayer.GetComponent<bl_PlayerVoice>().OnPushToTalkChange(b);
        }
        PlayerPrefs.SetInt(PropertiesKeys.PushToTalk, b ? 1 : 0);
    }

    public void SetFrameActive(bool act)
    {
        int fr = (act == true) ? 1 : 0;
        PlayerPrefs.SetInt(PropertiesKeys.FrameRate, fr);
        FrameRateUI.SetActive(act);
    }

    bool useMotionBlur = false;
    void SendEffects()
    {
        bl_EventHandler.SetEffectChange(ChrAberration, Antialiasing, Bloom, ssao, useMotionBlur);
    }

    public void SetAFKCount(float seconds)
    {
        AFKText.gameObject.SetActive(true);
#if LOCALIZATION
        AFKText.text = string.Format(LocaleStrings[9], seconds.ToString("F2"));
#else
        AFKText.text = string.Format(bl_GameTexts.AFKWarning, seconds.ToString("F2"));
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    void LoadSettings()
    {
        int qid = PlayerPrefs.GetInt(PropertiesKeys.Quality, bl_GameData.Instance.DefaultSettings.DefaultQualityLevel);
        int anisoid = PlayerPrefs.GetInt(PropertiesKeys.Aniso, bl_GameData.Instance.DefaultSettings.DefaultAnisoTropic);
        float v = PlayerPrefs.GetFloat(PropertiesKeys.Volume, bl_GameData.Instance.DefaultSettings.DefaultVolume);
        float s = PlayerPrefs.GetFloat(PropertiesKeys.Sensitivity, bl_GameData.Instance.DefaultSettings.DefaultSensitivity);
        float saim = PlayerPrefs.GetFloat(PropertiesKeys.SensitivityAim, bl_GameData.Instance.DefaultSettings.DefaultSensitivityAim);
        int fov = PlayerPrefs.GetInt(PropertiesKeys.WeaponFov, bl_GameData.Instance.DefaultSettings.DefaultWeaponFoV);
        bool fr = (PlayerPrefs.GetInt(PropertiesKeys.FrameRate, bl_GameData.Instance.DefaultSettings.DefaultShowFrameRate ? 1 : 0) == 1);
        RoomMenu.SetIMV = (PlayerPrefs.GetInt(PropertiesKeys.InvertMouseVertical, 0) == 1);
        RoomMenu.SetIMH = (PlayerPrefs.GetInt(PropertiesKeys.InvertMouseHorizontal, 0) == 1);
        MuteVoiceToggle.isOn = (PlayerPrefs.GetInt(PropertiesKeys.MuteVoiceChat, 0) == 1);
        PushToTalkToggle.isOn = (PlayerPrefs.GetInt(PropertiesKeys.PushToTalk, 0) == 1);
        MotionBlurToogle.isOn = (PlayerPrefs.GetInt(PropertiesKeys.MotionBlur, bl_GameData.Instance.DefaultSettings.DefaultMotionBlur ? 1 : 0) == 1);
        useMotionBlur = MotionBlurToogle.isOn;

        List<Dropdown.OptionData> od = new List<Dropdown.OptionData>();
        for(int i = 0; i < QualitySettings.names.Length; i++)
        {
            Dropdown.OptionData data = new Dropdown.OptionData();
            data.text = QualitySettings.names[i].ToUpper();
            od.Add(data);
        }
        QualityDropdown.AddOptions(od);
        QualityDropdown.value = qid;
        AnisoDropdown.value = anisoid;
        VolumeSlider.value = v;
        SensitivitySlider.value = s;
        SensitivityAimSlider.value = saim;
        FovSlider.value = fov;
        FrameRateUI.SetActive(fr);
        RoomMenu.WeaponCameraFov = fov;
        RoomMenu.m_sensitive = s;
        RoomMenu.SensitivityAim = saim;
        IMVToggle.isOn = RoomMenu.SetIMV;
        IMHToggle.isOn = RoomMenu.SetIMH;
        FrameRateToggle.isOn = fr;
    }

    public void OnChangeAniso(int id)
    {
        if (id == 0)
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        }
        else if (id == 1)
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        }
        else
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        }
        PlayerPrefs.SetInt(PropertiesKeys.Aniso, id);
    }

    /// <summary>
    /// Use for change player class for next Re spawn
    /// </summary>
    public void ChangeClass(int m_class)
    {
        switch (m_class)
        {
            case 0:
                bl_RoomMenu.PlayerClass = PlayerClass.Assault;
                break;
            case 1:
                bl_RoomMenu.PlayerClass = PlayerClass.Engineer;
                break;
            case 2:
                bl_RoomMenu.PlayerClass = PlayerClass.Recon;
                break;
            case 3:
                bl_RoomMenu.PlayerClass = PlayerClass.Support;
                break;
        }

        bl_RoomMenu.PlayerClass.SavePlayerClass();
        OnChangeClass(bl_RoomMenu.PlayerClass);

#if CLASS_CUSTOMIZER
        bl_ClassManager.Instance.m_Class = bl_RoomMenu.PlayerClass;
#endif
        RoomMenu.ChangeClass();
    }

    /// <summary>
    /// 
    /// </summary>
    public void ResetRound()
    {
        FinalUI.SetActive(false);
        inTeam = false;
        SetUpUI();
        Button[] b = JoinBusttons.GetComponentsInChildren<Button>();
        foreach(Button but in b) { but.interactable = true; }
    }

    void StartPingKick()
    {
        bl_PhotonNetwork.Instance.OnPingKick();
        PhotonNetwork.LeaveRoom();
    }

    public IEnumerator FinalFade(bool fadein, bool goToLobby = true, float delay = 1)
    {
        if (!goToLobby)
        {
            if (bl_RoomMenu.Instance.isFinish) { FinalFadeAlpha.alpha = 0; yield break; }
        }
        if (FinalFadeAlpha == null) yield break;
        if (fadein)
        {
            FinalFadeAlpha.alpha = 0;
            FinalFadeAlpha.gameObject.SetActive(true);
            while (FinalFadeAlpha.alpha < 1)
            {
                FinalFadeAlpha.alpha += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
#if ULSP
            if(bl_DataBase.Instance != null && bl_DataBase.Instance.IsRunningTask)
            {
                while (bl_DataBase.Instance.IsRunningTask) { yield return null; }
            }
#endif
            if (goToLobby)
            {
                bl_UtilityHelper.LoadLevel(bl_GameData.Instance.MainMenuScene);
            }
        }
        else
        {
            FinalFadeAlpha.alpha = 1;
            FinalFadeAlpha.gameObject.SetActive(true);
            yield return new WaitForSeconds(delay);
            while (FinalFadeAlpha.alpha > 0)
            {
                FinalFadeAlpha.alpha -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            FinalFadeAlpha.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool isMenuActive
    {
        get
        {
            return !(State == RoomMenuState.Hidde);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool isOnlyMenuActive
    {
        get
        {
            return (State == RoomMenuState.Options);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool isScoreboardActive
    {
        get
        {
            return (State == RoomMenuState.Scoreboard);
        }
    }


    private bl_PlayerScoreboardUI GetPlayerScoreboardUI(Player player)
    {
        for (int i = 0; i < cachePlayerScoreboard.Count; i++)
        {
            if (cachePlayerScoreboard[i].name == (player.NickName + player.ActorNumber))
            {
                return cachePlayerScoreboard[i];
            }
        }

        return null;
    }

    private bl_PlayerScoreboardUI GetBotScoreboardUI(bl_AIMananger.BotsStats bot)
    {
        for (int i = 0; i < cachePlayerScoreboard.Count; i++)
        {
            if (cachePlayerScoreboard[i].name == (bot.Name))
            {
                return cachePlayerScoreboard[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    private bool ExistPlayerOnList(Player player)
    {
        for (int i = 0; i < cachePlayerScoreboard.Count; i++)
        {
            if (cachePlayerScoreboard[i].name == (player.NickName + player.ActorNumber))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="psui"></param>
    public void RemoveUIPlayer(bl_PlayerScoreboardUI psui)
    {
        if (cachePlayerScoreboard.Contains(psui))
        {
            cachePlayerScoreboard.Remove(psui);
        }
    }

    public void RemoveUIBot(string Name)
    {
        if (cachePlayerScoreboard.Exists(x => x.name == Name))
        {
            int id = cachePlayerScoreboard.FindIndex(x => x.name == Name);
            Destroy(cachePlayerScoreboard[id].gameObject);
            cachePlayerScoreboard.RemoveAt(id);
        }
    }

    public void OnLeftRoom()
    {
        ShowMenu(false);
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
       
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (ExistPlayerOnList(otherPlayer))
        {
            bl_PlayerScoreboardUI pscui = GetPlayerScoreboardUI(otherPlayer);
            RemoveUIPlayer(pscui);
            pscui.Destroy();
        }
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        UpdateScoreboard();
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
       
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        UpdateScoreboard();
    }

    [System.Serializable]
    public enum RoomMenuState
    {
        Scoreboard = 0,
        Options = 1,
        Full = 2,
        Hidde = 3,
        Init = 4,
    }

    private static bl_UIReferences _instance;
    public static bl_UIReferences Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<bl_UIReferences>(); }
            return _instance;
        }
    }

    private bl_PlayerUIBank _PlayerBank;
    public bl_PlayerUIBank PlayerUI
    {
        get
        {
            if(_PlayerBank == null) { _PlayerBank = transform.GetComponentInChildren<bl_PlayerUIBank>(); }
            return _PlayerBank;
        }
    }
}