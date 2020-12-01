/////////////////////////////////////////////////////////////////////////////////
////////////////////////////bl_DamageIndicator.cs////////////////////////////////
////////////////////Use this to signal the last attack received///////////////
/////////////////////////////////////////////////////////////////////////////////
////////////////////////////////Lovatto Studio///////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using UnityEngine.UI;

public class bl_DamageIndicator : bl_MonoBehaviour
{

    /// <summary>
    /// Attack from direction
    /// </summary>
    [HideInInspector] public Vector3 attackDirection;
    /// <summary>
    /// time reach for fade arrow
    /// </summary>
    [Range(1, 5)] public float FadeTime = 3;
    /// <summary>
    /// the transform root of player 
    /// </summary>
    public Transform target;
    //Private
    private Vector2 pivotPoint;
    private float alpha = 0.0f;
    private float rotationOffset;
    private Transform IndicatorPivot;
    private CanvasGroup IndicatorImage;
    Vector3 eulerAngle = Vector3.zero;
    Vector3 forward;
    Vector3 rhs;

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        IndicatorImage = bl_UIReferences.Instance.PlayerUI.DamageIndicator.GetComponent<CanvasGroup>();
        if (IndicatorImage != null) { IndicatorPivot = IndicatorImage.transform.parent; }
    }

    /// <summary>
    /// Use this to send a new direction of attack
    /// </summary>
    public void AttackFrom(Vector3 dir)
    {
        if (dir == Vector3.zero)
            return;
        
        this.attackDirection = dir;
        this.alpha = 3f;
    }
    /// <summary>
    /// if this is visible Update position
    /// </summary>
    public override void OnUpdate()
    {
        if (this.alpha > 0)
        {
            this.alpha -= Time.deltaTime;
            this.UpdateDirection();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnDisable()
    {
        if (IndicatorImage != null)
            IndicatorImage.alpha = 0;
    }

    /// <summary>
    /// update direction as the arrow shows
    /// </summary>
    void UpdateDirection()
    {
        rhs = attackDirection - target.position;
        rhs.y = 0;
        rhs.Normalize();
        if (bl_GameManager.Instance.CameraRendered != null)
        {
            forward = bl_GameManager.Instance.CameraRendered.transform.forward;
        }
        else
        {
            forward = transform.forward;
        }
        float GetPos = Vector3.Dot(forward, rhs);
        if (Vector3.Cross(forward, rhs).y > 0)
        {
            rotationOffset = (1f - GetPos) * 90;
        }
        else
        {
            rotationOffset = (1f - GetPos) * -90;
        }
        if (IndicatorPivot != null)
        {
            IndicatorImage.alpha = alpha;
            eulerAngle.z = -rotationOffset;
            IndicatorPivot.eulerAngles = eulerAngle;
        }
    }
}