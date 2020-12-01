using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class bl_GameFinish : bl_PhotonHelper
{

    [SerializeField] private Text PlayerNameText;
    [SerializeField] private Text KillsText;
    [SerializeField] private Text DeathsText;
    [SerializeField] private Text ScoreText;
    [SerializeField] private Text KDRText;
    [SerializeField] private Text TimePlayedText;
    [SerializeField] private Text WinScoreText;
    [SerializeField] private Text HeadshotsText;
    [SerializeField] private Text TotalScoreText;
    [SerializeField] private Text CoinsText;
    [SerializeField] private GameObject Content;
#if ULSP
    private bl_DataBase DataBase;
#endif

    /// <summary>
    /// 
    /// </summary>
    public void CollectData()
    {
        int kills = PhotonNetwork.LocalPlayer.GetKills();
        int deaths = PhotonNetwork.LocalPlayer.GetDeaths();
        int score = PhotonNetwork.LocalPlayer.GetPlayerScore();
        int kd = kills;
        if (kills <= 0) { kd = -deaths; }
        else if (deaths > 0) { kd = kills / deaths; }
        int timePlayed = Mathf.RoundToInt(bl_GameManager.Instance.PlayedTime);
        int scorePerTime = timePlayed * bl_GameData.Instance.ScoreReward.ScorePerTimePlayed;
        int hsscore = bl_GameManager.Instance.Headshots * bl_GameData.Instance.ScoreReward.ScorePerHeadShot;
        bool winner = bl_GameManager.Instance.isLocalPlayerWinner();
        int winScore = (winner) ? bl_GameData.Instance.ScoreReward.ScoreForWinMatch : 0;
        PlayerNameText.text = PhotonNetwork.NickName;
        int tscore = score + winScore + scorePerTime;

        int coins = 0;
        if (tscore > 0 && bl_GameData.Instance.VirtualCoins.CoinScoreValue > 0 && tscore > bl_GameData.Instance.VirtualCoins.CoinScoreValue)
        {
            coins = tscore / bl_GameData.Instance.VirtualCoins.CoinScoreValue;
        }
#if LOCALIZATION
        KillsText.text = string.Format("{0}: <b>{1}</b>", bl_Localization.Instance.GetText(126).ToUpper(), kills);
        DeathsText.text = string.Format("{0}: <b>{1}</b>", bl_Localization.Instance.GetTextPlural(58).ToUpper(), deaths);
        ScoreText.text = string.Format("{0}: <b>{1}</b>", bl_Localization.Instance.GetText(59).ToUpper(), score - hsscore);
        WinScoreText.text = string.Format(bl_Localization.Instance.GetText(61).ToUpper(), winScore);
        KDRText.text = string.Format("{0}\n<size=10>KDR</size>", kd);
        TimePlayedText.text = string.Format("{0} <b>{1}</b> +{2}", bl_Localization.Instance.GetText(60).ToUpper(), bl_UtilityHelper.GetTimeFormat((float)timePlayed / 60, timePlayed), scorePerTime);
        HeadshotsText.text = string.Format("{0} <b>{1}</b> +{2}", bl_Localization.Instance.GetTextPlural(16).ToUpper(), bl_GameManager.Instance.Headshots, hsscore);
        TotalScoreText.text = string.Format("{0}\n<size=9>{1}</size>", tscore, bl_Localization.Instance.GetText(35).ToUpper());
        CoinsText.text = string.Format("+{0}\n<size=9>COINS</size>", coins);
#else
        KillsText.text = string.Format("{0}: <b>{1}</b>", bl_GameTexts.Kills.ToUpper(), kills);
        DeathsText.text = string.Format("{0}: <b>{1}</b>", bl_GameTexts.Deaths.ToUpper(), deaths);
        ScoreText.text = string.Format("{0}: <b>{1}</b>", bl_GameTexts.Score.ToUpper(), score - hsscore);
        WinScoreText.text = string.Format(bl_GameTexts.WinMatch, winScore);
        KDRText.text = string.Format("{0}\n<size=10>KDR</size>", kd);
        TimePlayedText.text = string.Format("{0} <b>{1}</b> +{2}", bl_GameTexts.TimePlayed.ToUpper(), bl_UtilityHelper.GetTimeFormat((float)timePlayed / 60, timePlayed), scorePerTime);
        HeadshotsText.text = string.Format("{0} <b>{1}</b> +{2}", bl_GameTexts.HeadShot.ToUpper(), bl_GameManager.Instance.Headshots, hsscore);
        TotalScoreText.text = string.Format("{0}\n<size=9>{1}</size>", tscore, bl_GameTexts.TotalScore.ToUpper());
        CoinsText.text = string.Format("+{0}\n<size=9>COINS</size>", coins);
#endif

#if ULSP
        DataBase = bl_DataBase.Instance;
        if (DataBase != null)
        {
            Player p = PhotonNetwork.LocalPlayer;
            DataBase.SaveData(tscore, p.GetKills(), p.GetDeaths());
            DataBase.StopAndSaveTime();
            if (coins > 0)
            {
                DataBase.SaveNewCoins(coins);
            }
#if CLANS
            DataBase.SetClanScore(score);
#endif
        }
#else
        if (coins > 0)
        {
            bl_GameData.Instance.VirtualCoins.SetCoins(coins, PhotonNetwork.NickName);
        }
#endif
    }

    public void Show()
    {
        Content.SetActive(true);
        Invoke("AutoLeave", 60);//maximum time out to leave.
    }

    void AutoLeave()
    {
        GoToLobby();
    }

    /// <summary>
    /// 
    /// </summary>
    public void GoToLobby()
    {
        CancelInvoke();
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            bl_UtilityHelper.LoadLevel(bl_GameData.Instance.MainMenuScene);
        }
    }

}