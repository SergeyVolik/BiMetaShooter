using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(AudioSource))]
public class bl_Bullet : bl_MonoBehaviour
{
    private int hitCount = 0;         // hit counter for counting bullet impacts for bullet penetration
    [HideInInspector]
    public string GunName = ""; //Weapon name
    private Vector3 velocity = Vector3.zero; // bullet velocity
    private Vector3 newPos = Vector3.zero;   // bullet's new position
    private Vector3 oldPos = Vector3.zero;   // bullet's previous location
    private bool hasHit = false;             // has the bullet hit something?
    private Vector3 direction;               // direction bullet is travelling
    public LayerMask HittableLayers;

    [SerializeField] private TrailRenderer Trail;
    public string AIFrom { get; set; }
    public int ActorViewID { get; set; }
    private Team AITeam;
    private Vector3 dir = Vector3.zero;
    RaycastHit hit;
    private Transform m_Transform;
    float travelDistance = 0;
    private BulletData bulletData;

    /// <summary>
    /// 
    /// </summary>
    public void SetUp(BulletData info) // information sent from gun to bullet to change bullet properties
    {
        ResetBullet();
        m_Transform = transform;
        bulletData = info;
        float maxInaccuracy = info.MaxSpread;       // max inaccuracy of the bullet
        float variableInaccuracy = info.Spread;  // current inaccuracy... mostly for machine guns that lose accuracy over time

        ActorViewID = bl_GameManager.LocalPlayerViewID;
        // direction bullet is traveling
        direction = m_Transform.TransformDirection(Random.Range(-maxInaccuracy, maxInaccuracy) * variableInaccuracy, Random.Range(-maxInaccuracy, maxInaccuracy) * variableInaccuracy, 1);

        newPos = m_Transform.position;   // bullet's new position
        oldPos = newPos;               // bullet's old position
        velocity = info.Speed * m_Transform.forward; // bullet's velocity determined by direction and bullet speed
        if (Trail != null) { if (!bl_GameData.Instance.BulletTracer) { Destroy(Trail); } }
        // schedule for destruction if bullet never hits anything
        Invoke("Disable", 5);
    }

    /// <summary>
    /// 
    /// </summary>
    public void AISetUp(string AIname, int viewID, Team aiTeam)
    {
        AIFrom = AIname;
        ActorViewID = viewID;
        AITeam = aiTeam;
    }

    /// <summary>
    /// 
    /// </summary>
    void ResetBullet()
    {
        hasHit = false;
        AIFrom = "";
        ActorViewID = 0;
        AITeam = Team.All;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnUpdate()
    {
        if (hasHit)
            return; // if bullet has already hit its max hits... exit

        Travel();
    }

    /// <summary>
    /// 
    /// </summary>
    void Travel()
    {
        // assume we move all the way
        newPos += (velocity + direction) * Time.deltaTime;
        // Check if we hit anything on the way
        dir = newPos - oldPos;
        travelDistance = dir.magnitude;

        if (travelDistance > 0)
        {
            dir /= travelDistance;
            if (Physics.Raycast(oldPos, dir, out hit, travelDistance, HittableLayers, QueryTriggerInteraction.Ignore))
            {
                newPos = hit.point;
                OnHit(hit);
                hasHit = true;
                Disable();
            }
        }

        oldPos = m_Transform.position;  // set old position to current position
        m_Transform.position = newPos;  // set current position to the new position
    }

    /// <summary>
    /// 
    /// </summary>
    private void Disable()
    {
        AIFrom = "";
        gameObject.SetActive(false);
    }

    #region Bullet On Hits

    void OnHit(RaycastHit hit)
    {
        Ray mRay = new Ray(m_Transform.position, m_Transform.forward);
        if (!bulletData.isNetwork)
        {
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic) // if we hit a rigi body... apply a force
            {
                float mAdjust = 1.0f / (Time.timeScale * (0.02f / Time.fixedDeltaTime));
                hit.rigidbody.AddForceAtPosition(((mRay.direction * bulletData.ImpactForce) / Time.timeScale) / mAdjust, hit.point);
            }
        }
        switch (hit.transform.tag) // decide what the bullet collided with and what to do with it
        {
            case "Projectile":
                // do nothing if 2 bullets collide
                break;
            case "BodyPart"://Send Damage for other players
                SendPlayerDamage(hit);
                break;
            case "AI":
                //Bullet hit a bot collider, check if we have to apply damage.
                SendBotDamage(hit);
                break;
            case "Player":
                //A bot hit a real player
                SendPlayerDamageFromBot(hit);
                break;
            case "Wood":
                hitCount++; // add another hit to counter
                InstanceHitParticle("decalw", hit);
                break;
            case "Concrete":
                hitCount += 2; // add 2 hits to counter... concrete is hard
                InstanceHitParticle("decalc", hit);
                break;
            case "Metal":
                hitCount += 3; // metal slows bullets alot
                InstanceHitParticle("decalm", hit);
                break;
            case "Dirt":
                hasHit = true; // ground kills bullet
                InstanceHitParticle("decals", hit);
                break;
            case "Water":
                hasHit = true; // water kills bullet
                InstanceHitParticle("decalwt", hit);
                break;
            default:
                hitCount++; // add a hit
                InstanceHitParticle("decal", hit);
                break;
        }
        Disable();
    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    void SendPlayerDamage(RaycastHit hit)
    {
        if (bulletData.isNetwork) return;

        IMFPSDamageable damageable = hit.transform.GetComponent<IMFPSDamageable>();
        if(damageable != null)
        {
            DamageData damageData = BuildBaseDamageData();
            //check if the bullet comes from a bot or a real player.
            if (bulletData.MFPSActor != null)
            {
                damageData.Cause = bulletData.MFPSActor.isRealPlayer ? DamageCause.Player : DamageCause.Bot;
            }
            else
            {
                damageData.Cause = DamageCause.Player;
            }
            damageable.ReceiveDamage(damageData);
        }

        if (bl_GameData.Instance.ShowBlood)
        {
            GameObject go = bl_ObjectPooling.Instance.Instantiate("blood", hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
            go.transform.parent = hit.transform;
        }
        Disable();
    }

    /// <summary>
    /// 
    /// </summary>
    void SendBotDamage(RaycastHit hit)
    {
        //apply damage only if this is the Local Player, we could use only On Master Client to have a more "Authoritative" logic but since the
        //Ping of Master Clients can be volatile since it depend of the client connection, that could affect all the players in the room.
        if (!bulletData.isNetwork)
        {
            if (bulletData.MFPSActor == null) { Debug.LogError("MFPS actor has not been assigned"); return; }
            if (bulletData.MFPSActor.Name == hit.transform.root.name) { return; }
            IMFPSDamageable damageable = hit.transform.GetComponent<IMFPSDamageable>();

            if(damageable != null)
            {
                //callback is in bl_AIHitBox.cs
                var damageData = BuildBaseDamageData();
                damageable.ReceiveDamage(damageData);
            }
        }

        GameObject go = bl_ObjectPooling.Instance.Instantiate("blood", hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
        go.transform.parent = hit.transform;
        Disable();
    }

    /// <summary>
    /// 
    /// </summary>
    void SendPlayerDamageFromBot(RaycastHit hit)
    {
        if (bulletData.isNetwork) return;
        //Bots doesn't hit the players HitBoxes like other real players does, instead they hit the Character Controller Collider,
        //So instead of communicate with the hit box script we have to communicate with the player health script directly
        bl_PlayerHealthManager pdm = hit.transform.GetComponent<bl_PlayerHealthManager>();
        if (pdm != null && !string.IsNullOrEmpty(AIFrom))
        {
            if (!isOneTeamMode)
            {
                if (pdm.GetComponent<bl_PlayerSettings>().PlayerTeam == AITeam)//if hit a team mate player
                {
                    Disable();
                    return;
                }
            }
            DamageData info = BuildBaseDamageData();
            info.Actor = null;
            info.Cause = DamageCause.Bot;
            pdm.GetDamage(info);
            Disable();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void InstanceHitParticle(string poolPrefab, RaycastHit hit)
    {
        GameObject go = bl_ObjectPooling.Instance.Instantiate(poolPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        if(go != null)
        go.transform.parent = hit.transform;
    }

    DamageData BuildBaseDamageData()
    {
        DamageData data = new DamageData()
        {
            Damage = (int)bulletData.Damage,
            Direction = bulletData.Position,
            MFPSActor = bulletData.MFPSActor,
            ActorViewID = bulletData.ActorViewID,
            GunID = bulletData.WeaponID,
        };
        if(data.MFPSActor != null) { data.From = data.MFPSActor.Name; }
        return data;
    }
}