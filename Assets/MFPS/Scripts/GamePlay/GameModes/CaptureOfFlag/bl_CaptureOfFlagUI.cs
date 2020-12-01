using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bl_CaptureOfFlagUI : MonoBehaviour
{

    public GameObject Content;
    public Text Team1ScoreText, Team2ScoreText;
    public Image FlagImg1, FlagImg2;

    public void SetScores(int team1, int team2)
    {
        Team1ScoreText.text = team1.ToString();
        Team2ScoreText.text = team2.ToString();
    }

    public void ShowUp()
    {
        Content.SetActive(true);
        Team1ScoreText.color = Team.Team1.GetTeamColor();
        Team2ScoreText.color = Team.Team2.GetTeamColor();
        FlagImg1.color = Team.Team1.GetTeamColor();
        FlagImg2.color = Team.Team2.GetTeamColor();
    }

    public void Hide()
    {
        Content.SetActive(false);
    }

    private static bl_CaptureOfFlagUI _instance;
    public static bl_CaptureOfFlagUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<bl_CaptureOfFlagUI>();
            }
            return _instance;
        }
    }
}