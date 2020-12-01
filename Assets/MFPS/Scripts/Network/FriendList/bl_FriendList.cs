using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine.Networking;

public class bl_FriendList : bl_PhotonHelper, IMatchmakingCallbacks, IConnectionCallbacks, ILobbyCallbacks
{
    [HideInInspector]
    public List<string> Friends = new List<string>();

    public const string FriendSaveKey = "LSFriendList";
    private char splitChar = '/';

    [HideInInspector] public bool WaitForEvent = false;
    private bl_FriendListUI FriendUI;
    [HideInInspector]public List<FriendInfo> FriendList = new List<FriendInfo>();
#if ULSP
    private bl_DataBase DataBase;
#endif

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        FriendUI = bl_LobbyUI.Instance.FriendUI;
        PhotonNetwork.AddCallbackTarget(this);
#if ULSP
        DataBase = bl_DataBase.Instance;
#endif
        if (PhotonNetwork.IsConnected)
        {
            GetFriendsStore();
            InvokeRepeating("UpdateList", 1, 1);
        }
    }

    void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    /// <summary>
    /// 
    /// </summary>
    void GetFriendsStore()
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby)
            return;
        //Get all friends saved 
#if ULSP
        if(DataBase != null)
        {
            Friends.AddRange(DataBase.LocalUser.FriendList.ToArray());
        }
        else
        {
            string cacheFriend = PlayerPrefs.GetString(SaveKey, "Null");
            if (!string.IsNullOrEmpty(cacheFriend))
            {
                string[] splitFriends = cacheFriend.Split(splitChar);
                Friends.AddRange(splitFriends);
            }
        }
#else
        string cacheFriend = PlayerPrefs.GetString(SaveKey, "Null");
        if (!string.IsNullOrEmpty(cacheFriend))
        {
            string[] splitFriends = cacheFriend.Split(splitChar);
            Friends.AddRange(splitFriends);
        }
#endif
        //Find all friends names in photon list.
        if (Friends.Count > 0)
        {
            PhotonNetwork.FindFriends(Friends.ToArray());
            //Update the list UI 
            FriendUI.UpdateFriendList(true);
        }
        else
        {
           // Debug.Log("No friends saved");
            return;
        }
    }

    /// <summary>
    /// Call For Update List of friends.
    /// </summary>
    void UpdateList()
    {
        if (!PhotonNetwork.IsConnected  || PhotonNetwork.InRoom)
            return;

        if (Friends.Count > 0)
        {
            if (Friends.Count > 1 && Friends.Contains("Null"))
            {
                Friends.Remove("Null");
                Friends.Remove("Null");
                SaveFriends();
            }
            if (PhotonNetwork.IsConnectedAndReady && Friends.Count > 0)
            {
                PhotonNetwork.FindFriends(Friends.ToArray());
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void SaveFriends()
    {
        string allfriends = string.Join(splitChar.ToString(), Friends.ToArray());
#if ULSP
        if(DataBase != null && !DataBase.isGuest)
        {
            StartCoroutine(SaveInDataBase(allfriends));
        }
        else
        {
            PlayerPrefs.SetString(SaveKey, allfriends);
        }
#else
         PlayerPrefs.SetString(SaveKey, allfriends);
#endif
    }

#if ULSP
    IEnumerator SaveInDataBase(string line)
    {
        WWWForm wf = new WWWForm();
        string hash = bl_DataBaseUtils.Md5Sum(DataBase.LocalUser.LoginName + bl_LoginProDataBase.Instance.SecretKey).ToLower();
        wf.AddField("name", DataBase.LocalUser.LoginName);
        wf.AddField("flist", line);
        wf.AddField("typ", 5);
        wf.AddField("hash", hash);

        using (UnityWebRequest www = UnityWebRequest.Post(bl_LoginProDataBase.Instance.GetUrl(bl_LoginProDataBase.URLType.DataBase), wf))
        {
            yield return www.SendWebRequest();

            if (!www.isNetworkError)
            {
                if (www.downloadHandler.text.Contains("save"))
                {
                    Debug.Log("Friend list save in database!");
                    DataBase.LocalUser.SetFriends(line);
                }
                else
                {
                    Debug.Log(www.downloadHandler.text);
                }
            }
            else
            {
                Debug.LogError(www.error);
            }
        }
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    /// <param name="field"></param>
    public void AddFriend(InputField field)
    {
        if(Friends.Count > bl_GameData.Instance.MaxFriendsAdded)
        {
            FriendUI.ShowLog("Max friends reached!");
            return;
        }
        string t = field.text;
        if (string.IsNullOrEmpty(t))
            return;

        if(FriendUI != null && FriendUI.hasThisFriend(t))
        {
            FriendUI.ShowLog("Already has added this friend.");
            return;
        }
        if(t == PhotonNetwork.NickName)
        {
            FriendUI.ShowLog("You can't add yourself.");
            return;
        }

        Friends.Add(t);
        PhotonNetwork.FindFriends(Friends.ToArray());
        FriendUI.UpdateFriendList(true);
        SaveFriends();
        WaitForEvent = true;

        field.text = string.Empty;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="field"></param>
    public void AddFriend(string friend)
    {
        if (Friends.Contains(friend)) return;

        Friends.Add(friend);
        PhotonNetwork.FindFriends(Friends.ToArray());
        FriendUI.UpdateFriendList(true);
        SaveFriends();
        WaitForEvent = true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="friend"></param>
    public void RemoveFriend(string friend)
    {
        if (Friends.Contains(friend))
        {
            Friends.Remove(friend);
            SaveFriends();
            if (Friends.Count > 0)
            {
                if(Friends.Count > 1 && Friends.Contains("Null"))
                {
                    Friends.Remove("Null");
                    Friends.Remove("Null");
                    SaveFriends();
                }
                if(Friends.Count > 0)
                PhotonNetwork.FindFriends(Friends.ToArray());
            }
            else
            {
                AddFriend("Null");
                if (Friends.Count > 0)
                    PhotonNetwork.FindFriends(Friends.ToArray());
            }

            FriendUI.UpdateFriendList(true);
            WaitForEvent = true;
        }
        else { Debug.Log("This user doesn't exist"); }
    }

    /// <summary>
    /// custom key for each player can save multiple friend list in a same device.
    /// </summary>
    private string SaveKey
    {
        get
        {
            return PhotonNetwork.NickName + FriendSaveKey;
        }
    }

    public bool haveFriendsSlots { get { return Friends.Count < bl_GameData.Instance.MaxFriendsAdded; } }
    public bool haveThisPlayerAsFriend(string playerName) { return Friends.Contains(playerName); }

    bool firstBuild = false;
    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        bool build = (friendList.Count != FriendList.Count);
        if (friendList.Count == 1)
        {
            if (friendList[0].UserId != "Null")
            {
                if (!firstBuild) { build = true; firstBuild = true; }
            }
            else firstBuild = false;
           
        }
        else firstBuild = false;

        FriendList = friendList;
        UpdateList();
        FriendUI.UpdateFriendList(build);
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

    }

    public void OnLeftRoom()
    {

    }

    public void OnJoinedRoom()
    {
        CancelInvoke("UpdateList");
    }

    public void OnConnected()
    {
      
    }

    public void OnConnectedToMaster()
    {

    }

    public void OnJoinedLobby()
    {
        GetFriendsStore();
        InvokeRepeating("UpdateList", 1, 1);
        FriendUI.Panel.SetActive(true);
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        CancelInvoke("UpdateList");
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

    public void OnLeftLobby()
    {
       
    }

    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
      
    }

    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
      
    }

    private static bl_FriendList _instance;
    public static bl_FriendList Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<bl_FriendList>();
            }
            return _instance;
        }
    }
}