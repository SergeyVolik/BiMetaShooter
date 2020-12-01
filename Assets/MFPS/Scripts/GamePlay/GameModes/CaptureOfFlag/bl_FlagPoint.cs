using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class bl_FlagPoint : bl_PhotonHelper
{
    //Flag GUI
    public Team Team;
    public FlagState State = FlagState.InHome;
    private Color IconColor;
    public Texture2D FlagIcon;
    public Transform IconTarget;
    public Vector2 IconSize = new Vector2(7, 7);  
    public float ReturnTime;

    float m_ReturnTimer;
    Vector3 m_HomePosition;
    bl_PlayerSettings m_CarryingPlayer;

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        m_HomePosition = transform.position;
        IconColor = Team.GetTeamColor();
    }

    /// <summary>
    /// 
    /// </summary>
    void LateUpdate()
    {
        HandleFlagDrop();
        UpdatePosition();
        HandleFlagCapture();
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateReturnTimer();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag(bl_PlayerSettings.LocalTag))
        {
            bl_PlayerSettings logic = collider.gameObject.GetComponent<bl_PlayerSettings>();
            if (CanBePickedUpBy(logic) == true)
            {
                photonView.RPC("OnChangeFlagState", RpcTarget.AllBuffered, FlagState.PickUp);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void UpdateReturnTimer()
    {
        if (m_ReturnTimer == -1f)
        {
            return;
        }

        m_ReturnTimer -= Time.deltaTime;

        if (m_ReturnTimer <= 0f)
        {
            m_ReturnTimer = -1f;
            ReturnFlag();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void UpdatePosition()
    {
        if (m_CarryingPlayer == null)
        {
            return;
        }

        transform.position = m_CarryingPlayer.FlagPosition.position;
    }

    /// <summary>
    /// If the local player died, send the drop flag event to all players
    /// </summary>
    void HandleFlagDrop()
    {
        if (m_CarryingPlayer == null)
        {
            return;
        }
        bl_PlayerHealthManager playerhealth = m_CarryingPlayer.GetComponent<bl_PlayerHealthManager>();
        if (playerhealth.health <= 0)
        {
            DropFlag();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void HandleFlagCapture()
    {
        if (m_CarryingPlayer == null)
            return;
        if (m_CarryingPlayer.View.Owner.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("No Mine");
            return;
        }

        bl_FlagPoint oppositeFlag = bl_CaptureOfFlag.Instance.GetFlag(bl_CaptureOfFlag.GetOppositeTeam(Team));
        if (oppositeFlag.IsHome() == true && bl_UtilityHelper.Distance(transform.position, oppositeFlag.transform.position) < 3f)
        {
            photonView.RPC("OnChangeFlagState", RpcTarget.AllBuffered, FlagState.Captured);
            Debug.Log("Capture");
        }
    }

    /// <summary>
    /// Determines whether this instance is at the home base
    /// </summary>
    /// <returns></returns>
    public bool IsHome()
    {
        return transform.position == m_HomePosition;
    }

    /// <summary>
    /// 
    /// </summary>
    void DropFlag()
    {
        if (PhotonNetwork.IsMasterClient == true)
        {
            photonView.RPC("OnDropFlag", RpcTarget.AllBuffered, new object[] { transform.position });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void ReturnFlag()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("OnChangeFlagState", RpcTarget.AllBuffered, FlagState.InHome);
        }
    }

    #region PUN RPC
    [PunRPC]
    void OnDropFlag(Vector3 position)
    {
        State = FlagState.Dropped;
        m_CarryingPlayer = null;
        transform.position = position;
        m_ReturnTimer = ReturnTime;
    }

    [PunRPC]
    public void OnChangeFlagState(FlagState newState, PhotonMessageInfo info)
    {
        State = newState;
        switch (newState)
        {
            case FlagState.Captured:
                OnCapture(info.Sender);
                break;
            case FlagState.PickUp:
                OnPickup(info.Sender);
                break;
            case FlagState.InHome:
                OnReturn();
                break;
        }
    }
    #endregion


    /// <summary>
    /// 
    /// </summary>
    void OnCapture(Player m_actor)
    {
        m_CarryingPlayer = null;
        transform.position = m_HomePosition;

        //Only the player who captures the flag, updates the properties
        if (PhotonNetwork.LocalPlayer == m_actor)
        {
            bl_KillFeed.Instance.SendTeamHighlightMessage(PhotonNetwork.NickName, bl_GameTexts.CaptureTheFlag, m_actor.GetPlayerTeam());
            OnLocalCaptureOne();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnReturn()
    {
        transform.position = m_HomePosition;
        if (PhotonNetwork.IsMasterClient)
        {
            //just remove in buffer RPC's cuz the flag return to the initial state so it doesn't have any difference for new players
            PhotonNetwork.RemoveRPCs(photonView);
        }
    }

    public void OnPickup(Player actor)
    {
        Transform actorTransform = bl_GameManager.Instance.FindActor(actor);
        if (actorTransform != null)
        {
            bl_PlayerSettings logic = actorTransform.GetComponent<bl_PlayerSettings>();
            if (CanBePickedUpBy(logic) == true)
            {
                if (logic.PlayerTeam == Team)
                {
                    if (IsHome() == false)
                    {
                        ReturnFlag();
                    }
                }
                else
                {
                    m_CarryingPlayer = logic;
                }
                //show capture notification
                if (PhotonNetwork.LocalPlayer.ActorNumber == actor.ActorNumber)
                {
                    Team enemyTeam = bl_CaptureOfFlag.GetOppositeTeam(PhotonNetwork.LocalPlayer.GetPlayerTeam());
                    string obtainedText = string.Format(bl_GameTexts.ObtainedFlag, enemyTeam.GetTeamName());
                    bl_KillFeed.Instance.SendTeamHighlightMessage(PhotonNetwork.NickName, obtainedText, PhotonNetwork.LocalPlayer.GetPlayerTeam());
                }
            }
        }

    }
    /// <summary>
    /// 
    /// </summary>
    void OnLocalCaptureOne()
    {
        bl_GameManager.Instance.SetPointFromLocalPlayer(1, GameMode.CTF);

        //Add Point for personal score
        PhotonNetwork.LocalPlayer.PostScore(bl_CaptureOfFlag.Instance.ScorePerCapture);
    }

    public bool CanBePickedUpBy(bl_PlayerSettings logic)
    {
        //If the flag is at its home position, only the enemy team can grab it
        if (IsHome() == true)
        {
            return logic.PlayerTeam != Team;
        }

        //If another player is already carrying the flag, no one else can grab it
        if (m_CarryingPlayer != null)
        {
            return false;
        }

        return true;
    }

    #region GUI
    void OnGUI()
    {
        GUI.color = IconColor;
        if (bl_GameManager.Instance.CameraRendered)
        {
            Vector3 vector = bl_GameManager.Instance.CameraRendered.WorldToScreenPoint(this.IconTarget.position);
            if (vector.z > 0)
            {
                GUI.DrawTexture(new Rect(vector.x - 5, (Screen.height - vector.y) - 7, 13 + IconSize.x, 13 + IconSize.y), this.FlagIcon);
            }
        }
        GUI.color = Color.white;
    }

    private SphereCollider SpheCollider;
    private void OnDrawGizmos()
    {
        if (SpheCollider != null)
        {
            Vector3 v = SpheCollider.bounds.center;
            v.y = transform.position.y;
            bl_UtilityHelper.DrawWireArc(v, SpheCollider.radius * transform.lossyScale.x, 360, 20, Quaternion.identity);
        }
        else
        {
            SpheCollider = GetComponent<SphereCollider>();
        }
    }
    #endregion

    [System.Serializable]
    public enum FlagState
    {
        InHome = 0,
        PickUp = 1,
        Captured = 2,
        Dropped = 3,
    }
}