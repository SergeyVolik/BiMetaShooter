using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class bl_GunPickUpManager : bl_PhotonHelper
{

    [Range(100,500)] public float ForceImpulse = 350;

    private bl_GameData GameData;
    public bl_GunPickUp LastTrigger { get; set; }

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        GameData = bl_GameData.Instance;
    }

    /// <summary>
    /// 
    /// </summary>
    public void TrownGun(int id, Vector3 point, int[] info, bool AutoDestroy)
    {
        int[] i = new int[3];
        i[0] = info[0];
        i[1] = info[1];
        i[2] = bl_GameManager.LocalPlayerViewID;
        //prevent the go has an existing name
        int rand = Random.Range(0, 9999);
        //unique go name
        string prefix = PhotonNetwork.NickName + rand;
        photonView.RPC("TrowGunRPC", RpcTarget.All, id, point, prefix, i, AutoDestroy);
    }

    [PunRPC]
    void TrowGunRPC(int id, Vector3 point, string prefix, int[] info, bool AutoDestroy)
    {
        GameObject trow = null;
        bl_GunInfo ginfo = GameData.GetWeapon(id);

        if (ginfo.PickUpPrefab == null)
        {
            Debug.LogError(string.Format("The weapon: '{0}' not have a pick up prefab in Gun info", ginfo.Name));
            return;
        }
        trow = ginfo.PickUpPrefab.gameObject;

        GameObject p = FindPlayerRoot(info[2]);
        GameObject gun = Instantiate(trow, point, Quaternion.identity) as GameObject;
        Collider[] c = p.GetComponentsInChildren<Collider>();
        for (int i = 0; i < c.Length; i++)
        {
            Physics.IgnoreCollision(c[i], gun.GetComponent<Collider>());
        }
        gun.GetComponent<Rigidbody>().AddForce(p.transform.forward * ForceImpulse);
        int clips = info[0];
        bl_GunPickUp gp = gun.GetComponent<bl_GunPickUp>();
        gp.Info.Clips = clips;
        gp.Info.Bullets = info[1];
        gp.AutoDestroy = AutoDestroy;
        gun.name = gun.name.Replace("(Clone)", string.Empty);
        gun.name += prefix;
        gun.transform.parent = transform;
    }

    /// <summary>
    /// 
    /// </summary>
    public void SendPickUp(string n, int id, bl_GunPickUp.m_Info gp)
    {
        int[] i = new int[2];
        i[0] = gp.Clips;
        i[1] = gp.Bullets;
        photonView.RPC("PickUp", RpcTarget.All, n, id, i);
    }

    /// <summary>
    /// 
    /// </summary>
    [PunRPC]
    void PickUp(string mName, int id, int[] info, PhotonMessageInfo msgInfo)
    {
        // one of the messages might be ours
        // note: you could check "active" first, if you're not interested in your own, failed pickup-attempts.
        GameObject g = GameObject.Find(mName);
        if (g == null)
        {
            Debug.LogError("This Gun does not exist in this scene");
            return;
        }
        //is mine
        if (msgInfo.Sender == PhotonNetwork.LocalPlayer)
        {
            GunPickUpData pi = new GunPickUpData();
            pi.ID = id;
            pi.ItemObject = g;
            pi.Clips = info[0];
            pi.Bullets = info[1];
            bl_EventHandler.OnPickUpGun(pi);
        }
        Destroy(g);
    }

}