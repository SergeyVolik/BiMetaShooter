using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class bl_LocalKillUI : MonoBehaviour
{
    [SerializeField]private Text NameText;
    [SerializeField]private Text ValueText;
    [SerializeField] private Text ExtraText;
    [SerializeField] private Animator CircleAnim;
    private CanvasGroup Alpha;
    private bl_KillFeed KillFeed;
    private Animator Anim;

    public void InitMultiple(KillInfo info, bool headShot)
    {
        if (!headShot)
        {
            NameText.text = info.Killed;
            ValueText.text = bl_GameData.Instance.ScoreReward.ScorePerKill.ToString();
        }
        else
        {
            NameText.text = bl_GameTexts.HeatShotBonus;
            ValueText.text = bl_GameData.Instance.ScoreReward.ScorePerHeadShot.ToString();
        }
        Alpha = GetComponent<CanvasGroup>();
        StartCoroutine(Hide(true));
    }

    public void InitIndividual(KillInfo info)
    {
        if(KillFeed == null) { KillFeed = FindObjectOfType<bl_KillFeed>(); }
        if(Anim == null) { Anim = GetComponent<Animator>(); }
        NameText.text = info.Killed;
        ValueText.text = (info.byHeadShot) ? bl_GameTexts.HeadShot.ToUpper() : bl_GameTexts.KillingInAction.ToUpper();
        int spk = bl_GameData.Instance.ScoreReward.ScorePerKill;
        if (info.byHeadShot)
        {
            ExtraText.text = string.Format("{0} <b>+{1}</b>\n{2} +{3}", info.KillMethod.ToUpper(), spk, bl_GameTexts.HeadShot.ToUpper(), bl_GameData.Instance.ScoreReward.ScorePerHeadShot);
        }
        else
        {
            ExtraText.text = string.Format("{0} <b>+{1}</b>", info.KillMethod.ToUpper(), spk);
        }
        gameObject.SetActive(true);
        if (CircleAnim != null) { CircleAnim.Play("play", 0, 0); }
        Anim.SetBool("show", true);
        Anim.Play("show", 0, 0);
        if (Alpha == null)
        {
            Alpha = GetComponent<CanvasGroup>();
        }
        StartCoroutine(HideAnimated());
    }

    IEnumerator Hide(bool destroy)
    {
        yield return new WaitForSeconds(7);
        while(Alpha.alpha > 0)
        {
            Alpha.alpha -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        if (destroy)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
            KillFeed.LocalDisplayDone();
        }
    }

    IEnumerator HideAnimated()
    {
        yield return new WaitForSeconds(KillFeed.IndividualShowTime);
        Anim.SetBool("show", false);
        yield return new WaitForSeconds(Anim.GetCurrentAnimatorStateInfo(0).length);
        gameObject.SetActive(false);
        KillFeed.LocalDisplayDone();
    }

}