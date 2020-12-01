using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bl_CountDownUI : MonoBehaviour
{
    public GameObject Content;
    public Text CountDownText;
    public AudioClip CountAudio;

    private Animator CountAnim;
    private bool started = false;
    private AudioSource ASource;

    public void SetCount(int count)
    {
        if (CountAudio != null)
        {
            if (ASource == null) { ASource = GetComponent<AudioSource>(); }
            ASource.clip = CountAudio;
            ASource.Play();
        }

        CountDownText.text = count.ToString();
        if (!started) { Content.SetActive(true); started = true; }
        else if (count <= 0) { Content.SetActive(false); started = false; }
        else
        {
            CountAnim = Content.transform.GetComponent<Animator>();
            CountAnim.Play("count", 0, 0);
        }
    }

    private static bl_CountDownUI _instance;
    public static bl_CountDownUI Instance
    {
        get
        {
            if (_instance == null) { _instance = FindObjectOfType<bl_CountDownUI>(); }
            return _instance;
        }
    }
}