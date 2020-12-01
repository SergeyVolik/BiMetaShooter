using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using MFPSEditor;

public class bl_GameData : ScriptableObject
{
    [Header("Game Settings")]
    [LovattoToogle] public bool offlineMode = false;
    [LovattoToogle] public bool UseLobbyChat = true;
    [LovattoToogle] public bool UseVoiceChat = true;
    [LovattoToogle] public bool BulletTracer = false;
    [LovattoToogle] public bool DropGunOnDeath = true;
    [LovattoToogle] public bool SelfGrenadeDamage = true;
    [LovattoToogle] public bool CanFireWhileRunning = true;
    [LovattoToogle] public bool HealthRegeneration = true;
    [LovattoToogle] public bool ShowTeamMateHealthBar = true;
    [LovattoToogle] public bool CanChangeTeam = false;
    [LovattoToogle] public bool ShowBlood = true;
    [LovattoToogle] public bool DetectAFK = false;
    [LovattoToogle] public bool MasterCanKickPlayers = true;
    [LovattoToogle] public bool ArriveKitsCauseDamage = true;
    [LovattoToogle] public bool CalculateNetworkFootSteps = false;
    [LovattoToogle] public bool ShowNetworkStats = false;
    [LovattoToogle] public bool RememberPlayerName = true;
    [LovattoToogle] public bool ShowWeaponLoadout = true;
    [LovattoToogle] public bool useCountDownOnStart = true;
    [LovattoToogle] public bool showCrosshair = true;
    [LovattoToogle] public bool doSpawnHandMeshEffect = true;
    [LovattoToogle] public bool playerCameraWiggle = true;
#if MFPSM
    [LovattoToogle] public bool AutoWeaponFire = false;
#endif
#if LM
    [LovattoToogle] public bool LockWeaponsByLevel = true;
#endif
    public AmmunitionType AmmoType = AmmunitionType.Bullets;
    public KillFeedWeaponShowMode killFeedWeaponShowMode = KillFeedWeaponShowMode.WeaponIcon;
    public LobbyJoinMethod lobbyJoinMethod = LobbyJoinMethod.WaitingRoom;
    public bl_KillCam.KillCameraType killCameraType = bl_KillCam.KillCameraType.ObserveDeath;

    [Header("Rewards")]
    public ScoreRewards ScoreReward;
    public VirtualCoin VirtualCoins;

    [Header("Settings")]
    public string GameVersion = "1.0";
    [Range(0, 10)] public int SpawnProtectedTime = 5;
    [Range(1, 60)] public int CountDownTime = 7;
    [Range(1, 10)] public float PlayerRespawnTime = 5.0f;
    [Range(1, 100)] public int MaxFriendsAdded = 25;
    public float AFKTimeLimit = 60;
    public int MaxChangeTeamTimes = 3;
    public string MainMenuScene = "MainMenu";
    public string OnDisconnectScene = "MainMenu";
    public Color highLightColor = Color.green;

    [Header("Levels Manager")]
    [Reorderable]
    public List<SceneInfo> AllScenes = new List<SceneInfo>();

    [Header("Weapons")]
    /* [Reorderable]*/
    public List<bl_GunInfo> AllWeapons = new List<bl_GunInfo>();

    [Header("Default Settings")]
    public DefaultSettingsData DefaultSettings;

    [Header("Game Modes Available"), Reorderable]
    public List<GameModesEnabled> gameModes = new List<GameModesEnabled>();

    [Header("Teams")]
    public string Team1Name = "Team1";
    public Color Team1Color = Color.blue;
    [Space(5)]
    public string Team2Name = "Team2";
    public Color Team2Color = Color.green;

    [Header("Players")]
    public bl_PlayerNetwork Player1;
    public bl_PlayerNetwork Player2;

    [Header("Bots")]
    public bl_AIShooterAgent BotTeam1;
    public bl_AIShooterAgent BotTeam2;

    [Header("Game Team")]
    public List<GameTeamInfo> GameTeam = new List<GameTeamInfo>();

    public GameTeamInfo CurrentTeamUser { get; set; } = null;
    [HideInInspector] public bool isChating = false;

    [HideInInspector] public string _MFPSLicense = string.Empty;
    [HideInInspector] public int _MFPSFromStore = 2;
    [HideInInspector] public string _keyToken = "";
    
    public bl_GunInfo GetWeapon(int ID)
    {
        if (ID < 0 || ID > AllWeapons.Count - 1)
            return AllWeapons[0];
        
        return AllWeapons[ID];
    }

    public string[] AllWeaponStringList()
    {
        return AllWeapons.Select(x => x.Name).ToList().ToArray();
    }
    
    public int GetWeaponID(string gunName)
    {
        int id = -1;
        if(AllWeapons.Exists(x => x.Name == gunName))
        {
            id = AllWeapons.FindIndex(x => x.Name == gunName);
        }
        return id;
    }

    /// <summary>
    /// 
    /// </summary>
    public int CheckPlayerName(string pName)
    {
        for (int i = 0; i < GameTeam.Count; i++)
        {
            if (pName == GameTeam[i].UserName)
            {
                return 1;
            }
        }
        if (pName.Contains('[') || pName.Contains('{'))
        {
            return 2;
        }
        CurrentTeamUser = null;
        return 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public bool CheckPasswordUse(string PName, string Pass)
    {
        for (int i = 0; i < GameTeam.Count; i++)
        {
            if (PName == GameTeam[i].UserName)
            {
               if(Pass == GameTeam[i].Password)
                {
                    CurrentTeamUser = GameTeam[i];
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
#if CLANS
    private string _role = string.Empty;
#endif
    public string RolePrefix
    {
        get
        {
#if !CLANS
            if (CurrentTeamUser != null && !string.IsNullOrEmpty(CurrentTeamUser.UserName))
            {
                return string.Format("<color=#{1}>[{0}]</color>", CurrentTeamUser.m_Role.ToString(), ColorUtility.ToHtmlStringRGBA(CurrentTeamUser.m_Color));
            }
            else
            {
                return string.Empty;
            }
#else
            if(bl_DataBase.Instance == null || !bl_DataBase.Instance.isLogged || !bl_DataBase.Instance.LocalUser.Clan.haveClan)
            {
                return string.Empty;
            }
            else
            {
                if (string.IsNullOrEmpty(_role))
                {
                    _role = string.Format("[{0}]", bl_DataBase.Instance.LocalUser.Clan.Name);
                }
                return _role;
            }
#endif
        }

    }

    void OnDisable()
    {
        isDataCached = false;
    }

    [System.Serializable]
    public class GameTeamInfo
    {
        public string UserName;
        public Role m_Role = Role.Moderator;
        public string Password;
        public Color m_Color;

        public enum Role
        {
            Admin = 0,
            Moderator = 1,
        }
    }

    /// <summary>
    /// cache the GameData from Resources asynchronous to avoid overhead and freeze the main thread the first time we access to the instance
    /// </summary>
    /// <returns></returns>
    public static IEnumerator AsyncLoadData()
    {
        if (m_Data == null)
        {
            isCaching = true;
            ResourceRequest rr = Resources.LoadAsync("GameData", typeof(bl_GameData));
            while (!rr.isDone) { yield return null; }
            m_Data = rr.asset as bl_GameData;
            isCaching = false;
        }
        isDataCached = true;
    }

    public static bool isDataCached = false;
    private static bool isCaching = false;
    private static bl_GameData m_Data;
    public static bl_GameData Instance
    {
        get
        {
            if (m_Data == null && !isCaching)
            {
                m_Data = Resources.Load("GameData", typeof(bl_GameData)) as bl_GameData;
            }
            return m_Data;
        }
    }

    [System.Serializable]
    public class ScoreRewards
    {
        public int ScorePerKill = 50;
        public int ScorePerHeadShot = 25;
        public int ScoreForWinMatch = 100;
        [Tooltip("Per minute played")]
        public int ScorePerTimePlayed = 3;
    }

    [System.Serializable]
    public class VirtualCoin
    {
        public int InitialCoins = 1000;
        [Tooltip("how much score/xp worth one coin")]
        public int CoinScoreValue = 1000;//how much score/xp worth one coin

        public int UserCoins { get; set; }

        public void LoadCoins(string userName)
        {
            UserCoins = PlayerPrefs.GetInt(string.Format("{0}.{1}", userName, PropertiesKeys.UserCoins), InitialCoins);
        }

        public void SetCoins(int coins, string userName)
        {
            LoadCoins(userName);
            int total = UserCoins + coins;
            PlayerPrefs.SetInt(string.Format("{0}.{1}", userName, PropertiesKeys.UserCoins), total);
            UserCoins = total;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        for (int i = 0; i < AllScenes.Count; i++)
        {
            if (AllScenes[i].m_Scene == null) continue;
            AllScenes[i].RealSceneName = AllScenes[i].m_Scene.name;
        }
    }
#endif

    [Serializable]
    public class SceneInfo
    {
        public string ShowName;
        [SerializeField]
        public Object m_Scene;
        [HideInInspector] public string RealSceneName;
        [SpritePreview] public Sprite Preview;
    }

    [Serializable]
    public class DefaultSettingsData
    {
        [Range(1, 20)] public float DefaultSensitivity = 5.0f;
        [Range(1, 20)] public float DefaultSensitivityAim = 2;
        public int DefaultQualityLevel = 3;
        public int DefaultAnisoTropic = 2;
        [Range(0, 1)] public float DefaultVolume = 1;
        [Range(40, 100)] public int DefaultWeaponFoV = 60;
        public bool DefaultShowFrameRate = false;
        public bool DefaultMotionBlur = true;
        public int[] frameRateOptions = new int[] { 30, 60, 120, 144, 200, 260, 0 };
        public int defaultFrameRate = 2;
    }

    [Serializable]
    public class GameModesEnabled
    {
        public string ModeName;
        public GameMode gameMode;
        public bool isEnabled = true;

        [Header("Settings")]
        public bool AutoTeamSelection = false;
        [Range(1,16)] public int RequiredPlayersToStart = 1;
        public int[] GameGoalsOptions = new int[] { 50, 100, 150, 200 };
        public string GoalName = "Kills";
        public OnRoundStartedSpawn onRoundStartedSpawn = OnRoundStartedSpawn.SpawnAfterSelectTeam;
        public OnPlayerDie onPlayerDie = OnPlayerDie.SpawnAfterDelay;

        public string GetGoalFullName(int goalID) { return string.Format("{0} {1}", GameGoalsOptions[goalID], GoalName); }

        [System.Serializable]
        public enum OnRoundStartedSpawn
        {
            SpawnAfterSelectTeam,
            WaitUntilRoundFinish,
        }

        [System.Serializable]
        public enum OnPlayerDie
        {
            SpawnAfterDelay,
            SpawnAfterRoundFinish,
        }
    }
}