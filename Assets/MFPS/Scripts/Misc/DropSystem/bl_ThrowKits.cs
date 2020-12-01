using UnityEngine;

public class bl_ThrowKits : bl_MonoBehaviour
{
    [Header("SETTINGS")]
    /// <summary>
    /// key to instantiate MedKit
    /// </summary>
    public KeyCode ThrowKey = KeyCode.J;
    public int AmountOfKits = 3;
    /// <summary>
    /// force when it is instantiated prefabs
    /// </summary>
    public float ForceImpulse = 500;
    [Range(1, 4)] public float CallDelay = 1.4f;

    [Header("REFERENCES")]
    /// <summary>
    /// Prefabs for instantiate
    /// </summary>
    public GameObject DropCallerPrefab;
    /// <summary>
    /// Reference position where the kit will be instantiated
    /// </summary>
    public Transform InstancePoint;
    public AudioClip SpawnSound;
#if !CLASS_CUSTOMIZER
    private PlayerClass m_class = PlayerClass.Assault;
#endif

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
#if !CLASS_CUSTOMIZER
        m_class = bl_RoomMenu.PlayerClass;
#endif
    }

#if MFPSM
    protected override void OnEnable()
    {
        base.OnEnable();
        bl_TouchHelper.OnKit += OnMobileClick;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        bl_TouchHelper.OnKit -= OnMobileClick;
    }

    /// <summary>
    /// 
    /// </summary>
     void OnMobileClick()
    {
        if (AmountOfKits <= 0 || DropCallerPrefab == null) return;

        int id = 0;
#if !CLASS_CUSTOMIZER
        if ((m_class == PlayerClass.Assault || m_class == PlayerClass.Recon))
        {
            id = 1;
        }
#else
        id = bl_ClassManager.Instance.ClassKit;
#endif
        ThrowCaller(id);
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (bl_GameData.Instance.isChating) return;
        if (AmountOfKits <= 0 || DropCallerPrefab == null) return;

#if !INPUT_MANAGER
#if !CLASS_CUSTOMIZER
        if (Input.GetKeyDown(ThrowKey))
        {
            int id = 0;
            if((m_class == PlayerClass.Assault || m_class == PlayerClass.Recon))
            {
                id = 1;
            }
            ThrowCaller(id);
        }
#else
        ClassCutomizeInput();
#endif
#else
#if !CLASS_CUSTOMIZER
        if (bl_Input.isButtonDown("ThrowItem"))
        {
            int id = 0;
            if ((m_class == PlayerClass.Assault || m_class == PlayerClass.Recon))
            {
                id = 1;
            }
            ThrowCaller(id);
        }
#else
        ClassCutomizeInput();
#endif
#endif

    }

#if CLASS_CUSTOMIZER
    void ClassCutomizeInput()
    {
#if !INPUT_MANAGER
        if (Input.GetKeyDown(ThrowKey))
        {
            int id = bl_ClassManager.Instance.ClassKit;
            ThrowCaller(id);
        }
#else
        if (bl_Input.isButtonDown("ThrowItem"))
        {
            int id = bl_ClassManager.Instance.ClassKit;
            ThrowCaller(id);
        }
#endif
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    void ThrowCaller(int id)
    {
        AmountOfKits--;
        GameObject kit = Instantiate(DropCallerPrefab, InstancePoint.position, Quaternion.identity) as GameObject;
        kit.GetComponent<bl_DropCaller>().SetUp(id, CallDelay);
        kit.GetComponent<Rigidbody>().AddForce(transform.forward * ForceImpulse);
        if (SpawnSound)
        {
            AudioSource.PlayClipAtPoint(SpawnSound, this.transform.position, 1.0f);
        }
    }
}