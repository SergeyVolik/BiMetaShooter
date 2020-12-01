using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bl_GraphicTeamColor : MonoBehaviour
{
    public Team team = Team.All;
    public bool applyColor = true;
    public bool applyTeamName = false;
    public bool toUpper = true;
    private Graphic graphic;

    /// <summary>
    /// 
    /// </summary>
    private void OnEnable()
    {
        if(graphic == null) { graphic = GetComponent<Graphic>(); }
        if (applyColor)
        {
            graphic.color = team.GetTeamColor();
        }

        if(applyTeamName && GetComponent<Text>() != null)
        {
            GetComponent<Text>().text = toUpper ? team.GetTeamName().ToUpper() : team.GetTeamName();
        }
    }
}