using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class bl_KillFeedUI : bl_PhotonHelper
{
    [SerializeField]private Text KillerText;
    [SerializeField]private Text KilledText;
    [SerializeField]private Text WeaponText;
    public Image WeaponIconImg;
    [SerializeField]private Image KillTypeImage;
    private CanvasGroup Alpha;

    /// <summary>
    /// 
    /// </summary>
    public void Init(KillFeed feed)
    {
        switch (feed.messageType)
        {
            case KillFeedMessageType.WeaponKillEvent:
                OnKillMessage(feed);
                break;
            case KillFeedMessageType.Message:
                OnMessage(feed);
                break;
            case KillFeedMessageType.TeamHighlightMessage:
                OnTeamHighight(feed);
                break;
        }
        Alpha = GetComponent<CanvasGroup>();
        StartCoroutine(Hide(10));
    }

    /// <summary>
    /// 
    /// </summary>
    void OnKillMessage(KillFeed info)
    {
        KillerText.text = info.Killer;
        KilledText.text = info.Killed;
        KillerText.color = isLocalPlayerName(info.Killer) ? bl_GameData.Instance.highLightColor : info.KillerTeam.GetTeamColor();
        KilledText.color = isLocalPlayerName(info.Killed) ? bl_GameData.Instance.highLightColor : GetOppositeTeam(info.KillerTeam).GetTeamColor();
        if (bl_GameData.Instance.killFeedWeaponShowMode == KillFeedWeaponShowMode.WeaponName)
        {
            WeaponIconImg.gameObject.SetActive(false);
            WeaponText.text = info.Message;
        }
        else
        {
            WeaponText.gameObject.SetActive(false);
            Sprite icon = null;
            if(info.GunID >= 0)
            {
              icon = bl_GameData.Instance.GetWeapon(info.GunID).GunIcon;
            }
            else
            {
                if(!string.IsNullOrEmpty(info.Message))
                icon = bl_KillFeed.Instance.GetCustomIcon(info.Message);

                if(icon == null)
                {
                    int normalizedID = Mathf.Abs(info.GunID);
                    if(normalizedID <= bl_KillFeed.Instance.customIcons.Count - 1)
                    {
                        icon = bl_KillFeed.Instance.customIcons[normalizedID].Icon;
                    }
                }
            }
            WeaponIconImg.gameObject.SetActive(icon != null);
            WeaponIconImg.sprite = icon;
        }
        KillTypeImage.gameObject.SetActive(info.HeadShot);
    }

    /// <summary>
    /// 
    /// </summary>
    void OnMessage(KillFeed info)
    {
        DisableAll();
        KillerText.gameObject.SetActive(true);
        KillerText.text = info.Message;
    }

    /// <summary>
    /// 
    /// </summary>
    void OnTeamHighight(KillFeed info)
    {
        DisableAll();
        KillerText.gameObject.SetActive(true);
        string hex = ColorUtility.ToHtmlStringRGB(info.KillerTeam.GetTeamColor());
        KillerText.text = string.Format("<color=#{0}>{1}</color> {2}", hex, info.Killer, info.Message);
    }

    /// <summary>
    /// 
    /// </summary>
    void DisableAll()
    {
        KillerText.gameObject.SetActive(false);
        KilledText.gameObject.SetActive(false);
        WeaponText.gameObject.SetActive(false);
        WeaponIconImg.gameObject.SetActive(false);
        KillTypeImage.gameObject.SetActive(false);
    }

    Team GetOppositeTeam(Team team)
    {
        if (isOneTeamMode || team == Team.None) { return team; }

        return (team == Team.Team1) ? Team.Team2 : Team.Team1;
    }

    bool isLocalPlayerName(string playerName) { return playerName == Photon.Pun.PhotonNetwork.NickName; }

    IEnumerator Hide(float time)
    {
        yield return new WaitForSeconds(time);
        while(Alpha.alpha > 0)
        {
            Alpha.alpha -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);
    }

}