// Creates the bootstrap scene and registers it in Build Settings, so CI builds always have a scene.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ProjectSetup
{
    const string BootScene = "Assets/Scenes/Boot.unity";
    const string RenderSettingsDir = "Assets/Settings/Rendering";
    const string PipelineAssetPath = RenderSettingsDir + "/URP_Default.asset";
    const string RendererAssetPath = RenderSettingsDir + "/URP_Default_Renderer.asset";
    const string DefaultPostProcessData = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

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

    // Creates the URP asset (once) and makes every quality level use it.
    [MenuItem("AETHVIRSE/Setup Render Pipeline")]
    public static void EnsureRenderPipeline()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
        if (pipeline == null)
        {
            Directory.CreateDirectory(RenderSettingsDir);
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostProcessData);
            AssetDatabase.CreateAsset(renderer, RendererAssetPath);
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }
        QualitySettings.SetQualityLevel(current, false);
        AssetDatabase.SaveAssets();
        Debug.Log("AETHVIRSE: URP pipeline ready");
    }
}
