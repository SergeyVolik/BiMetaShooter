///////////////////////////////////////////////////////////////////////////////////////
// bl_FrameRate.cs
//
// Help us get the current Frame Rate the game
// place it in the scena and adds the UI Text
//                           
//                                 Lovatto Studio
///////////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using UnityEngine.UI;
using System;

public class bl_FrameRate : bl_MonoBehaviour
{
    private float accum;
    private int frames;
    public Text TextUI = null;
    //Privates
    private string framerate;
    private float timeleft;
    private float updateInterval = 0.5f;
    float rate = 0;

    void Start()
    {
        this.timeleft = this.updateInterval;
    }

    public override void OnUpdate()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;
        if (timeleft <= 0)
        {
            rate = accum / frames;
            framerate = rate.ToString("000");
            timeleft = updateInterval;
            accum = 0;
            frames = 0;
        }
        if (TextUI != null)
        {
            TextUI.text = string.Format("FPS: <color=#FFE300>{0}</color>", framerate);
        }
        else
        {
            Destroy(this);
        }

    }
}