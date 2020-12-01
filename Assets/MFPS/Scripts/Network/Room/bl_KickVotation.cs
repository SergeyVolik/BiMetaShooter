using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class bl_KickVotation : bl_MonoBehaviour
{

    [SerializeField] private KeyCode YesKey = KeyCode.F1;
    [SerializeField] private KeyCode NoKey = KeyCode.F2;

    private PhotonView View;
    private bool IsOpen = false;

    private int YesCount = 0;
    private int NoCount = 0;
    private bl_KickVotationUI UI;
    private Player TargetPlayer;
    private bool isAgainMy = false;
    private bool Voted = false;
    private int AllVoters = 0;

    protected override void Awake()
    {
        base.Awake();
        View = PhotonView.Get(this);
        UI = FindObjectOfType<bl_KickVotationUI>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        bl_PhotonCallbacks.PlayerLeftRoom += OnPhotonPlayerDisconnected;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        bl_PhotonCallbacks.PlayerLeftRoom -= OnPhotonPlayerDisconnected;
    }

    public void RequestKick(Player player)
    {
        if (IsOpen || player == null)
            return;
        if(PhotonNetwork.PlayerList.Length < 3)
        {
            Debug.Log("there are not enough players.");
            return;
        }
        if (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("You can not send a vote for yourself.");
            return;
        }

        View.RPC("RpcRequestKick", RpcTarget.All, player);
    }

    [PunRPC]
    void RpcRequestKick(Player player, PhotonMessageInfo info)
    {
        if (IsOpen)
            return;

        AllVoters = PhotonNetwork.PlayerListOthers.Length;
        TargetPlayer = player;
        ResetVotation();
        isAgainMy = (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber);
        UI.OpenVotatation(player, info.Sender);
        IsOpen = true;
    }

    /// <summary>
    /// 
    /// </summary>
    void ResetVotation()
    {
        YesCount = 0;
        NoCount = 0;
        isAgainMy = false;
        Voted = false;
    }

    public override void OnUpdate()
    {
        if (!IsOpen || isAgainMy || Voted)
            return;
        if (TargetPlayer == null)
            return;

        if (Input.GetKeyDown(YesKey))
        {
            SendVote(true);
            Voted = true;
        }
        else if (Input.GetKeyDown(NoKey))
        {
            SendVote(false);
            Voted = true;
        }
    }

    void SendVote(bool yes)
    {
        View.RPC("RPCReceiveVote", RpcTarget.All, yes);
        UI.OnSendLocalVote(yes);
    }

    [PunRPC]
    void RPCReceiveVote(bool yes, PhotonMessageInfo info)
    {
        if (yes)
        {
            YesCount++;
        }
        else
        {
            NoCount++;
        }
        UI.OnReceiveVote(YesCount, NoCount);
        if (PhotonNetwork.IsMasterClient)//master count all votes to determine if kick or not
        {
            CountVotes();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void CountVotes()
    {
        int half = (AllVoters / 2);
        bool kicked = false;
        if (YesCount > half)//kick
        {
            bl_PhotonNetwork.Instance.KickPlayer(TargetPlayer);
            kicked = true;
            View.RPC("EndVotation", RpcTarget.All, kicked);
        }
        else if (NoCount > half)//no kick
        {
            View.RPC("EndVotation", RpcTarget.All, kicked);
        }
    }

    [PunRPC]
    void EndVotation(bool finaledKicked)
    {
        IsOpen = false;
        Voted = true;
        UI.OnFinish(finaledKicked);
        TargetPlayer = null;
    }

    public void OnPhotonPlayerDisconnected(Player otherPlayer)
    {
        if (TargetPlayer == null)
            return;

       if(otherPlayer.ActorNumber == TargetPlayer.ActorNumber)
        {
            //cancel voting due player left the room by himself
            UI.OnFinish(true);
        }
    }

    private static bl_KickVotation _instance;
    public static bl_KickVotation Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<bl_KickVotation>(); }
            return _instance;
        }
    }
}