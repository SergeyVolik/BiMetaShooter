using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bl_KillCamUI : MonoBehaviour
{

    [SerializeField] private Text KillerNameText;
    [SerializeField] private Text KillerHealthText;
    [SerializeField] private Text GunNameText;
    [SerializeField] private Image GunImage;
    public Text KillCamSpectatingText;

    public void Show(string killer, int gunID)
    {
        bl_GunInfo info = bl_GameData.Instance.GetWeapon(gunID);
        GunImage.sprite = info.GunIcon;
        GunNameText.text = info.Name.ToUpper();
        killer = killer.Replace("(die)", "");
        KillerNameText.text = killer;
#if LOCALIZATION
        KillCamSpectatingText.text = string.Format("<size=8>{0}:</size>\n{1}", bl_Localization.Instance.GetText(26).ToUpper(), killer);
#else
        KillCamSpectatingText.text = string.Format("<size=8>{0}:</size>\n{1}", bl_GameTexts.Spectating.ToUpper(), killer);
#endif
        MFPSPlayer actor = bl_GameManager.Instance.FindActor(killer);
        if(actor != null)
        {
            if (actor.isRealPlayer)
            {
                bl_PlayerHealthManager pdm = actor.Actor.GetComponent<bl_PlayerHealthManager>();
                int health = Mathf.FloorToInt(pdm.health);
                if (pdm != null) { KillerHealthText.text = string.Format("HEALTH: {0}", health); }
            }
            else
            {
                bl_AIShooterHealth pdm = actor.Actor.GetComponent<bl_AIShooterHealth>();
                int health = Mathf.FloorToInt(pdm.Health);
                if (pdm != null) { KillerHealthText.text = string.Format("HEALTH: {0}", health); }
            }
        }
        else
        {
            KillerHealthText.text = string.Empty;
        }
    }
}