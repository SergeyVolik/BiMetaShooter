//////////////////////////////////////////////////////////////////////////////
// bl_Extensions.cs
//
// this facilitates access to properties 
// more authoritatively for each photon player, ex: PhotonNetwork.player.GetKills();
//
//                       Lovatto Studio
//////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine.UI;
using System.Collections;

static class bl_Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static void PostScore(this Player player, int ScoreToAdd = 0)
    {
        int current = player.GetPlayerScore();
        current = current + ScoreToAdd;

        Hashtable score = new Hashtable();  // using PUN's implementation of Hashtable
        score[PropertiesKeys.ScoreKey] = current;

        player.SetCustomProperties(score);  // this locally sets the score and will sync it in-game asap.
    }

    /// <summary>
    /// 
    /// </summary>
    public static int GetPlayerScore(this Player player)
    {
        int s = 0;

        if (player.CustomProperties.ContainsKey(PropertiesKeys.ScoreKey))
        {
            s = (int)player.CustomProperties[PropertiesKeys.ScoreKey];
            return s;
        }

        return s;
    }

    /// <summary>
    /// 
    /// </summary>
    public static int GetKills(this Player p)
    {
        int k = 0;
        if (p.CustomProperties.ContainsKey(PropertiesKeys.KillsKey))
        {
            k = (int)p.CustomProperties[PropertiesKeys.KillsKey];
            return k;
        }
        return k;
    }

    /// <summary>
    /// 
    /// </summary>
    public static int GetDeaths(this Player p)
    {
        int d = 0;
        if (p.CustomProperties.ContainsKey(PropertiesKeys.DeathsKey))
        {
            d = (int)p.CustomProperties[PropertiesKeys.DeathsKey];
            return d;
        }
        return d;
    }

    /// <summary>
    /// 
    /// </summary>
    public static void PostKill(this Player p, int kills)
    {
        int current = p.GetKills();
        current = current + kills;

        Hashtable score = new Hashtable();  // using PUN's implementation of Hashtable
        score[PropertiesKeys.KillsKey] = current;

        p.SetCustomProperties(score);  // this locally sets the score and will sync it in-game asap.
    }

    /// <summary>
    /// 
    /// </summary>
    public static void PostDeaths(this Player p, int deaths)
    {
        int current = p.GetDeaths();
        current = current + deaths;

        Hashtable score = new Hashtable();  // using PUN's implementation of Hashtable
        score[PropertiesKeys.DeathsKey] = current;

        p.SetCustomProperties(score);  // this locally sets the score and will sync it in-game asap.
    }

    public static int GetRoomScore(this Room room, Team team)
    {
        object teamId;
        if (team == Team.Team1)
        {
            if (room.CustomProperties.TryGetValue(PropertiesKeys.Team1Score, out teamId))
            {
                return (int)teamId;
            }
        } else if (team == Team.Team2)
        {
            if (room.CustomProperties.TryGetValue(PropertiesKeys.Team2Score, out teamId))
            {
                return (int)teamId;
            }
        }

        return 0;
    }

    public static void SetTeamScore(this Room r, Team t, int scoreToAdd = 1)
    {
        if (t == Team.None) return;

        int score = 0;
        score = r.GetRoomScore(t);
        score += scoreToAdd;
        string key = (t == Team.Team1) ? PropertiesKeys.Team1Score : PropertiesKeys.Team2Score;
        Hashtable h = new Hashtable();
        h.Add(key, score);
        r.SetCustomProperties(h);
    }

    public static Team GetPlayerTeam(this Player p)
    {
        if (p == null) return Team.None;
        object teamId;
        string t = (string)p.CustomProperties[PropertiesKeys.TeamKey];
        if (p.CustomProperties.TryGetValue(PropertiesKeys.TeamKey, out teamId))
        {
            switch ((string)teamId)
            {
                case "Team2":
                    return Team.Team2;
                case "Team1":
                    return Team.Team1;
                case "All":
                    return Team.All;
                case "None":
                    return Team.None;

            }
        }
        return Team.None;
    }

    public static void SetPlayerTeam(this Player player, Team team)
    {
        Hashtable PlayerTeam = new Hashtable();
        PlayerTeam.Add(PropertiesKeys.TeamKey, team.ToString());
        player.SetCustomProperties(PlayerTeam);
    }

    public static string GetTeamName(this Team t)
    {
        switch (t)
        {
            case Team.Team1:
                return bl_GameData.Instance.Team1Name;
            case Team.Team2:
                return bl_GameData.Instance.Team2Name;
            default:
                return "Solo";
        }
    }

    public static Color GetTeamColor(this Team t, float alpha = 0)
    {
        Color c = Color.white;//default color
        switch (t)
        {
            case Team.Team1:
               c = bl_GameData.Instance.Team1Color;
                break;
            case Team.Team2:
               c = bl_GameData.Instance.Team2Color;
                break;
            case Team.All:
                c = Color.white;
                break;
        }
        if(alpha > 0) { c.a = alpha; }

        return c;
    }

    public static bl_GameData.GameModesEnabled GetModeInfo(this GameMode mode)
    {
        for (int i = 0; i < bl_GameData.Instance.gameModes.Count; i++)
        {
            if(bl_GameData.Instance.gameModes[i].gameMode == mode) { return bl_GameData.Instance.gameModes[i]; }
        }
        return null;
    }

    private const string PLAYER_CLASS_KEY = "{0}.playerclass";
    public static void SavePlayerClass(this PlayerClass pc)
    {
        string key = string.Format(PLAYER_CLASS_KEY, Application.productName);
        PlayerPrefs.SetInt(key, (int)pc);
    }

    public static PlayerClass GetSavePlayerClass(this PlayerClass pc)
    {
        string key = string.Format(PLAYER_CLASS_KEY, Application.productName);
        int id = PlayerPrefs.GetInt(key, 0);
        PlayerClass pclass = PlayerClass.Assault;
        switch (id)
        {
            case 1:
                pclass = PlayerClass.Recon;
                break;
            case 2:
                pclass = PlayerClass.Support;
                break;
            case 3:
                pclass = PlayerClass.Engineer;
                break;
        }
        return pclass;
    }

    public static string NickNameAndRole(this Player p)
    {
        object role = "";
        if (p.CustomProperties.TryGetValue(PropertiesKeys.UserRole, out role))
        {
            return string.Format("<b>{1}</b> {0}", p.NickName, (string)role);
        }
        return p.NickName;
    }

    public static string GetName(this GameMode mode)
    {
        bl_GameData.GameModesEnabled info = mode.GetModeInfo();
        if(info != null) { return info.ModeName; }
        else { return string.Format("Not define: " + mode.ToString()); }
    }

    public static bl_GameData.GameModesEnabled GetGameModeInfo(this GameMode mode)
    {
        return bl_GameData.Instance.gameModes.Find(x => x.gameMode == mode);
    }
    /// <summary>
    /// is this player in the same team that local player?
    /// </summary>
    /// <returns></returns>
    public static bool isTeamMate(this Player p)
    {
        bool b = false;
        if(p.GetPlayerTeam() == PhotonNetwork.LocalPlayer.GetPlayerTeam()) { b = true; }
        return b;
    }

    public static Player[] GetPlayersInTeam(this Player[] player, Team team)
    {
        List<Player> list = new List<Player>();
        for(int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            if(PhotonNetwork.PlayerList[i].GetPlayerTeam() == team) { list.Add(PhotonNetwork.PlayerList[i]); }
        }
        return list.ToArray();
    }

    /// <summary>
    /// Get current gamemode
    /// </summary>
    public static GameMode GetGameMode(this RoomInfo room)
    {
        string gm = (string)room.CustomProperties[PropertiesKeys.GameModeKey];
        GameMode mode = (GameMode)Enum.Parse(typeof(GameMode), gm);
        return mode;
    }

    public static RoomProperties GetRoomInfo(this Room room)
    {
        return new RoomProperties(room);
    }

    public static Team OppsositeTeam(this Team team)
    {
        if (team == Team.Team1) { return Team.Team2; }
        else if (team == Team.Team2) { return Team.Team1; }
        else
        {
            return Team.All;
        }
    }

    public static void InvokeAfter(this MonoBehaviour mono, float time, Action callback)
    {
        mono.StartCoroutine(WaitToExecute(time, callback));
    }

    static IEnumerator WaitToExecute(float time, Action callback)
    {
        yield return new WaitForSeconds(time);
        if (callback != null) callback.Invoke();
    }

    public static Vector2 SizeToParent(this RawImage image, float padding = 0)
    {
        float w = 0, h = 0;
        var parent = image.transform.parent.GetComponent<RectTransform>();
        var imageTransform = image.GetComponent<RectTransform>();

        // check if there is something to do
        if (image.texture != null)
        {
            if (!parent) { return imageTransform.sizeDelta; } //if we don't have a parent, just return our current width;
            padding = 1 - padding;
            float ratio = image.texture.width / (float)image.texture.height;
            var bounds = new Rect(0, 0, parent.rect.width, parent.rect.height);
            if (Mathf.RoundToInt(imageTransform.eulerAngles.z) % 180 == 90)
            {
                //Invert the bounds if the image is rotated
                bounds.size = new Vector2(bounds.height, bounds.width);
            }
            //Size by height first
            h = bounds.height * padding;
            w = h * ratio;
            if (w > bounds.width * padding)
            { //If it doesn't fit, fallback to width;
                w = bounds.width * padding;
                h = w / ratio;
            }
        }
        imageTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        imageTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
        return imageTransform.sizeDelta;
    }

    public static void RandomizeAudioOutput(this AudioSource source)
    {
        source.pitch = UnityEngine.Random.Range(0.92f, 1.1f);
        source.spread = UnityEngine.Random.Range(0.98f, 1.25f);
    }
}