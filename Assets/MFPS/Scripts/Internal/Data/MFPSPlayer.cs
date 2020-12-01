using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[Serializable]
public class MFPSPlayer
{
    public string Name;
    public Transform Actor;
    public PhotonView ActorView;
    public bool isRealPlayer = true;
    public bool isAlive = true;
    public Team Team = Team.None;
    public Transform AimPosition;
}