using System;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;
using System.IO;
using System.Reflection;

public class ImportUPMDependencys
{
    public static AddRequest Request;

    [InitializeOnLoadMethod]
    static void Add()
    {
        AssetDatabase.importPackageCompleted += ImportCompleted;
       // Debug.Log("Import Call Added");
    }

    static void ImportCompleted(string packageName)
    {
       // Debug.Log("Import Called: " + packageName);
        AssetDatabase.importPackageCompleted -= ImportCompleted;
        CheckDependencys();
    }

    static void CheckDependencys()
    {
        ClearConsole();
        if (!Directory.Exists("Packages/com.unity.postprocessing/"))
        {
            Request = Client.Add("com.unity.postprocessing");
            EditorApplication.update += Progress;
            Debug.Log("Begin installing post processing package...");
        }
        else
        {
            if (Request == null)
            {
                Debug.Log("Post Processing package is installed.");
                DeleteImporter();
            }
            else
            {
                Debug.Log("Processing package is installed. :" + Request.Status.ToString());
                if (!Request.IsCompleted) { EditorApplication.update += Progress; }
            }
        }
    }

    static void DeleteImporter()
    {
        File.Delete("Assets/MFPS/Scripts/Internal/Editor/MFPS/ImportUPMDependencys.cs.meta");
        File.Delete("Assets/MFPS/Scripts/Internal/Editor/MFPS/ImportUPMDependencys.cs");
    }

    static void ClearConsole()
    {
        try
        {
            clearConsoleMethod.Invoke(new object(), null);
        }
        catch { }
    }

    static void Progress()
    {
        if (Request.IsCompleted)
        {
            Debug.Log("Installation finish");
            if (Request.Status == StatusCode.Success)
            {
                Debug.Log("Installed: " + Request.Result.packageId);
                Debug.Log("Reimporting MFPS assets to integrate post processing...");
#if UNITY_2019_3_OR_NEWER
                AssetDatabase.Refresh();
                var folder = AssetDatabase.LoadAssetAtPath("Assets/MFPS", typeof(UnityEngine.Object));
                Selection.activeObject = folder;
                EditorApplication.ExecuteMenuItem("Assets/Reimport");
#else
                AssetDatabase.ImportAsset("Assets/MFPS", ImportAssetOptions.Default | ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
#endif

                ClearConsole();
                Debug.Log("All Done!");
                DeleteImporter();
            }
            else if (Request.Status >= StatusCode.Failure)
                Debug.Log(Request.Error.message);            

            EditorApplication.update -= Progress;
        }
    }

    static MethodInfo _clearConsoleMethod;
    static MethodInfo clearConsoleMethod
    {
        get
        {
            if (_clearConsoleMethod == null)
            {
                Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
                Type logEntries = assembly.GetType("UnityEditor.LogEntries");
                _clearConsoleMethod = logEntries.GetMethod("Clear");
            }
            return _clearConsoleMethod;
        }
    }

    public static void ClearLogConsole()
    {
        clearConsoleMethod.Invoke(new object(), null);
    }
}