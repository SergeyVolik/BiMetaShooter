using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class bl_PhotonStatistics : bl_PhotonHelper, IConnectionCallbacks

{
    [Header("Settings")]
    [Range(1,5)]public float RandomTime = 2;
    [Range(1,5)]public float UpdateEach = 10;
	[Header("References")]
    [SerializeField]private GameObject RootUI;
    [SerializeField]private Text AllRoomText;
    [SerializeField]private Text AllPlayerText;
    [SerializeField]private Text AllPlayerInRoomText;
    [SerializeField]private Text AllPlayerInLobbyText;
    [SerializeField]private Text PingText;
    [SerializeField]private Image PingImage;

    private float GetTime;
    private int AllRooms;
    private int AllPlayers;
    private int AllPlayerInRoom;
    private int AllPlayerInLobby;
    private bool Started = false;
#if LOCALIZATION
    private int[] LocalizatedKeysID = new int[] { 48, 49, 50, 51 };
    private string[] LocalizedTexts = new string[4];
#endif

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        if (!Started)
        {
            RootUI.SetActive(PhotonNetwork.IsConnected);
            Started = true;
        }
        PhotonNetwork.AddCallbackTarget(this);
    }

    /// <summary>
    /// 
    /// </summary>
    void OnEnable()
    {
#if LOCALIZATION
        LocalizedTexts = bl_Localization.Instance.GetTextArray(LocalizatedKeysID);
        bl_Localization.Instance.OnLanguageChange += OnLangChange;
#endif
        Refresh();
        InvokeRepeating("UpdateRepeting", 0, UpdateEach);
    }

    /// <summary>
    /// 
    /// </summary>
    void UpdateRepeting()
    {
        Refresh();
    }

    /// <summary>
    /// 
    /// </summary>
    void OnDisable()
    {
        CancelInvoke();
        StopAllCoroutines();
        PhotonNetwork.RemoveCallbackTarget(this);
#if LOCALIZATION
        bl_Localization.Instance.OnLanguageChange -= OnLangChange;
#endif
    }

#if LOCALIZATION
    void OnLangChange(Dictionary<string, string> lang)
    {
        LocalizedTexts = bl_Localization.Instance.GetTextArray(LocalizatedKeysID);
        Refresh();
    }
#endif
    /// <summary>
    /// 
    /// </summary>
    public void Refresh()
    {
        StopAllCoroutines();
        StartCoroutine(GetStaticsIE());
        GetPing();
    }

    /// <summary>
    /// 
    /// </summary>
    void GetPing()
    {
        int ping = PhotonNetwork.GetPing();
        if (ping <= 150)
        {
            PingImage.color = Color.green;
        }
        else if (ping > 150 && ping < 225)
        {
            PingImage.color = Color.yellow;
        }
        else if (ping > 225)
        {
            PingImage.color = Color.red;
        }
        float percet = ping * 100 / 500;
        PingImage.fillAmount = 1 - (percet * 0.01f);
        PingText.text = ping.ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator GetStaticsIE()
    {
        GetTime = RandomTime;
        while(GetTime > 0)
        {
            GetTime -= Time.deltaTime;
            AllRooms = Random.Range(0, 999);
            AllPlayers = Random.Range(0, 999);
            AllPlayerInRoom = Random.Range(0, 999);
            AllPlayerInLobby = Random.Range(0, 999);
            Set();
            yield return new WaitForEndOfFrame();
        }
        GetPhotonStatics();
        Set();
    }

    /// <summary>
    /// 
    /// </summary>
    void GetPhotonStatics()
    {
        AllRooms = PhotonNetwork.CountOfRooms;
        AllPlayers = PhotonNetwork.CountOfPlayers;
        AllPlayerInRoom = PhotonNetwork.CountOfPlayersInRooms;
        AllPlayerInLobby = PhotonNetwork.CountOfPlayersOnMaster;
    }

    /// <summary>
    /// 
    /// </summary>
    void Set()
    {
#if LOCALIZATION
        AllRoomText.text = string.Format(LocalizedTexts[0].ToUpper(), AllRooms);
        AllPlayerText.text = string.Format(LocalizedTexts[1].ToUpper(), AllPlayers);
        AllPlayerInRoomText.text = string.Format(LocalizedTexts[2].ToUpper(), AllPlayerInRoom);
        AllPlayerInLobbyText.text = string.Format(LocalizedTexts[3].ToUpper(), AllPlayerInLobby);
#else
        AllRoomText.text = string.Format(bl_GameTexts.RoomsCreated, AllRooms);
        AllPlayerText.text = string.Format(bl_GameTexts.PlayersOnline, AllPlayers);
        AllPlayerInRoomText.text = string.Format(bl_GameTexts.PlayersPlaying, AllPlayerInRoom);
        AllPlayerInLobbyText.text = string.Format(bl_GameTexts.PlayersInLobby, AllPlayerInLobby);
#endif
    }

    public void OnConnected()
    {
        RootUI.SetActive(true);
    }

    public void OnConnectedToMaster()
    {
        RootUI.SetActive(true);
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        RootUI.SetActive(false);
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
}