using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
#if !UNITY_WEBGL && PVOICE
using Photon.Voice.Unity;
using Photon.Voice.PUN;
#endif

public class bl_PlayerVoice : bl_MonoBehaviour
{
    public KeyCode PushButton = KeyCode.P;

    private GameObject RecorderIcon;
#if !UNITY_WEBGL && PVOICE
    private Recorder VoiceRecorder;
    private Speaker Speaker;
    private bool PushToTalk = false;
#endif
    private PhotonView View;

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        View = photonView;
        if (!PhotonNetwork.InRoom || PhotonNetwork.OfflineMode)
            return;

        RecorderIcon = bl_UIReferences.Instance.PlayerUI.SpeakerIcon;
#if !UNITY_WEBGL && PVOICE
        VoiceRecorder = FindObjectOfType<Recorder>();
        Speaker = GetComponent<Speaker>();
#endif
#if !UNITY_WEBGL && PVOICE
        if (View.IsMine)
        {
            if(VoiceRecorder != null)
            VoiceRecorder.enabled = true;
            PushToTalk = bl_UIReferences.Instance.PushToTalkToggle.isOn;
            if(Speaker != null)
            Speaker.enabled = !bl_UIReferences.Instance.MuteVoiceToggle.isOn;
        }
        else
        {
            if (GetGameMode != GameMode.FFA)
            {
                if (Speaker != null)
                    Speaker.enabled = photonView.Owner.GetPlayerTeam() == PhotonNetwork.LocalPlayer.GetPlayerTeam();
            }
        }
#else
        RecorderIcon.SetActive(false);
#endif
    }

    protected override void OnEnable()
    {
        base.OnEnable();
#if MFPSM
        if (View.IsMine)
        {
            bl_TouchHelper.OnTransmit += OnPushToTalkMobile;
        }
#endif
    }

    protected override void OnDisable()
    {
        base.OnDisable();
#if MFPSM
        if (View.IsMine)
        {
            bl_TouchHelper.OnTransmit -= OnPushToTalkMobile;
        }
#endif
    }

    public void OnPushToTalkMobile(bool transmit)
    {
#if !UNITY_WEBGL && PVOICE
        if (VoiceRecorder != null)
            VoiceRecorder.TransmitEnabled = transmit;
#endif
    }

#if !UNITY_WEBGL && PVOICE
    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (VoiceRecorder == null) return;
        if (!PhotonNetwork.InRoom || PhotonNetwork.OfflineMode)
            return;

        if (View.IsMine)
        {
            RecorderIcon.SetActive(VoiceRecorder.TransmitEnabled && VoiceRecorder.IsCurrentlyTransmitting && VoiceRecorder.LevelMeter.CurrentPeakAmp > 0.1f);
            if (PushToTalk)
            {
                VoiceRecorder.TransmitEnabled = Input.GetKey(PushButton);
            }
        }
    }
#endif

    public void OnMuteChange(bool b)
    {
        if (View.IsMine)
        {
#if !UNITY_WEBGL && PVOICE
            if (Speaker != null)
                Speaker.enabled = !b;
#endif
        }
    }

    public void OnPushToTalkChange(bool b)
    {
        if (View.IsMine)
        {
#if !UNITY_WEBGL && PVOICE
            PushToTalk = b;
#endif
#if MFPSM
            if (bl_TouchHelper.Instance != null)
            {
                bl_TouchHelper.Instance.OnPushToTalkChange(b);
            }
#endif
        }
    }
}