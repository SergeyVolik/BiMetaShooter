using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System;

public class bl_LobbyUI : MonoBehaviour
{
    public string MainWindowName = "room list";
    public List<WindowUI> windows = new List<WindowUI>();
    public List<PopUpWindows> popUpWindows = new List<PopUpWindows>();
    private string currentWindow = "";

    [Header("References")]
    public CanvasGroup FadeAlpha;
    public Text PlayerNameText = null;
    public Text PlayerCoinsText = null;
    public GameObject RoomInfoPrefab;
    public GameObject PhotonStaticticsUI;
    public GameObject PhotonGamePrefab;
    public GameObject EnterPasswordUI;
    public GameObject SeekingMatchUI;
    public GameObject DisconnectCauseUI;
    public Transform RoomListPanel;
    public Text MaxPlayerText = null;
    public Text RoundTimeText = null;
    public Text GameModeText = null;
    public Text MaxKillsText = null;
    public Text MaxPingText = null;
    public Text MapNameText = null;
    [SerializeField] private Text PasswordLogText = null;
    public Text LoadingScreenText;
    [SerializeField] private Text NoRoomText;
    [SerializeField] private Slider VolumeSlider;
    [SerializeField] private Slider BackgroundVolumeSlider;
    [SerializeField] private Slider SensitivitySlider;
    [SerializeField] private Slider AimSensitivitySlider;
    [SerializeField] private Slider WeaponFOVSlider;
    [SerializeField] private Dropdown QualityDropdown;
    [SerializeField] private Dropdown AnisoDropdown;
    [SerializeField] private Image MapPreviewImage = null;
    public Image LevelIcon;
    public InputField PlayerNameField = null;
    public InputField RoomNameField = null;
    public InputField PassWordField;
    [SerializeField] private Toggle MuteVoiceToggle;
    [SerializeField] private Toggle PushToTalkToogle;
    [SerializeField] private Toggle InvertMouseXToogle;
    [SerializeField] private Toggle InvertMouseYToogle;
    [SerializeField] private Toggle FrameRateToggle;
    public Toggle WithBotsToggle;
    [SerializeField] private Dropdown ServersDropdown;
    public CanvasGroup LoadingScreen;
    public CanvasGroup BlackScreen;
    public bl_FriendListUI FriendUI;
    [SerializeField] private Button[] ClassButtons;
    public GameObject[] OptionsWindows;
    public GameObject[] AddonsButtons;
    public LevelUI m_LevelUI;
    public AnimationCurve FadeCurve;
    private Dictionary<string, RoomInfo> cachedRoomList;

    #region Private Variables
    private List<GameObject> CacheRoomList = new List<GameObject>();
    private int m_currentQuality = 3;
    private float m_volume = 1.0f;
    private float BackgroundVolume = 0.3f;
    private float m_sensitive = 15;
    private float AimSensitivity = 7;
    private bool FrameRate = false;
    private bool imh;
    private bool imv;
    private bool MuteVoice;
    private bool PushToTalk;
    private int m_stropic = 0;
    private int m_WeaponFov;
    #endregion

    /// <summary>
    /// 
    /// </summary>
    public void InitialSetup()
    {
        cachedRoomList = new Dictionary<string, RoomInfo>();
#if LOCALIZATION
        bl_Localization.Instance.OnLanguageChange += OnLanguageChange;
#endif
        windows.ForEach(x => { if (x.UIRoot != null) { x.UIRoot.SetActive(false); } });//disable all windows
        if (PhotonStaticticsUI != null) { PhotonStaticticsUI.SetActive(bl_Lobby.Instance.ShowPhotonStatistics); }
        BlackScreen.gameObject.SetActive(true);
        BlackScreen.alpha = 1;
    }

    /// <summary>
    /// 
    /// </summary>
    void OnDisable()
    {
#if LOCALIZATION
        bl_Localization.Instance.OnLanguageChange -= OnLanguageChange;
#endif
    }

    /// <summary>
    /// display all available rooms
    /// </summary>
    public void SetRoomList(List<RoomInfo> rooms)
    {
        //Removed old list
        if (CacheRoomList.Count > 0)
        {
            CacheRoomList.ForEach(x => Destroy(x));
            CacheRoomList.Clear();
        }
        UpdateCachedRoomList(rooms);
        InstanceRoomList();
    }

    /// <summary>
    /// 
    /// </summary>
    void InstanceRoomList()
    {
        if (cachedRoomList.Count > 0)
        {
            NoRoomText.text = string.Empty;
            foreach (RoomInfo info in cachedRoomList.Values)
            {
                if (info.Name == bl_Lobby.Instance.justCreatedRoomName) continue;

                GameObject entry = Instantiate(RoomInfoPrefab);
                entry.transform.SetParent(RoomListPanel, false);
                entry.GetComponent<bl_RoomInfo>().GetInfo(info);
                CacheRoomList.Add(entry);
            }

        }
        else
        {
#if LOCALIZATION
            NoRoomText.text = bl_Localization.Instance.GetText("norooms");
#else

            NoRoomText.text = bl_GameTexts.NoRoomsCreated;
#endif
        }
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // Remove room from cached room list if it got closed, became invisible or was marked as removed
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList.Remove(info.Name);
                }

                continue;
            }

            // Update cached room info
            if (cachedRoomList.ContainsKey(info.Name))
            {
                cachedRoomList[info.Name] = info;
            }
            // Add new room info to cache
            else
            {
                cachedRoomList.Add(info.Name, info);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void ChangeWindow(string window)
    {
        if (window == currentWindow) return;//return if we are trying to open the opened window
        WindowUI w = windows.Find(x => x.Name == window);
        if (w == null) return;//the window with that windowName doesn't exist

        StopCoroutine("DoChangeWindow");
        StartCoroutine("DoChangeWindow", w);
        currentWindow = window;
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetEnableWindow(string windowName,bool active)
    {
        WindowUI w = windows.Find(x => x.Name == windowName);
        if (w == null || w.UIRoot == null) return;//the window with that windowName doesn't exist

        w.UIRoot.SetActive(active);
    }

    public void Home() { ChangeWindow(MainWindowName); }
    public void HideAll() { currentWindow = ""; windows.ForEach(x => { if (x.UIRoot != null) { x.UIRoot.SetActive(false); } }); }//disable all windows 

    /// <summary>
    /// 
    /// </summary>
    IEnumerator DoChangeWindow(WindowUI window)
    {
        //now change the windows
        for (int i = 0; i < windows.Count; i++)
        {
            WindowUI w = windows[i];
            if (w.UIRoot == null) continue;

            if (w.isPersistent)
            {
                w.UIRoot.SetActive(!window.hidePersistents);
            }
            else
            {
                w.UIRoot.SetActive(false);
            }
            window.UIRoot.SetActive(true);
            if (window.showTopMenu)
            {
                SetEnableWindow("top menu", true);
            }
        }

        if (window.playFadeInOut)
        {
            float d = 0;
            while (d < 1)
            {
                d += Time.deltaTime * 2;
                FadeAlpha.alpha = d;
                yield return null;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void ChangeOptionsWindow(int id)
    {
        foreach (GameObject g in OptionsWindows) { g.SetActive(false); }
        OptionsWindows[id].SetActive(true);
    }

    /// <summary>
    /// 
    /// </summary>
    public void ShowPopUpWindow(string popUpName)
    {
        PopUpWindows w = popUpWindows.Find(x => x.Name == popUpName);
        if (w == null || w.Window == null) return;

        w.Window.SetActive(true);
    }

    /// <summary>
    /// 
    /// </summary>
    private void Update()
    {
        if (PhotonNetwork.IsConnected && bl_GameData.isDataCached)
        {
            PlayerNameText.text = PhotonNetwork.NickName;
        }
    }

    public void SignOut()
    {
        bl_Lobby.Instance.SignOut();
    }

    #region Settings
    /// <summary>
    /// 
    /// </summary>
    public void LoadSettings()
    {
        m_currentQuality = PlayerPrefs.GetInt(PropertiesKeys.Quality, bl_GameData.Instance.DefaultSettings.DefaultQualityLevel);
        m_stropic = PlayerPrefs.GetInt(PropertiesKeys.Aniso, bl_GameData.Instance.DefaultSettings.DefaultAnisoTropic);
        m_volume = PlayerPrefs.GetFloat(PropertiesKeys.Volume, bl_GameData.Instance.DefaultSettings.DefaultVolume);
        BackgroundVolume = PlayerPrefs.GetFloat(PropertiesKeys.BackgroundVolume, 0.5f);
        AudioListener.volume = m_volume;
        m_sensitive = PlayerPrefs.GetFloat(PropertiesKeys.Sensitivity, bl_GameData.Instance.DefaultSettings.DefaultSensitivity);
        AimSensitivity = PlayerPrefs.GetFloat(PropertiesKeys.SensitivityAim, bl_GameData.Instance.DefaultSettings.DefaultSensitivityAim);
        m_WeaponFov = PlayerPrefs.GetInt(PropertiesKeys.WeaponFov, bl_GameData.Instance.DefaultSettings.DefaultWeaponFoV);
        FrameRate = (PlayerPrefs.GetInt(PropertiesKeys.FrameRate, bl_GameData.Instance.DefaultSettings.DefaultShowFrameRate ? 1 : 0) == 1);
        imv = (PlayerPrefs.GetInt(PropertiesKeys.InvertMouseVertical, 0) == 1);
        imh = (PlayerPrefs.GetInt(PropertiesKeys.InvertMouseHorizontal, 0) == 1);
        MuteVoice = (PlayerPrefs.GetInt(PropertiesKeys.MuteVoiceChat, 0) == 1);
        PushToTalk = (PlayerPrefs.GetInt(PropertiesKeys.PushToTalk, 0) == 1);
        int df = PlayerPrefs.GetInt(PropertiesKeys.FrameRateOption, bl_GameData.Instance.DefaultSettings.defaultFrameRate);
        Application.targetFrameRate = bl_GameData.Instance.DefaultSettings.frameRateOptions[df];
        bool km = PlayerPrefs.GetInt(PropertiesKeys.KickKey, 0) == 1;
        if (km) { ShowPopUpWindow("kicked"); }
        if (bl_PhotonNetwork.Instance.hasPingKick) { ShowPopUpWindow("ping kick"); bl_PhotonNetwork.Instance.hasPingKick = false; }
        PlayerPrefs.SetInt(PropertiesKeys.KickKey, 0);
        bl_Lobby.Instance.rememberMe = !string.IsNullOrEmpty(PlayerPrefs.GetString(PropertiesKeys.RememberMe, string.Empty));
        if (bl_PhotonNetwork.Instance.hasAFKKick) { ShowPopUpWindow("afk kick"); bl_PhotonNetwork.Instance.hasAFKKick = false; }
#if INPUT_MANAGER
        bl_Input.Initialize();
        bl_Input.CheckGamePadRequired();
#endif
#if LM
        if (bl_LevelManager.Instance.isNewLevel)
        {
            LevelInfo info = bl_LevelManager.Instance.GetLevel();
            m_LevelUI.Icon.sprite = info.Icon;
            m_LevelUI.LevelNameText.text = info.Name;
            m_LevelUI.Root.SetActive(true);
            bl_LevelManager.Instance.Refresh();
        }
        bl_LevelManager.Instance.GetInfo();
#endif
#if ULSP
        if (bl_DataBase.Instance != null && bl_DataBase.Instance.isLogged)
        {
            bl_GameData.Instance.VirtualCoins.UserCoins = bl_DataBase.Instance.LocalUser.Coins;
        }
#endif
#if CUSTOMIZER
        AddonsButtons[2].SetActive(true);
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public void FullSetUp()
    {
#if LOCALIZATION
        MaxPlayerText.text = string.Format("{0} {1}", bl_Lobby.Instance.maxPlayers[bl_Lobby.Instance.players], bl_Localization.Instance.GetTextPlural("player"));
        RoundTimeText.text = (bl_Lobby.Instance.RoomTime[bl_Lobby.Instance.r_Time] / 60) + string.Format(" {0}", bl_Localization.Instance.GetTextPlural("minute")); 
        MaxKillsText.text = bl_Lobby.Instance.GetSelectedGoalFullName();
        GameModeText.text = bl_Localization.Instance.GetTextPlural(bl_Lobby.Instance.GameModes[bl_Lobby.Instance.CurrentGameMode].gameMode.ToString().ToLower());
#else
        MaxPlayerText.text = string.Format("{0} {1}", bl_Lobby.Instance.maxPlayers[bl_Lobby.Instance.players], bl_GameTexts.Players);
        RoundTimeText.text = (bl_Lobby.Instance.RoomTime[bl_Lobby.Instance.r_Time] / 60) + " Minutes";
        MaxKillsText.text = bl_Lobby.Instance.GetSelectedGoalFullName();
        GameModeText.text = bl_Lobby.Instance.GameModes[bl_Lobby.Instance.CurrentGameMode].gameMode.GetName();
#endif
        MapNameText.text = bl_GameData.Instance.AllScenes[bl_Lobby.Instance.CurrentScene].ShowName;
        MapPreviewImage.sprite = bl_GameData.Instance.AllScenes[bl_Lobby.Instance.CurrentScene].Preview;
        SensitivitySlider.value = m_sensitive;
        AimSensitivitySlider.value = AimSensitivity;
        if (bl_AudioController.Instance != null)
        {
            BackgroundVolumeSlider.maxValue = bl_AudioController.Instance.MaxBackgroundVolume;
            BackgroundVolumeSlider.value = BackgroundVolume;
        }
        VolumeSlider.value = m_volume;
        QualityDropdown.ClearOptions();
        List<Dropdown.OptionData> od = new List<Dropdown.OptionData>();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            Dropdown.OptionData data = new Dropdown.OptionData();
            data.text = QualitySettings.names[i].ToUpper();
            od.Add(data);
        }
        WithBotsToggle.gameObject.SetActive(true);
        QualityDropdown.AddOptions(od);
        QualityDropdown.value = m_currentQuality;
        PlayerClass p = PlayerClass.Assault; p = p.GetSavePlayerClass();
        bl_LobbyUI.Instance.OnChangeClass((int)p);
        MaxPingText.text = string.Format("{0} ms", bl_Lobby.Instance.MaxPing[bl_Lobby.Instance.CurrentMaxPing]);
        InvertMouseXToogle.isOn = imh;
        InvertMouseYToogle.isOn = imv;
        PushToTalkToogle.isOn = PushToTalk;
        MuteVoiceToggle.isOn = MuteVoice;
        FrameRateToggle.isOn = FrameRate;

        OnChangeQuality(m_currentQuality);
        OnChangeAniso(m_stropic);

#if LM
        LevelIcon.gameObject.SetActive(true);
        LevelInfo pli = bl_LevelManager.Instance.GetLevel();
        LevelIcon.sprite = pli.Icon;
        Text plt = LevelIcon.GetComponentInChildren<Text>();
        if (plt != null) plt.text = pli.LevelID.ToString();
#else
        LevelIcon.gameObject.SetActive(false);
#endif
#if ULSP && CLANS
        AddonsButtons[7].SetActive(true);
#endif
#if SHOP
        AddonsButtons[9].SetActive(true);
#endif
#if PSELECTOR
        AddonsButtons[10].SetActive(MFPS.PlayerSelector.bl_PlayerSelectorData.Instance.PlayerSelectorMode == MFPS.PlayerSelector.bl_PlayerSelectorData.PSType.InLobby);
#endif
        AddonsButtons[0].SetActive(false);
#if CLASS_CUSTOMIZER
        AddonsButtons[0].SetActive(true);
#endif
        SetRegionDropdown();
    }

    /// <summary>
    /// 
    /// </summary>
    void OnLanguageChange(Dictionary<string, string> lang)
    {
#if LOCALIZATION
        NoRoomText.text = bl_Localization.Instance.GetText("norooms");
        MaxPlayerText.text = string.Format("{0} {1}", bl_Lobby.Instance.maxPlayers[bl_Lobby.Instance.players], bl_Localization.Instance.GetTextPlural("player"));
        RoundTimeText.text = (bl_Lobby.Instance.RoomTime[bl_Lobby.Instance.r_Time] / 60) + string.Format(" {0}", bl_Localization.Instance.GetTextPlural("minute"));
        if (bl_GameData.isDataCached)
            MaxKillsText.text = bl_Lobby.Instance.GetSelectedGoalFullName();
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public void ResetSettings()
    {
        LoadSettings();
        FullSetUp();
    }

    /// <summary>
    /// 
    /// </summary>
    void SetRegionDropdown()
    {
        //when Photon Server is used
        if (!PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer)
        {
            //disable the dropdown server selection since it doesn't work with Photon Server.
            ServersDropdown.gameObject.SetActive(false);
            return;
        }
        string key = PlayerPrefs.GetString(PropertiesKeys.PreferredRegion, bl_Lobby.Instance.DefaultServer.ToString());
        string[] Regions = Enum.GetNames(typeof(SeverRegionCode));
        for (int i = 0; i < Regions.Length; i++)
        {
            if (key == Regions[i])
            {
                int id = i;
                if (id > 4) { id--; }
                ServersDropdown.value = id;
                break;
            }
        }
        ServersDropdown.RefreshShownValue();
    }

    /// <summary>
    /// 
    /// </summary>
    public void Save()
    {
        PlayerPrefs.SetInt(PropertiesKeys.Quality, m_currentQuality);
        PlayerPrefs.SetInt(PropertiesKeys.Aniso, m_stropic);
        PlayerPrefs.SetFloat(PropertiesKeys.Volume, m_volume);
        PlayerPrefs.SetFloat(PropertiesKeys.BackgroundVolume, BackgroundVolume);
        PlayerPrefs.SetFloat(PropertiesKeys.Sensitivity, m_sensitive);
        PlayerPrefs.SetFloat(PropertiesKeys.SensitivityAim, AimSensitivity);
        PlayerPrefs.SetInt(PropertiesKeys.WeaponFov, m_WeaponFov);
        PlayerPrefs.SetInt(PropertiesKeys.Quality, m_currentQuality);
        PlayerPrefs.SetInt(PropertiesKeys.Aniso, m_stropic);
        PlayerPrefs.SetInt(PropertiesKeys.MuteVoiceChat, MuteVoice ? 1 : 0);
        PlayerPrefs.SetInt(PropertiesKeys.PushToTalk, PushToTalk ? 1 : 0);
        PlayerPrefs.SetInt(PropertiesKeys.FrameRate, FrameRate ? 1 : 0);
        PlayerPrefs.SetInt(PropertiesKeys.InvertMouseHorizontal, imh ? 1 : 0);
        PlayerPrefs.SetInt(PropertiesKeys.InvertMouseVertical, imv ? 1 : 0);
    }
#endregion

#region UI Callbacks
    /// <summary>
    /// 
    /// </summary>
    public void EnterName(InputField field = null)
    {
        if (field == null || string.IsNullOrEmpty(field.text))
            return;

        int check = bl_GameData.Instance.CheckPlayerName(field.text);
        if (check == 1)
        {
            bl_Lobby.Instance.CachePlayerName = field.text;
            SetEnableWindow("user password", true);
            return;
        }
        else if (check == 2)
        {
            field.text = string.Empty;
            return;
        }
        bl_Lobby.Instance.SetPlayerName(field.text);
#if !ULSP
        PlayerCoinsText.text = bl_GameData.Instance.VirtualCoins.UserCoins.ToString();
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public void EnterPassword(InputField field = null)
    {
        if (field == null || string.IsNullOrEmpty(field.text))
            return;

        string pass = field.text;
        if (!bl_Lobby.Instance.EnterPassword(pass))
        {
            field.text = string.Empty;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void CheckRoomPassword(RoomInfo room)
    {
        EnterPasswordUI.SetActive(true);
        bl_Lobby.Instance.CheckRoomPassword(room);
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnEnterPassworld(InputField pass)
    {
        if (!bl_Lobby.Instance.SetRoomPassworld(pass.text))
        {
            PasswordLogText.text = "Wrong room password";
        }
    }

    public void OnChangeClass(int classID)
    {
        foreach (Button b in ClassButtons) { b.interactable = true; }
        ClassButtons[classID].interactable = false;
        PlayerClass p = (PlayerClass)classID;
        p.SavePlayerClass();
    }

    public void ChangeServerCloud(int id)
    {
        bl_Lobby.Instance.ChangeServerCloud(id);
    }

    public void LoadLocalLevel(string level)
    {
        bl_Lobby.Instance.LoadLocalLevel(level);
    }

    public void UpdateCoinsText()
    {
#if ULSP
        if (bl_DataBase.Instance != null && !bl_DataBase.Instance.isGuest)
        {
            PlayerCoinsText.text = bl_DataBase.Instance.LocalUser.Coins.ToString();
        }
#else
            bl_GameData.Instance.VirtualCoins.LoadCoins(PhotonNetwork.NickName);
            PlayerCoinsText.text = bl_GameData.Instance.VirtualCoins.UserCoins.ToString();
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public void QuitGame(bool b)
    {
        if (b)
        {           
            Application.Quit();
            Debug.Log("Game Exit, this only work in standalone version");
        }
        else
        {
            ChangeWindow("room list");
        }
    }

    public void AutoMatch() { bl_Lobby.Instance.AutoMatch(); }
    public void CreateRoom() { bl_Lobby.Instance.CreateRoom(); }
    public void SetRememberMe(bool value) { bl_Lobby.Instance.SetRememberMe(value); }
#endregion

#region Room Creation Callbacks
    /// <summary>
    /// 
    /// </summary>
    public void ChangeMap(bool plus)
    {
        List<bl_GameData.SceneInfo> m_scenes = bl_GameData.Instance.AllScenes;
        if (!plus)
        {
            bl_Lobby.Instance.CurrentScene = (bl_Lobby.Instance.CurrentScene + 1) % m_scenes.Count;
        }
        else
        {
            if (bl_Lobby.Instance.CurrentScene < m_scenes.Count)
            {
                bl_Lobby.Instance.CurrentScene--;
                if (bl_Lobby.Instance.CurrentScene < 0) bl_Lobby.Instance.CurrentScene = m_scenes.Count - 1;
            }
        }
        MapNameText.text = m_scenes[bl_Lobby.Instance.CurrentScene].ShowName;
        MapPreviewImage.sprite = m_scenes[bl_Lobby.Instance.CurrentScene].Preview;
    }

    public void ChangeMaxPlayer(bool plus)
    {
        if (plus)
        {
            bl_Lobby.Instance.players = (bl_Lobby.Instance.players + 1) % bl_Lobby.Instance.maxPlayers.Length;
        }
        else
        {
            if (bl_Lobby.Instance.players < bl_Lobby.Instance.maxPlayers.Length)
            {
                bl_Lobby.Instance.players--;
                if (bl_Lobby.Instance.players < 0) bl_Lobby.Instance.players = bl_Lobby.Instance.maxPlayers.Length - 1;
            }
        }
#if LOCALIZATION
        MaxPlayerText.text = string.Format("{0} {1}", bl_Lobby.Instance.maxPlayers[ bl_Lobby.Instance.players], bl_Localization.Instance.GetTextPlural("player"));
#else
        MaxPlayerText.text = string.Format("{0} {1}", bl_Lobby.Instance.maxPlayers[bl_Lobby.Instance.players], bl_GameTexts.Players);
#endif
    }

    public void ChangeRoundTime(bool plus)
    {
        if (!plus)
        {
            bl_Lobby.Instance.r_Time = (bl_Lobby.Instance.r_Time + 1) % bl_Lobby.Instance.RoomTime.Length;
        }
        else
        {
            if (bl_Lobby.Instance.r_Time < bl_Lobby.Instance.RoomTime.Length)
            {
                bl_Lobby.Instance.r_Time--;
                if (bl_Lobby.Instance.r_Time < 0) bl_Lobby.Instance.r_Time = bl_Lobby.Instance.RoomTime.Length - 1;
            }
        }
#if LOCALIZATION
        string rtl = string.Format(" {0}", bl_Localization.Instance.GetTextPlural("minute"));
        RoundTimeText.text = (bl_Lobby.Instance.RoomTime[bl_Lobby.Instance.r_Time] / 60) + rtl;
#else
        RoundTimeText.text = (bl_Lobby.Instance.RoomTime[bl_Lobby.Instance.r_Time] / 60) + " Minutes";
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public void ChangeMaxKills(bool plus)
    {
        int[] goals = bl_Lobby.Instance.GetSelectedGameMode().GameGoalsOptions;
        if (!plus)
        {
            bl_Lobby.Instance.CurrentGameGoal = (bl_Lobby.Instance.CurrentGameGoal + 1) % goals.Length;
        }
        else
        {
            if (bl_Lobby.Instance.CurrentGameGoal < goals.Length)
            {
                bl_Lobby.Instance.CurrentGameGoal--;
                if (bl_Lobby.Instance.CurrentGameGoal < 0) bl_Lobby.Instance.CurrentGameGoal = goals.Length - 1;
            }
        }
#if LOCALIZATION
        MaxKillsText.text = bl_Lobby.Instance.GetSelectedGoalFullName();
#else
        MaxKillsText.text = bl_Lobby.Instance.GetSelectedGoalFullName();
#endif
    }

    public void ChangeMaxPing(bool fowr)
    {
        if (fowr)
        {
            bl_Lobby.Instance.CurrentMaxPing = (bl_Lobby.Instance.CurrentMaxPing + 1) % bl_Lobby.Instance.MaxPing.Length;
        }
        else
        {
            if (bl_Lobby.Instance.CurrentMaxPing < bl_Lobby.Instance.MaxPing.Length)
            {
                bl_Lobby.Instance.CurrentMaxPing--;
                if (bl_Lobby.Instance.CurrentMaxPing < 0) bl_Lobby.Instance.CurrentMaxPing = bl_Lobby.Instance.MaxPing.Length - 1;
            }
        }
#if LOCALIZATION
        string f = (bl_Lobby.Instance.MaxPing[bl_Lobby.Instance.CurrentMaxPing] == 0) ? bl_Localization.Instance.GetText(25) : "{0} ms";
        MaxPingText.text = string.Format(f, bl_Lobby.Instance.MaxPing[bl_Lobby.Instance.CurrentMaxPing]);
#else
        string f = (bl_Lobby.Instance.MaxPing[bl_Lobby.Instance.CurrentMaxPing] == 0) ? bl_GameTexts.NoLimit : "{0} ms";
        MaxPingText.text = string.Format(f, bl_Lobby.Instance.MaxPing[bl_Lobby.Instance.CurrentMaxPing]);
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public void ChangeGameMode(bool plus)
    {
        if (plus)
        {
            bl_Lobby.Instance.CurrentGameMode = (bl_Lobby.Instance.CurrentGameMode + 1) % bl_Lobby.Instance.GameModes.Length;
        }
        else
        {
            if (bl_Lobby.Instance.CurrentGameMode < bl_Lobby.Instance.GameModes.Length)
            {
                bl_Lobby.Instance.CurrentGameMode--;
                if (bl_Lobby.Instance.CurrentGameMode < 0) bl_Lobby.Instance.CurrentGameMode = bl_Lobby.Instance.GameModes.Length - 1;
            }
        }
        bl_Lobby.Instance.CurrentGameGoal = 0;
        MaxKillsText.text = bl_Lobby.Instance.GetSelectedGoalFullName();
        WithBotsToggle.gameObject.SetActive(bl_Lobby.Instance.GameModes[bl_Lobby.Instance.CurrentGameMode].gameMode == GameMode.FFA || bl_Lobby.Instance.GameModes[bl_Lobby.Instance.CurrentGameMode].gameMode == GameMode.TDM);
#if LOCALIZATION
        GameModeText.text = bl_Localization.Instance.GetText(bl_Lobby.Instance.GameModes[bl_Lobby.Instance.CurrentGameMode].gameMode.ToString().ToLower());
#else
        GameModeText.text = bl_Lobby.Instance.GameModes[bl_Lobby.Instance.CurrentGameMode].gameMode.GetName();
#endif
    }
#endregion

#region Settings Callbacks
    public void OnChangeQuality(int id)
    {
        QualitySettings.SetQualityLevel(id, true);
        m_currentQuality = id;
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
        m_stropic = id;
    }

    public void ChangeAutoTeamSelection(bool b) { bl_Lobby.Instance.AutoTeamSelection = b; }
    public void ChangeFriendlyFire(bool b) { bl_Lobby.Instance.FriendlyFire = b; }
    public void ChangeGamePerRound(bool b) { bl_Lobby.Instance.GamePerRounds = b; }
    public void ChangeRoomName(string t) { bl_Lobby.Instance.hostName = t; }

    public void ChangeVolume(float v) { m_volume = v; }
    public void ChangeSensitivity(float s) { m_sensitive = s; }
    public void ChangeAimSensitivity(float s) { AimSensitivity = s; }
    public void ChangeWeaponFov(float s) { m_WeaponFov = Mathf.FloorToInt(s); }
    public void OnChangeFrameRate(bool b) { FrameRate = b; }
    public void OnChangeIMV(bool b) { imv = b; }
    public void OnChangeIMH(bool b) { imh = b; }
    public void OnChangeMuteVoice(bool b) { MuteVoice = b; }
    public void OnChangePushToTalk(bool b) { PushToTalk = b; }
    public void OnBackgroundVolume(float v) { BackgroundVolume = v; if (bl_AudioController.Instance != null) { bl_AudioController.Instance.BackgroundVolume = v; } }
#endregion

    public IEnumerator DoBlackFade(bool fadeIn, float duration = 1)
    {
        if (fadeIn)
        {
            BlackScreen.gameObject.SetActive(true);
            float d = 0;
            while(d < 1)
            {
                d += Time.deltaTime / duration;
                BlackScreen.alpha = FadeCurve.Evaluate(d);
                yield return new WaitForEndOfFrame();
            }
        }
        else
        {
            BlackScreen.gameObject.SetActive(true);
            float d = 1;
            while (d > 0)
            {
                d -= Time.deltaTime / duration;
                BlackScreen.alpha = FadeCurve.Evaluate(d);
                yield return new WaitForEndOfFrame();
            }
            BlackScreen.gameObject.SetActive(false);
        }
    }

#region Classes
    [System.Serializable]
    public class WindowUI
    {
        public string Name;
        public GameObject UIRoot;

        public bool isPersistent = false;//this window will stay show up when change window?
        public bool hidePersistents = false;//force hide persistent windows
        public bool playFadeInOut = false;
        public bool showTopMenu = true;
    }

    [System.Serializable]
    public class PopUpWindows
    {
        public string Name;
        public GameObject Window;
    }

    [System.Serializable]
    public class LevelUI
    {
        public GameObject Root;
        public Image Icon;
        public Text LevelNameText;
    }

    private static bl_LobbyUI _instance;
    public static bl_LobbyUI Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindObjectOfType<bl_LobbyUI>();
            }
            return _instance;
        }
    }
#endregion
}