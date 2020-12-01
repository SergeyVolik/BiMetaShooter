using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bl_PlayerUIBank : MonoBehaviour
{
    [Header("REFERENCES")]
    public Canvas PlayerUICanvas;
    public GameObject PickUpUI;
    public GameObject PickUpIconUI;
    public GameObject KillZoneUI;
    public GameObject AmmoUI;
    public GameObject MaxKillsUI;
    public GameObject SpeakerIcon;

    public Image DamageIndicator;
    public Image PlayerStateIcon;
    public Image SniperScope;
    public Image HealthBar;

    public Text AmmoText;
    public Text ClipText;
    public Text HealthText;
    public Text TimeText;
    public Text FireTypeText;
    public Text PickUpText;
    public CanvasGroup DamageAlpha;
    public bl_WeaponLoadoutUI LoadoutUI;
    public Gradient AmmoTextColorGradient;

    public void UpdateWeaponState(bl_Gun gun)
    {
        int bullets = gun.bulletsLeft;
        int clips = gun.numberOfClips;
        float per = (float)bullets / (float)gun.bulletsPerClip;
        Color c = AmmoTextColorGradient.Evaluate(per);

        if (gun.Info.Type != GunType.Knife)
        {
            AmmoText.text = bullets.ToString();
            ClipText.text = ClipText.text = string.Format("/ {0}", clips.ToString("F0"));
            AmmoText.color = c;
            ClipText.color = c;
        }
        else
        {
            AmmoText.text = "--";
            ClipText.text = ClipText.text = "/ --";
            AmmoText.color = Color.white;
            ClipText.color = Color.white;
        }
    }
}