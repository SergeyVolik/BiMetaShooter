using UnityEngine;
using Photon.Realtime;

public class DamageData
{
    public int Damage = 10;
    public string From = "";
    public DamageCause Cause = DamageCause.Player;
    public Vector3 Direction { get; set; } = Vector3.zero;
    public bool isHeadShot { get; set; } = false;
    public int GunID { get; set; } = 0;
    public Player Actor { get; set; }
    public MFPSPlayer MFPSActor { get; set; }
    public int ActorViewID { get; set; }
}