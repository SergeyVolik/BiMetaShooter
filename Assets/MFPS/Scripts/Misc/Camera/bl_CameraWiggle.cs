using UnityEngine;
using System.Collections;

public class bl_CameraWiggle : bl_MonoBehaviour
{
    [Header("Wiggle")]
    private Transform m_transform;
    public float smooth = 4f;
    public float tiltAngle = 6f;
    [Header("FallEffect")]
    [Range(0.01f, 1.0f)]
    public float m_time = 0.2f;
    public float DownAmount = 8;
    private bool wiggle = true;

    protected override void Awake()
    {
        base.Awake();
        this.m_transform = this.transform;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        bl_EventHandler.OnSmallImpact += this.OnSmallImpact;
        wiggle = bl_GameData.Instance.playerCameraWiggle;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        bl_EventHandler.OnSmallImpact -= this.OnSmallImpact;
    }

    public override void OnUpdate()
    {
        if (!wiggle) return;

        if (bl_RoomMenu.Instance.isCursorLocked)
        {
            float t_amount = -Input.GetAxis("Mouse X") * this.tiltAngle;
            t_amount = Mathf.Clamp(t_amount, -this.tiltAngle, this.tiltAngle);
            if (!Input.GetMouseButton(1))
            {
                m_transform.localRotation = Quaternion.Lerp(this.m_transform.localRotation, Quaternion.Euler(0, 0, t_amount), Time.deltaTime * this.smooth);
            }
            else
            {
                m_transform.localRotation = Quaternion.Lerp(this.m_transform.localRotation, Quaternion.Euler(0, 0, t_amount / 2), Time.deltaTime * this.smooth);
            }
        }
    }

    void OnSmallImpact()
    {
        StartCoroutine(FallEffect());
    }

    IEnumerator FallEffect()
    {
        Quaternion m_default = this.transform.localRotation;
        Quaternion m_finaly = this.transform.localRotation * Quaternion.Euler(new Vector3(DownAmount, 0, 0));
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