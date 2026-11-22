// Creates the bootstrap scene and registers it in Build Settings, so CI builds always have a scene.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProjectSetup
{
    const string BootScene = "Assets/Scenes/Boot.unity";

    [MenuItem("AETHVIRSE/Setup Boot Scene")]
    public static void EnsureBootScene()
    {
        if (!File.Exists(BootScene))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var label = new GameObject("AETHVIRSE");
            label.transform.position = Vector3.zero;
            EditorSceneManager.SaveScene(scene, BootScene);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
        PlayerSettings.companyName = "Kodok-Kejepit";
        PlayerSettings.productName = "AETHVIRSE";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.kodokkejepit.aethvirse");
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.kodokkejepit.aethvirse");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        AssetDatabase.SaveAssets();
        Debug.Log("AETHVIRSE: boot scene ready");
    }
}
