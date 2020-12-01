using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Serialization;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
public class bl_PlayerNetwork : bl_MonoBehaviour, IPunObservable
{
    /// <summary>
    /// the player's team is not ours
    /// </summary>
    public Team RemoteTeam { get; set; }

    /// <summary>
    /// the current state of the local player in FPV
    /// </summary>
    public PlayerFPState FPState = PlayerFPState.Idle;
    /// <summary>
    /// the object to which the player looked
    /// </summary>
    [FormerlySerializedAs("HeatTarget")]
    public Transform HeadTarget;
    /// <summary>
    /// smooth interpolation amount
    /// </summary>
    public float SmoothingDelay = 8f;
    /// <summary>
    /// list all remote weapons
    /// </summary>
    public List<bl_NetworkGun> NetworkGuns = new List<bl_NetworkGun>();

    [SerializeField]
    PhotonTransformViewPositionModel m_PositionModel = new PhotonTransformViewPositionModel();

    [SerializeField]
    PhotonTransformViewRotationModel m_RotationModel = new PhotonTransformViewRotationModel();

    PhotonTransformViewPositionControl m_PositionControl;
    PhotonTransformViewRotationControl m_RotationControl;

    bool m_ReceivedNetworkUpdate = false;
    [Space(5)]
    //Script Needed
    [Header("Necessary script")]
    public bl_GunManager gunManager;
    public bl_PlayerAnimations m_PlayerAnimation;
    //Material for apply when disable a NetGun
    public Material InvicibleMat;
    //private
    private bl_FirstPersonController Controller;
    public bl_NetworkGun CurrenGun { get; set; }
    private bl_PlayerHealthManager PDM;
    private bl_NamePlateDrawer DrawName;
    private bool FrienlyFire = false;
    private bool SendInfo = false;
    public bool isFriend { get; set; }
    private CharacterController m_CController;
    private bool isWeaponBlocked = false;
#if UMM
     private bl_MiniMapItem MiniMapItem = null;
#endif

#pragma warning disable 0414
    [SerializeField]
    bool ObservedComponentsFoldoutOpen = true;
#pragma warning disable 0414
    private bl_NetworkGun currentGun;
    public int currentGunID { get; set; } = -1;

    protected override void Awake()
    {
        base.Awake();
        bl_PhotonCallbacks.PlayerEnteredRoom += OnPhotonPlayerConnected;
        if (!PhotonNetwork.IsConnected)
            Destroy(this);
        if (!PhotonNetwork.InRoom)
            return;

        //FirstUpdate = false;
        if (!isMine)
        {
            if (HeadTarget.gameObject.activeSelf == false)
            {
                HeadTarget.gameObject.SetActive(true);
            }
        }

        m_PositionControl = new PhotonTransformViewPositionControl(m_PositionModel);
        m_RotationControl = new PhotonTransformViewRotationControl(m_RotationModel);
        Controller = GetComponent<bl_FirstPersonController>();
        PDM = GetComponent<bl_PlayerHealthManager>();
        DrawName = GetComponent<bl_NamePlateDrawer>();
        m_CController = GetComponent<CharacterController>();
        FrienlyFire = (bool)PhotonNetwork.CurrentRoom.CustomProperties[PropertiesKeys.RoomFriendlyFire];
#if UMM
        MiniMapItem = this.GetComponent<bl_MiniMapItem>();
        if (isMine && MiniMapItem != null)
        {
            MiniMapItem.enabled = false;
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    private void Start()
    {
        InvokeRepeating("SlowLoop", 0, 1);
    }

    /// <summary>
    /// serialization method of photon
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

        m_PositionControl.OnPhotonSerializeView(transform.localPosition, stream, info);
        m_RotationControl.OnPhotonSerializeView(transform.localRotation, stream, info);
        if (isMine == false)
        {
            DoDrawEstimatedPositionError();
        }
        if (stream.IsWriting)
        {
            //We own this player: send the others our data
            stream.SendNext(HeadTarget.position);
            stream.SendNext(HeadTarget.rotation);
            stream.SendNext((int)Controller.State);
            stream.SendNext(Controller.isGrounded);
            stream.SendNext(gunManager.GetCurrentGunID);
            stream.SendNext((int)FPState);
            stream.SendNext(Controller.Velocity);
        }
        else
        {
            //Network player, receive data
            HeadPos = (Vector3)stream.ReceiveNext();
            HeadRot = (Quaternion)stream.ReceiveNext();
            networkBodyState = (int)stream.ReceiveNext();
            networkIsCrouching = (bool)stream.ReceiveNext();
            networkGunID = (int)stream.ReceiveNext();
            FPState = (PlayerFPState)((int)stream.ReceiveNext());
            velocity = (Vector3)stream.ReceiveNext();

            m_ReceivedNetworkUpdate = true;
        }
    }

    private Vector3 HeadPos = Vector3.zero;// Head Look to
    private Quaternion HeadRot = Quaternion.identity;
    private int networkBodyState;
    private bool networkIsCrouching;
    private string RemotePlayerName = string.Empty;
    private int networkGunID;
    private Vector3 velocity;

    protected override void OnDisable()
    {
        base.OnDisable();
        if (bl_GameData.Instance.DropGunOnDeath)
        {
            NetGunsRoot.gameObject.SetActive(false);
        }
        bl_PhotonCallbacks.PlayerEnteredRoom -= OnPhotonPlayerConnected;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        ///if the player is not ours, then
        if (isConnected == false || !PhotonNetwork.InRoom) return;

        if (photonView != null && isMine == false)
        {
            UpdatePosition();
            UpdateRotation();

            HeadTarget.position = Vector3.Lerp(HeadTarget.position, HeadPos, Time.smoothDeltaTime * m_PositionModel.InterpolateLerpSpeed);
            HeadTarget.rotation = HeadRot;
            m_PlayerAnimation.BodyState = (PlayerState)networkBodyState;//send the state of player local for remote animation
            m_PlayerAnimation.grounded = networkIsCrouching;
            m_PlayerAnimation.velocity = velocity;
            m_PlayerAnimation.FPState = FPState;


            if (!isOneTeamMode)
            {
                //Determine if remote player is teamMate or enemy
                if (isFriend)
                {
                    TeamMate();
                }
                else
                {
                    Enemy();
                }
            }
            else
            {
                Enemy();
            }

            if (!isWeaponBlocked)
            {
                CurrentTPVGun();
                currentGunID = networkGunID;
            }
        }
        else
        {
            m_PlayerAnimation.BodyState = Controller.State;//send the state of player local for remote animation
            m_PlayerAnimation.grounded = Controller.isGrounded;
            m_PlayerAnimation.velocity = Controller.Velocity;
            m_PlayerAnimation.FPState = FPState;
        }
    }

    void CurrentTPVGun(bool local = false)
    {
        if (gunManager == null)
            return;
        if (networkGunID == currentGunID) return;

        //Get the current gun ID local and sync with remote
        for(int i = 0; i < NetworkGuns.Count; i++)
        {
            currentGun = NetworkGuns[i];
            if (currentGun == null) continue;

            int currentID = (local) ? gunManager.GetCurrentWeapon().GunID : networkGunID;
            if (currentGun.GetWeaponID == currentID)
            {
                currentGun.gameObject.SetActive(true);
                if (!local)
                {
                    CurrenGun = currentGun.gameObject.GetComponent<bl_NetworkGun>();
                    CurrenGun.SetUpType();
                }
            }
            else
            {
                if(currentGun != null)
                    currentGun.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// use this function to set all details for enemy
    /// </summary>
    void Enemy()
    {
        PDM.DamageEnabled = true;
        DrawName.enabled = bl_RoomMenu.Instance.SpectatorMode;
#if UMM
        if (bl_MiniMapData.Instance.showEnemysWhenFire)
        {
#if KSA
            if (bl_KillStreakHandler.Instance.activeAUVs > 0) return;
#endif
            if (FPState == PlayerFPState.Firing || FPState == PlayerFPState.FireAiming)
            {
                MiniMapItem?.ShowItem();
            }
            else
            {
                MiniMapItem?.HideItem();
            }
        }
#endif
    }

    /// <summary>
    /// use this function to set all details for teammate
    /// </summary>
    void TeamMate()
    {
        PDM.DamageEnabled = FrienlyFire;
        DrawName.enabled = true;
        m_CController.enabled = false;

        if (!SendInfo)
        {
            SendInfo = true;
            GetComponentInChildren<bl_BodyPartManager>().IgnorePlayerCollider();
        }

#if UMM
   MiniMapItem?.ShowItem();
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    void SlowLoop()
    {
        if (photonView == null || photonView.Owner == null) return;
        RemotePlayerName = photonView.Owner.NickName;
        RemoteTeam = photonView.Owner.GetPlayerTeam();
        gameObject.name = RemotePlayerName;
        if (DrawName != null) { DrawName.SetName(RemotePlayerName); }
        isFriend = (RemoteTeam == PhotonNetwork.LocalPlayer.GetPlayerTeam());
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetNetworkWeapon(GunType weaponType, bl_NetworkGun networkGun)
    {
        m_PlayerAnimation?.SetNetworkWeapon(weaponType, networkGun);
    }

    /// <summary>
    /// public method to send the RPC shot synchronization
    /// </summary>
    public void IsFire(GunType m_type, Vector3 hitPosition)
    {
        photonView.RPC("FireSync", RpcTarget.Others, new object[] { (int)m_type, hitPosition });
    }

    /// <summary>
    /// public method to send the RPC shot synchronization
    /// </summary>
    public void IsFireGrenade(float t_spread, Vector3 pos, Quaternion rot, Vector3 angular)
    {
        photonView.RPC("FireGrenadeRpc", RpcTarget.Others, new object[] { t_spread, pos, rot, angular });
    }

    public Transform NetGunsRoot
    {
        get { if (!bl_GameData.Instance.DropGunOnDeath) { CurrentTPVGun(true); } return NetworkGuns[0].transform.parent; }
    }

    /// <summary>
    /// Synchronize the shot with the current remote weapon
    /// send the information necessary so that fire
    /// impact in the same direction as the local
    /// </summary>
    [PunRPC]
    void FireSync(int m_type, Vector3 hitPosition)
    {
        if (CurrenGun == null) return;
        GunType t = (GunType)(m_type);
        switch (t)
        {
            case GunType.Machinegun:
                CurrenGun.Fire(hitPosition);
                m_PlayerAnimation.PlayFireAnimation(GunType.Machinegun);
                break;
            case GunType.Pistol:
                CurrenGun.Fire(hitPosition);
                m_PlayerAnimation.PlayFireAnimation(GunType.Pistol);
                break;
            case GunType.Burst:
            case GunType.Sniper:
            case GunType.Shotgun:
                CurrenGun.Fire(hitPosition);
                break;
            case GunType.Knife:
                CurrenGun.KnifeFire();//if you need add your custom fire launcher in networkgun
                m_PlayerAnimation.PlayFireAnimation(GunType.Knife);
                break;
            default:
                Debug.LogWarning("Not defined weapon type to sync bullets.");
                break;
        }
    }

    public void SyncCustomProjectile(Hashtable data) => photonView.RPC(nameof(RPCFireCustom), RpcTarget.Others, data);

    [PunRPC]
    void RPCFireCustom(Hashtable data)
    {
        CurrenGun?.FireCustomLogic(data);
        if(CurrenGun != null)
        m_PlayerAnimation.PlayFireAnimation(CurrenGun.Info.Type);
    }

    [PunRPC]
    void FireGrenadeRpc(float m_spread, Vector3 pos, Quaternion rot, Vector3 angular)
    {
        CurrenGun.GetComponent<bl_NetworkGun>().GrenadeFire(m_spread, pos, rot, angular);
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetActiveGrenade(bool active)
    {
        photonView.RPC("SyncOffAmmoGrenade", RpcTarget.Others, active);
    }

    [PunRPC]
    void SyncOffAmmoGrenade(bool active)
    {
        if (CurrenGun == null)
        {
            Debug.LogError("Grenade is not active on TPS Player");
            return;
        }
        CurrenGun.GetComponent<bl_NetworkGun>().DesactiveGrenade(active, InvicibleMat);
    }

    public void SetWeaponBlocked(bool isBlocked)
    {
        isWeaponBlocked = isBlocked;
        photonView.RPC("RPCSetWBlocked", RpcTarget.Others, isBlocked);
    }

    [PunRPC]
    public void RPCSetWBlocked(bool isBlocked)
    {
        isWeaponBlocked = isBlocked;
        if (isWeaponBlocked)
        {
            for (int i = 0; i < NetworkGuns.Count; i++)
            {
                NetworkGuns[i].gameObject.SetActive(false);
            }
        }
        m_PlayerAnimation.OnWeaponBlock(isBlocked);
        currentGunID = -1;
    }

#if CUSTOMIZER
    [PunRPC]
    void SyncCustomizer(int weaponID, string line, PhotonMessageInfo info)
    {
        if (photonView.ViewID != info.photonView.ViewID) return;

        bl_NetworkGun ng = NetworkGuns.Find(x => x.LocalGun.GunID == weaponID);
        if(ng != null)
        {
            if(ng.GetComponent<bl_CustomizerWeapon>() != null)
            {
                ng.GetComponent<bl_CustomizerWeapon>().ApplyAttachments(line);
            }
            else
            {
                Debug.LogWarning("You have not setup the attachments in the TPWeapon: " + weaponID);
            }
        }
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    void UpdatePosition()
    {
        if (m_PositionModel.SynchronizeEnabled == false || m_ReceivedNetworkUpdate == false)
        {
            return;
        }

        transform.localPosition = m_PositionControl.UpdatePosition(transform.localPosition);
    }
    /// <summary>
    /// 
    /// </summary>
    void UpdateRotation()
    {
        if (m_RotationModel.SynchronizeEnabled == false || m_ReceivedNetworkUpdate == false)
        {
            return;
        }

        transform.localRotation = m_RotationControl.GetRotation(transform.localRotation);
    }

    /// <summary>
    /// 
    /// </summary>
    void DoDrawEstimatedPositionError()
    {
        Vector3 targetPosition = m_PositionControl.GetNetworkPosition();

        Debug.DrawLine(targetPosition, transform.position, Color.red, 2f);
        Debug.DrawLine(transform.position, transform.position + Vector3.up, Color.green, 2f);
        Debug.DrawLine(targetPosition, targetPosition + Vector3.up, Color.red, 2f);
    }
    /// <summary>
    /// These values are synchronized to the remote objects if the interpolation mode
    /// or the extrapolation mode SynchronizeValues is used. Your movement script should pass on
    /// the current speed (in units/second) and turning speed (in angles/second) so the remote
    /// object can use them to predict the objects movement.
    /// </summary>
    /// <param name="speed">The current movement vector of the object in units/second.</param>
    /// <param name="turnSpeed">The current turn speed of the object in angles/second.</param>
    public void SetSynchronizedValues(Vector3 speed, float turnSpeed)
    {
        m_PositionControl.SetSynchronizedValues(speed, turnSpeed);
    }

    public void OnPhotonPlayerConnected(Player newPlayer)
    {
        if (photonView.IsMine)
        {
            photonView.RPC("RPCSetWBlocked", RpcTarget.Others, isWeaponBlocked);
        }
    }
}