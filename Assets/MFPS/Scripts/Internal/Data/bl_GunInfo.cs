using System;
using UnityEngine;

[Serializable]
public class bl_GunInfo
{
    [Header("Info")]
    public string Name;
    public GunType Type = GunType.Machinegun;

    [Header("Settings")]
    [Range(1,100)] public int Damage;
    [Range(0.01f, 2f)] public float FireRate = 0.1f;
    [Range(0.5f, 10)] public float ReloadTime = 2.5f;
    [Range(1, 700)] public int Range;
    [Range(0.01f, 5)] public int Accuracy;
    [Range(0, 4)] public int Weight;
    public int Price = 0;
#if LM
    public int LevelUnlock = 0;
#endif

    [Header("References")]
    public bl_GunPickUp PickUpPrefab;
    public Sprite GunIcon;
}