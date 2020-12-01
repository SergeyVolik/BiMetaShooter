using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class bl_CameraShaker : MonoBehaviour
{
    private Vector3 OrigiPosition;
    private Quaternion DefaultCamRot;

    private Dictionary<string, ShakerPresent> shakersRunning = new Dictionary<string, ShakerPresent>();
    private Transform m_Transform;

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        m_Transform = transform;
        DefaultCamRot = m_Transform.localRotation;
        OrigiPosition = m_Transform.localPosition;

    }
    /// <summary>
    /// 
    /// </summary>
    private void OnEnable()
    {
        bl_EventHandler.onLocalPlayerShake += OnShake;
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnDisable()
    {
        bl_EventHandler.onLocalPlayerShake -= OnShake;
    }

    void OnShake(ShakerPresent present, string key, float influence)
    {
        AddShake(present, key, influence);
    }

    /// <summary>
    /// move the camera in a small range
    /// with the presets Gun
    /// </summary>
    /// <returns></returns>
    IEnumerator DoSimpleCameraShake(float amount = 0.2f, float duration = 0.4f, float intense = 0.25f, bool aim = false)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
           // elapsed += Time.deltaTime;
            float percentComplete = elapsed / duration;
            float damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);

            // map value to [-1, 1]
            float x = Random.value * 2.0f - 1.0f;
            float y = Random.value * 2.0f - 1.0f;
            x *= intense * damper;
            y *= intense * damper;
            float mult = (aim) ? 1 : 3;
            float multr = (aim) ? 25 : 40;

            transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(x * mult, y * mult, OrigiPosition.z), Time.deltaTime * 17);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(new Vector3(x * multr, y * multr, DefaultCamRot.z)), Time.deltaTime * 12);
            yield return null;
        }

        transform.localPosition = OrigiPosition;
        transform.localRotation = DefaultCamRot;

    }
    
    public void AddShake(ShakerPresent present, string key, float influenced = 1)
    {
        if (present == null) return;
        bool first = shakersRunning.Count <= 0;
        if (shakersRunning.ContainsKey(key))
        {
            shakersRunning.Remove(key);
        }
        present.currentTime = 1;
        present.starting = false;
        present.influence = influenced;
        shakersRunning.Add(key, present);

        if (first)
        {
            StartCoroutine(UpdateShake());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator UpdateShake()
    {
        Vector2 pos = Vector2.zero;
        while (true)
        {
            if(shakersRunning.Count <= 0) { yield break; }
            pos = Vector2.zero;
            for (int i = 0; i < shakersRunning.Count; i++)
            {
                ShakerPresent p = shakersRunning.Values.ElementAt(i);
                if (p.starting)
                {
                    p.currentTime += Time.deltaTime / (p.Duration * p.fadeInTime);
                    if(p.currentTime >= 1) { p.currentTime = 1; p.starting = false; }
                }
                else
                {
                    p.currentTime -= Time.deltaTime / (p.Duration - (p.Duration * p.fadeInTime));
                }             
                float amplitude = p.amplitude * p.currentTime;
                pos += Shake(amplitude, p.frequency, p.octaves, p.persistance, p.lacunarity, p.burstFrequency, p.burstContrast, p.influence);
                if (!p.starting && p.currentTime <= 0)
                {
                    shakersRunning.Remove(shakersRunning.ElementAt(i).Key);
                }
            }
            m_Transform.localPosition = pos;
            yield return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static Vector2 Shake(float amplitude, float frequency, int octaves, float persistance, float lacunarity, float burstFrequency, int burstContrast, float influence)
    {
        float valX = 0;
        float valY = 0;

        float iAmplitude = 1;
        float iFrequency = frequency;
        float maxAmplitude = 0;
        float time = Time.time;
        // Burst frequency
        float burstCoord = time / (1 - burstFrequency);

        // Sample diagonally trough perlin noise
        float burstMultiplier = Mathf.PerlinNoise(burstCoord, burstCoord);

        //Apply contrast to the burst multiplier using power, it will make values stay close to zero and less often peak closer to 1
        burstMultiplier = Mathf.Pow(burstMultiplier, burstContrast);

        for (int i = 0; i < octaves; i++) // Iterate trough octaves
        {
            float noiseFrequency = time / (1 - iFrequency) / 10;

            float perlinValueX = Mathf.PerlinNoise(noiseFrequency, 0.5f);
            float perlinValueY = Mathf.PerlinNoise(0.5f, noiseFrequency);

            // Adding small value To keep the average at 0 and   *2 - 1 to keep values between -1 and 1.
            perlinValueX = (perlinValueX + 0.0352f) * 2 - 1;
            perlinValueY = (perlinValueY + 0.0345f) * 2 - 1;

            valX += perlinValueX * iAmplitude;
            valY += perlinValueY * iAmplitude;

            // Keeping track of maximum amplitude for normalizing later
            maxAmplitude += iAmplitude;

            iAmplitude *= persistance;
            iFrequency *= lacunarity;
        }

        valX *= burstMultiplier;
        valY *= burstMultiplier;

        // normalize
        valX /= maxAmplitude;
        valY /= maxAmplitude;

        valX *= (amplitude * influence);
        valY *= (amplitude * influence);

        return new Vector2(valX, valY);
    }
}