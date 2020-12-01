using UnityEngine;
using System.Collections;

public class bl_DropCaller : MonoBehaviour
{
    public GameObject DestroyEffect;
    private int KitID = 0;

    public void SetUp(int kitToCall, float delay)
    {
        KitID = kitToCall;
        StartCoroutine(CallProcess(delay));
    }

    IEnumerator CallProcess(float delay)
    {
        yield return new WaitForSeconds(delay);
        bl_EventHandler.KitAirEvent(transform.position, KitID);
        if(DestroyEffect != null)
        {
            Instantiate(DestroyEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}