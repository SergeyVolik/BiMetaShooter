using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class bl_GunBase : bl_MonoBehaviour
{

    public bl_GunInfo Info;
    public int GunID;

   public int bulletsLeft { get; set; }
   public bool isAiming { get; set; }
   public bool isFiring { get; set; }
}