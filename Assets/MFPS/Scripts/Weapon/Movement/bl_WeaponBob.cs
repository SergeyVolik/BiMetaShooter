using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class bl_WeaponBob : bl_MonoBehaviour
{
    public bl_WeaponBobSettings settings;
    Vector3 midpoint;
    Vector3 localRotation;
    GameObject player;
    float timer = 0.0f;
    float lerp = 2;
    float bobbingSpeed;
    bl_FirstPersonController motor;
    float BobbingAmount;
    float tempWalkSpeed = 0;
    float tempRunSpeed = 0;
    float tempIdleSpeed = 0;
    float waveslice = 0.0f;
    float waveslice2 = 0.0f;
    public bool isAim { get; set; }
    float eulerZ = 0;
    float eulerX = 0;
    private bool rightFoot = false;
    public float Intensitity { get; set; }
    private Transform m_Transform;
    Vector3 currentPosition = Vector3.zero;
    Vector3 currentRotation = Vector3.zero;
    public bool useAnimation { get; set; }
    private Action<PlayerState> AnimateCallback = null;

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        player = transform.root.gameObject;
        motor = player.GetComponent<bl_FirstPersonController>();
        midpoint = transform.localPosition;
        localRotation = transform.localEulerAngles;
        Intensitity = 1;
        m_Transform = transform;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (motor == null) return;

        if (useAnimation)
        {
            if (AnimateCallback == null) return;
            AnimateCallback.Invoke(motor.State);
        }
        StateControl();
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnFixedUpdate()
    {
        Movement();
    }

    /// <summary>
    /// 
    /// </summary>
    void StateControl()
    {
        if (motor.State == PlayerState.Jumping) return;
        if (motor.VelocityMagnitude > 0.1f && motor.State != PlayerState.Running)
        {
            bobbingSpeed = tempWalkSpeed;
            BobbingAmount = settings.WalkOscillationAmount;
            lerp = settings.WalkLerpSpeed;
            eulerZ = settings.EulerZAmount;
            eulerX = settings.EulerXAmount;
        }
        else if (motor.State == PlayerState.Running)
        {
            bobbingSpeed = tempRunSpeed;
            BobbingAmount = settings.RunOscillationAmount;
            lerp = settings.RunLerpSpeed;
            eulerZ = settings.RunEulerZAmount;
            eulerX = settings.RunEulerXAmount;
        }

        if (motor.State != PlayerState.Running && motor.VelocityMagnitude < 0.1f || !bl_RoomMenu.Instance.isCursorLocked)
        {
            bobbingSpeed = tempIdleSpeed;
            BobbingAmount = settings.WalkOscillationAmount * 0.1f;
            lerp = settings.WalkLerpSpeed;
            eulerZ = settings.EulerZAmount;
            eulerX = settings.EulerXAmount;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void Movement()
    {
        tempWalkSpeed = 0;
        tempRunSpeed = 0;
        tempIdleSpeed = 0;

        if (tempIdleSpeed != settings.idleBobbingSpeed)
        {
            tempWalkSpeed = motor.speed * 0.06f * settings.WalkSpeedMultiplier;
            tempRunSpeed = motor.speed * 0.03f * settings.RunSpeedMultiplier;
            tempIdleSpeed = settings.idleBobbingSpeed;
        }

        waveslice = Mathf.Sin(timer * 2);
        waveslice2 = Mathf.Sin(timer);
        timer = timer + bobbingSpeed;
        if (timer > Mathf.PI * 2)
        {
            timer = timer - (Mathf.PI * 2);
        }
        ApplyMovement();
        UpdateFootStep();
    }

    /// <summary>
    /// 
    /// </summary>
    void ApplyMovement()
    {
        if (useAnimation) return;
        float time = Time.smoothDeltaTime;
        if (waveslice != 0)
        {
            float TranslateChange = waveslice * BobbingAmount * Intensitity;
            float TranslateChange2 = waveslice2 * BobbingAmount * Intensitity;
            float rotChange = waveslice2 * eulerZ;
            float rotChange2 = waveslice * eulerX;

            if (motor.isGrounded)
            {
                //if player is moving
                if (motor.VelocityMagnitude > 0.1f && motor.State != PlayerState.Idle)
                {
                    currentPosition = new Vector3(midpoint.x + TranslateChange2, midpoint.y + TranslateChange, currentPosition.z);
                    currentRotation = new Vector3(localRotation.x + rotChange2, localRotation.y, localRotation.z + rotChange);
                    Vector3 bob = new Vector3(0, 0, (rotChange * motor.headBobMagnitude));
                    m_Transform.localRotation = Quaternion.Slerp(m_Transform.localRotation, Quaternion.Euler(currentRotation), time * lerp);
                    motor.CameraRoot.localRotation = Quaternion.Slerp(motor.CameraRoot.localRotation, Quaternion.Euler(bob), time * lerp);
                }
                else//is player is idle
                {
                    currentPosition = new Vector3(midpoint.x, midpoint.y + TranslateChange, currentPosition.z);
                    m_Transform.localRotation = Quaternion.Slerp(m_Transform.localRotation, Quaternion.Euler(Vector3.zero), time * 10);
                    motor.CameraRoot.localRotation = Quaternion.Slerp(motor.CameraRoot.localRotation, Quaternion.Euler(Vector3.zero), time * lerp);
                }
            }
            else
            {
                //Player not moving
                MoveToDefault();
            }
        }
        else
        {
            //Player not moving
            MoveToDefault();
        }
        m_Transform.localPosition = Vector3.Lerp(m_Transform.localPosition, currentPosition, time * lerp);
    }

    /// <summary>
    /// 
    /// </summary>
    void MoveToDefault()
    {
        currentPosition = midpoint;
        m_Transform.localRotation = Quaternion.Slerp(m_Transform.localRotation, Quaternion.Euler(Vector3.zero), Time.smoothDeltaTime * 12);
        motor.CameraRoot.localRotation = Quaternion.Slerp(motor.CameraRoot.localRotation, Quaternion.Euler(Vector3.zero), Time.smoothDeltaTime * lerp);
    }

    /// <summary>
    /// 
    /// </summary>
    void UpdateFootStep()
    {
        if (motor.VelocityMagnitude > 0.1f)
        {
            if (waveslice2 >= 0.97f && !rightFoot)
            {
                motor.PlayFootStepAudio(true);
                rightFoot = true;
            }
            else if (waveslice2 <= (-0.97f) && rightFoot)
            {
                motor.PlayFootStepAudio(true);
                rightFoot = false;
            }
        }
    }

    public void AnimatedThis(Action<PlayerState> callback, bool useAnim)
    {
        AnimateCallback = callback;
        useAnimation = useAnim;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(bl_WeaponBob))]
public class bl_WeaponEditorBob : Editor
{
    bl_WeaponBob script;

    private void OnEnable()
    {
        script = (bl_WeaponBob)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        GUILayout.Space(5);
        SerializedProperty so = serializedObject.FindProperty("settings");
        EditorGUILayout.PropertyField(so);
        if (so != null && so.objectReferenceValue != null)
        {
            var editor = Editor.CreateEditor(so.objectReferenceValue);
            if (editor != null)
            {
                EditorGUILayout.BeginVertical("box");
                editor.DrawDefaultInspector();
                EditorGUILayout.EndVertical();
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif