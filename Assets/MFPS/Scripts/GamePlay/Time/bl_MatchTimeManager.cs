/////////////////////////////////////////////////////////////////////////////////
///////////////////////////////bl_MatchTimeManager.cs///////////////////////////////////
///////////////Use this to manage time in rooms//////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
////////////////////////////////Lovatto Studio///////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable; //Replace default Hashtables with Photon hashtables
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class bl_MatchTimeManager : bl_MonoBehaviour
{
    public RoundStyle roundStyle { get; set; }
    public RoomTimeState TimeState { get; set; }
    public int RoundDuration { get; set; }
    public float CurrentTime { get; set; }

    //private
    private const string StartTimeKey = "RoomTime";       // the name of our "start time" custom property.
    private float m_Reference;
    private int m_countdown = 10;
    public bool isFinish { get; set; }
    private bl_RoomSettings RoomSettings;
    private bl_RoomMenu RoomMenu;
    public Text TimeText { get; set; }
    private bool roomClose = false;
    public bool Initialized { get; set; }
    public bool Pause { get; set; }
    private int countDown = 5;

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        if (!PhotonNetwork.IsConnected && !bl_GameData.Instance.offlineMode)
        {
            bl_UtilityHelper.LoadLevel(bl_GameData.Instance.MainMenuScene);
            return;
        }
        base.Awake();
        RoomSettings = bl_RoomSettings.Instance;
        RoomMenu = bl_RoomMenu.Instance;
        TimeText = bl_UIReferences.Instance.PlayerUI.TimeText;
        Pause = false;
    }

    /// <summary>
    /// 
    /// </summary>
    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
            return;

        //only master client initialized by default, other players will wait until master sync match information after they join.
        if (PhotonNetwork.IsMasterClient)
        {
            Init();
        }
        else
        {
            RemoteInit();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        bl_PhotonCallbacks.MasterClientSwitched += OnMasterClientSwitch;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        bl_PhotonCallbacks.MasterClientSwitched -= OnMasterClientSwitch;
    }

    /// <summary>
    /// Master Client Initialize the room time
    /// </summary>
    public void Init()
    {
        //if the match is waiting for a minimum amount of players to start
        if (bl_GameManager.Instance.GameMatchState == MatchState.Waiting)
        {           
            bl_UIReferences.Instance.SetWaitingPlayersText(string.Format(bl_GameTexts.WaitingPlayers, PhotonNetwork.PlayerList.Length, 2), true);
            return;
        }
        if (bl_GameData.Instance.useCountDownOnStart)
        {
            StartCountDown();
            return;
        }
        else
        {
            SetTimeState(RoomTimeState.Started, true);
        }
#if LMS
        if (GetGameMode == GameMode.LSM)return;
#endif
        GetTime(true);
    }

    /// <summary>
    /// 
    /// </summary>
    public void InitAfterWait()
    {
        GetTime(true);
        photonView.RPC("RpcStartTime", RpcTarget.AllBuffered, 3);
    }

    /// <summary>
    /// get the current time and verify if it is correct
    /// </summary>
    public void GetTime(bool ResetReference)
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.TimeRoomKey] != null)
        {
            //get the time duration from the room properties 
            RoundDuration = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.TimeRoomKey];
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (ResetReference)//get the server time again?
            {
                m_Reference = (float)PhotonNetwork.Time;//get a reference time from the server

                Hashtable startTimeProp = new Hashtable();//create a property to store the reference time
                startTimeProp.Add(StartTimeKey, m_Reference);
                PhotonNetwork.CurrentRoom.SetCustomProperties(startTimeProp);//send to the room hash tables so other clients can access to it
            }
        }
        else//Non master clients
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties[StartTimeKey] != null)//if there's a reference time available
            {
                m_Reference = (float)PhotonNetwork.CurrentRoom.CustomProperties[StartTimeKey];//get it from the room hash tables
            }
        }
        if (!Initialized) { bl_GameManager.Instance.SetGameState(MatchState.Playing); }//is this the first round?
        Initialized = true;
    }

    /// <summary>
    /// 
    /// </summary>
    void RemoteInit()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PropertiesKeys.TimeState))
        {
            TimeState = (RoomTimeState)(int)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.TimeState];
            if (TimeState == RoomTimeState.Started)
            {
                GetTime(false);
            }
            SetTimeState(TimeState);
        }
    }

    #region CountDown
    /// <summary>
    /// 
    /// </summary>
    void StartCountDown()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        countDown = bl_GameData.Instance.CountDownTime;
        SetTimeState(RoomTimeState.Countdown, true);
        InvokeRepeating("SetCountDown", 1, 1);
    }

    /// <summary>
    /// 
    /// </summary>
    void SetCountDown()
    {
        countDown--;
        photonView.RPC("RpcCountDown", RpcTarget.All, countDown);
    }

    [PunRPC]
    void RpcCountDown(int count)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            countDown = count;
        }
        if (countDown <= 0)
        {
            RoundDuration = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.TimeRoomKey];
            m_Reference = (float)PhotonNetwork.Time;
            if (PhotonNetwork.IsMasterClient)
            {
                CancelInvoke("SetCountDown");
                Hashtable startTimeProp = new Hashtable();
                startTimeProp.Add(StartTimeKey, m_Reference);
                PhotonNetwork.CurrentRoom.SetCustomProperties(startTimeProp);
                SetTimeState(RoomTimeState.Started, true);
            }
            else
            {
                GetTime(false);
                SetTimeState(RoomTimeState.Started);
            }
            if (!Initialized) { bl_GameManager.Instance.SetGameState(MatchState.Playing); }
            Initialized = true;
            Pause = false;            
        }
        bl_CountDownUI.Instance.SetCount(countDown);
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetTimeState(RoomTimeState state, bool sync = false)
    {
        if (sync && PhotonNetwork.IsMasterClient)
        {
            Hashtable table = new Hashtable();
            table.Add(PropertiesKeys.TimeState, (int)state);
            PhotonNetwork.CurrentRoom.SetCustomProperties(table);
        }
        TimeState = state;
        if (TimeState == RoomTimeState.Started)
        {
            bl_EventHandler.CallOnMatchStart();
        }
    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    public void RestartTime()
    {
       //get the room time duration from hash tables
        if (PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.TimeRoomKey] != null)
        {
            RoundDuration = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.TimeRoomKey];
        }
        //cause everyone is already in the room, all will get the reference locally from the server
        m_Reference = (float)PhotonNetwork.Time;
        //Master Client will take care of store the reference time for future players
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable startTimeProp = new Hashtable();  // only use ExitGames.Client.Photon.Hashtable for Photon
            startTimeProp.Add(StartTimeKey, m_Reference);
            PhotonNetwork.CurrentRoom.SetCustomProperties(startTimeProp);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnFixedUpdate()
    {
        if (!Initialized || Pause || isFinish)
            return;

        //calculate seconds from the reference time and the current server time
        float seconds = RoundDuration - ((float)PhotonNetwork.Time - m_Reference);
        if (seconds > 0.0001f)
        {
            CurrentTime = seconds;
            //if the game is about to finish, close the room so it will not be listed in the lobby anymore
            if(CurrentTime <= 30 && !roomClose && PhotonNetwork.IsMasterClient)
            {
                roomClose = true;
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
             //   Debug.Log("Close room to prevent player join");
            }
        }
        else if (seconds <= 0.001 && GetTimeServed == true)//Round Finished
        {
            CurrentTime = 0;
            FinishRound();
        }
        else//if we cant get the server time yet
        {
            Refresh();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void FinishRound()
    {
        if (!PhotonNetwork.IsConnected)
            return;

        bl_EventHandler.OnRoundEndEvent();
        if (!isFinish)
        {
            isFinish = true;
            if (RoomMenu) { RoomMenu.isFinish = true; }
            bl_GameManager.Instance.OnGameTimeFinish(roundStyle == RoundStyle.OneMacht); 
            if (roundStyle == RoundStyle.OneMacht)
            {
                FindObjectOfType<bl_GameFinish>().CollectData();
            }
            bl_UIReferences.Instance.SetCountDown(m_countdown);
            InvokeRepeating("FinalCountdown", 1, 1);
            bl_UCrosshair.Instance.Show(false);
            bl_GameManager.Instance.SetGameState(MatchState.Finishing);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        TimeControll();
    }

    /// <summary>
    /// 
    /// </summary>
    void TimeControll()
    {
        if (CurrentTime == 0) return;

        //Calculate and Format time in order to display in the HUD
        int normalSecons = 60;
        float remainingTime = Mathf.CeilToInt(CurrentTime);
        int m_Seconds = Mathf.FloorToInt(remainingTime % normalSecons);
        int m_Minutes = Mathf.FloorToInt((remainingTime / normalSecons) % normalSecons);
        string t_time = bl_UtilityHelper.GetTimeFormat(m_Minutes, m_Seconds);

        if (TimeText != null)
        {
            TimeText.text = t_time;
        }
    }

    /// <summary>
    /// with this fixed the problem of the time lag in the Photon
    /// </summary>
    void Refresh()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        if (PhotonNetwork.IsMasterClient)
        {
            m_Reference = (float)PhotonNetwork.Time;

            Hashtable startTimeProp = new Hashtable();  // only use ExitGames.Client.Photon.Hashtable for Photon
            startTimeProp.Add(StartTimeKey, m_Reference);
            PhotonNetwork.CurrentRoom.SetCustomProperties(startTimeProp);
        }
        else
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(StartTimeKey))
            {
                m_Reference = (float)PhotonNetwork.CurrentRoom.CustomProperties[StartTimeKey];
            }
        }
    }

    /// <summary>
    /// Count down before leave the room after finish the game
    /// </summary>
    void FinalCountdown()
    {
        m_countdown--;
        bl_UIReferences.Instance.SetCountDown(m_countdown);
        if (m_countdown <= 0)
        {
            FinishGame();
            CancelInvoke("FinalCountdown");
            m_countdown = 10;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void FinishGame()
    {
        bl_UtilityHelper.LockCursor(false);
        if (roundStyle == RoundStyle.OneMacht)
        {
            FindObjectOfType<bl_GameFinish>().Show();
        }
        if (roundStyle == RoundStyle.Rounds)
        {
#if ULSP
            if (bl_DataBase.Instance != null)
            {
                Player p = PhotonNetwork.LocalPlayer;
                bl_DataBase.Instance.SaveData(p.GetPlayerScore(), p.GetKills(), p.GetDeaths());
                bl_DataBase.Instance.StopAndSaveTime();
            }
#endif
            GetTime(true);
            if (RoomSettings)
            {
                RoomSettings.ResetRoom();
            }
            isFinish = false;

            bl_GameManager.Instance.GameFinish = false;
            bl_GameManager.Instance.DestroyPlayer(true);

            if (RoomMenu != null)
            {
                RoomMenu.isFinish = false;
                RoomMenu.isPlaying = false;
                bl_UtilityHelper.LockCursor(false);
            }
            bl_UIReferences.Instance.ResetRound();
            bl_UIReferences.Instance.OnKillCam(false);
            m_countdown = 10;
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void StartNewRoundAndKeepData()
    {
        if (bl_GameData.Instance.useCountDownOnStart)
        {
            StartCountDown();
        }
        else
        {
            GetTime(true);
            Pause = false;
        }
        isFinish = false;
        if (RoomMenu != null)
        {
            RoomMenu.isFinish = false;
            RoomMenu.isPlaying = false;
            bl_UtilityHelper.LockCursor(false);
        }
        bl_GameManager.Instance.SpawnPlayer(PhotonNetwork.LocalPlayer.GetPlayerTeam());
    }

    [PunRPC]
    void RpcStartTime(int wait)
    {
        if (bl_GameManager.Instance.LocalPlayerTeam == Team.None) return;

        SetTimeState(RoomTimeState.Started, PhotonNetwork.IsMasterClient);
        bl_UIReferences.Instance.SetWaitingPlayersText("STARTING MATCH...", true);
        Invoke("StartTime", wait);
    }

    /// <summary>
    /// 
    /// </summary>
    void StartTime()
    {
        if (bl_GameData.Instance.useCountDownOnStart)
        {
            StartCountDown();
        }
        else
        {
            GetTime(false);
        }
        Pause = false;
        bl_UIReferences.Instance.SetWaitingPlayersText();
        bl_GameManager.Instance.SpawnPlayer(PhotonNetwork.LocalPlayer.GetPlayerTeam());
    }

    void OnMasterClientSwitch(Player newMaster)
    {
        if (newMaster.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            if (TimeState == RoomTimeState.Countdown)
            {
                StartCountDown();
            }
        }
    }

    bool GetTimeServed
    {
        get
        {
            bool m_bool = false;
            if (Time.timeSinceLevelLoad > 7)
            {
                m_bool = true;
            }
            return m_bool;
        }
    }

    private static bl_MatchTimeManager _instance;
    public static bl_MatchTimeManager Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<bl_MatchTimeManager>(); }
            return _instance;
        }
    }
}