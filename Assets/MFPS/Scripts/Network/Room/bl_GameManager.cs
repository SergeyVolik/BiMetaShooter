using System;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Random = UnityEngine.Random;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class bl_GameManager : bl_PhotonHelper, IInRoomCallbacks, IConnectionCallbacks {

    public static int LocalPlayerViewID = -1;
    public static int SuicideCount = 0;
    public static bool Joined = false;
    public int Headshots { get; set; }
    public IGameMode GameModeLogic { get; private set; }
    public bool GameFinish { get; set; }
    public GameObject LocalPlayer { get; set; }

    public MatchState GameMatchState;

    [Header("References")]
    public bool DrawSpawnPoints = true;
    public Mesh SpawnPointPlayerGizmo;
    [HideInInspector]public List<Transform> AllSpawnPoints = new List<Transform>();
    private List<Transform> ReconSpawnPoint = new List<Transform>();
    private List<Transform> DeltaSpawnPoint = new List<Transform>();
    private int currentReconSpawnPoint = 0;
    private int currentDeltaSpawnPoint = 0;
    public List<Player> connectedPlayerList = new List<Player>();
    private bool EnterInGamePlay = false;
    public List<MFPSPlayer> OthersActorsInScene = new List<MFPSPlayer>();
    public MFPSPlayer LocalActor { get; set; } = new MFPSPlayer();
    public Team LocalPlayerTeam { get; set; }
    public bool spawnInQueque { get; set; }
    private int WaitingPlayersAmount = 1;
    private float StartPlayTime;
    private bool registered = false;
    //Events
    public Action onAllPlayersRequiredIn;

    private Camera cameraRender = null;
    public Camera CameraRendered
    {
        get
        {
            if(cameraRender == null)
            {
               // Debug.Log("Not Camera has been setup.");
                return Camera.current;
            }
            return cameraRender;
        }
        set
        {
            if(cameraRender != null && cameraRender.isActiveAndEnabled)
            {
                //if the current render over the set camera, keep it as renderer camera
                if (cameraRender.depth >= value.depth) return;
            }
            cameraRender = value;
        }
    }
#if UMM
    private Canvas MiniMapCanvas = null;
#endif

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        if (!registered) { PhotonNetwork.AddCallbackTarget(this); registered = true; }

        PhotonNetwork.IsMessageQueueRunning = true;
        bl_UtilityHelper.BlockCursorForUser = false;
        Joined = false;
        SuicideCount = 0;
        StartPlayTime = Time.time;

        LocalActor.isRealPlayer = true;
        LocalActor.Name = PhotonNetwork.NickName;

        bl_UCrosshair.Instance.Show(false);
#if UMM
        bl_MiniMap mm = FindObjectOfType<bl_MiniMap>();
        if (mm != null)
        {
            MiniMapCanvas = mm.m_Canvas;
            MiniMapCanvas.enabled = false;
        }
        else
        {
            Debug.Log("Minimap is enabled but not integrated in this map");
        }
#endif
        if (bl_GameData.Instance.lobbyJoinMethod == LobbyJoinMethod.WaitingRoom && PhotonNetwork.LocalPlayer.GetPlayerTeam() != Team.None)
        {
            Invoke("SpawnPlayerWithCurrentTeam", 2);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        if(GameModeLogic == null)//check on start cuz game mode should be assigned on awake
        {
            Debug.LogWarning("No Game Mode has been assigned yet!");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnEnable()
    {
        bl_EventHandler.RemoteActorsChange += OnRemoteActorChange;
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnDisable()
    {
        bl_EventHandler.RemoteActorsChange -= OnRemoteActorChange;
        if(registered)
        PhotonNetwork.RemoveCallbackTarget(this);
        if(GameModeLogic != null)
        {
            bl_PhotonCallbacks.PlayerEnteredRoom -= GameModeLogic.OnOtherPlayerEnter;
            bl_PhotonCallbacks.RoomPropertiesUpdate -= GameModeLogic.OnRoomPropertiesUpdate;
            bl_PhotonCallbacks.PlayerLeftRoom -= GameModeLogic.OnOtherPlayerLeave;
            bl_EventHandler.onLocalPlayerDeath -= GameModeLogic.OnLocalPlayerDeath;
        }
    }

    /// <summary>
    /// Spawn Player Function
    /// </summary>
    public bool SpawnPlayer(Team t_team)
    {
        if (spawnInQueque) return false;//there's a reserved spawn incoming, don't spawn before that

        if (!bl_RoomMenu.Instance.SpectatorMode)
        {
            if (LocalPlayer != null)//if there is a local player already instance
            {
                PhotonNetwork.Destroy(LocalPlayer);//destroy it
            }
            if (!GameFinish)//if the game still not finish
            {
                //set the player team to the player properties
                Hashtable PlayerTeam = new Hashtable();
                PlayerTeam.Add(PropertiesKeys.TeamKey, t_team.ToString());

                PhotonNetwork.LocalPlayer.SetCustomProperties(PlayerTeam);
                LocalPlayerTeam = t_team;

                //spawn the player model
#if !PSELECTOR
                SpawnPlayerModel(t_team);
                AfterSpawnSetup();
#else
                bl_PlayerSelector ps = FindObjectOfType<bl_PlayerSelector>();
                if (MFPS.PlayerSelector.bl_PlayerSelectorData.Instance.PlayerSelectorMode == MFPS.PlayerSelector.bl_PlayerSelectorData.PSType.InMatch)
                {
                    if (ps.IsSelected && !ps.isChangeOfTeam)
                    {
                        ps.SpawnSelected(t_team);
                    }
                    else
                    {
                        ps.OpenSelection(t_team);
                    }
                }
                else
                {
                    if(!PhotonNetwork.OfflineMode)
                    SpawnSelectedPlayer(MFPS.PlayerSelector.bl_PlayerSelectorData.Instance.GetSelectedPlayerFromTeam(t_team), t_team);
                    else
                    {
                        SpawnPlayerModel(t_team);
                        AfterSpawnSetup();
                    }
                }
#endif
                return true;
            }
            else
            {
                bl_RoomCamera.Instance?.SetActive(false);
                return false;
            }
        }
        else
        {
            this.GetComponent<bl_RoomMenu>().WaitForSpectator = true;
            return false;
        }
    }

    void AfterSpawnSetup()
    {
        bl_RoomCamera.Instance?.SetActive(false);
        StartCoroutine(bl_UIReferences.Instance.FinalFade(false, false, 0));
        bl_UtilityHelper.LockCursor(true);
        if (!Joined) { StartPlayTime = Time.time; }
        Joined = true;

#if UMM
        if (MiniMapCanvas != null)
        {
            MiniMapCanvas.enabled = true;
        }
        else
        {
            Debug.LogWarning("MiniMap addon is enabled but not integrated in this scene");
        }
#endif
    }

    /// <summary>
    /// Assign a player to a team but not instance it
    /// </summary>
    public void SetLocalPlayerToTeam(Team team)
    {
        Hashtable PlayerTeam = new Hashtable();
        PlayerTeam.Add(PropertiesKeys.TeamKey, team.ToString());
        PhotonNetwork.LocalPlayer.SetCustomProperties(PlayerTeam, null);
        LocalPlayerTeam = team;
        Joined = true;
    }

    /// <summary>
    /// Instance a Player only if already has been instanced and is alive
    /// </summary>
    public void SpawnPlayerIfAlreadyInstanced()
    {
        if (LocalPlayer == null)
            return;

        Team t = PhotonNetwork.LocalPlayer.GetPlayerTeam();
        SpawnPlayer(t);
    }

    /// <summary>
    /// If Player exist, them destroy
    /// </summary>
    public void DestroyPlayer(bool ActiveCamera)
    {
        if (LocalPlayer != null)
        {
            PhotonNetwork.Destroy(LocalPlayer);
        }
        bl_RoomCamera.Instance?.SetActive(ActiveCamera);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public void SpawnPlayerModel(Team playerTeam)
    {
        Vector3 pos;
        Quaternion rot;
        GameObject playerPrefab = bl_GameData.Instance.Player1.gameObject;
        if (playerTeam == Team.Team2)
        {
            GetSpawn(ReconSpawnPoint.ToArray(), out pos, out rot);
            playerPrefab = bl_GameData.Instance.Player2.gameObject;
        }
        else if (playerTeam == Team.Team1)
        {
            GetSpawn(DeltaSpawnPoint.ToArray(), out pos, out rot);
            playerPrefab = bl_GameData.Instance.Player1.gameObject;
        }
        else
        {
            GetSpawn(AllSpawnPoints.ToArray(), out pos, out rot);
        }

        //set the some common data that will be sync right after the player is instanced in the other clients
        var commonData = new object[1];
        commonData[0] = playerTeam;

        //instantiate the player prefab
        LocalPlayer = PhotonNetwork.Instantiate(playerPrefab.name, pos, rot, 0, commonData);

        LocalActor.Actor = LocalPlayer.transform;
        LocalActor.ActorView = LocalPlayer.GetComponent<PhotonView>();
        LocalActor.Team = playerTeam;
        LocalActor.AimPosition = LocalPlayer.GetComponent<bl_PlayerSettings>().AimPositionReference.transform;

        if (!EnterInGamePlay && bl_MatchInformationDisplay.Instance != null) { bl_MatchInformationDisplay.Instance.DisplayInfo(); }
        EnterInGamePlay = true;
        bl_EventHandler.PlayerLocalSpawnEvent();
        bl_UCrosshair.Instance.Show(true);
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnGameTimeFinish(bool gameOver)
    {
        GameFinish = true;
        if(GameModeLogic != null) { GameModeLogic.OnFinishTime(gameOver); }
    }

    /// <summary>
    /// 
    /// </summary>
    public void SpawnPlayerWithCurrentTeam()
    {
        SpawnPlayer(PhotonNetwork.LocalPlayer.GetPlayerTeam());
        bl_RoomMenu.Instance.OnAutoTeam();
    }

    #region GameModes
    public bool IsGameMode(GameMode mode, IGameMode logic)
    {
        bool isIt = GetGameMode == mode;
        if (isIt)
        {
            if (GameModeLogic != null)
            {
                Debug.LogError("A GameMode has been assigned before, only 1 game mode can be assigned per match.");
                return false;
            }
            GameModeLogic = logic;
            bl_PhotonCallbacks.PlayerEnteredRoom += logic.OnOtherPlayerEnter;
            bl_PhotonCallbacks.RoomPropertiesUpdate += logic.OnRoomPropertiesUpdate;
            bl_PhotonCallbacks.PlayerLeftRoom += logic.OnOtherPlayerLeave;
            bl_EventHandler.onLocalPlayerDeath += GameModeLogic.OnLocalPlayerDeath;
            Debug.Log("Game Mode: " + mode.GetName());
        }
        return isIt;
    }

    /// <summary>
    /// Should called when the local player score in game
    /// </summary>
    public void SetPointFromLocalPlayer(int points, GameMode mode)
    {
        if (GameModeLogic == null) return;
        if (mode != GetGameMode) return;

        GameModeLogic.OnLocalPoint(points, PhotonNetwork.LocalPlayer.GetPlayerTeam());
    }

    /// <summary>
    /// Should called when a non player object score in game, ex: AI
    /// This should be called by master client only
    /// </summary>
    public void SetPoint(int points, GameMode mode, Team team)
    {
        if (GameModeLogic == null) return;
        if (mode != GetGameMode) return;

        GameModeLogic.OnLocalPoint(points, team);
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnLocalPlayerKill()
    {
        if (GameModeLogic == null) return;

        GameModeLogic.OnLocalPlayerKill();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool isLocalPlayerWinner()
    {
        if (GameModeLogic == null) return false;
        return GameModeLogic.isLocalPlayerWinner;
    }
    #endregion

#if PSELECTOR
    /// <summary>
    /// 
    /// </summary>
    public void SpawnSelectedPlayer(MFPS.PlayerSelector.bl_PlayerSelectorInfo info,Team playerTeam)
    {
        Vector3 pos;
        Quaternion rot;
        if (playerTeam == Team.Team2)
        {
            GetSpawn(ReconSpawnPoint.ToArray(), out pos, out rot);
        }
        else if (playerTeam == Team.Team1)
        {
            GetSpawn(DeltaSpawnPoint.ToArray(), out pos, out rot);
        }
        else
        {
            GetSpawn(AllSpawnPoints.ToArray(), out pos, out rot);
        }

        //set the some common data that will be sync right after the player is instanced in the other clients
        var commonData = new object[1];
        commonData[0] = playerTeam;

        LocalPlayer = PhotonNetwork.Instantiate(info.Prefab.name, pos, rot, 0, commonData);

        LocalActor.Actor = LocalPlayer.transform;
        LocalActor.ActorView = LocalPlayer.GetComponent<PhotonView>();
        LocalActor.Team = playerTeam;
        LocalActor.AimPosition = LocalPlayer.GetComponent<bl_PlayerSettings>().AimPositionReference.transform;

        AfterSpawnSetup();
        if (!EnterInGamePlay && bl_MatchInformationDisplay.Instance != null) { bl_MatchInformationDisplay.Instance.DisplayInfo(); }
        EnterInGamePlay = true;
        bl_EventHandler.PlayerLocalSpawnEvent();
        bl_UCrosshair.Instance.Show(true);
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    public bool WaitForPlayers(int MinPlayers)
    {
        if (MinPlayers > 1)
        {
            if (isOneTeamMode)
            {
                if (PhotonNetwork.PlayerList.Length >= MinPlayers) return false;
            }
            else
            {
                if (PhotonNetwork.PlayerList.Length >= MinPlayers)
                {
                    if (PhotonNetwork.PlayerList.GetPlayersInTeam(Team.Team1).Length > 0 && PhotonNetwork.PlayerList.GetPlayersInTeam(Team.Team2).Length > 0)
                    {
                        if(onAllPlayersRequiredIn != null) { onAllPlayersRequiredIn.Invoke(); }
                        return false;
                    }
                }
            }
        }
        WaitingPlayersAmount = MinPlayers;
        SetGameState(MatchState.Waiting);
        return true;
    }

    public float PlayedTime => (Time.time - StartPlayTime);

    /// <summary>
    /// 
    /// </summary>
    public void GetSpawn(Transform[] list, out Vector3 position, out Quaternion Rotation)
    {
       int random = Random.Range(0, list.Length);
       Vector3 s = Random.insideUnitSphere * list[random].GetComponent<bl_SpawnPoint>().SpawnSpace;
       position = list[random].position + new Vector3(s.x, 0.55f, s.z);
       Rotation = list[random].rotation;
    }

    /// <summary>
    /// 
    /// </summary>
    public void RegisterSpawnPoint(bl_SpawnPoint point)
    {
        switch (point.m_Team)
        {
            case Team.Team1:
                DeltaSpawnPoint.Add(point.transform);
                break;
            case Team.Team2:
                ReconSpawnPoint.Add(point.transform);
                break;
        }
        AllSpawnPoints.Add(point.transform);
    }

    public Transform GetAnSpawnPoint => AllSpawnPoints[Random.Range(0, AllSpawnPoints.Count)];

    public Transform GetAnTeamSpawnPoint(Team team, bool sequencial = false)
    {
        if (team == Team.Team2)
        {
            if (sequencial)
            {
                currentReconSpawnPoint = (currentReconSpawnPoint + 1) % ReconSpawnPoint.Count;
                return ReconSpawnPoint[currentReconSpawnPoint];
            }
            return ReconSpawnPoint[Random.Range(0, ReconSpawnPoint.Count)];
        }
        else if (team == Team.Team1)
        {
            if (sequencial)
            {
                currentDeltaSpawnPoint = (currentDeltaSpawnPoint + 1) % DeltaSpawnPoint.Count;
                return DeltaSpawnPoint[currentDeltaSpawnPoint];
            }
            return DeltaSpawnPoint[Random.Range(0, DeltaSpawnPoint.Count)];
        }
        else
        {
            if (sequencial)
            {
                currentReconSpawnPoint = (currentReconSpawnPoint + 1) % AllSpawnPoints.Count;
                return AllSpawnPoints[currentReconSpawnPoint];
            }
            return AllSpawnPoints[Random.Range(0, AllSpawnPoints.Count)];
        }
    }

    /// <summary>
    /// This is a event callback
    /// here is caching all 'actors' in the scene (players and bots)
    /// </summary>
    public void OnRemoteActorChange(string actorName, MFPSPlayer playerData, bool spawning)
    {
        if (OthersActorsInScene.Exists(x => x.Name == actorName))
        {
            int id = OthersActorsInScene.FindIndex(x => x.Name == actorName);
            if (spawning)
            {
                if (playerData != null)
                {
                    OthersActorsInScene[id] = playerData;
                }
                else
                {
                    OthersActorsInScene[id].isAlive = spawning;
                }
            }
            else
            {
                if(OthersActorsInScene[id].Actor == null)
                {
                    OthersActorsInScene[id].isAlive = false;
                }
            }
        }
        else
        {
            if (spawning)
            {
               if(playerData == null) { Debug.LogWarning($"Actor data for {actorName} has not been build yet."); return; }
               if(playerData.ActorView == null) { playerData.ActorView = playerData.Actor?.GetComponent<PhotonView>(); }
                OthersActorsInScene.Add(playerData);
            }
        }
    }

    /// <summary>
    /// Find a player or bot by their PhotonView ID
    /// </summary>
    /// <returns></returns>
    public Transform FindActor(int ViewID)
    {
        for (int i = 0; i < OthersActorsInScene.Count; i++)
        {
            if(OthersActorsInScene[i].ActorView != null && OthersActorsInScene[i].ActorView.ViewID == ViewID) 
            {
                return OthersActorsInScene[i].Actor;
            }
        }
        if(LocalPlayer != null && LocalPlayer.GetPhotonView().ViewID == ViewID) { return LocalPlayer.transform; }
        return null;
    }

    /// <summary>
    /// Find a player or bot by their PhotonPlayer
    /// </summary>
    /// <returns></returns>
    public Transform FindActor(Player player)
    {
        if (player == null) return null;
        for (int i = 0; i < OthersActorsInScene.Count; i++)
        {
            if (OthersActorsInScene[i].ActorView != null && OthersActorsInScene[i].ActorView.Owner != null && OthersActorsInScene[i].ActorView.Owner.ActorNumber == player.ActorNumber)
            {
                return OthersActorsInScene[i].Actor;
            }
        }
        if (LocalPlayer != null && LocalPlayer.GetPhotonView().Owner.ActorNumber == player.ActorNumber) { return LocalPlayer.transform; }
        return null;
    }

    /// <summary>
    /// Find a player or bot by their PhotonPlayer
    /// </summary>
    /// <returns></returns>
    public MFPSPlayer FindActor(string actorName)
    {
        for (int i = 0; i < OthersActorsInScene.Count; i++)
        {
            if (OthersActorsInScene[i].ActorView != null && OthersActorsInScene[i].Actor.name == actorName)
            {
                return OthersActorsInScene[i];
            }
        }
        if (LocalPlayer != null && LocalPlayer.GetPhotonView().Owner.NickName == actorName) { return LocalActor; }
        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    public MFPSPlayer GetMFPSPlayer(string nickName)
    {
        MFPSPlayer player = OthersActorsInScene.Find(x => x.Name == nickName);
        if(player == null && nickName == LocalName)
        {
            player = LocalActor;
        }
        return player;
    }

    public List<MFPSPlayer> GetNonTeamMatePlayers(bool includeBots = true)
    {
        Team playerTeam = bl_PhotonNetwork.LocalPlayer.GetPlayerTeam();
        List<MFPSPlayer> list = new List<MFPSPlayer>();
        for (int i = 0; i < OthersActorsInScene.Count; i++)
        {
            if (OthersActorsInScene[i].Team != playerTeam)
            {
                if (OthersActorsInScene[i].isRealPlayer) { list.Add(OthersActorsInScene[i]); }
                else if (includeBots) { list.Add(OthersActorsInScene[i]); }
            }
        }
        return list;
    }

    #region PUN

    [PunRPC]
    void RPCSyncGame(MatchState state)
    {
        Debug.Log("Game sync by master, match state: " + state.ToString());
        GameMatchState = state;
        if (!PhotonNetwork.IsMasterClient)
        {
            bl_MatchTimeManager.Instance.Init();
        }
    }

    public void SetGameState(MatchState state)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }
        PhotonNetwork.RegisterPhotonView(photonView);
       // Debug.Log("Game State Update: " + state.ToString());
        photonView.RPC(nameof(RPCMatchState), RpcTarget.All, state);
    }

    [PunRPC]
    void RPCMatchState(MatchState state)
    {
        GameMatchState = state;
    }

    /// <summary>
    /// 
    /// </summary>
    void CheckPlayersInMatch()
    {
        //if still waiting
        if (!bl_MatchTimeManager.Instance.Initialized && GameMatchState == MatchState.Waiting)
        {
            bool ready = false;
            if (isOneTeamMode)
            {
                ready = PhotonNetwork.PlayerList.Length >= WaitingPlayersAmount;
            }
            else
            {
                //if the minimum amount of players are in the game
                if (PhotonNetwork.PlayerList.Length >= WaitingPlayersAmount)
                {
                    //and they are split in both teams
                    if ((PhotonNetwork.PlayerList.GetPlayersInTeam(Team.Team1).Length > 0 && PhotonNetwork.PlayerList.GetPlayersInTeam(Team.Team2).Length > 0) || WaitingPlayersAmount <= 1)
                    {
                        //we are ready to start
                        ready = true;
                    }
                    else
                    {
                        //otherwise wait until player split in both teams
#if LOCALIZATION
                        bl_UIReferences.Instance.SetWaitingPlayersText(bl_Localization.Instance.GetText(128), true);
#else
                        bl_UIReferences.Instance.SetWaitingPlayersText("Waiting for balance team players", true);
#endif
                        return;
                    }
                }
            }
            if (ready)//all needed players in game
            {
                //master set the call to start the match
                if (PhotonNetwork.IsMasterClient)
                {
                    bl_MatchTimeManager.Instance.InitAfterWait();
                }
                SetGameState(MatchState.Starting);
                bl_MatchTimeManager.Instance.SetTimeState(RoomTimeState.Started, true);
                onAllPlayersRequiredIn?.Invoke();
                bl_UIReferences.Instance.SetWaitingPlayersText("", false);
            }
            else
            {
                bl_UIReferences.Instance.SetWaitingPlayersText(string.Format(bl_GameTexts.WaitingPlayers, PhotonNetwork.PlayerList.Length, 2), true);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnApplicationQuit()
    {
        PhotonNetwork.Disconnect();
    }

    //PLAYER EVENTS
    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player connected: " + newPlayer.NickName);
        if (PhotonNetwork.IsMasterClient)
        {
            //master sync the require match info to be sure all players have the same info at the start
            photonView.RPC("RPCSyncGame", newPlayer, GameMatchState);
        }
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Player disconnected: " + otherPlayer.NickName);
    }

    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
      
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        //when a player has join to a team
        if (changedProps.ContainsKey(PropertiesKeys.TeamKey))
        {
            //make sure has join to a team
            if ((string)changedProps[PropertiesKeys.TeamKey] != Team.None.ToString())
            {
                CheckPlayersInMatch();
            }
            else
            {
                if (GameMatchState == MatchState.Waiting)
                {
                    bl_UIReferences.Instance.SetWaitingPlayersText(string.Format(bl_GameTexts.WaitingPlayers, PhotonNetwork.PlayerList.Length, WaitingPlayersAmount), true);
                }
            }
        }
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("The old masterclient left, we have a new masterclient: " + newMasterClient.NickName);
        this.GetComponent<bl_ChatRoom>().AddLine("We have a new masterclient: " + newMasterClient.NickName);
    }

    public void OnConnected()
    {
      
    }

    public void OnConnectedToMaster()
    {
       
    }

    public void OnDisconnected(DisconnectCause cause)
    {
#if UNITY_EDITOR
        if (bl_RoomMenu.Instance.isApplicationQuitting) { return; }
#endif
        Debug.Log("Clean up a bit after server quit, cause: " + cause.ToString());
        PhotonNetwork.IsMessageQueueRunning = false;
        bl_UtilityHelper.LoadLevel(bl_GameData.Instance.OnDisconnectScene);
    }

    public void OnRegionListReceived(RegionHandler regionHandler)
    {
     
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
       
    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {
       
    }
#endregion

    public bool alreadyEnterInGame
    {
        get
        {
            return EnterInGamePlay;
        }
        set
        {
            EnterInGamePlay = value;
        }
    }

    public static bool isLocalAlive
    {
        get
        {
            return bl_GameManager.Instance.LocalActor.isAlive;
        }
        set
        {
            bl_GameManager.Instance.LocalActor.isAlive = value;
        }
    }

    public GameObject m_RoomCamera { get { return bl_RoomCamera.Instance.gameObject; } }

    private static bl_GameManager _instance;
    public static bl_GameManager Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<bl_GameManager>(); }
            return _instance;
        }
    }
}		