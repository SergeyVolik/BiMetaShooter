using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class bl_CameraRay : bl_MonoBehaviour
{
    public int CheckFrameRate = 5;
    [Range(0.1f, 10)] public float DistanceCheck = 2;
    public LayerMask DetectLayers;

    private int currentFrame = 0;
    private RaycastHit RayHit;
    private bl_GunPickUp gunPickup = null;
    public bool Checking { get; set; }
    private List<byte> activers = new List<byte>();
    private Dictionary<string, Action<bool>> triggers = new Dictionary<string, Action<bool>>();
    bool hasDectected = false;

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (!Checking) { currentFrame = 0; return; }

        if(currentFrame == 0)
        {
            Fire();
        }
        currentFrame = (currentFrame + 1) % CheckFrameRate;
    }

    /// <summary>
    /// 
    /// </summary>
    void Fire()
    {
        Ray r = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(r, out RayHit, DistanceCheck, DetectLayers, QueryTriggerInteraction.Ignore))
        {
            hasDectected = true;
            OnHit();
            //check in each register trigger
            if (triggers.Count > 0)
            {
                foreach (var item in triggers.Keys)
                {
                    //if the object that is in front have the same name that the register trigger -> call their callback
                    if (RayHit.transform.name == item)
                    {
                        triggers[item].Invoke(true);
                    }
                }
            }
        }
        else
        {
            if (gunPickup != null) { gunPickup.FocusThis(false); gunPickup = null; }
            if (triggers.Count > 0 && hasDectected)
            {
                foreach (var item in triggers.Values)
                {
                    item.Invoke(false);
                }
            }
            hasDectected = false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnHit()
    {
        bl_GunPickUp gp = RayHit.transform.GetComponent<bl_GunPickUp>();
        if (gp != null)
        {
            if (gunPickup != null && gunPickup != gp)
            {
                gunPickup.FocusThis(false);
            }
            gunPickup = gp;
            gunPickup.FocusThis(true);
        }
        else
        {
            if (gunPickup != null) { gunPickup.FocusThis(false); gunPickup = null; }
        }
    }

    /// <summary>
    /// If you wanna detect when an object is in front of the local player view
    /// register a callback in this function
    /// </summary>
    public void AddTrigger(string objectName, Action<bool> callback, byte id)
    {
        if (triggers.ContainsKey(objectName)) { Debug.Log("This trigger already list."); return; }

        triggers.Add(objectName, callback);
        SetActiver(true, id);
    }


    /// <summary>
    /// Make sure of remove the trigger when you don't need to detect it anymore.
    /// </summary>
    public void RemoveTrigger(string objectName, byte id)
    {
        if (!triggers.ContainsKey(objectName)) return;
        triggers.Remove(objectName);
        SetActiver(false, id);
    }

    public void SetActiver(bool add, byte id)
    {
        if (add)
        {
            if (!activers.Contains(id))
            {
                activers.Add(id);
            }
            Checking = true;
        }
        else
        {
            if (activers.Contains(id))
            {
                activers.Remove(id);
            }
            if (activers.Count <= 0) Checking = false;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawRay(transform.position, transform.forward * DistanceCheck);
    }
}