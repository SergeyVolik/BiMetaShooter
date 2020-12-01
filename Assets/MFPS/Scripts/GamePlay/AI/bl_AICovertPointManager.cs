using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bl_AICovertPointManager : MonoBehaviour
{
    public float MaxDistance = 50;
    public float UsageTime = 10;
    public bool ShowGizmos = true;

    public static List<bl_AICoverPoint> AllCovers = new List<bl_AICoverPoint>();

    public static void Register(bl_AICoverPoint co)
    {
        AllCovers.Add(co);
    }

    private void OnDestroy()
    {
        AllCovers.Clear();
    }

    public bl_AICoverPoint GetCloseCover(Transform target)
    {
        bl_AICoverPoint cover = null;
        float d = MaxDistance;
        for(int i = 0; i < AllCovers.Count; i++)
        {
            float dis = bl_UtilityHelper.Distance(target.position, AllCovers[i].transform.position);
            if(dis < MaxDistance && dis < d)
            {
                d = dis;
                cover = AllCovers[i];
            }
        }
        cover = CheckCoverUsage(cover);
        return cover;
    }

    public bl_AICoverPoint GetCoverOnRadius(Transform target, float radius)
    {
        List<bl_AICoverPoint> list = new List<bl_AICoverPoint>();
        for (int i = 0; i < AllCovers.Count; i++)
        {
            float dis = bl_UtilityHelper.Distance(target.position, AllCovers[i].transform.position);
            if (dis <= radius)
            {
                list.Add(AllCovers[i]);
            }
        }
        bl_AICoverPoint cp = null;
        if (list.Count > 0)
        {
            cp = list[Random.Range(0, list.Count)];
        }
        if(cp == null) { cp = AllCovers[Random.Range(0, AllCovers.Count)]; }

        return cp;
    }

    public bl_AICoverPoint GetCloseCoverForced(Transform target)
    {
        bl_AICoverPoint cover = null;
        float d = 100000;
        for (int i = 0; i < AllCovers.Count; i++)
        {
            float dis = bl_UtilityHelper.Distance(target.position, AllCovers[i].transform.position);
            if (dis < d)
            {
                d = dis;
                cover = AllCovers[i];
            }
        }
        cover = CheckCoverUsage(cover);
        return cover;
    }

    public bl_AICoverPoint GetCloseCover(Transform target, bl_AICoverPoint overrdidePoint)
    {
        bl_AICoverPoint cover = null;
        float d = MaxDistance;
        for (int i = 0; i < AllCovers.Count; i++)
        {
            float dis = bl_UtilityHelper.Distance(target.position, AllCovers[i].transform.position);
            if (dis < MaxDistance && dis < d && AllCovers[i] != overrdidePoint)
            {
                d = dis;
                cover = AllCovers[i];
            }
        }
        cover = CheckCoverUsage(cover);
        return cover;
    }

    public bl_AICoverPoint CheckCoverUsage(bl_AICoverPoint coverSource)
    {
        if (coverSource == null) return null;

        if ((Time.time - coverSource.lastUseTime) <= UsageTime && coverSource.NeighbordPoints.Count > 0)
        {
            coverSource = coverSource.NeighbordPoints[Random.Range(0, coverSource.NeighbordPoints.Count)];
        }
        coverSource.lastUseTime = Time.time;
        return coverSource;
    }

#if UNITY_EDITOR
    [ContextMenu("Fix Points")]
    private void Fixed()
    {
        bl_AICoverPoint[] sp = FindObjectsOfType<bl_AICoverPoint>();
        RaycastHit r;
        for (int i = 0; i < sp.Length; i++)
        {
            Transform t = sp[i].transform;
            Ray ray = new Ray(t.position + t.up, Vector3.down);
            if (Physics.Raycast(ray, out r, 100))
            {
                t.position = r.point;
            }
        }
    }

    [ContextMenu("Calculate Neighbors")]
    private void CalcuNeighbords()
    {
        bl_AICoverPoint[] sp = FindObjectsOfType<bl_AICoverPoint>();
        for (int i = 0; i < sp.Length; i++)
        {
            Transform t = sp[i].transform;
            sp[i].NeighbordPoints.Clear();
            for (int e = 0; e < sp.Length; e++)
            {
                if (sp[i] == sp[e]) continue;
                if (Vector3.Distance(sp[i].transform.position, sp[e].transform.position) <= 20)
                {
                    sp[i].NeighbordPoints.Add(sp[e]);
                }
            }
            UnityEditor.EditorUtility.SetDirty(sp[i]);
        }
    }
#endif
}