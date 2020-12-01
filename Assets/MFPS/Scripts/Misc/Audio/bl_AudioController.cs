using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bl_AudioController : MonoBehaviour
{
    public bl_AudioBank AudioBank;

    [Header("Background")]
    [SerializeField] private AudioClip BackgroundClip;
    public float MaxBackgroundVolume = 0.3f;
    public AudioSource backgroundSource;

    /// <summary>
    /// 
    /// </summary>
    private void OnEnable()
    {
        MaxBackgroundVolume = PlayerPrefs.GetFloat(PropertiesKeys.BackgroundVolume, MaxBackgroundVolume);
    }

    public bl_AudioBank.AudioInfo GetAudioInfo(string name)
    {
        if (AudioBank == null) return null;
        return AudioBank.AudioBank.Find(x => x.Name == name);
    }

    public bl_AudioBank.AudioInfo GetAudioInfo(int indexID)
    {
        if (AudioBank == null) return null;
        return AudioBank.AudioBank[indexID];
    }

    /// <summary>
    /// 
    /// </summary>
    public void PlayBackground()
    {
        if (BackgroundClip == null) return;
        if(backgroundSource == null) { backgroundSource = gameObject.AddComponent<AudioSource>(); }

        backgroundSource.clip = BackgroundClip;
        backgroundSource.volume = 0;
        backgroundSource.playOnAwake = false;
        backgroundSource.loop = true;
        StartCoroutine(FadeAudio(backgroundSource, true, MaxBackgroundVolume));
    }

    /// <summary>
    /// 
    /// </summary>
    public void StopBackground()
    {
        if (backgroundSource == null) return;

        FadeAudio(backgroundSource, false);
    }

    /// <summary>
    /// 
    /// </summary>
    IEnumerator FadeAudio(AudioSource source, bool up, float volume = 1)
    {
        if (up)
        {
            source.Play();
            while (source.volume < volume)
            {
                source.volume += Time.deltaTime * 0.01f;
                yield return new WaitForEndOfFrame();
            }
        }
        else
        {
            while (source.volume > 0)
            {
                source.volume -= Time.deltaTime * 0.5f;
                yield return new WaitForEndOfFrame();
            }
        }
    }

    public float BackgroundVolume
    {
        set
        {
            if(backgroundSource != null) { backgroundSource.volume = value; }
        }
    }



    private static bl_AudioController _instance;
    public static bl_AudioController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<bl_AudioController>();
            }
            return _instance;
        }
    }
}