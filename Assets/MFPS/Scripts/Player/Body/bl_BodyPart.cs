using UnityEngine;
using System;

public class bl_BodyPart : MonoBehaviour, IMFPSDamageable {

    public int HitBoxIdentifier = 0;
    public bl_PlayerHealthManager HealtScript;
    public bl_BodyPartManager BodyManager;

    /// <summary>
    /// Use this for receive damage local and sync for all other
    /// </summary>
    public void ReceiveDamage(DamageData damageData)
    {
        damageData.Damage = Mathf.FloorToInt(damageData.Damage * HitBox.DamageMultiplier);
        damageData.isHeadShot = HitBox.Bone == HumanBodyBones.Head;
        if(HealtScript != null)
        HealtScript?.GetDamage(damageData);
    }

    // [Obsolete("This method is deprecated use the IMFPSDamageable implementation 'ReceiveDamage' instead.")]
    public void GetDamage(float damage, string t_from, DamageCause cause, Vector3 direction, int weapon_ID = 0)
    {
        int m_TotalDamage = Mathf.FloorToInt(damage * HitBox.DamageMultiplier);

        DamageData e = new DamageData();
        e.Damage = m_TotalDamage;
        e.Direction = direction;
        e.Cause = cause;
        e.isHeadShot = HitBox.Bone == HumanBodyBones.Head;
        e.GunID = weapon_ID;
        e.From = t_from;

        if (HealtScript != null)
        {
            HealtScript.GetDamage(e);
        }
    }

    public BodyHitBox HitBox
    {
        get
        {
            return BodyManager.GetHitBox(HitBoxIdentifier);
        }
    }
}

[System.Serializable]
public class BodyHitBox
{
    public string Name;
    public HumanBodyBones Bone;
    [Range(0.5f,10)] public float DamageMultiplier = 1.0f;
    [Header("References")]
    public Collider collider;
}