using UnityEngine;
using System.Collections;
using Photon.Pun;

public class bl_GunPickUp : bl_MonoBehaviour
{
    [GunID] public int GunID = 0;
    public DetectMode m_DetectMode = DetectMode.Raycast;
    [HideInInspector]
    public bool PickupOnCollide = true;
    [HideInInspector]
    public bool SentPickup = false;
    [Space(5)]
    public bool AutoDestroy = true;
    public float DestroyIn = 15f;
    //
    private bool Into = false;
    private bl_GunPickUpManager PUM;

    //Cache info
    [System.Serializable]
    public class m_Info
    {
        public int Clips = 0;
        public int Bullets = 0;
    }
    public m_Info Info;
    private bl_UIReferences UIReferences;
    private bl_GunInfo CacheGun;
    private bool Equiped = false;
    private bool isFocus = false;
    private PhotonView localPlayerIn = null;

    public bool IsFocus { set { isFocus = value; } }
    private byte uniqueLocal = 0;

    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        if (!PhotonNetwork.IsConnected) return;
        base.Awake();
        PUM = FindObjectOfType<bl_GunPickUpManager>();
        UIReferences = bl_UIReferences.Instance;
        CacheGun = bl_GameData.Instance.GetWeapon(GunID);
        uniqueLocal = (byte)Random.Range(0, 9998);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator Start()
    {
        if (AutoDestroy)
        {
            Destroy(gameObject, DestroyIn);
        }
        PickupOnCollide = false;
        yield return new WaitForSeconds(2f);
        PickupOnCollide = true;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Into)
        {
            if (UIReferences != null)
            {
                UIReferences.SetPickUp(false);
            }
            if (localPlayerIn != null)
            {
                if (m_DetectMode == DetectMode.Raycast)
                {
                    bl_CameraRay cr = localPlayerIn.GetComponentInChildren<bl_CameraRay>();
                    if (cr != null)
                    {
                        cr.SetActiver(false, uniqueLocal);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="c"></param>
    void OnTriggerEnter(Collider c)
    {
        if (bl_GameManager.Instance.GameMatchState == MatchState.Waiting)
            return;
#if GR
        if(GetGameMode == GameMode.GR)
        {
            return;
        }
#endif
        // we only call Pickup() if "our" character collides with this PickupItem.
        // note: if you "position" remote characters by setting their translation, triggers won't be hit.

        PhotonView v = c.GetComponent<PhotonView>();
        if (PickupOnCollide && v != null && v.IsMine && c.CompareTag(bl_PlayerSettings.LocalTag))
        {
            Into = true;
            PUM.LastTrigger = this;
            localPlayerIn = v;
            if (CacheGun.Type == GunType.Knife)
            {
                if (v.GetComponentInChildren<bl_GunManager>(true).PlayerEquip.Exists(x => x != null && x.GunID == GunID))
                {
                    Equiped = true;
                }
                else
                {
                    Equiped = false;
                }
            }
            if (m_DetectMode == DetectMode.Raycast)
            {
                bl_CameraRay cr = v.GetComponentInChildren<bl_CameraRay>();
                if (cr != null)
                {
                    cr.SetActiver(true, uniqueLocal);
                }
            }
            else if (m_DetectMode == DetectMode.Trigger)
            {
                UIReferences.SetPickUp(true, GunID, this, Equiped);
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    void OnTriggerExit(Collider c)
    {
        if (c.transform.CompareTag(bl_PlayerSettings.LocalTag) && Into)
        {
            Into = false;
            UIReferences.SetPickUp(false);
            if (localPlayerIn != null)
            {
                if (m_DetectMode == DetectMode.Raycast)
                {
                    bl_CameraRay cr = localPlayerIn.GetComponentInChildren<bl_CameraRay>();
                    if (cr != null)
                    {
                        cr.SetActiver(false, uniqueLocal);
                    }
                }
                localPlayerIn = null;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (m_DetectMode == DetectMode.Trigger)
        {
            if (!Into || Equiped) return;
#if !INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.E) && PUM.LastTrigger == this)
            {
                Pickup();
            }
#else
            if (bl_Input.isButtonDown("Interact") && PUM.LastTrigger == this)
            {
                Pickup();
            }
#endif
        }
        else if (m_DetectMode == DetectMode.Raycast)
        {
            if (!Into || !isFocus || Equiped) return;
#if !INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.E))
            {
                Pickup();
            }
#else
            if (bl_Input.isButtonDown("Interact"))
            {
                Pickup();
            }
#endif
        }      
    }

    /// <summary>
    /// 
    /// </summary>
    public void Pickup()
    {
        if (SentPickup)
            return;
#if GR
        if (GetGameMode == GameMode.GR)
        {
            return;
        }
#endif

        SentPickup = true;
        PUM.SendPickUp(gameObject.name, GunID, Info);
        SentPickup = false;
        UIReferences.SetPickUp(false);
    }

    public void FocusThis(bool focus)
    {
        isFocus = focus;
        UIReferences.SetPickUp(focus, GunID, this, Equiped);
    }

    private SphereCollider SpheCollider;
    private void OnDrawGizmos()
    {
        if (SpheCollider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 1f);
            bl_UtilityHelper.DrawWireArc(SpheCollider.bounds.center, SpheCollider.radius * transform.lossyScale.x, 360, 20, Quaternion.identity);
        }
        else
        {
            SpheCollider = GetComponent<SphereCollider>();
        }
    }

    [System.Serializable]
    public enum DetectMode
    {
        Raycast,
        Trigger,
    }
}