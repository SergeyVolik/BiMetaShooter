using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class bl_DropDispacher : MonoBehaviourPun
{

    [Header("Delivery")]
    /// <summary>
    /// Kit Instance Effect
    /// </summary>
    public GameObject DropDeliveryPrefab;
    /// <summary>
    /// Speed Kit flying effect
    /// </summary>
    public float DeliveryTime = 10;

    [Header("Available Kits")]
    public List<DropInfo> AvailableDrops = new List<DropInfo>();

    /// <summary>
    /// when activated, record this in the event
    /// </summary>
    void OnEnable()
    {
        bl_EventHandler.onAirKit += SendDevilery;
    }
    /// <summary>
    /// when disabled, quit this in the event
    /// </summary>
    void OnDisable()
    {
        bl_EventHandler.onAirKit -= SendDevilery;
    }

    /// <summary>
    /// This is called by an internal event
    /// </summary>
    public void SendDevilery(Vector3 position, int kitID)
    {
        photonView.RPC("RpcKitDelivery", RpcTarget.All, position, kitID);
    }

    [PunRPC]
    void RpcKitDelivery(Vector3 position, int kitID)
    {
        GameObject newInstance = Instantiate(DropDeliveryPrefab, transform.position, Quaternion.identity) as GameObject;
        newInstance.GetComponent<bl_DropDelivery>().Dispatch(position, DeliveryTime, AvailableDrops[kitID].Prefab);
    }

    [System.Serializable]
    public class DropInfo
    {
        public string Name;
        public GameObject Prefab;
    }
}