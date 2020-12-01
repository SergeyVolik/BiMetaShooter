using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using Photon.Pun;
using Photon.Realtime;

public class bl_Lobby : bl_PhotonHelper, IConnectionCallbacks, ILobbyCallbacks, IMatchmakingCallbacks
{
    private string playerName;
    public string hostName { get; set; } //Name of room
    [Header("Photon")]
    [SerializeField] private string RoomNamePrefix = "LovattoRoom {0}";
    [SerializeField] private string PlayerNamePrefix = "Guest {0}";
    public SeverRegionCode DefaultServer = SeverRegionCode.usw;
    public bool ShowPhotonStatistics;

    [Header("Room Options")]
    //Max players in game
    public int[] maxPlayers = new int[] { 6, 2, 4, 8 };
    public int players { get; set; }
    //Room Time in seconds
    public int[] RoomTime = new int[] { 600, 300, 900, 1200 };
    public int r_Time { get; set; }
    //Room Max Kills
    public int CurrentGameGoal { get; set; }
    //Room Max Ping
    public int[] MaxPing = new int[] { 100, 200, 500, 1000 };
    public int CurrentMaxPing { get; set; }

    public bl_GameData.GameModesEnabled[] GameModes { get; set; }
    public int CurrentGameMode { get; set; }

    [Header("References")]
    [SerializeField] private GameObject PhotonGamePrefab;

    public int CurrentScene { get; set; }
    public bool GamePerRounds { get; set; }
    public bool AutoTeamSelection { get; set; }
    public bool FriendlyFire { get; set; }
    public bool rememberMe { get; set; }

    private bl_LobbyChat Chat;
    public string CachePlayerName { get; set;}
    private RoomInfo checkingRoom;
    private int PendingRegion = -1;
    private bool FirstConnectionMade = false;
    private bool AppQuit = false;
    private bool isSeekingMatch = false;
    bool alreadyLoadHome = false;
    public string justCreatedRoomName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
#if ULSP
        if (bl_DataBase.Instance == null && bl_LoginProDataBase.Instance.ForceLoginScene)
        {
            bl_UtilityHelper.LoadLevel("Login");
            return;
        }
#endif
        PhotonNetwork.AddCallbackTarget(this);
        PhotonNetwork.UseRpcMonoBehaviourCache = true;
        bl_UtilityHelper.BlockCursorForUser = false;
        PhotonNetwork.OfflineMode = false;
        PhotonNetwork.IsMessageQueueRunning = true;
        bl_UtilityHelper.LockCursor(false);

        StartCoroutine(StartFade());//show loading screen
        Chat = GetComponent<bl_LobbyChat>();
        if (FindObjectOfType<bl_PhotonNetwork>() == null) { Instantiate(PhotonGamePrefab); }
        if(bl_AudioController.Instance != null) { bl_AudioController.Instance.PlayBackground(); }
        if (bl_GameData.isDataCached)
        {
            SetUpGameModes();
            bl_LobbyUI.Instance.LoadSettings();
            bl_LobbyUI.Instance.FullSetUp();
        }
        bl_LobbyUI.Instance.InitialSetup();
    }

    /// <summary>
    /// 
    /// </summary>
    void SetUpGameModes()
    {
        List<bl_GameData.GameModesEnabled> gm = new List<bl_GameData.GameModesEnabled>();
        for (int i = 0; i < bl_GameData.Instance.gameModes.Count; i++)
        {
            if (bl_GameData.Instance.gameModes[i].isEnabled)
            {
                gm.Add(bl_GameData.Instance.gameModes[i]);
            }
        }
        GameModes = gm.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    /// <summary>
    /// 
    /// </summary>
    public void ConnectPhoton()
    {
        // the following line checks if this client was just created (and not yet online). if so, we connect
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.AuthValues = new AuthenticationValues(PhotonNetwork.NickName);
            //if we don't have a custom region to connect or we are using a self hosted server
            if (PendingRegion == -1 || !PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer)
            {
                //connect using the default PhotonServerSettings
                PhotonNetwork.GameVersion = bl_GameData.Instance.GameVersion;
                PhotonNetwork.ConnectUsingSettings();
            }
            else
            {
                //Change the cloud server region
                FirstConnectionMade = true;
                ChangeServerCloud(PendingRegion);
            }
#if LOCALIZATION
            bl_LobbyUI.Instance.LoadingScreenText.text = bl_Localization.Instance.GetText(40);
#else
            bl_LobbyUI.Instance.LoadingScreenText.text = bl_GameTexts.ConnectingToGameServer;
#endif
            StartCoroutine(ShowLoadingScreen());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void Disconect()
    {
        if (Chat != null && Chat.isConnected()) { Chat.Disconnect(); }
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }
    void DelayDisconnect() { PhotonNetwork.Disconnect(); }

    /// <summary>
    /// 
    /// </summary>
    private void OnApplicationQuit()
    {
        AppQuit = true;
        bl_GameData.isDataCached = false;
        Disconect();
    }

    /// <summary>
    /// 
    /// </summary>
    public void ServerList(List<RoomInfo> roomList)
    {
        bl_LobbyUI.Instance.SetRoomList(roomList);
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetPlayerName(string InputName)
    {
        CachePlayerName = InputName;       
        playerName = CachePlayerName;
        playerName = playerName.Replace("\n", "");
        PlayerPrefs.SetString(PropertiesKeys.PlayerName, playerName);
        PhotonNetwork.NickName = playerName;
        ConnectPhoton();
        if (rememberMe)
        {
            PlayerPrefs.SetString(PropertiesKeys.RememberMe, playerName);
        }
        //load the user coins
        //NOTE: Coins are store locally, so is highly recommended to store in a database, you can use ULogin for it.
#if !ULSP
        bl_GameData.Instance.VirtualCoins.LoadCoins(PhotonNetwork.NickName);
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public bool EnterPassword(string password)
    {
        if (bl_GameData.Instance.CheckPasswordUse(CachePlayerName, password))
        {
            playerName = CachePlayerName;
            playerName = playerName.Replace("\n", "");
            PhotonNetwork.NickName = playerName;
            ConnectPhoton();
            if (rememberMe)
            {
                PlayerPrefs.SetString(PropertiesKeys.RememberMe, playerName);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void AutoMatch()
    {
        if (isSeekingMatch)
            return;

        isSeekingMatch = true;
        StartCoroutine(AutoMatchIE());
    }

    /// <summary>
    /// 
    /// </summary>
    IEnumerator AutoMatchIE()
    {
        //active the search match UI
        bl_LobbyUI.Instance.SeekingMatchUI.SetActive(true);
        yield return new WaitForSeconds(3);
        PhotonNetwork.JoinRandomRoom();
        isSeekingMatch = false;
        bl_LobbyUI.Instance.SeekingMatchUI.SetActive(false);
    }

    /// <summary>
    /// When there is not rooms to join (when matchmaking)
    /// </summary>
    public void OnNoRoomsToJoin(short returnCode, string message)
    {
        Debug.Log("No games to join found on matchmaking, creating one.");
        justCreatedRoomName = hostName;
        //create random room properties
        int propsCount = 11;
        string roomName = string.Format("[PUBLIC] {0}{1}", PhotonNetwork.NickName.Substring(0, 2), Random.Range(0, 9999));
        int scid = Random.Range(0, bl_GameData.Instance.AllScenes.Count);
        int maxPlayersRandom = Random.Range(0, maxPlayers.Length);
        int timeRandom = Random.Range(0, RoomTime.Length);
        int modeRandom = Random.Range(0, GameModes.Length);
        int randomGoal = Random.Range(0, GameModes[modeRandom].GameGoalsOptions.Length);

        ExitGames.Client.Photon.Hashtable roomOption = new ExitGames.Client.Photon.Hashtable();
        roomOption[PropertiesKeys.TimeRoomKey] = RoomTime[timeRandom];
        roomOption[PropertiesKeys.GameModeKey] = GameModes[modeRandom].gameMode.ToString();
        roomOption[PropertiesKeys.SceneNameKey] = bl_GameData.Instance.AllScenes[scid].RealSceneName;
        roomOption[PropertiesKeys.RoomRoundKey] = RoundStyle.OneMacht;
        roomOption[PropertiesKeys.TeamSelectionKey] =  GameModes[modeRandom].AutoTeamSelection;
        roomOption[PropertiesKeys.CustomSceneName] = bl_GameData.Instance.AllScenes[scid].ShowName;
        roomOption[PropertiesKeys.RoomGoal] = GameModes[modeRandom].GameGoalsOptions[randomGoal];
        roomOption[PropertiesKeys.RoomFriendlyFire] = false;
        roomOption[PropertiesKeys.MaxPing] = MaxPing[CurrentMaxPing];
        roomOption[PropertiesKeys.RoomPassword] = string.Empty;
        roomOption[PropertiesKeys.WithBotsKey] = (GameModes[CurrentGameMode].gameMode == GameMode.FFA || GameModes[CurrentGameMode].gameMode == GameMode.TDM) ? true : false;

        string[] properties = new string[propsCount];
        properties[0] = PropertiesKeys.TimeRoomKey;
        properties[1] = PropertiesKeys.GameModeKey;
        properties[2] = PropertiesKeys.SceneNameKey;
        properties[3] = PropertiesKeys.RoomRoundKey;
        properties[4] = PropertiesKeys.TeamSelectionKey;
        properties[5] = PropertiesKeys.CustomSceneName;
        properties[6] = PropertiesKeys.RoomGoal;
        properties[7] = PropertiesKeys.RoomFriendlyFire;
        properties[8] = PropertiesKeys.MaxPing;
        properties[9] = PropertiesKeys.RoomPassword;
        properties[10] = PropertiesKeys.WithBotsKey;

        PhotonNetwork.CreateRoom(roomName, new RoomOptions()
        {
            MaxPlayers = (byte)maxPlayers[maxPlayersRandom],
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = roomOption,
            CleanupCacheOnLeave = true,
            CustomRoomPropertiesForLobby = properties,
            BroadcastPropsChangeToAll = true,

        }, null);
        StartCoroutine(bl_LobbyUI.Instance.DoBlackFade(true, 0.33f));
        if (bl_AudioController.Instance != null) { bl_AudioController.Instance.StopBackground(); }
    }


    /// <summary>
    /// 
    /// </summary>
    public void CreateRoom()
    {
        if (Chat != null && Chat.isConnected()) { Chat.Disconnect(); }
        int propsCount = 11;
        PhotonNetwork.NickName = playerName;
        justCreatedRoomName = hostName;
        //Save Room properties for load in room
        ExitGames.Client.Photon.Hashtable roomOption = new ExitGames.Client.Photon.Hashtable();
        roomOption[PropertiesKeys.TimeRoomKey] = RoomTime[r_Time];
        roomOption[PropertiesKeys.GameModeKey] = GameModes[CurrentGameMode].gameMode.ToString();
        roomOption[PropertiesKeys.SceneNameKey] = bl_GameData.Instance.AllScenes[CurrentScene].RealSceneName;
        roomOption[PropertiesKeys.RoomRoundKey] = GamePerRounds ? RoundStyle.Rounds : RoundStyle.OneMacht;
        roomOption[PropertiesKeys.TeamSelectionKey] = GameModes[CurrentGameMode].AutoTeamSelection ? true : AutoTeamSelection;
        roomOption[PropertiesKeys.CustomSceneName] = bl_GameData.Instance.AllScenes[CurrentScene].ShowName;
        roomOption[PropertiesKeys.RoomGoal] = GetSelectedRoomGoal();
        roomOption[PropertiesKeys.RoomFriendlyFire] = FriendlyFire;
        roomOption[PropertiesKeys.MaxPing] = MaxPing[CurrentMaxPing];
        roomOption[PropertiesKeys.RoomPassword] = bl_LobbyUI.Instance.PassWordField.text;
        roomOption[PropertiesKeys.WithBotsKey] = (GameModes[CurrentGameMode].gameMode == GameMode.FFA || GameModes[CurrentGameMode].gameMode == GameMode.TDM) ? bl_LobbyUI.Instance.WithBotsToggle.isOn : false;

        string[] properties = new string[propsCount];
        properties[0] = PropertiesKeys.TimeRoomKey;
        properties[1] = PropertiesKeys.GameModeKey;
        properties[2] = PropertiesKeys.SceneNameKey;
        properties[3] = PropertiesKeys.RoomRoundKey;
        properties[4] = PropertiesKeys.TeamSelectionKey;
        properties[5] = PropertiesKeys.CustomSceneName;
        properties[6] = PropertiesKeys.RoomGoal;
        properties[7] = PropertiesKeys.RoomFriendlyFire;
        properties[8] = PropertiesKeys.MaxPing;
        properties[9] = PropertiesKeys.RoomPassword;
        properties[10] = PropertiesKeys.WithBotsKey;

        PhotonNetwork.CreateRoom(hostName, new RoomOptions()
        {
            MaxPlayers = (byte)maxPlayers[players],
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = roomOption,
            CleanupCacheOnLeave = true,
            CustomRoomPropertiesForLobby = properties,
            PublishUserId = true,
            EmptyRoomTtl = 0,
            BroadcastPropsChangeToAll = true,
        }, null);
        StartCoroutine(bl_LobbyUI.Instance.DoBlackFade(true, 0.33f));
        if (bl_AudioController.Instance != null) { bl_AudioController.Instance.StopBackground(); }
    }

    /// <summary>
    /// 
    /// </summary>
    public void SignOut()
    {
        PlayerPrefs.SetString(PropertiesKeys.RememberMe, string.Empty);
        Disconect();
        bl_LobbyUI.Instance.ChangeWindow("player name");
    }

    #region UGUI
    public void SetRememberMe(bool value)
    {
        rememberMe = value;
    }

    /// <summary>
    /// 
    /// </summary>
    public void CheckRoomPassword(RoomInfo room)
    {
        checkingRoom = room;
    }

    /// <summary>
    /// 
    /// </summary>
    public bool SetRoomPassworld(string pass)
    {
        if (checkingRoom == null)
        {
            Debug.Log("Checking room is not assigned more!");
            return false;
        }

        if ((string)checkingRoom.CustomProperties[PropertiesKeys.RoomPassword] == pass && checkingRoom.PlayerCount < checkingRoom.MaxPlayers)
        {
            if (PhotonNetwork.GetPing() < (int)checkingRoom.CustomProperties[PropertiesKeys.MaxPing])
            {
                StartCoroutine(bl_LobbyUI.Instance.DoBlackFade(true, 1));
                if (checkingRoom.PlayerCount < checkingRoom.MaxPlayers)
                {
                    PhotonNetwork.JoinRoom(checkingRoom.Name);
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    private bool serverchangeRequested = false;
    public void ChangeServerCloud(int id)
    {
        if (PhotonNetwork.IsConnected && FirstConnectionMade)
        {
            serverchangeRequested = true;
#if LOCALIZATION
            bl_LobbyUI.Instance.LoadingScreenText.text = bl_Localization.Instance.GetText(40);
#else
            bl_LobbyUI.Instance.LoadingScreenText.text = bl_GameTexts.ConnectingToGameServer;
#endif
            StartCoroutine(ShowLoadingScreen());
            PendingRegion = id;
            Invoke("DelayDisconnect", 0.2f);
            return;
        }
        if (!string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime))
        {
            if (!FirstConnectionMade)
            {
                PendingRegion = id;
                serverchangeRequested = true;
                return;
            }
            serverchangeRequested = false;
            SeverRegionCode code = SeverRegionCode.usw;
            if (id > 3) { id++; }
            code = (SeverRegionCode)id;
            PhotonNetwork.NetworkingClient.AppId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
            PhotonNetwork.ConnectToRegion(code.ToString());
            PlayerPrefs.SetString(PropertiesKeys.PreferredRegion, code.ToString());
        }
        else
        {
            Debug.LogWarning("Need your AppId for change server, please add it in PhotonServerSettings");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void LoadLocalLevel(string level)
    {
        Disconect();
        bl_UtilityHelper.LoadLevel(level);
    }
    #endregion

    public bl_GameData.GameModesEnabled GetSelectedGameMode()
    {
        return GameModes[CurrentGameMode];
    }

    public int GetSelectedRoomGoal()
    {
        return GameModes[CurrentGameMode].GameGoalsOptions[CurrentGameGoal];
    }

    public string GetSelectedGoalFullName()
    {
        if (GetSelectedRoomGoal() > 0)
        {
            return string.Format("{0} {1}", GetSelectedRoomGoal(), GameModes[CurrentGameMode].GoalName);
        }
        else { return GameModes[CurrentGameMode].GoalName; }
    }

    /// <summary>
    /// 
    /// </summary>
    void GetPlayerName()
    {
        bool isNameEmpty = string.IsNullOrEmpty(PhotonNetwork.NickName);
        if (isNameEmpty)
        {
#if ULSP
            if (bl_DataBase.Instance != null && !bl_DataBase.Instance.isGuest)
            {
                playerName = bl_DataBase.Instance.LocalUser.NickName;
                PhotonNetwork.NickName = playerName;
                bl_LobbyUI.Instance.UpdateCoinsText();
                GoToMainMenu();
            }
            else
            {
                GeneratePlayerName();
            }
#else
            GeneratePlayerName();
#endif
        }
        else
        {
            bl_LobbyUI.Instance.UpdateCoinsText();
            playerName = PhotonNetwork.NickName;
            GoToMainMenu();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void GeneratePlayerName()
    {
        if (!rememberMe)
        {
            if (!PlayerPrefs.HasKey(PropertiesKeys.PlayerName) || !bl_GameData.Instance.RememberPlayerName)
            {
                playerName = string.Format(PlayerNamePrefix, Random.Range(1, 9999));
            }
            else if (bl_GameData.Instance.RememberPlayerName)
            {
                playerName = PlayerPrefs.GetString(PropertiesKeys.PlayerName, string.Format(PlayerNamePrefix, Random.Range(1, 9999)));
            }
             bl_LobbyUI.Instance.PlayerNameField.text = playerName;
            PhotonNetwork.NickName = playerName;
            bl_LobbyUI.Instance.ChangeWindow("player name");
        }
        else
        {
            playerName = PlayerPrefs.GetString(PropertiesKeys.RememberMe);
            PhotonNetwork.NickName = playerName;
            GoToMainMenu();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void GoToMainMenu()
    {
        if (!PhotonNetwork.IsConnected)
        {
            ConnectPhoton();
        }
        else
        {
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
            else
            {
                if (!alreadyLoadHome) { bl_LobbyUI.Instance.Home(); alreadyLoadHome = true; }

                if (Chat != null && !Chat.isConnected() && bl_GameData.Instance.UseLobbyChat) { Chat.Connect(bl_GameData.Instance.GameVersion); }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private IEnumerator MoveToGameScene()
    {
        //Wait for check
        if (Chat != null && Chat.isConnected()) { Chat.Disconnect(); }
        while (!PhotonNetwork.InRoom)
        {
            yield return null;
        }
        PhotonNetwork.IsMessageQueueRunning = false;
        bl_UtilityHelper.LoadLevel((string)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.SceneNameKey]);
    }

    /// <summary>
    /// 
    /// </summary>
    public void ShowLevelList()
    {
#if LM
        bl_LevelPreview lp = FindObjectOfType<bl_LevelPreview>();
        if (lp != null)
        {
            lp.ShowList();
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public void ShowBuyCoins()
    {
#if SHOP
        bl_ShopManager.Instance.BuyCoinsWindow.SetActive(true);
#else
        Debug.Log("Require shop addon.");
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator StartFade()
    {
        bl_LobbyUI.Instance.LoadingScreen.gameObject.SetActive(true);
#if LOCALIZATION
         bl_LobbyUI.Instance.LoadingScreenText.text = bl_Localization.Instance.GetText(39);
#else
        bl_LobbyUI.Instance.LoadingScreenText.text = bl_GameTexts.LoadingLocalContent;
#endif
        if (!bl_GameData.isDataCached)
        {
            yield return StartCoroutine(bl_GameData.AsyncLoadData());
            yield return new WaitForEndOfFrame();
            SetUpGameModes();
            bl_LobbyUI.Instance.LoadSettings();
            bl_LobbyUI.Instance.FullSetUp();
        }
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(bl_LobbyUI.Instance.DoBlackFade(false, 1));
        yield return StartCoroutine(ShowLoadingScreen(true, 2));
        GetPlayerName();
    }

    /// <summary>
    /// 
    /// </summary>
    IEnumerator ShowLoadingScreen(bool autoHide = false, float showTime = 2)
    {
        bl_LobbyUI.Instance.LoadingScreen.gameObject.SetActive(true);
        bl_LobbyUI.Instance.LoadingScreen.alpha = 1;
        Animator bottomAnim = bl_LobbyUI.Instance.LoadingScreen.GetComponentInChildren<Animator>();
        bottomAnim.SetBool("show", true);
        bottomAnim.Play("show", 0, 0);
        if (autoHide)
        {
            yield return new WaitForSeconds(showTime);
            float d = 1;
            bottomAnim.SetBool("show", false);
            while (d > 0)
            {
                d -= Time.deltaTime / 0.5f;
                bl_LobbyUI.Instance.LoadingScreen.alpha = bl_LobbyUI.Instance.FadeCurve.Evaluate(d);
                yield return new WaitForEndOfFrame();
            }
            bl_LobbyUI.Instance.LoadingScreen.gameObject.SetActive(false);
        }
    }

    #region Photon Callbacks
    /// <summary>
    /// 
    /// </summary>
    public void OnConnected()
    {
        FirstConnectionMade = true;
        if (PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer)
        {
            Debug.Log("Server connection established to: " + PhotonNetwork.CloudRegion);
        }
        else
        {
            Debug.Log($"Server connection established to: {PhotonNetwork.ServerAddress} ({PhotonNetwork.Server.ToString()})");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        if (cause == DisconnectCause.DisconnectByClientLogic)
        {
            if (AppQuit)
            {
                Debug.Log("Disconnect from Server!");
                return;
            }
            if (PendingRegion == -1)
            {
                Debug.Log("Disconnect from cloud!");
            }
            else if (serverchangeRequested)
            {
                Debug.Log("Changing server!");
                ChangeServerCloud(PendingRegion);
            }
            else
            {
                Debug.Log("Disconnect from Server.");
            }
        }
        else
        {
            StartCoroutine(bl_LobbyUI.Instance.DoBlackFade(false, 1));
#if LOCALIZATION
           bl_LobbyUI.Instance. DisconnectCauseUI.GetComponentInChildren<Text>().text = string.Format(bl_Localization.Instance.GetText(41), cause.ToString());
#else
            bl_LobbyUI.Instance.DisconnectCauseUI.GetComponentInChildren<Text>().text = string.Format(bl_GameTexts.DisconnectCause, cause.ToString());
#endif
            bl_LobbyUI.Instance.DisconnectCauseUI.SetActive(true);
            Debug.LogWarning("Failed to connect to server, cause: " + cause);
        }
    }

    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("Room list updated, total rooms: " + roomList.Count);
        ServerList(roomList);
    }

    public void OnRegionListReceived(RegionHandler regionHandler)
    {

    }

    public void OnJoinedLobby()
    {
        Debug.Log($"Player <b>{PhotonNetwork.LocalPlayer.UserId}</b> joined to the lobby");
        bl_LobbyUI.Instance.Home();
        if (PendingRegion != -1) { }
        StartCoroutine(ShowLoadingScreen(true, 2));

        if (Chat != null && !Chat.isConnected() && bl_GameData.Instance.UseLobbyChat) { Chat.Connect(bl_GameData.Instance.GameVersion); }
        ResetValues();
    }

    public void ResetValues()
    {
        //Create a random name for a future room that player create
        hostName = string.Format(RoomNamePrefix, Random.Range(10, 999));
        bl_LobbyUI.Instance.RoomNameField.text = hostName;
        PhotonNetwork.IsMessageQueueRunning = true; 
    }

    public void OnJoinedRoom()
    {
        if (bl_GameData.Instance.lobbyJoinMethod == LobbyJoinMethod.DirectToMap)
        {
            Debug.Log($"Local client joined to the room '{PhotonNetwork.CurrentRoom.Name}'");
            StartCoroutine(MoveToGameScene());
        }
        else
        {
            if (Chat != null && Chat.isConnected()) { Chat.Disconnect(); }
        }
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {

    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {
    }

    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
    }

    public void OnLeftLobby()
    {
    }

    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {
    }

    public void OnCreatedRoom()
    {
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        OnNoRoomsToJoin(returnCode, message);
    }

    public void OnLeftRoom()
    {
    }
    #endregion

    public AddonsButtonsHelper AddonsButtons = new AddonsButtonsHelper();
    public class AddonsButtonsHelper
    {
        public GameObject this[int index]
        {
            get
            {
                return bl_LobbyUI.Instance.AddonsButtons[index];
            }
        }
    }

    private static bl_Lobby _instance;
    public static bl_Lobby Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<bl_Lobby>();
            }
            return _instance;
        }
    }
}