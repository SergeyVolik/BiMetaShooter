/////////////////////////////////////////////////////////////////////////////////
//////////////////////////////bl_AmmoKit.cs//////////////////////////////////////
//////////////////Use this to create new internal events of AmmoKit Pick Up///////
/////////////////////////////////////////////////////////////////////////////////
////////////////////////////////Briner Games/////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;

public class bl_AmmoKit : MonoBehaviour
{

    /// <summary>
    /// add this amount clip to player
    /// </summary>
    public int Bullets = 30;
    public int Clips = 3;
    public int Projectiles = 2;
    public AudioClip PickSound;
    [HideInInspector] public int m_id = 0;
    //private
    private bl_ItemManager m_manager;
    private bool Ready = false;
    private int typekit = 0;

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        if (this.transform.root.GetComponent<bl_ItemManager>() != null)//if this default kit 
        {
            m_manager = this.transform.root.GetComponent<bl_ItemManager>();
            typekit = 1;
        }
        else
            if (GameObject.FindWithTag("ItemManager") != null)//if this kit instance
        {
            this.transform.parent = GameObject.FindWithTag("ItemManager").transform;
            m_manager = GameObject.FindWithTag("ItemManager").GetComponent<bl_ItemManager>();
            typekit = 2;
            gameObject.name = "Kit" + bl_ItemManager.CurrentCount;
            bl_ItemManager.CurrentCount++;
        }
        else//if any destroy this
        {
            Debug.LogError("need to have a ItemManager in the scene");
            Destroy(this.gameObject);
        }
    }

    void OnEnable()
    {
        Ready = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="m_other"></param>
    void OnTriggerEnter(Collider m_other)
    {
        if (m_other.transform.CompareTag(bl_PlayerSettings.LocalTag))
        {
            if (!Ready)
            {
                Ready = true;
                bl_EventHandler.OnAmmo(Bullets, Clips, Projectiles);
                if (PickSound)
                {
                    AudioSource.PlayClipAtPoint(PickSound, transform.position, 1.0f);
                }
            }
            if (typekit == 1)
            {
                m_manager.DisableNew(m_id);
            }
            else if (typekit == 2)
            {
                m_manager.DestroyGO(this.gameObject.name);
            }
        }
    }
}