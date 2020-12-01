using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bl_GrenadeLauncher : MonoBehaviour, IWeapon
{
    public float explosionRadious = 10;
    private bl_Gun FPWeapon;
    private bl_NetworkGun TPWeapon;

    public void Initialitate(bl_Gun gun)
    {
        FPWeapon = gun;
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnFire()
    {
      
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnFireDown()
    {
        if (!FPWeapon.FireRatePassed) return;

        FPWeapon.nextFireTime = Time.time;
        Shoot();
    }

    /// <summary>
    /// 
    /// </summary>
    public void TPFire(bl_NetworkGun networkGun, ExitGames.Client.Photon.Hashtable data)
    {
        TPWeapon = networkGun;
        if(FPWeapon == null) { FPWeapon = TPWeapon.LocalGun; }
        GameObject projectile = null;
        Vector3 instancePosition = (Vector3)data["position"];
        Quaternion instanceRotation = (Quaternion)data["rotation"];

        if (FPWeapon.bulletInstanceMethod == bl_Gun.BulletInstanceMethod.Pooled)
        {
            projectile = bl_ObjectPooling.Instance.Instantiate(FPWeapon.BulletName, instancePosition, instanceRotation);
        }
        else
        {
            projectile = Instantiate(FPWeapon.bulletPrefab, instancePosition, instanceRotation) as GameObject;
        }
        bl_GrenadeLauncherProjectile glp = projectile.GetComponent<bl_GrenadeLauncherProjectile>();
        TPWeapon.m_BulletData.Damage = 0;
        TPWeapon.m_BulletData.isNetwork = true;
        TPWeapon.m_BulletData.Speed = TPWeapon.LocalGun.bulletSpeed;
        TPWeapon.m_BulletData.Position = TPWeapon.transform.position;
        glp.SetInfo(TPWeapon.m_BulletData);
        TPWeapon.PlayLocalFireAudio();
    }

    /// <summary>
    /// 
    /// </summary>
    void Shoot()
    {
        GameObject projectile = null;
        Vector3 position = FPWeapon.muzzlePoint.position;
        Quaternion rotation = FPWeapon.PlayerCamera.transform.rotation;
        if (FPWeapon.bulletInstanceMethod == bl_Gun.BulletInstanceMethod.Pooled)
        {
            projectile = bl_ObjectPooling.Instance.Instantiate(FPWeapon.BulletName, position, rotation);
        }
        else
        {
            projectile = Instantiate(FPWeapon.bulletPrefab, position, rotation) as GameObject;
        }

        bl_GrenadeLauncherProjectile glp = projectile.GetComponent<bl_GrenadeLauncherProjectile>();
        FPWeapon.BuildBulletData();
        glp.SetInfo(FPWeapon.BulletSettings);
        glp.ExplosionRadius = explosionRadious;
        FPWeapon.PlayFireAudio();
        FPWeapon.WeaponAnimation?.AimFire();
        if (FPWeapon.muzzleFlash != null) { FPWeapon.muzzleFlash.Play(); }
        FPWeapon.bulletsLeft--;
        FPWeapon.Kick();
        FPWeapon.UpdateUI();
        FPWeapon.CheckBullets(1);

        var netData = bl_UtilityHelper.CreatePhotonHashTable();
        netData.Add("position", position);
        netData.Add("rotation", rotation);

        FPWeapon.PlayerNetwork.SyncCustomProjectile(netData);
    }
}