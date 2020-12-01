/////////////////////////////////////////////////////////////////////////////////
//////////////////// bl_EventHandler.cs/////////////////////////////////////////
////////////////////Use this to create new internal events///////////////////////
//this helps to improve the communication of the script through delegated events/
////////////////////////////////Lovatto Studio////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System;

public class bl_EventHandler
{
    //Call all script when Fall Events
    public delegate void FallEvent(float m_amount);
    public static FallEvent OnFall;

    //Call all script when Kill Events
    public delegate void KillFeedEvent(string kl, string kd, string hw, string m_team, int gid, int hs);
    public static KillFeedEvent onKillFeed;
    //Item EventPick Up
    public delegate void ItemsPickUpEvent(int Amount);
    public static ItemsPickUpEvent onPickUpItem;
    //Call new Kit Air 
    public delegate void KitAir(Vector3 m_position, int type);
    public static KitAir onAirKit;
    //Pick Up Ammo
    public delegate void AmmoKit(int bullets, int Clips, int projectiles);
    public static AmmoKit OnKitAmmo;
    //On Kill Event
    public delegate void LocalKillEvent(KillInfo killInfo);
    public static LocalKillEvent onLocalKill;
    //On Round End
    public delegate void RoundEnd();
    public static RoundEnd OnRoundEnd;
    //small impact
    public delegate void SmallImpact();
    public static SmallImpact OnSmallImpact;
    //Receive Damage
    public delegate void GetDamage(DamageData e);
    public static GetDamage OnDamage;
    //When Local Player Death
    public delegate void LocalPlayerDeath();
    public static LocalPlayerDeath onLocalPlayerDeath;
    //When Local Player is Instantiate
    public delegate void LocalPlayerSpawn();
    public static LocalPlayerSpawn onLocalPlayerSpawn;
    //When Local Player is Instantiate
    public delegate void LocalPlayerShakeEvent(ShakerPresent present, string key, float influence = 1);
    public static LocalPlayerShakeEvent onLocalPlayerShake;

    public delegate void EffectChange(bool chrab,bool anti,bool bloom, bool ssao, bool motionb);
    public static EffectChange OnEffectChange;

    public delegate void PickUpWeapon(GunPickUpData e);
    public static PickUpWeapon OnPickUpGun;

    public delegate void ChangeWeapon(int GunID);
    public static ChangeWeapon OnChangeWeapon;

    public static Action<string, MFPSPlayer, bool> RemoteActorsChange;

    public static Action onMatchStart;
    public static void CallOnMatchStart() { if (onMatchStart != null) { onMatchStart.Invoke(); } }

    /// <summary>
    /// Called when the LOCAL player change of weapon
    /// </summary>
    public static void ChangeWeaponEvent(int GunID)
    {
        if (OnChangeWeapon != null)
            OnChangeWeapon(GunID);
    }

    /// <summary>
    /// Called event when recive Fall Impact
    /// </summary>
    public static void EventFall(float m_amount)
    {
        if (OnFall != null)
            OnFall(m_amount);
    }

    /// <summary>
    /// Event Called when receive a new kill feed message
    /// </summary>
   /* public static void KillEvent(string kl, string kd, string hw, string t_team, int gid, int hs)
    {
        if (onKillFeed != null)
            onKillFeed(kl, kd, hw, t_team, gid, hs);
    }*/

    /// <summary>
    /// Called event when pick up a med kit
    /// </summary>
    public static void PickUpEvent(int t_amount)
    {
        if (onPickUpItem != null)
            onPickUpItem(t_amount);
    }

    /// <summary>
    /// Called event when call a new kit 
    /// </summary>
    public static void KitAirEvent(Vector3 t_position, int type)
    {
        if (onAirKit != null)
            onAirKit(t_position, type);
    }

    /// <summary>
    /// Called Event when pick up ammo
    /// </summary>
    public static void OnAmmo(int bullets, int clips, int projectiles)
    {
        if (OnKitAmmo != null)
            OnKitAmmo(bullets, clips, projectiles);
    }

    /// <summary>
    /// Called this when killed a new player
    /// </summary>
    public static void FireLocalKillEvent(KillInfo killInfo)
    {
        if (onLocalKill != null)
            onLocalKill(killInfo);
    }

    /// <summary>
    /// Call This when room is finish a round
    /// </summary>
    public static void OnRoundEndEvent()
    {
        if (OnRoundEnd != null)
            OnRoundEnd();
    }
    /// <summary>
    /// 
    /// </summary>
    public static void OnSmallImpactEvent()
    {
        if (OnSmallImpact != null)
            OnSmallImpact();
    }

    /// <summary>
    /// 
    /// </summary>
    public static void LocalGetDamageEvent(DamageData e)
    {
        if (OnDamage != null)
            OnDamage(e);
    }

    /// <summary>
    /// 
    /// </summary>
    public static void PlayerLocalDeathEvent()
    {
        if (onLocalPlayerDeath != null)
        {
            onLocalPlayerDeath();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static void PlayerLocalSpawnEvent()
    {
        if (onLocalPlayerSpawn != null)
        {
            onLocalPlayerSpawn();
        }
    }

    public static void DoPlayerCameraShake(ShakerPresent present, string key, float influence = 1)
    {
        if (onLocalPlayerShake != null)
        {
            onLocalPlayerShake(present, key, influence);
        }
    }

    public static void PlayerLocalSpawnEvent(bool chrab, bool anti, bool bloom, bool ssao, bool motionBlur)
    {
        if (OnEffectChange != null)
        {
            OnEffectChange(chrab, anti, bloom, ssao, motionBlur);
        }
    }

    public static void PickUpGunEvent(GunPickUpData e)
    {
        if (OnPickUpGun != null)
            OnPickUpGun(e);
    }

    public static void OnRemoteActorChange(string actorName, MFPSPlayer playerData, bool spawning)
    {
        if(RemoteActorsChange != null)
        {
            RemoteActorsChange.Invoke(actorName, playerData, spawning);
        }
    }

    public static void SetEffectChange(bool chrab, bool anti, bool bloom, bool ssao, bool motionb)
    {
        if (OnEffectChange != null)
            OnEffectChange(chrab, anti, bloom, ssao, motionb);
    }
}