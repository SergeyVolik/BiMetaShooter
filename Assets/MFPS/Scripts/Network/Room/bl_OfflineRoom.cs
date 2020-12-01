using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[DefaultExecutionOrder(-1000)]
public class bl_OfflineRoom : MonoBehaviour, IConnectionCallbacks
{
    [Header("Offline Room")]
    public GameMode gameMode = GameMode.FFA;
    public bool withBots = false;
    public bool autoTeamSelection = true;
    [Range(1, 10)] public int maxPlayers = 1;

    [Header("References")]
    public GameObject PhotonObject;

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        PhotonNetwork.AddCallbackTarget(this);
        if (!bl_PhotonNetwork.IsConnectedInRoom)
        {
            if (bl_GameData.Instance.offlineMode)
            {
#if CLASS_CUSTOMIZER
                bl_ClassManager.Instance.Init();
#endif
#if INPUT_MANAGER
                bl_Input.Initialize();
#endif
                PhotonNetwork.OfflineMode = true;
                PhotonNetwork.NickName = "Offline Player";
                Instantiate(PhotonObject);
            }
            else
            {
                PhotonNetwork.OfflineMode = false;
            }
        }
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
    public void OnConnectedToMaster()
    {
        Debug.Log("Offline Connected to Master");
        Hashtable roomOption = new ExitGames.Client.Photon.Hashtable();
        roomOption[PropertiesKeys.TimeRoomKey] = 9989;
        roomOption[PropertiesKeys.GameModeKey] = gameMode.ToString();
        roomOption[PropertiesKeys.SceneNameKey] = bl_GameData.Instance.AllScenes[0].RealSceneName;
        roomOption[PropertiesKeys.RoomRoundKey] = RoundStyle.OneMacht;
        roomOption[PropertiesKeys.TeamSelectionKey] = autoTeamSelection;
        roomOption[PropertiesKeys.CustomSceneName] = bl_GameData.Instance.AllScenes[0].ShowName;
        roomOption[PropertiesKeys.RoomGoal] = 100;
        roomOption[PropertiesKeys.RoomFriendlyFire] = false;
        roomOption[PropertiesKeys.MaxPing] = 1000;
        roomOption[PropertiesKeys.RoomPassword] = "";
        roomOption[PropertiesKeys.WithBotsKey] = withBots;
        PhotonNetwork.CreateRoom("Offline Room", new RoomOptions()
        {
            MaxPlayers = (byte)maxPlayers,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = roomOption,
            CleanupCacheOnLeave = true,
            PublishUserId = true,
            EmptyRoomTtl = 0,
        }, null);
    }


    public void OnConnected()
    {
    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {      
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {     
    }

    public void OnDisconnected(DisconnectCause cause)
    {     
    }

    public void OnRegionListReceived(RegionHandler regionHandler)
    {      
    }
}