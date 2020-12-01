using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class bl_ChatRoom : bl_MonoBehaviour
{

    public KeyCode AllKey = KeyCode.T;
    public KeyCode TeamKey = KeyCode.Y;
    public int MaxMsn = 7;

    public static readonly string ChatRPC = "";
    private List<string> messages = new List<string>();
    private MessageTarget messageTarget = MessageTarget.All;
    private GameObject InputUI;
    private InputField m_InputField;


    protected override void Awake()
    {
        base.Awake();
        InputUI = bl_UIReferences.Instance.ChatInputField;
        m_InputField = InputUI.GetComponentInChildren<InputField>();
        bl_GameData.Instance.isChating = false;
        Invoke("HideChat", 5);
    }

    private void Start()
    {
#if LOCALIZATION
        AddLine(bl_Localization.Instance.GetText(29));
#else
       AddLine(bl_GameTexts.OpenChatStart);
#endif
    }

    public override void OnUpdate()
    {
        if (!bl_GameData.Instance.isChating)
        {
            if (Input.GetKeyDown(AllKey))
            {
                messageTarget = MessageTarget.All;
                SetChat();
            }
            if (Input.GetKeyDown(TeamKey))
            {
                messageTarget = MessageTarget.Team;
                SetChat();
            }
        }
        else
        {
            if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SetChat();
            }
        }
    }

    public void SetChat(string txt)
    {
        if (!PhotonNetwork.IsConnected)
            return;

        photonView.RPC("Chat", RpcTarget.All, txt, messageTarget);
        SetChat();
    }


    /// <summary>
    /// 
    /// </summary>
    public void SetChat()
    {
        bl_GameData.Instance.isChating = !bl_GameData.Instance.isChating;
        m_InputField.text = string.Empty;
        InputUI.SetActive(bl_GameData.Instance.isChating);
        if (bl_GameData.Instance.isChating)
        {
            m_InputField.ActivateInputField();
        }
        else { m_InputField.DeactivateInputField(); }
    }

    /// <summary>
    /// Sync Method
    /// </summary>
    [PunRPC]
    public void Chat(string newLine, MessageTarget mt, PhotonMessageInfo mi)
    {
        if (mt == MessageTarget.Team && mi.Sender.GetPlayerTeam() != PhotonNetwork.LocalPlayer.GetPlayerTeam())//check if team message and only team receive it
            return;

        string senderName = "anonymous";

        if (mi.Sender != null)
        {
            if (!string.IsNullOrEmpty(mi.Sender.NickName))
            {
                senderName = mi.Sender.NickName;
            }
            else
            {
                senderName = "Player " + mi.Sender.ActorNumber;
            }
        }

        string txt = string.Format("[{0}] [{1}]:{2}", mt.ToString().ToUpper(), senderName, newLine);
        this.messages.Add(txt);
        if (messages.Count > MaxMsn)
        {
            messages.RemoveAt(0);
        }

        bl_UIReferences.Instance.ChatText.text = string.Empty;
        foreach (string m in messages)
        {
            bl_UIReferences.Instance.ChatText.text += m + "\n";
        }
        CancelInvoke("HideChat");
        bl_UIReferences.Instance.ChatText.CrossFadeAlpha(1, 0.3f, true);
        Invoke("HideChat", 5);
    }

    void HideChat()
    {
        bl_UIReferences.Instance.ChatText.CrossFadeAlpha(0, 2, true);
    }

    /// <summary>
    /// Local Method
    /// </summary>
    /// <param name="newLine"></param>
    public void AddLine(string newLine)
    {
        this.messages.Add(newLine);
        if (messages.Count > MaxMsn)
        {
            messages.RemoveAt(0);
        }
    }

    public void Refresh()
    {
        bl_UIReferences.Instance.ChatText.text = string.Empty;
        foreach (string m in messages)
            bl_UIReferences.Instance.ChatText.text += m + "\n";
    }

    public enum MessageTarget
    {
        All,
        Team,
    }
}