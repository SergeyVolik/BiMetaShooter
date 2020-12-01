using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bl_AIHitBox : MonoBehaviour, IMFPSDamageable
{

    public bl_AIShooterHealth AI;
    public Collider m_Collider;
    public bool isHead = false;
    public int ID = 0;

    public void DoDamage(int damage, string wn, Vector3 direction, int viewID, bool fromBot, Team team)
    {
        if (AI == null)
            return;

        if (isHead) { damage = 100; }
        AI.DoDamage(damage, wn, direction, viewID, fromBot, team, isHead, ID);
    }

    public void ReceiveDamage(DamageData damageData)
    {
        if (AI == null)
            return;

        if (isHead) { damageData.Damage = 100; }
        string weaponName = bl_GameData.Instance.GetWeapon(damageData.GunID).Name;
        //if was not killed by a listed weapon
        if(damageData.GunID < 0)
        {
            //try to find the custom weapon name
            var iconData = bl_KillFeed.Instance.GetCustomIconByIndex(Mathf.Abs(damageData.GunID));
            if (iconData != null)
            {
                weaponName = $"cmd:{iconData.Name}";
            }
        }
        AI.DoDamage(damageData.Damage, weaponName, damageData.Direction, damageData.ActorViewID, !damageData.MFPSActor.isRealPlayer, damageData.MFPSActor.Team, isHead, ID);
    }
}