using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class bl_SniperScope : bl_MonoBehaviour {

    public Sprite Scope;
    /// <summary>
    /// Object to deactivate when is aimed
    /// </summary>
    public List<GameObject> OnScopeDisable = new List<GameObject>();
    public bool ShowDistance = true;
    public float FadeSpeed = 12;

    //private
    private bl_Gun m_gun;
    private float m_alpha = 0;
    private Vector3 m_point = Vector3.zero;
    private float m_dist = 0.0f;
    private Image ScopeImage;
    private CanvasGroup Alpha;
    private Text DistanceText;
    private bool returnedAim = true;

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        m_gun = GetComponent<bl_Gun>();
        ScopeImage = bl_UIReferences.Instance.PlayerUI.SniperScope;
       
        if (ScopeImage)
        {
            ScopeImage.sprite = Scope;
            Alpha = ScopeImage.gameObject.GetComponent<CanvasGroup>();
            DistanceText = ScopeImage.gameObject.GetComponentInChildren<Text>();
            if (Alpha != null)
            {
                Alpha.alpha = 0;
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (Alpha != null)
        {
            Alpha.alpha = 0;
        }
    }

    public override void OnUpdate()
    {
        if (m_gun == null)
            return;
        if (Scope == null || ScopeImage == null)
            return;

        if (m_gun.isAiming && !m_gun.isReloading)
        {
            if (ShowDistance)
            {
                GetDistance();
            }
            //add a little fade in to avoid the impact of appearing once
            m_alpha = Mathf.Lerp(m_alpha, 1.0f, Time.deltaTime * FadeSpeed);
            foreach (GameObject go in OnScopeDisable)
            {
                go.SetActive(false);
            }
            returnedAim = false;
        }
        else
        {
            m_alpha = Mathf.Lerp(m_alpha, 0.0f, Time.deltaTime * FadeSpeed);
            if (m_alpha < 0.1f)
            {
                if (!returnedAim && m_gun.Info != null)
                {
                    m_gun.SetToAim();
                    returnedAim = true;
                }
                foreach (GameObject go in OnScopeDisable)
                {
                    go.SetActive(true);
                }
            }
        }
        if(ShowDistance && DistanceText)
        {
            DistanceText.text = m_dist.ToString("00") + "<size=10>m</size>";
        }
        Alpha.alpha = m_alpha;
    }

    /// <summary>
    /// calculate the distance to the first object that raycast hits
    /// </summary>
    RaycastHit m_ray;
    void GetDistance()
    {
        Vector3 fwd = bl_GameManager.Instance.CameraRendered.transform.forward;
        if (Physics.Raycast(bl_GameManager.Instance.CameraRendered.transform.position, fwd, out m_ray, 1000))
        {
            m_point = m_ray.point;
            m_dist = bl_UtilityHelper.Distance(m_point, bl_GameManager.Instance.CameraRendered.transform.position);
        }
        else
        {
            m_dist = 0.0f;
        }
    }

}