using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using Photon.Pun;

public class bl_FreeForAll : MonoBehaviour, IGameMode
{

    private bool isSub = false;
    public List<Player> FFAPlayerSort = new List<Player>();

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        if (!PhotonNetwork.IsConnected)
            return;

        Initialize();
    }

    /// <summary>
    /// 
    /// </summary>
    void OnDisable()
    {
        if (isSub)
        {
            bl_PhotonCallbacks.PlayerPropertiesUpdate -= OnPlayerPropertiesUpdate;
        }
    }

    public void OnPlayerPropertiesUpdate(Player target, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PropertiesKeys.KillsKey))
        {
            CheckScore();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void CheckScore()
    {
        FFAPlayerSort.Clear();
        FFAPlayerSort.AddRange(PhotonNetwork.PlayerList);
        Player player = null;
        if (FFAPlayerSort.Count > 0 && FFAPlayerSort != null)
        {
            FFAPlayerSort.Sort(bl_UtilityHelper.GetSortPlayerByKills);
            player = FFAPlayerSort[0];
        }
        else
        {
            player = PhotonNetwork.LocalPlayer;
        }
        bl_FreeForAllUI.Instance.SetScores(player);
        //check if the best player reach the max kills
        if(player.GetKills() >= bl_RoomSettings.Instance.GameGoal && !PhotonNetwork.OfflineMode)
        {
            bl_MatchTimeManager.Instance.FinishRound();
            return;
        }
        //check if bots have not reach max kills
        if (bl_AIMananger.Instance != null && bl_AIMananger.Instance.BotsActive && bl_AIMananger.Instance.BotsStatistics.Count > 0)
        {
            if (bl_AIMananger.Instance.GetBotWithMoreKills().Kills >= bl_RoomSettings.Instance.GameGoal)
            {
                bl_MatchTimeManager.Instance.FinishRound();
            }
        }
    }

    public Player GetBestPlayer()
    {
        if (FFAPlayerSort.Count > 0 && FFAPlayerSort != null)
        {
            return FFAPlayerSort[0];
        }
        else
        {
            return PhotonNetwork.LocalPlayer;
        }
    }

    #region Interface
    public bool isLocalPlayerWinner
    {
        get
        {
            string winner = GetBestPlayer().NickName;
            if (bl_AIMananger.Instance != null && bl_AIMananger.Instance.GetBotWithMoreKills().Kills >= bl_RoomSettings.Instance.GameGoal)
            {
                winner = bl_AIMananger.Instance.GetBotWithMoreKills().Name;
            }
            return winner == PhotonNetwork.LocalPlayer.NickName;

        }
    }

    public void Initialize()
    {
        //check if this is the game mode of this room
        if (bl_GameManager.Instance.IsGameMode(GameMode.FFA, this))
        {
            bl_GameManager.Instance.SetGameState(MatchState.Starting);
            bl_PhotonCallbacks.PlayerPropertiesUpdate += OnPlayerPropertiesUpdate;
            bl_FreeForAllUI.Instance.ShowUp();
            isSub = true;
        }
        else
        {
            bl_FreeForAllUI.Instance.Hide();
        }
    }

    public void OnFinishTime(bool gameOver)
    {
        bl_UIReferences.Instance.SetFinalText(GetBestPlayer().NickName);
    }

    public void OnLocalPlayerDeath()
    {

    }

    public void OnLocalPlayerKill()
    {

    }

    public void OnLocalPoint(int points, Team teamToAddPoint)
    {
        CheckScore();
    }

    public void OnOtherPlayerEnter(Player newPlayer)
    {

    }

    public void OnOtherPlayerLeave(Player otherPlayer)
    {

    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {

    } 
    #endregion
}