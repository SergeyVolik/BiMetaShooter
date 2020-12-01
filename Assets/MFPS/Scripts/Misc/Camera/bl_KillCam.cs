using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;

public class bl_KillCam : bl_MonoBehaviour
{

    /// <summary>
    /// Target to follow
    /// </summary>
    public Transform target = null;
    /// <summary>
    /// Distance from camera to target
    /// </summary>
	public float distance = 10.0f;
    /// <summary>
    /// Maxime Distance to target
    /// </summary>
    public float distanceMax = 15f;
    /// <summary>
    /// Min Distance to target
    /// </summary>
	public float distanceMin = 0.5f;
    /// <summary>
    /// X vector speed
    /// </summary>
	public float xSpeed = 120f;
    /// <summary>
    /// maxime y vector Limit
    /// </summary>
	public float yMaxLimit = 80f;
    /// <summary>
    /// minime Y vector limit
    /// </summary>
	public float yMinLimit = -20f;
    /// <summary>
    /// Y vector speed
    /// </summary>
	public float ySpeed = 120f;
    public LayerMask layers;

    float x = 0;
    float y = 0;
    private int CurrentTarget = 0;
    private bl_GameManager Manager;
    private bool canManipulate = false;
    private KillCameraType cameraType = KillCameraType.ObserveDeath;

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        transform.parent = null;
        Manager = bl_GameManager.Instance;
        cameraType = bl_GameData.Instance.killCameraType;
        if (target != null)
        {
            transform.LookAt(target);
            StartCoroutine(ZoomOut());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (target != null)
        {
            Orbit();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void ChangeTarget(bool next)
    {
        if (Manager.OthersActorsInScene.Count <= 0)
            return;

        if (next) { CurrentTarget = (CurrentTarget + 1) % Manager.OthersActorsInScene.Count; }
        else
        {
            if (CurrentTarget > 0) { CurrentTarget--; } else { CurrentTarget = Manager.OthersActorsInScene.Count - 1; }
        }
        target = Manager.OthersActorsInScene[CurrentTarget].Actor;
    }

    /// <summary>
    /// update camera movements
    /// </summary>
    void Orbit()
    {
        if (!canManipulate || cameraType != KillCameraType.OrbitTarget)
            return;

        if (target != null)
        {
            x += ((Input.GetAxis("Mouse X") * this.xSpeed) * this.distance) * 0.02f;
            y -= (Input.GetAxis("Mouse Y") * this.ySpeed) * 0.02f;
            y = bl_UtilityHelper.ClampAngle(this.y, this.yMinLimit, this.yMaxLimit);
            Quaternion quaternion = Quaternion.Euler(this.y, this.x, 0f);
            this.distance = Mathf.Clamp(this.distance - (Input.GetAxis("Mouse ScrollWheel") * 5f), distanceMin, distanceMax);

            Vector3 vector = new Vector3(0f, 0f, -distance);
            Vector3 vector2 = target.position;
            vector2.y = target.position.y + 1f;
            Vector3 vector3 = (quaternion * vector) + vector2;
            transform.rotation = quaternion;
            transform.position = vector3;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="view"></param>
    public void SetTarget(Player view, DamageCause cause, string killer, Transform secureTarget = null)
    {
        if (secureTarget != null) { target = secureTarget; }
        GameObject v = null;
        if (cause == DamageCause.Bot)
        {
            v = GameObject.Find(killer);
        }
        else
        {
            Transform g = bl_GameManager.Instance.FindActor(view);
            if (g != null)
                v = g.gameObject;
        }
        if (v != null && (view.NickName != PhotonNetwork.NickName || cause == DamageCause.Bot))
        {
            target = v.transform;
            canManipulate = true;
        }
    }

    public void SpectPlayer(Transform player)
    {
        target = player;
        canManipulate = true;
    }

    /// <summary>
    /// 
    /// </summary>
    public void PositionedAndLookAt(Transform reference)
    {
        float distanceFromLocal = 2.5f;
        Vector3 position = reference.position - new Vector3(0, 0.6f, 0);
        transform.position = position - (reference.forward * distanceFromLocal);
        RaycastHit rayHit;
        if(Physics.Raycast(reference.position,-reference.forward, out rayHit, distanceFromLocal, layers, QueryTriggerInteraction.Ignore))
        {
            transform.position = rayHit.point;
        }
        transform.LookAt(position);
    }

    IEnumerator ZoomOut()
    {
        float d = 0;
        Vector3 next = target.position + transform.TransformDirection(new Vector3(0, 0, -3));
        Vector3 origin = target.position;
        transform.position = target.position;
        while (d < 1)
        {
            d += Time.deltaTime;
            transform.position = Vector3.Lerp(origin, next, d);
            transform.LookAt(target);
            yield return null;
        }
    }

    public enum KillCameraType
    {
        OrbitTarget,
        ObserveDeath,
    }
}