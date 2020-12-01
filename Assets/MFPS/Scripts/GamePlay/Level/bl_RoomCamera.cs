using UnityEngine;
using System.Collections;

public class bl_RoomCamera : bl_MonoBehaviour
{
    [Header("Auto Rotation")]
    [LovattoToogle] public bool autoRotation = true;
    public float rotationSpeed = 4;

    [Header("Fly Camera")]
    public float cameraSensitivity = 90;
    public float climbSpeed = 4;
    public float normalMoveSpeed = 10;
    public float slowMoveFactor = 0.25f;
    public float fastMoveFactor = 3;

    private float rotationX = 0.0f;
    private float rotationY = 0.0f;
    public bool cameraControl { get; set; } = false;
    private Transform m_Transform;

    /// <summary>
    /// 
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        Camera.SetupCurrent(GetComponent<Camera>());
        m_Transform = transform;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        FlyMovement();
        Rotate();
    }

    void Rotate()
    {
        if (!autoRotation || cameraControl) return;

        m_Transform.Rotate(Vector3.up * Time.deltaTime * rotationSpeed);
    }

    /// <summary>
    /// 
    /// </summary>
    void FlyMovement()
    {
        if (!cameraControl) return;

        rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
        rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
        rotationY = Mathf.Clamp(rotationY, -90, 90);

        transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
        transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            transform.position += transform.forward * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
            transform.position += transform.right * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            transform.position += transform.forward * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
            transform.position += transform.right * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
        }
        else
        {
            transform.position += transform.forward * normalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
            transform.position += transform.right * normalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
        }


        if (Input.GetKey(KeyCode.Q)) { transform.position += transform.up * climbSpeed * Time.deltaTime; }
        if (Input.GetKey(KeyCode.E)) { transform.position -= transform.up * climbSpeed * Time.deltaTime; }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            bl_UtilityHelper.LockCursor((bl_RoomMenu.Instance.isCursorLocked == false) ? true : false);
        }
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    private static bl_RoomCamera _instance;
    public static bl_RoomCamera Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<bl_RoomCamera>();
            }
            return _instance;
        }
    }
}