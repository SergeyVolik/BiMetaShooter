using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

public class bl_PostProcessEffects : MonoBehaviour
{
#if UNITY_POST_PROCESSING_STACK_V2
    public PostProcessProfile processProfile;
     
    /// <summary>
    /// 
    /// </summary>
    private void OnEnable()
    {
        bl_EventHandler.OnEffectChange += OnPostEffect;
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnDisable()
    {
        bl_EventHandler.OnEffectChange -= OnPostEffect;
    }

    /// <summary>
    /// 
    /// </summary>
    void OnPostEffect(bool chrab, bool anti, bool bloom, bool ssao, bool motionBlur)
    {
        if (processProfile == null) return;

        if (processProfile.HasSettings(typeof(ChromaticAberration)))
        {
            processProfile.GetSetting<ChromaticAberration>().active = chrab;
        }
        if (processProfile.HasSettings(typeof(Bloom)))
        {
            processProfile.GetSetting<Bloom>().active = bloom;
        }
        if (processProfile.HasSettings(typeof(AmbientOcclusion)))
        {
            processProfile.GetSetting<AmbientOcclusion>().active = ssao;
        }
        if (processProfile.HasSettings(typeof(MotionBlur)))
        {
            processProfile.GetSetting<MotionBlur>().active = motionBlur;
        }
    }
#endif
}