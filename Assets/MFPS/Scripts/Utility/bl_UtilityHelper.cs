using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.SceneManagement;
#if UNITY_EDITOR && !UNITY_WEBGL
using System.IO;
#endif
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public static class bl_UtilityHelper
{ 

    public static void LoadLevel(string scene)
    {
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }

    public static void LoadLevel(int scene)
    {
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }

    /// <summary>
    /// Sort Player by Kills,for more info watch this: http://answers.unity3d.com/questions/233917/custom-sorting-function-need-help.html
    /// </summary>
    /// <returns></returns>
    public static int GetSortPlayerByKills(Player player1, Player player2)
    {
        if (player1.CustomProperties[PropertiesKeys.KillsKey] != null && player2.CustomProperties[PropertiesKeys.KillsKey] != null)
        {
            return (int)player2.CustomProperties[PropertiesKeys.KillsKey] - (int)player1.CustomProperties[PropertiesKeys.KillsKey];
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Get ClampAngle
    /// </summary>
    /// <returns></returns>
    public static float ClampAngle(float ang, float min, float max)
    {
        if (ang < -360f)
        {
            ang += 360f;
        }
        if (ang > 360f)
        {
            ang -= 360f;
        }
        return Mathf.Clamp(ang, min, max);
    }

    public static GameObject GetGameObjectView(PhotonView m_view)
    {
        GameObject go = PhotonView.Find(m_view.ViewID).gameObject;
        return go;
    }
    /// <summary>
    /// obtain only the first two values
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    public static string GetDoubleChar(float f)
    {
        return f.ToString("00");
    }
    /// <summary>
    /// obtain only the first three values
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    public static string GetThreefoldChar(float f)
    {
        return f.ToString("000");
    }

    public static string GetTimeFormat(float m, float s)
    {
        return string.Format("{0:00}:{1:00}", m, s);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="force"></param>
    /// <returns></returns>
    public static Vector3 CorrectForceSize(UnityEngine.Vector3 force)
    {
        float num = (1.2f / Time.timeScale) - 0.2f;
        force = (force * num);
        return force;
    }

    /// <summary>
    /// Helper for Cursor locked in Unity 5
    /// </summary>
    /// <param name="mLock">cursor state</param>
    public static void LockCursor(bool mLock)
    {
        if (BlockCursorForUser) return;
        if (mLock == true)
        {
            CursorLockMode cm = isMobile ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = false;
            Cursor.lockState = cm;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    public static bool BlockCursorForUser = false;

    /// <summary>
    /// 
    /// </summary>
    public static bool GetCursorState
    {
        get
        {
#if (UNITY_ANDROID || UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
            return true;
#else
            if ((Cursor.visible && Cursor.lockState != CursorLockMode.Locked))
            {
                return false;
            }
            else
            {
                return true;
            }
#endif
        }
    }

    public static bool isMobile
    {
        get
        {
#if (UNITY_ANDROID || UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
            return true;
#else
#if MFPSM && UNITY_EDITOR
            if (EditorApplication.isRemoteConnected) return true;
#endif
            return false;
#endif
        }
    }

    // The angle between dirA and dirB around axis
    public static float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
    {
        // Project A and B onto the plane orthogonal target axis
        dirA = dirA - Vector3.Project(dirA, axis);
        dirB = dirB - Vector3.Project(dirB, axis);

        // Find (positive) angle between A and B
        float angle = Vector3.Angle(dirA, dirB);

        // Return angle multiplied with 1 or -1
        return angle * (Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) < 0 ? -1 : 1);
    }

    public static void PlayClipAtPoint(AudioClip clip,Vector3 position,float volume,AudioSource sourc)
    {
        GameObject obj2 = new GameObject("One shot audio")
        {
            transform = { position = position }
        };
        AudioSource source = (AudioSource)obj2.AddComponent(typeof(AudioSource));
        if (sourc != null)
        {
            source.minDistance = sourc.minDistance;
            source.maxDistance = sourc.maxDistance;
            source.panStereo = sourc.panStereo;
            source.spatialBlend = sourc.spatialBlend;
            source.rolloffMode = sourc.rolloffMode;
            source.priority = sourc.priority;
        }
        source.clip = clip;
        source.volume = volume;
        source.Play();
        Object.Destroy(obj2, clip.length * Time.timeScale);
    }

    public static void DrawWireCircle(Vector3 center, float radius, int segments = 20, Quaternion rotation = default(Quaternion))
    {
        DrawWireArc(center, radius, 360, segments, rotation);
    }


    public static void DrawWireArc(Vector3 center, float radius, float angle, int segments = 20, Quaternion rotation = default(Quaternion))
    {
        var old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        Vector3 from = Vector3.forward * radius;
        var step = Mathf.RoundToInt(angle / segments);
        for (int i = 0; i <= angle; i += step)
        {
            var to = new Vector3(radius * Mathf.Sin(i * Mathf.Deg2Rad), 0, radius * Mathf.Cos(i * Mathf.Deg2Rad));
            Gizmos.DrawLine(from, to);
            from = to;
        }

        Gizmos.matrix = old;
    }

    public static float Distance(Vector3 from, Vector3 to)
    {
        Vector3 v = new Vector3(from.x - to.x, from.y - to.y, from.z - to.z);
        return Mathf.Sqrt((v.x * v.x + v.y * v.y + v.z * v.z));
    }

#if UNITY_EDITOR
    public static string CreateAsset<T>(string path = "", bool autoFocus = true, string customName = "") where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();

        if(string.IsNullOrEmpty(path))
        path = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(path))
        {
            path = "Assets";
        }
#if !UNITY_WEBGL
        else if (Path.GetExtension(path) != string.Empty)
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }
#endif
        string fileName = string.IsNullOrEmpty(customName) ? $"New {typeof(T).ToString()}" : customName;
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}/{fileName}.asset");

        AssetDatabase.CreateAsset(asset, assetPathAndName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (autoFocus)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
        return assetPathAndName;
    }
#endif

        public static Vector3 CalculateCenter(params Transform[] aObjects)
    {
        Bounds b = new Bounds();
        foreach (var o in aObjects)
        {
            var renderers = o.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.GetComponent<ParticleSystem>() != null) continue;
                if (b.size == Vector3.zero)
                    b = r.bounds;
                else
                    b.Encapsulate(r.bounds);
            }
            var colliders = o.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                if (b.size == Vector3.zero)
                    b = c.bounds;
                else
                    b.Encapsulate(c.bounds);
            }
        }
        return b.center;
    }

    public static GameObject FindInChildren(this GameObject go, string name, bool inactiveObjects = true)
    {
        return (from x in go.GetComponentsInChildren<Transform>(inactiveObjects)
                where x.gameObject.name == name
                select x.gameObject).First();
    }

    public static ExitGames.Client.Photon.Hashtable CreatePhotonHashTable()
    {
        return new ExitGames.Client.Photon.Hashtable();
    }
}