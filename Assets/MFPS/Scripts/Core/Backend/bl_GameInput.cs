#if INPUT_MANAGER
using MFPS.InputManager;
#endif
using UnityEngine;

public class bl_GameInput 
{
    public static float Vertical
    {
        get
        {
            if (!bl_RoomMenu.Instance.isCursorLocked || bl_GameData.Instance.isChating) return 0;
#if !INPUT_MANAGER
            return Input.GetAxis("Vertical");
#else
            return bl_Input.VerticalAxis;
#endif
        }
    }

    public static float Horizontal
    {
        get
        {
            if (!bl_RoomMenu.Instance.isCursorLocked || bl_GameData.Instance.isChating) return 0;
#if !INPUT_MANAGER
            return Input.GetAxis("Horizontal");
#else
            return bl_Input.HorizontalAxis;
#endif
        }
    }

    public static bool Run(GameInputType inputType = GameInputType.Hold)
    {
#if INPUT_MANAGER
        if (bl_InputData.Instance.runWithButton)
            return GetInputManager("Run", inputType);
        else
            return Input.GetAxis("Vertical") >= 1f;
#else
        return GetButton(KeyCode.LeftShift, inputType);
#endif
    }

    public static bool Crouch(GameInputType inputType = GameInputType.Hold)
    {
#if INPUT_MANAGER
        return GetInputManager("Crouch", inputType);
#else
        return GetButton(KeyCode.C, inputType);
#endif
    }

    public static bool Jump(GameInputType inputType = GameInputType.Down)
    {
#if INPUT_MANAGER
        return GetInputManager("Jump", inputType);
#else
        return GetButton(KeyCode.Space, inputType);
#endif
    }

    public static bool WeaponSlot(int slotID)
    {
#if INPUT_MANAGER
        return GetInputManager($"Weapon{slotID}", GameInputType.Down);
#else
        // return GetButton(KeyCode.Space, inputType);
        return false;
#endif
    }

    public static bool GetButton(KeyCode key, GameInputType inputType)
    {
        if (!bl_RoomMenu.Instance.isCursorLocked || bl_GameData.Instance.isChating) return false;

            if (inputType == GameInputType.Hold) { return Input.GetKey(key); }
        else if(inputType == GameInputType.Down) { return Input.GetKeyDown(key); }
        else { return Input.GetKeyUp(key); }
    }

#if INPUT_MANAGER
    public static bool GetInputManager(string key, GameInputType inputType)
    {
    if (!bl_RoomMenu.Instance.isCursorLocked || bl_GameData.Instance.isChating) return false;
        if(inputType == GameInputType.Hold) { return bl_Input.isButton(key); }
        else if (inputType == GameInputType.Down) { return bl_Input.isButtonDown(key); }
        else { return bl_Input.isButtonUp(key); }
    }
#endif
}

public enum GameInputType
{
    Down,
    Up,
    Hold,
}