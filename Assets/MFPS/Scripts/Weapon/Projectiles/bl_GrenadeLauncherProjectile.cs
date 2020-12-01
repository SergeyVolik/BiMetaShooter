using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bl_GrenadeLauncherProjectile : MonoBehaviour
{
    public GameObject explosionPrefab;
    public bool Pooled = false;
    private bool detecting = true;
    BulletData bulletData;
    private Rigidbody m_rigidbody;
    private float instanceTime = 0;
    public float ExplosionRadius { get; set; } = 10;

    public void SetInfo(BulletData data)
    {
        if (m_rigidbody == null)
            m_rigidbody = GetComponent<Rigidbody>();

        bulletData = data;
        m_rigidbody.AddForce(transform.forward * bulletData.Speed, ForceMode.Impulse);
        instanceTime = Time.time;
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnEnable()
    {
        detecting = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnHit(collision);
    }

    void OnHit(Collision collision)
    {
        if (!detecting) return;
        if(bulletData != null)
        {
            if (!bulletData.isNetwork)
            {
                bl_PlayerNetwork pn = collision.gameObject.GetComponent<bl_PlayerNetwork>();
                if (pn != null && pn.isMine) { return; }//if the player hit itself
            }
            else
            {
                if (Time.time - instanceTime <= 0.005f) return;
            }
        }

        GameObject e = Instantiate(explosionPrefab, collision.contacts[0].point, Quaternion.identity) as GameObject;
        bl_ExplosionDamage blast = e.GetComponent<bl_ExplosionDamage>();
        if (blast != null)
        {
            blast.isNetwork = bulletData.isNetwork;
            bulletData.Position = transform.position;
            blast.explosionRadius = ExplosionRadius;
            blast.SetUp(bulletData, bl_GameManager.LocalPlayerViewID);
        }
        detecting = false;
        if (Pooled) { gameObject.SetActive(false); }
        else { Destroy(gameObject); }
    }
}