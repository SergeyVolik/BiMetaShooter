using UnityEngine;

public class BulletData 
{
    public string WeaponName;
    public float Damage;
    public Vector3 Position;
    public float ImpactForce;
    public float MaxSpread;
    public float Spread;
    public float Speed;
    public float LifeTime;
    public int WeaponID;
    public bool isNetwork;
    public int ActorViewID { get; set; }
    public MFPSPlayer MFPSActor { get; set; }
}