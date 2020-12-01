using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bl_FootStepsLibrary : ScriptableObject
{
    public AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
    public AudioClip[] DirtStepSounds;
    public AudioClip[] WatertepSounds;
    public AudioClip[] MetalStepSounds;
}