using UnityEngine;

public interface IWeapon 
{
    /// <summary>
    /// Called from FPWeapon at start when this weapon is in the player load out
    /// </summary>
    /// <param name="gun"></param>
    void Initialitate(bl_Gun gun);

    /// <summary>
    /// Called when Fire button is clicked (one time)
    /// </summary>
    void OnFireDown();

    /// <summary>
    /// Called when Fire button is pressed (keep)
    /// </summary>
    void OnFire();

    /// <summary>
    /// Called from TPWeapon (Network Gun) when a projectile has to be instance
    /// use this to implement your custom fire logic
    /// </summary>
    void TPFire(bl_NetworkGun tpWeapon, ExitGames.Client.Photon.Hashtable data);
}