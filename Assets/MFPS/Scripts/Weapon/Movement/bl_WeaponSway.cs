///////////////////////////////////////////////////////////////////////////////////////
// bl_WeaponSway.cs
//
// Generates a soft effect and delayed rotation for added realism
// place it in the top of the hierarchy of Weapon Manager
//                           
//                                 Lovatto Studio
///////////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;

public class bl_WeaponSway : bl_MonoBehaviour
{

    private float maxAmount = 0.05F;
    [Header("Delay Effect")]
    [Range(0.2f, 5)] public float MovementAmount = 2;
    public float Smoothness = 3.0F;
    [Header("FallEffect")]
    [Range(0.01f, 1.0f)]
    public float m_time = 0.2f;
    public float m_ReturnSpeed = 5;
    public float SliderAmount = 12;
    public float DownAmount = 13;
    //private
    private Vector3 def;
    private Quaternion DefaultRot;
    public float Amount { get; set; }

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        def = transform.localPosition;
        DefaultRot = this.transform.localRotation;
        Amount = MovementAmount;
    }

    public void ResetSettings()
    {
        Amount = MovementAmount;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (!bl_RoomMenu.Instance.isCursorLocked)
            return;

        float delta = Time.smoothDeltaTime;
        if (!bl_UtilityHelper.isMobile)
        {
            float factorX = -Input.GetAxis("Mouse X") * Time.deltaTime * Amount;
            float factorY = -Input.GetAxis("Mouse Y") * Time.deltaTime * Amount;
            factorX = Mathf.Clamp(factorX, -maxAmount, maxAmount);
            factorY = Mathf.Clamp(factorY, -maxAmount, maxAmount);
            Vector3 Final = new Vector3(def.x + factorX, def.y + factorY, def.z);
            transform.localPosition = Vector3.Lerp(transform.localPosition, Final, delta * Smoothness);
        }
        this.transform.localRotation = Quaternion.Slerp(this.transform.localRotation, DefaultRot, delta * m_ReturnSpeed);

    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        bl_EventHandler.OnFall += this.OnFall;
        bl_EventHandler.OnSmallImpact += this.OnSmallImpact;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        bl_EventHandler.OnFall -= this.OnFall;
        bl_EventHandler.OnSmallImpact -= this.OnSmallImpact;
    }

    /// <summary>
    /// On event fall 
    /// </summary>
    void OnFall(float t_amount)
    {
        StartCoroutine(FallEffect());
    }
    /// <summary>
    /// On Impact event
    /// </summary>
    void OnSmallImpact()
    {
        StartCoroutine(FallEffect());
    }
    /// <summary>
    /// create a soft impact effect
    /// </summary>
    /// <returns></returns>
    public IEnumerator FallEffect()
    {
        Quaternion m_default = this.transform.localRotation;
        Quaternion m_finaly = this.transform.localRotation * Quaternion.Euler(new Vector3(DownAmount, Random.Range(-SliderAmount, SliderAmount), 0));
        float t_rate = 1.0f / m_time;
        float t_time = 0.0f;
        while (t_time < 1.0f)
        {
            t_time += Time.deltaTime * t_rate;
            this.transform.localRotation = Quaternion.Slerp(m_default, m_finaly, t_time);
            yield return t_rate;
        }
    }
}