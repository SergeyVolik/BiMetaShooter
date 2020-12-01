using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[DefaultExecutionOrder(-1000)]
public class bl_Game : MonoBehaviour
{
    public static class Input
    {
        [Flags]
        public enum Blocker
        {
            None = 0,
            Console = 1,
            Chat = 2,
            Debug = 4,
        }
        static Blocker blocks;

        public static void SetBlock(Blocker b, bool value)
        {
            if (value)
                blocks |= b;
            else
                blocks &= ~b;
        }

        internal static float GetAxisRaw(string axis)
        {
            return blocks != Blocker.None ? 0.0f : UnityEngine.Input.GetAxisRaw(axis);
        }

        internal static bool GetKey(KeyCode key)
        {
            return blocks != Blocker.None ? false : UnityEngine.Input.GetKey(key);
        }

        internal static bool GetKeyDown(KeyCode key)
        {
            return blocks != Blocker.None ? false : UnityEngine.Input.GetKeyDown(key);
        }

        internal static bool GetMouseButton(int button)
        {
            return blocks != Blocker.None ? false : UnityEngine.Input.GetMouseButton(button);
        }

        internal static bool GetKeyUp(KeyCode key)
        {
            return blocks != Blocker.None ? false : UnityEngine.Input.GetKeyUp(key);
        }
    }
}