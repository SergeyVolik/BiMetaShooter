using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bl_AICoverPoint : MonoBehaviour
{

    public bool Crouch = false;
    public float lastUseTime { get; set; } = 0;

    public List<bl_AICoverPoint> NeighbordPoints = new List<bl_AICoverPoint>();
    [HideInInspector] public bl_AICovertPointManager CoverManager;
    private int TryTimes = 0;

    private void Awake()
    {
        bl_AICovertPointManager.Register(this);
    }

    private void OnDrawGizmos()
    {
        if(CoverManager == null)
        {
            CoverManager = FindObjectOfType<bl_AICovertPointManager>();
            TryTimes++;
            if (TryTimes > 2)
            {
                Debug.LogWarning("There is not a cover point manager in scene!");
            }
            return;
        }
        if (!CoverManager.ShowGizmos)
            return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 1);
        Gizmos.DrawCube(transform.position, new Vector3(1, 0.1f, 1));

        Gizmos.color = Color.red;
        if (NeighbordPoints.Count > 0)
        {
            for (int i = 0; i < NeighbordPoints.Count; i++)
            {
                if (NeighbordPoints[i] == null) continue;
                Gizmos.DrawLine(transform.position, NeighbordPoints[i].transform.position);
            }
        }
    }
}