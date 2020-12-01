//////////////////////////////////////////////////////////////////////////////
// bl_DeathZone.cs
//
// -Put this script in an Object that itself contains one Collider component in trigger mode.
//  You can use this as a limit zones, where the player can not enter or stay.
//                          Lovatto Studio
//////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class bl_DeathZone : bl_PhotonHelper {
    /// <summary>
    /// Time maximum that may be prohibited in the area before dying
    /// </summary>
    public int TimeToDeath = 5;
    /// <summary>
    /// message that will appear in the UI when this within the zone
    /// </summary>
    public string CustomMessage = "you're in a zone prohibited \n returns to the playing area or die at \n";
    private bool mOn = false;
    private GameObject KillZoneUI = null;
    private int CountDown;
    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        CountDown = TimeToDeath;
        if (this.transform.GetComponent<Collider>() != null)
        {
            transform.GetComponent<Collider>().isTrigger = true;
        }
        else
        {
            Debug.LogError("This Go " + gameObject.name + " need have a collider");
            Destroy(this);
        }
        if (KillZoneUI == null)
        {
            KillZoneUI = bl_UIReferences.Instance.PlayerUI.KillZoneUI;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mCol"></param>
  void OnTriggerEnter(Collider mCol)
    {
        if (mCol.transform.CompareTag(bl_PlayerSettings.LocalTag))//when is player local enter
        {
            bl_PlayerHealthManager pdm = mCol.transform.root.GetComponent<bl_PlayerHealthManager>();// get the component damage

            if (pdm != null && pdm.health > 0 && !mOn)
            {
                InvokeRepeating("regressive", 1, 1);
                if (KillZoneUI != null)
                {
                    KillZoneUI.SetActive(true);
                    Text mText = KillZoneUI.GetComponentInChildren<Text>();
                    mText.text = CustomMessage + "<color=red><size=25>" + CountDown.ToString("00") + "</size>s</color>";
                }
                mOn = true;
            }

        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mCol"></param>
    void OnTriggerExit(Collider mCol)
    {
        if (mCol.transform.CompareTag(bl_PlayerSettings.LocalTag))// if player exit of zone then cancel countdown
        {
                CancelInvoke("regressive");
                CountDown = TimeToDeath; // restart time
                if (KillZoneUI != null)
                {
                    KillZoneUI.SetActive(false);
                }
                mOn = false;                
        }
    }
    /// <summary>
    /// Start CountDown when player is on Trigger
    /// </summary>
    void regressive()
    {
        CountDown--;
        if (KillZoneUI != null)
        {
            Text mText = KillZoneUI.GetComponentInChildren<Text>();
            mText.text = CustomMessage + "<color=red><size=25>"+ CountDown.ToString("00")+"</size>s</color>";
        }
        if (CountDown <= 0)
        {
            GameObject player = FindPlayerRoot(bl_GameManager.LocalPlayerViewID);
            if (player != null)
            {
                player.GetComponent<bl_PlayerHealthManager>().Suicide();
            }
            CancelInvoke("regressive");
            CountDown = TimeToDeath;
            if (KillZoneUI != null)
            {
                KillZoneUI.SetActive(false);
            }
            mOn = false;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var c = GetComponent<BoxCollider>();
        Gizmos.matrix = transform.localToWorldMatrix;
        if (gameObject == UnityEditor.Selection.activeGameObject)
        {
            // If we are directly selected (and not just our parent is selected)
            // draw with negative size to get an 'inside out' cube we can see from the inside
            Gizmos.color = new Color(1.0f, 1.0f, 0.5f, 0.8f);
            Gizmos.DrawCube(c.center, -c.size);
        }
        Gizmos.color = new Color(1.0f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawCube(c.center, c.size);
    }
#endif
}