using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class bl_AFK : bl_MonoBehaviour
{
    private float lastInput;
    private Vector3 oldMousePosition = Vector3.zero;
    private bool Leaving = false;
    private bool Watching = false;
    private bl_UIReferences UIReferences;
    private float AFKTimeLimit = 60;

    protected override void Awake()
    {
        base.Awake();
        UIReferences = bl_UIReferences.Instance;
        AFKTimeLimit = bl_GameData.Instance.AFKTimeLimit;
        if (!bl_GameData.Instance.DetectAFK)
        {
            this.enabled = false;
        }
    }

    public override void OnUpdate()
    {
        float time = Time.time;
        //if no movement or action of the player is detected, then start again
        if ((PhotonNetwork.LocalPlayer == null || Input.anyKey) || ((oldMousePosition != Input.mousePosition)))
        {
            lastInput = time;
            if (Watching)
            {
                UIReferences.AFKText.gameObject.SetActive(false);
                Watching = false;
            }
        }
        else if ((time - lastInput) > AFKTimeLimit * 0.5f)
        {
            Watching = true;
        }
        oldMousePosition = Input.mousePosition;
        if (((lastInput + AFKTimeLimit) - 10f) < time)
        {
            float t = AFKTimeLimit - (time - lastInput);
            if (t >= 0)
            {
                UIReferences.SetAFKCount(t);
            }
        }
        //If the maximum time is AFK then meets back to the lobby.
        if ((lastInput + AFKTimeLimit) < time && !Leaving)
        {
            bl_UtilityHelper.LockCursor(false);
            bl_PhotonNetwork.Instance.hasAFKKick = true;
            LeaveMatch();
            Leaving = true;
        }
    }


    public void LeaveMatch()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            bl_UtilityHelper.LoadLevel(bl_GameData.Instance.MainMenuScene);
        }
    }

}