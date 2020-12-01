using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Audio Bank", menuName = "MFPS/Audio Bank", order = 301)]
public class bl_AudioBank : ScriptableObject
{
    public List<AudioInfo> AudioBank = new List<AudioInfo>();

    [System.Serializable]
    public class AudioInfo
    {
        public string Name;
        public AudioClip Clip;
        [Range(0, 1)] public float Volume = 0.9f;
        public bool SpacialAudio = false;
        public bool Loop = false;
    }
}