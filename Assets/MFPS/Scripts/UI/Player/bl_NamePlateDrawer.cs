using UnityEngine;
#if !UNITY_WEBGL && PVOICE
using Photon.Voice.Unity;
using Photon.Voice.PUN;
#endif
using UnityEngine.Serialization;

//[ExecuteInEditMode]
public class bl_NamePlateDrawer : bl_MonoBehaviour
{
    [Header("Settings")]
    public bool isBot = false;
    public float distanceModifier = 1;
    public float hideDistance = 25;

    [Header("References")]
    public bl_NamePlateStyle StylePresent;
    [FormerlySerializedAs("m_Target")]
    public Transform positionReference;

    //Private
    private float distance;
    public string m_PlayerName { get; set; }
    private Transform myTransform;
#if !UNITY_WEBGL && PVOICE
    private PhotonVoiceView voiceView;
    private float RightPand = 0;
    private float NameSize = 0;
#endif
    private bl_PlayerHealthManager DamagerManager;
    private bl_AIShooterHealth AIHealth;
    private bool ShowHealthBar = false;
    private bool isFinish = false;

    protected override void Awake()
    {
        base.Awake();
#if !UNITY_WEBGL && PVOICE
        if (!isBot)
        {
            voiceView = GetComponent<PhotonVoiceView>();
        }
#endif
        if (!isBot)
        {
            DamagerManager = GetComponent<bl_PlayerHealthManager>();
        }
        else
        {
            AIHealth = GetComponent<bl_AIShooterHealth>();
        }
        ShowHealthBar = bl_GameData.Instance.ShowTeamMateHealthBar;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        this.myTransform = this.transform;
        bl_EventHandler.OnRoundEnd += OnGameFinish;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        bl_EventHandler.OnRoundEnd -= OnGameFinish;
    }

    public void SetName(string DrawName)
    {
        m_PlayerName = DrawName;
#if !UNITY_WEBGL && PVOICE
        NameSize = StylePresent.style.CalcSize(new GUIContent(m_PlayerName)).x;
        RightPand = (NameSize / 2) + 2;
#endif
    }

    void OnGameFinish()
    {
        isFinish = true;
    }

    /// <summary>
    /// 
    /// </summary>
    void OnGUI()
    {
        if (bl_GameManager.Instance.CameraRendered == null || isFinish)
            return;
        if (StylePresent == null) return;

        Vector3 vector = bl_GameManager.Instance.CameraRendered.WorldToScreenPoint(positionReference.position);
        if (vector.z > 0)
        {
            int vertical = ShowHealthBar ? 15 : 10;
            if (this.distance < hideDistance)
            {
                float distanceDifference = Mathf.Clamp(distance - 0.1f, 1, 12);
                vector.y += distanceDifference * distanceModifier;

                GUI.Label(new Rect(vector.x - 5, (Screen.height - vector.y) - vertical, 10, 11), m_PlayerName, StylePresent.style);
                if (ShowHealthBar)
                {
                    float mh = (isBot) ? 100 : DamagerManager.maxHealth;
                    float h = (isBot) ? AIHealth.Health : DamagerManager.health;
                    GUI.color = StylePresent.HealthBackColor;
                    GUI.DrawTexture(new Rect(vector.x - (mh / 2), (Screen.height - vector.y), mh, StylePresent.HealthBarThickness), StylePresent.HealthBarTexture);
                    GUI.color = StylePresent.HealthBarColor;
                    GUI.DrawTexture(new Rect(vector.x - (mh / 2), (Screen.height - vector.y), h, StylePresent.HealthBarThickness), StylePresent.HealthBarTexture);
                    GUI.color = Color.white;
                }

#if !UNITY_WEBGL && PVOICE
                //voice chat icon
                if (!isBot && voiceView.IsSpeaking)
                {
                    GUI.DrawTexture(new Rect(vector.x + RightPand, (Screen.height - vector.y) - vertical, 14, 14), StylePresent.TalkingIcon);
                }
#endif
            }
            else
            {
                float iconSize = StylePresent.IndicatorIconSize;
                GUI.DrawTexture(new Rect(vector.x - (iconSize * 0.5f), (Screen.height - vector.y) - (iconSize * 0.5f), iconSize, iconSize), StylePresent.IndicatorIcon);
            }
        }
    }



    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (bl_GameManager.Instance.CameraRendered == null)
            return;

        distance = bl_UtilityHelper.Distance(myTransform.position, bl_GameManager.Instance.CameraRendered.transform.position);
    }
}