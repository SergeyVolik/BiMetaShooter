using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bl_CameraIdentity : MonoBehaviour
{
    private Camera m_Camera;

    private void OnEnable()
    {
        StartCoroutine(Set());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator Set()
    {
        yield return new WaitForSeconds(0.5f);
        if (m_Camera == null)
        {
            m_Camera = GetComponent<Camera>();
        }
        if (m_Camera != null)
            bl_GameManager.Instance.CameraRendered = m_Camera;
    }
}