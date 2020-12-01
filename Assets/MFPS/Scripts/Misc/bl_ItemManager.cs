/////////////////////////////////////////////////////////////////////////////////
//////////////////////////////bl_AmmoKit.cs//////////////////////////////////////
///////put one of these in each scene to handle Items////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
////////////////////////////////Briner Games/////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

public class bl_ItemManager : bl_MonoBehaviour
{ 
    /// <summary>
    /// instantiated reference necessary for synchronization
    /// </summary>
    public static int CurrentCount = 0;
    /// <summary>
    /// list of objects waiting to turn off again
    /// </summary>
    public List<GameObject> m_Objects = new List<GameObject>();
    /// <summary>
    /// timeouts to reactivate the gameobject (assign auto)
    /// </summary>
    private List<float> m_info = new List<float>();
    /// <summary>
    /// list of all components within this transform (assign auto)
    /// </summary>
    private bl_MedicalKit[] m_allmedkit;
    private bl_AmmoKit[] m_allammo;
    /// <summary>
    /// time to revive, defaults Kits
    /// </summary>
    public float TimeToRespawn = 15;
    public AudioClip PickSound;
    //private
    private int each_id = 0;
    private List<bl_MedicalKit> store = new List<bl_MedicalKit>();
    private List<bl_AmmoKit> storeAmmos = new List<bl_AmmoKit>();

    protected override void Awake()
    {
        base.Awake();
        m_allmedkit = this.transform.GetComponentsInChildren<bl_MedicalKit>();
        m_allammo = this.transform.GetComponentsInChildren<bl_AmmoKit>();
        //automatically place the id
        if (m_allmedkit.Length > 0)
        {
            foreach (bl_MedicalKit medit in m_allmedkit)
            {
                medit.m_id = each_id;
                each_id++;
                store.Add(medit);
            }
        }
        //continue with ammo kits.
        if (m_allammo.Length > 0)
        {
            foreach (bl_AmmoKit ammo in m_allammo)
            {
                ammo.m_id = each_id;
                each_id++;
                storeAmmos.Add(ammo);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (m_info.Count <= 0)
            return;
        //time management to revive
        for (int i = 0; i < m_info.Count; i++)
        {
            m_info[i] -= Time.deltaTime;
            if (m_info[i] <= 0)
            {
                EnableAgain(m_Objects[i]);
                m_info.Remove(m_info[i]);
                m_Objects.Remove(m_Objects[i]);
            }
        }
    }

    /// <summary>
    /// Call this to temporarily disable a Items
    /// </summary>
    /// <param name="t_id">Item to disable</param>
    public void DisableNew(int t_id)
    {
        photonView.RPC("DisableGO", RpcTarget.AllBuffered, t_id);
    }
    /// <summary>
    /// Enabled again the current finished item
    /// </summary>
    /// <param name="t_obj"></param>
    void EnableAgain(GameObject t_obj)
    {
        t_obj.SetActive(true);
    }
    /// <summary>
    /// called this when need destroy a item
    /// </summary>
    /// <param name="t_name">Item Name</param>
    public void DestroyGO(string t_name){
        photonView.RPC("DestroyGOSync", RpcTarget.All, t_name);
    }

    [PunRPC]
    void DestroyGOSync(string GOname)
    {
        Destroy(GameObject.Find(GOname).gameObject);
    }

    [PunRPC]
    void DisableGO(int m_id)
    {
        if (m_allmedkit.Length <= 0)
            return;

        foreach (bl_MedicalKit med in m_allmedkit)
        {
            if (med.m_id == m_id)
            {
                if (PickSound)
                {
                    AudioSource.PlayClipAtPoint(PickSound, med.transform.position, 1.0f);
                }
                m_Objects.Add(med.gameObject);
                m_info.Add(TimeToRespawn);
                med.gameObject.SetActive(false);
                return;
            }
        }

        foreach (bl_AmmoKit ammo in m_allammo)
        {
            if (ammo.m_id == m_id)
            {
                if (PickSound)
                {
                    AudioSource.PlayClipAtPoint(PickSound, ammo.transform.position, 1.0f);
                }
                m_Objects.Add(ammo.gameObject);
                m_info.Add(TimeToRespawn);
                ammo.gameObject.SetActive(false);
            }
        }
    }

    private static bl_ItemManager _instance;
    public static bl_ItemManager Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<bl_ItemManager>(); }
            return _instance;
        }
    }
}