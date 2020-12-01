using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bl_FrameRateManager : MonoBehaviour
{
    public Dropdown optionDropdown;

    /// <summary>
    /// 
    /// </summary>
    void OnStart()
    {
        if(optionDropdown != null)
        {
            optionDropdown.ClearOptions();
            List<Dropdown.OptionData> ol = new List<Dropdown.OptionData>();
            int[] options = bl_GameData.Instance.DefaultSettings.frameRateOptions;
            for (int i = 0; i < options.Length; i++)
            {
                if(options[i] == 0) { ol.Add(new Dropdown.OptionData() { text = "UNLIMITED" }); continue; }
                ol.Add(new Dropdown.OptionData() { text = options[i].ToString() });
            }
            optionDropdown.AddOptions(ol);
        }
        int df = PlayerPrefs.GetInt(PropertiesKeys.FrameRateOption, bl_GameData.Instance.DefaultSettings.defaultFrameRate);
        optionDropdown.value = df;
        Application.targetFrameRate = bl_GameData.Instance.DefaultSettings.frameRateOptions[df];
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnChange(int option)
    {        
        Application.targetFrameRate = bl_GameData.Instance.DefaultSettings.frameRateOptions[option];
        PlayerPrefs.SetInt(PropertiesKeys.FrameRateOption, option);
    }
}