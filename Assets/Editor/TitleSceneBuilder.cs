// Builds Assets/Scenes/Title.unity (title menu) and registers Boot → Title → Playground in Build Settings.
using Aethvirse.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TitleSceneBuilder
{
    public const string TitlePath = "Assets/Scenes/Title.unity";
    public const string PlaygroundPath = "Assets/Scenes/Playground.unity";
    public const string BootPath = "Assets/Scenes/Boot.unity";

    [MenuItem("AETHVIRSE/Build Title Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.04f, 0.09f);
        new GameObject("TitleMenu").AddComponent<TitleMenu>();
        EditorSceneManager.SaveScene(scene, TitlePath);
        RegisterScenes();
        Debug.Log("AETHVIRSE: title scene built");
    }

    public static void RegisterScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(BootPath, true),
            new EditorBuildSettingsScene(TitlePath, true),
            new EditorBuildSettingsScene(AcademyBuilder.AcademyPath, true),
            new EditorBuildSettingsScene(PlaygroundPath, true),
            new EditorBuildSettingsScene(AcademyBuilder.PenaltyPath, true),
        };
    }
}
