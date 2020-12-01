using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bl_TeamDeathMatchUI : MonoBehaviour
{
    public GameObject Content;
    public Text Team1ScoreText, Team2ScoreText;
    public Graphic[] Team1UI;
    public Graphic[] Team2UI;

    public void SetScores(int team1, int team2)
    {
        Team1ScoreText.text = team1.ToString();
        Team2ScoreText.text = team2.ToString();
    }

    public void ShowUp()
    {
        Content.SetActive(true);
        foreach(Graphic g in Team1UI) { g.color = Team.Team1.GetTeamColor(); }
        foreach (Graphic g in Team2UI) { g.color = Team.Team2.GetTeamColor(); }
    }

    public void Hide()
    {
        Content.SetActive(false);
    }

    private static bl_TeamDeathMatchUI _instance;
    public static bl_TeamDeathMatchUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<bl_TeamDeathMatchUI>();
            }
            return _instance;
        }
    }
}