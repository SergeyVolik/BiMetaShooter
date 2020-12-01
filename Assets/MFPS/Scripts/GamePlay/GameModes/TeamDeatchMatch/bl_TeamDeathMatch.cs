using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using Photon.Pun;

public class bl_TeamDeathMatch : bl_PhotonHelper, IGameMode
{

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        if (!PhotonNetwork.IsConnected)
            return;

        Initialize();
    }

    #region Interface
    /// <summary>
    /// 
    /// </summary>
    public void Initialize()
    {
        //check if this is the game mode of this room
        if (bl_GameManager.Instance.IsGameMode(GameMode.TDM, this))
        {
            bl_GameManager.Instance.SetGameState(MatchState.Starting);
            bl_TeamDeathMatchUI.Instance.ShowUp();
        }
        else
        {
            bl_TeamDeathMatchUI.Instance.Hide();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnFinishTime(bool gameOver)
    {
        if (PhotonNetwork.OfflineMode) return;

        //determine the winner
        string finalText = "";
        if(GetWinnerTeam() != Team.None)
        {
            finalText = GetWinnerTeam().GetTeamName();
        }
        else
        {
            finalText = bl_GameTexts.NoOneWonName;
        }
        bl_UIReferences.Instance.SetFinalText(finalText);
    }

    public void OnLocalPlayerKill()
    {
        PhotonNetwork.CurrentRoom.SetTeamScore(PhotonNetwork.LocalPlayer.GetPlayerTeam());
    }

    public void OnLocalPoint(int points, Team team)
    {
        PhotonNetwork.CurrentRoom.SetTeamScore(team);
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(PropertiesKeys.Team1Score) || propertiesThatChanged.ContainsKey(PropertiesKeys.Team2Score))
        {
            int team1 = PhotonNetwork.CurrentRoom.GetRoomScore(Team.Team1);
            int team2 = PhotonNetwork.CurrentRoom.GetRoomScore(Team.Team2);
            bl_TeamDeathMatchUI.Instance.SetScores(team1, team2);
            CheckScores(team1, team2);
        }
    }

    public void OnOtherPlayerLeave(Player otherPlayer)
    {   
    }

    public void OnOtherPlayerEnter(Player newPlayer)
    {       
    }

    public void OnLocalPlayerDeath()
    { 
    }

    public bool isLocalPlayerWinner
    {
        get
        {         
            return GetWinnerTeam() == PhotonNetwork.LocalPlayer.GetPlayerTeam();
        }
    }

    public Team GetWinnerTeam()
    {
        int team1 = PhotonNetwork.CurrentRoom.GetRoomScore(Team.Team1);
        int team2 = PhotonNetwork.CurrentRoom.GetRoomScore(Team.Team2);

        Team winner = Team.None;
        if (team1 > team2) { winner = Team.Team1; }
        else if (team1 < team2) { winner = Team.Team2; }
        else { winner = Team.None; }
        return winner;
    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    void CheckScores(int team1, int team2)
    {
        if (PhotonNetwork.OfflineMode) return;
        //check if any of the team reach the max kills
        if (team1 >= bl_RoomSettings.Instance.GameGoal)
        {
            bl_MatchTimeManager.Instance.FinishRound();
            return;
        }
        if (team2 >= bl_RoomSettings.Instance.GameGoal)
        {
            bl_MatchTimeManager.Instance.FinishRound();
        }
    }
}