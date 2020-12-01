using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class bl_PlayerScoreboardUI : MonoBehaviour
{
    [SerializeField] private Text NameText = null;
    [SerializeField] private Text KillsText = null;
    [SerializeField] private Text DeathsText = null;
    [SerializeField] private Text ScoreText = null;
    [SerializeField] private GameObject KickButton = null;
    [SerializeField] private Image LevelIcon = null;
    public Text levelNumberText;

    private Player cachePlayer = null;
    private bl_UIReferences UIReference;
    private bool isInitializated = false;
    private Image BackgroundImage;
    private Team InitTeam = Team.None;
    private bl_AIMananger.BotsStats Bot;

    public void Init(Player player, bl_UIReferences uir, bl_AIMananger.BotsStats bot = null)
    {
        Bot = bot;
        UIReference = uir;
        BackgroundImage = GetComponent<Image>();

        if (Bot != null)
        {
            InitBot();
            return;
        }

        cachePlayer = player;
        gameObject.name = player.NickName + player.ActorNumber;
        if (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Color c = BackgroundImage.color;
            c.a = 0.35f;
            BackgroundImage.color = c;
        }
        InitTeam = player.GetPlayerTeam();
        NameText.text = player.NickNameAndRole();
        if (!player.CustomProperties.ContainsKey(PropertiesKeys.KillsKey)) return;

        KillsText.text = player.CustomProperties[PropertiesKeys.KillsKey].ToString();
        DeathsText.text = player.CustomProperties[PropertiesKeys.DeathsKey].ToString();
        ScoreText.text = player.CustomProperties[PropertiesKeys.ScoreKey].ToString();
        if (bl_GameData.Instance.MasterCanKickPlayers && player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            KickButton.SetActive(PhotonNetwork.IsMasterClient);
        }
        else { KickButton.SetActive(false); }
#if LM
         LevelIcon.gameObject.SetActive(true);
        LevelInfo li = bl_LevelManager.Instance.GetPlayerLevelInfo(cachePlayer);
         LevelIcon.sprite = li.Icon;
        if (levelNumberText != null) levelNumberText.text = li.LevelID.ToString();
#else
        LevelIcon.gameObject.SetActive(false);
#endif
    }

    public void Refresh()
    {
        if (Bot != null) { InitBot(); return; }

        if (cachePlayer == null || cachePlayer.GetPlayerTeam() != InitTeam)
        {
            UIReference.RemoveUIPlayer(this);
            Destroy(gameObject);
        }
        if (!cachePlayer.CustomProperties.ContainsKey(PropertiesKeys.KillsKey)) return;

        NameText.text = cachePlayer.NickNameAndRole();
        KillsText.text = cachePlayer.CustomProperties[PropertiesKeys.KillsKey].ToString();
        DeathsText.text = cachePlayer.CustomProperties[PropertiesKeys.DeathsKey].ToString();
        ScoreText.text = cachePlayer.CustomProperties[PropertiesKeys.ScoreKey].ToString();

        if (bl_GameData.Instance.MasterCanKickPlayers && cachePlayer.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            KickButton.SetActive(PhotonNetwork.IsMasterClient);
        }
        else { KickButton.SetActive(false); }
#if LM
        LevelInfo li = bl_LevelManager.Instance.GetPlayerLevelInfo(cachePlayer);
        LevelIcon.sprite = li.Icon;
        if (levelNumberText != null) levelNumberText.text = li.LevelID.ToString();
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public void InitBot()
    {
        gameObject.name = Bot.Name;
        NameText.text = Bot.Name;
        KillsText.text = Bot.Kills.ToString();
        DeathsText.text = Bot.Deaths.ToString();
        ScoreText.text = Bot.Score.ToString();
        InitTeam = Bot.Team;
        KickButton.SetActive(false);
        LevelIcon.gameObject.SetActive(false);
    }

    public void Kick()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            bl_PhotonNetwork.Instance.KickPlayer(cachePlayer);
        }
    }

    public void OnClick()
    {
        if (cachePlayer == null)
            return;
        if (cachePlayer.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber && Bot == null)
        {
            bl_UIReferences.Instance.OpenScoreboardPopUp(true, cachePlayer);
        }
    }

    void OnEnable()
    {
        if (cachePlayer == null && isInitializated)
        {
            Destroy(gameObject);
            isInitializated = true;
        }
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }

    public int GetScore()
    {
        if (Bot == null) { return cachePlayer.GetPlayerScore(); }
        else { return Bot.Score; }
    }

    public Team GetTeam()
    {
        return InitTeam;
    }
}