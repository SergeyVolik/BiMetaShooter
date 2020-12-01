using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalDestroyer : MonoBehaviour {

    public bool Pooled = true;
	public float lifeTime = 5.0f;

	void OnEnable()
	{
        Invoke("Disable", lifeTime);
	}

    void Disable()
    {
        if (Pooled)
        {
            transform.parent = bl_ObjectPooling.Instance.transform;
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
