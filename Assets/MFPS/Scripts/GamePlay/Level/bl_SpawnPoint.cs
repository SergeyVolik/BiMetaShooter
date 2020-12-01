using UnityEngine;
#if UNITY_EDITOR
using MFPSEditor;
#endif

public class bl_SpawnPoint : bl_PhotonHelper {

    public Team m_Team = Team.All;
    public float SpawnSpace = 3f;
    private bl_GameManager Manager;

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        if (transform.GetComponent<Renderer>() != null)
        {
            GetComponent<Renderer>().enabled = false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnEnable()
    {
       bl_GameManager.Instance.RegisterSpawnPoint(this);
    }

#if UNITY_EDITOR
    DomeGizmo _gizmo = null;
    void OnDrawGizmosSelected()
    {
        Draw();
    }

    private void OnDrawGizmos()
    {
        if (Manager == null) { Manager = bl_GameManager.Instance; }
        if(Manager != null && Manager.DrawSpawnPoints) { Draw(); }
    }

    void Draw()
    {
        if (Manager == null) { Manager = bl_GameManager.Instance; }
        float h = 180;
        if (_gizmo == null || _gizmo.horizon != h)
        {
            _gizmo = new DomeGizmo(h);
        }

        Color c = (m_Team == Team.Team2) ? bl_GameData.Instance.Team2Color : bl_GameData.Instance.Team1Color;
        if (m_Team == Team.All) { c = Color.white; }
        Gizmos.color = c;
        _gizmo.Draw(transform, c, SpawnSpace);
        if (Manager != null && Manager.SpawnPointPlayerGizmo != null)
        {
            Gizmos.DrawWireMesh(Manager.SpawnPointPlayerGizmo, transform.position, transform.rotation, Vector3.one * 2.75f);
        }
        Gizmos.DrawLine(base.transform.position + ((base.transform.forward * this.SpawnSpace)), base.transform.position + (((base.transform.forward * 2f) * this.SpawnSpace)));
    }
#endif
}
