using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;

public class bl_WaitingPlayerUI : MonoBehaviour
{

    public Text NameText;
    public Image LevelImg;
    public Image TeamColorImg;
    public GameObject ReadyUI;
    public GameObject KickButton;
    public GameObject MasterClientUI;
    public Player ThisPlayer { get; set; }

    public void SetInfo(Player player)
    {
        ThisPlayer = player;
        NameText.text = string.Format(player.NickName);
        TeamColorImg.color = player.GetPlayerTeam().GetTeamColor();
        MasterClientUI.SetActive(player.IsMasterClient);
        if(bl_GameData.Instance.MasterCanKickPlayers && player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            KickButton.SetActive(PhotonNetwork.IsMasterClient);
        }
        else { KickButton.SetActive(false); }
        UpdateState();

#if LM
        LevelImg.gameObject.SetActive(true);
        LevelImg.sprite = bl_LevelManager.Instance.GetPlayerLevelInfo(player).Icon;
#else
        LevelImg.gameObject.SetActive(false);
#endif
    }

    public void UpdateState()
    {
        bool ready = bl_WaitingRoom.Instance.readyPlayers.Contains(ThisPlayer.ActorNumber);
        ReadyUI.SetActive(ready);
    }

    public void KickThis()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonNetwork.CloseConnection(ThisPlayer);
    }
}