/////////////////////////////////////////////////////////////////////////////////
////////////////////////////////bl_RoomMenu.cs///////////////////////////////////
/////////////////place this in a scene for handling menus of room////////////////
/////////////////////////////////////////////////////////////////////////////////
///////////////////////////////Lovatto Studio////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using System;

public class bl_RoomMenu : bl_MonoBehaviour
{
    public bool isPlaying { get; set; }
    [HideInInspector]
    public float m_sensitive = 2.0f,SensitivityAim;
    [HideInInspector]
    public bool ShowWarningPing = false;

    [HideInInspector]
    public bool showMenu = true;
    [HideInInspector]
    public bool isFinish = false;
    [HideInInspector]
    public bool SpectatorMode, WaitForSpectator = false;
    /// <summary>
    /// Reference of player class select
    /// </summary>
    public static PlayerClass PlayerClass = PlayerClass.Assault;
    [Header("Inputs")]
    public KeyCode ScoreboardKey = KeyCode.N;
    public KeyCode PauseMenuKey = KeyCode.Escape;
    [Header("LeftRoom")]
    [Range(0.0f,5)]
    public float DelayLeave = 1.5f;

    private bl_GameManager GM;  
    private bl_UIReferences UIReferences;
#if ULSP
    private bl_DataBase DataBase;
#endif
    public Action<Team> onWaitUntilRoundFinish; //event called when a player enter but have to wait until current round finish.
    public bool isCursorLocked { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        if (!isConnected)
            return;

        base.Awake();
        GM = FindObjectOfType<bl_GameManager>();
        UIReferences = FindObjectOfType<bl_UIReferences>();
#if ULSP
        DataBase = bl_DataBase.Instance;
        if (DataBase != null) { DataBase.RecordTime(); }
#endif
        ShowWarningPing = false;
        showMenu = true;
        GetPrefabs();
        bl_UIReferences.Instance.PlayerUI.PlayerUICanvas.enabled = false;

#if INPUT_MANAGER
        bl_Input.CheckGamePadRequired();
#endif
    }

    protected override void OnEnable()
    {
        bl_EventHandler.onLocalPlayerSpawn += OnPlayerSpawn;
        bl_EventHandler.onLocalPlayerDeath += OnPlayerLocalDeath;
#if MFPSM
        bl_TouchHelper.OnPause += OnPause;
#endif
        bl_PhotonCallbacks.LeftRoom += OnLeftRoom;
    }

    protected override void OnDisable()
    {
        bl_EventHandler.onLocalPlayerSpawn -= OnPlayerSpawn;
        bl_EventHandler.onLocalPlayerDeath -= OnPlayerLocalDeath;
#if MFPSM
        bl_TouchHelper.OnPause -= OnPause;
#endif
        bl_PhotonCallbacks.LeftRoom -= OnLeftRoom;
    }

    void OnPlayerSpawn()
    {
        bl_UIReferences.Instance.PlayerUI.PlayerUICanvas.enabled = true;
    }

    void OnPlayerLocalDeath()
    {
        bl_UIReferences.Instance.PlayerUI.PlayerUICanvas.enabled = false;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        isCursorLocked = bl_UtilityHelper.GetCursorState;
        PauseControll();
        ScoreboardControll();

        if (SpectatorMode && Input.GetKeyUp(KeyCode.Escape)) { bl_UtilityHelper.LockCursor(false); }
    }

    /// <summary>
    /// 
    /// </summary>
    void PauseControll()
    {
        bool pauseKey = Input.GetKeyDown(PauseMenuKey);
#if INPUT_MANAGER
        if (bl_Input.isGamePad)
        {
            pauseKey = bl_Input.isStartPad;
        }
#endif
        if (pauseKey && GM.alreadyEnterInGame && !isFinish && !SpectatorMode)
        {
            bool asb = UIReferences.isMenuActive;
            asb = !asb;
            UIReferences.ShowMenu(asb);
            bl_UtilityHelper.LockCursor(!asb);
            bl_UCrosshair.Instance.Show(!asb);
        }
    }

    public void OnPause()
    {
        if (GM.alreadyEnterInGame && !isFinish && !SpectatorMode)
        {
            bool asb = UIReferences.isMenuActive;
            asb = !asb;
            UIReferences.ShowMenu(asb);
            bl_UtilityHelper.LockCursor(!asb);
            bl_UCrosshair.Instance.Show(!asb);
        }
    }

    public bool isPaused { get { return UIReferences.isMenuActive; } }

    /// <summary>
    /// 
    /// </summary>
    void ScoreboardControll()
    {
        if (!UIReferences.isOnlyMenuActive && !isFinish)
        {
            if (Input.GetKeyDown(ScoreboardKey))
            {
                bool asb = UIReferences.isScoreboardActive;
                asb = !asb;
                UIReferences.ShowScoreboard(asb);
            }
            if (Input.GetKeyUp(ScoreboardKey))
            {
                bool asb = UIReferences.isScoreboardActive;
                asb = !asb;
                UIReferences.ShowScoreboard(asb);
            }
        }
    }

    public void OnSpectator(bool active)
    {
        SpectatorMode = active;
        bl_UtilityHelper.LockCursor(active);
        if (active)
        {
            this.GetComponentInChildren<Camera>().transform.rotation = Quaternion.identity;
        }
        GetComponentInChildren<bl_RoomCamera>().enabled = active;
    }

    /// <summary>
    /// Use for change player class for next Re spawn
    /// </summary>
    /// <param name="m_class"></param>
    public void ChangeClass()
    {
        if (isPlaying && GM.alreadyEnterInGame)
        {
            bl_UIReferences.Instance.ButtonsClassPlay.SetActive(false);
            bl_UtilityHelper.LockCursor(true);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnAutoTeam()
    {
        bl_UtilityHelper.LockCursor(true);
        showMenu = false;
        isPlaying = true;
        bl_UIReferences.Instance.ButtonsClassPlay.SetActive(false);
    }

    /// <summary>
    /// 
    /// </summary>
    public void JoinTeam(int id)
    {
        Team team = (Team)id;
        string tn = team.GetTeamName();
        string joinText = isOneTeamMode ? bl_GameTexts.JoinedInMatch : bl_GameTexts.JoinIn;

#if LOCALIZATION
        joinText = isOneTeamMode ? bl_Localization.Instance.GetText(17) : bl_Localization.Instance.GetText(23);
#endif
        if (isOneTeamMode)
        {
            bl_KillFeed.Instance.SendMessageEvent(string.Format("{0} {1}", PhotonNetwork.NickName, joinText));
        }
        else
        {
            string jt = string.Format("{0} {1}", joinText, tn);
            bl_KillFeed.Instance.SendTeamHighlightMessage(PhotonNetwork.NickName, jt, team);
        }
        showMenu = false;
#if !PSELECTOR
        bl_UtilityHelper.LockCursor(true);
        isPlaying = true;
#else
        if (MFPS.PlayerSelector.bl_PlayerSelectorData.Instance.PlayerSelectorMode == MFPS.PlayerSelector.bl_PlayerSelectorData.PSType.InLobby)
        {
            bl_UtilityHelper.LockCursor(true);
            isPlaying = true;
        }
#endif
        //if player only spawn when a new round start
        if (GetGameMode.GetGameModeInfo().onRoundStartedSpawn == bl_GameData.GameModesEnabled.OnRoundStartedSpawn.WaitUntilRoundFinish && GM.GameMatchState == MatchState.Playing)
        {
            //subscribe to the start round event
            if(onWaitUntilRoundFinish != null) { onWaitUntilRoundFinish.Invoke(team); }
            bl_GameManager.Instance.SetLocalPlayerToTeam(team);//set the player to the selected team but not spawn yet.
            return;
        }
        //set the player to the selected team and spawn the player
        GM.SpawnPlayer(team);
    }

    /// <summary>
    /// 
    /// </summary>
    public void LeftOfRoom()
    {
#if ULSP
        if (DataBase != null)
        {
            Player p = PhotonNetwork.LocalPlayer;
            DataBase.SaveData(p.GetPlayerScore(), p.GetKills(), p.GetDeaths());
            DataBase.StopAndSaveTime();
        }
#endif
        //Good place to save info before reset statistics
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
#if UNITY_EDITOR
            if (isApplicationQuitting) { return; }
#endif
            bl_UtilityHelper.LoadLevel(bl_GameData.Instance.MainMenuScene);
        }
    }

    public bool isApplicationQuitting { get; set; } = false;
    void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Suicide()
    {
        PhotonView view = PhotonView.Find(bl_GameManager.LocalPlayerViewID);
        if (view != null)
        {

            bl_PlayerHealthManager pdm = view.GetComponent<bl_PlayerHealthManager>();
            pdm.Suicide();
            bl_UtilityHelper.LockCursor(true);
            showMenu = false;
            if (view.IsMine)
            {
                bl_GameManager.SuicideCount++;
                //Debug.Log("Suicide " + bl_GameManager.SuicideCount + " times");
                //if player is a joker o abuse of suicide, them kick of room
                if (bl_GameManager.SuicideCount >= 3)//Max number of suicides  = 3, you can change
                {
                    isPlaying = false;
                    bl_GameManager.isLocalAlive = false;
                    bl_UtilityHelper.LockCursor(false);
                    LeftOfRoom();
                }
            }
        }
        else
        {
            Debug.LogError("This view " + bl_GameManager.LocalPlayerViewID + " is not found");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void GetPrefabs()
    {
        PlayerClass = PlayerClass.GetSavePlayerClass();
#if CLASS_CUSTOMIZER
        PlayerClass = bl_ClassManager.Instance.m_Class;
#endif
        UIReferences.OnChangeClass(PlayerClass);
    }

    private bool imv = false;
    public bool SetIMV
    {
        get
        {
            return imv;
        }set
        {
            imv = value;
            PlayerPrefs.SetInt(PropertiesKeys.InvertMouseVertical, (value) ? 1 : 0);
        }
    }

      private bool imh = false;
    public bool SetIMH
    {
        get
        {
            return imh;
        }
        set
        {
            imh = value;
            PlayerPrefs.SetInt(PropertiesKeys.InvertMouseHorizontal, (value) ? 1 : 0);
        }
    }

    public bool isMenuOpen
    {
        get
        {
            return UIReferences.State != bl_UIReferences.RoomMenuState.Hidde;
        }
    }

   public void OnLeftRoom()
   {
       Debug.Log("Local client left the room");
        PhotonNetwork.IsMessageQueueRunning = false;
       this.GetComponent<bl_MatchTimeManager>().enabled = false;
       StartCoroutine(UIReferences.FinalFade(true));
   }
    private int _weaponFov = -1;
    public int WeaponCameraFov
    {
        get
        {
            if(_weaponFov == -1) { _weaponFov = PlayerPrefs.GetInt(PropertiesKeys.WeaponFov, bl_GameData.Instance.DefaultSettings.DefaultWeaponFoV); }
            return _weaponFov;
        }
        set
        {
            _weaponFov = value;
        }
    }

    private static bl_RoomMenu _instance;
    public static bl_RoomMenu Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<bl_RoomMenu>(); }
            return _instance;
        }
    }
}