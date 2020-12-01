using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using Photon.Pun;

public class bl_CaptureOfFlag : bl_PhotonHelper, IGameMode
{
    [Header("Settings")]
    public int ScorePerCapture = 500;
    public int CapturesToWin = 3;

    [Header("References")]
    public GameObject Content;
    public bl_FlagPoint Team1Flag;
    public bl_FlagPoint Team2Flag;

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        if (!PhotonNetwork.IsConnected)
            return;

        Initialize();
    }

    public bl_FlagPoint GetFlag(Team team)
    {
        if (team == Team.Team1)
        {
            return Team1Flag;
        }

        return Team2Flag;
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

    public static Team GetOppositeTeam(Team team)
    {
        if (team == Team.Team1)
        {
            return Team.Team2;
        }

        return Team.Team1;
    }

    void CheckScores()
    {
        int team1 = PhotonNetwork.CurrentRoom.GetRoomScore(Team.Team1);
        int team2 = PhotonNetwork.CurrentRoom.GetRoomScore(Team.Team2);

        if(team1 >= CapturesToWin || team2 >= CapturesToWin)
        {
            bl_MatchTimeManager.Instance.FinishRound();
        }
    }

    #region GameMode Interface
    public void Initialize()
    {
        //check if this is the game mode of this room
        if (bl_GameManager.Instance.IsGameMode(GameMode.CTF, this))
        {
            Content.SetActive(true);
            bl_GameManager.Instance.WaitForPlayers(GameMode.CTF.GetGameModeInfo().RequiredPlayersToStart);
           //bl_GameManager.Instance.SetGameState(MatchState.Starting);
            bl_CaptureOfFlagUI.Instance.ShowUp();
        }
        else
        {
            Content.SetActive(false);
            bl_CaptureOfFlagUI.Instance.Hide();
        }
    }

    public void OnFinishTime(bool gameOver)
    {
        //determine the winner
        string finalText = "";
        if (GetWinnerTeam() != Team.None)
        {
            finalText = GetWinnerTeam().GetTeamName();
        }
        else
        {
            finalText = bl_GameTexts.NoOneWonName;
        }
        bl_UIReferences.Instance.SetFinalText(finalText);
    }

    public void OnLocalPlayerDeath()
    {
       
    }

    public void OnLocalPlayerKill()
    {
       
    }

    public void OnLocalPoint(int points, Team teamToAddPoint)
    {
        PhotonNetwork.CurrentRoom.SetTeamScore(teamToAddPoint);
    }

    public void OnOtherPlayerEnter(Player newPlayer)
    {
        
    }

    public void OnOtherPlayerLeave(Player otherPlayer)
    {
       
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(PropertiesKeys.Team1Score) || propertiesThatChanged.ContainsKey(PropertiesKeys.Team2Score))
        {
            int team1 = PhotonNetwork.CurrentRoom.GetRoomScore(Team.Team1);
            int team2 = PhotonNetwork.CurrentRoom.GetRoomScore(Team.Team2);
            bl_CaptureOfFlagUI.Instance.SetScores(team1, team2);
            CheckScores();
        }
    }

    public bool isLocalPlayerWinner
    {
        get
        {
            return (PhotonNetwork.LocalPlayer.GetPlayerTeam() == GetWinnerTeam());
        }
    }
    #endregion

    private static bl_CaptureOfFlag _instance;
    public static bl_CaptureOfFlag Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<bl_CaptureOfFlag>(); }
            return _instance;
        }
    }
}