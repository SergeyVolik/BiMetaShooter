using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class bl_UILeftNotifier : MonoBehaviour
{

    [SerializeField]private Text m_Text;


    public void SetInfo(string t,float time)
    {
        m_Text.text = t;
        StartCoroutine(Hide(time));
    }

    IEnumerator Hide(float t)
    {
        yield return new WaitForSeconds(t);
        Destroy(gameObject);
    }
}