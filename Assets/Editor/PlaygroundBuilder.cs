// Builds Assets/Scenes/Playground.unity: a graybox course for movement and camera tests (M1).
// Re-run from the AETHVIRSE menu after changing it; the scene is regenerated from scratch.
using System.IO;
using Aethvirse.Cameras;
using Aethvirse.Core;
using Aethvirse.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AmbientMode = UnityEngine.Rendering.AmbientMode;

public static class PlaygroundBuilder
{
    const string ScenePath = "Assets/Scenes/Playground.unity";
    const string MaterialDir = "Assets/Art/Materials/Graybox";
    const string MovementDataPath = "Assets/Data/Player/DefaultMovement.asset";

    const float PlayerHeight = 1.8f;
    const float PlayerRadius = 0.35f;
    const float CameraTargetHeight = 1.45f;

    [MenuItem("AETHVIRSE/Build Playground Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material ground = MakeMaterial("Ground", new Color(0.42f, 0.44f, 0.47f));
        Material block = MakeMaterial("Block", new Color(0.30f, 0.45f, 0.70f));
        Material ramp = MakeMaterial("Ramp", new Color(0.85f, 0.55f, 0.25f));
        Material player = MakeMaterial("Player", new Color(0.92f, 0.92f, 0.95f));
        Material accent = MakeMaterial("Accent", new Color(0.20f, 0.85f, 0.95f));

        BuildLight();
        BuildCourse(ground, block, ramp);
        PlayerController body = BuildPlayer(player, accent, out Transform cameraTarget);
        PlayerCameraRig rig = BuildCamera(cameraTarget);
        Wire(body, "cameraRig", rig);
        Wire(body, "movement", LoadOrCreateMovementData());

        var overlay = new GameObject("DebugOverlay").AddComponent<DebugOverlay>();
        Wire(overlay, "player", body);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        RegisterScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("AETHVIRSE: playground scene built");
    }

    static void BuildLight()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.3f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.75f);
        RenderSettings.ambientEquatorColor = new Color(0.40f, 0.42f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.22f, 0.24f);
    }

    static void BuildCourse(Material ground, Material block, Material ramp)
    {
        var course = new GameObject("Course").transform;
        Box(course, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(120f, 1f, 120f), ground);

        // Steps of rising height in front of the spawn: which ones can be walked up vs jumped.
        float[] stepHeights = { 0.2f, 0.35f, 0.5f, 1.0f, 1.5f, 2.5f };
        for (int i = 0; i < stepHeights.Length; i++)
        {
            float h = stepHeights[i];
            Box(course, $"Step_{h:0.00}m", new Vector3(-7.5f + i * 3f, h / 2f, 8f), new Vector3(2f, h, 2f), block);
        }

        // Ramps behind the spawn, rising away from it (-Z): 20° (walkable), 40° (walkable, steep),
        // 55° (too steep). They must stay clear of the spawn and the camera behind it.
        Ramp(course, "Ramp_20", new Vector3(-12f, 0f, -8f), 20f, ramp, yaw: 180f);
        Ramp(course, "Ramp_40", new Vector3(-6f, 0f, -8f), 40f, ramp, yaw: 180f);
        Ramp(course, "Ramp_55", new Vector3(0f, 0f, -8f), 55f, ramp, yaw: 180f);

        // Jump gaps between platforms: 2 m (run), 4 m (sprint), 6 m (sprint + air dash).
        float x = 10f;
        float[] gaps = { 2f, 4f, 6f };
        Box(course, "Platform_0", new Vector3(x, 1f, -6f), new Vector3(4f, 2f, 4f), block);
        foreach (float gap in gaps)
        {
            x += 4f + gap;
            Box(course, $"Platform_gap_{gap:0}m", new Vector3(x, 1f, -6f), new Vector3(4f, 2f, 4f), block);
        }

        // Camera collision: a long wall, a pillar forest and a low tunnel.
        Box(course, "Wall", new Vector3(0f, 2f, 18f), new Vector3(30f, 4f, 0.5f), block);
        for (int i = 0; i < 6; i++)
            Box(course, $"Pillar_{i}", new Vector3(14f + (i % 3) * 3f, 2.5f, 10f + (i / 3) * 3f), new Vector3(0.8f, 5f, 0.8f), block);
        Box(course, "Tunnel_Roof", new Vector3(-16f, 2.6f, 10f), new Vector3(4f, 0.4f, 8f), block);
        Box(course, "Tunnel_WallL", new Vector3(-18.2f, 1.2f, 10f), new Vector3(0.4f, 2.4f, 8f), block);
        Box(course, "Tunnel_WallR", new Vector3(-13.8f, 1.2f, 10f), new Vector3(0.4f, 2.4f, 8f), block);

        // A tower to fall from (fall recovery puts you back on the last safe ground).
        Box(course, "Tower", new Vector3(-20f, 4f, -16f), new Vector3(4f, 8f, 4f), block);
        // Rises along +Z to the tower top (8 m over 12 m).
        Ramp(course, "Tower_Ramp", new Vector3(-20f, 0f, -30f), 33.7f, ramp, length: 12f);
    }

    static PlayerController BuildPlayer(Material bodyMat, Material accentMat, out Transform cameraTarget)
    {
        var root = new GameObject("Player") { tag = "Player" };
        root.transform.position = new Vector3(0f, 0.05f, 0f);
        var controller = root.AddComponent<CharacterController>();
        controller.height = PlayerHeight;
        controller.radius = PlayerRadius;
        controller.center = new Vector3(0f, PlayerHeight / 2f, 0f);
        controller.stepOffset = 0.4f;
        controller.slopeLimit = 45f;
        controller.skinWidth = 0.04f;

        var visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        var capsule = Primitive(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, PlayerHeight / 2f, 0f), new Vector3(PlayerRadius * 2f, PlayerHeight / 2f, PlayerRadius * 2f), bodyMat);
        capsule.tag = "Player";
        // A visor shows which way the body faces.
        Primitive(PrimitiveType.Cube, "Visor", visual, new Vector3(0f, 1.5f, 0.3f), new Vector3(0.45f, 0.14f, 0.12f), accentMat).tag = "Player";

        cameraTarget = new GameObject("CameraTarget").transform;
        cameraTarget.SetParent(root.transform, false);
        cameraTarget.localPosition = new Vector3(0f, CameraTargetHeight, 0f);

        var body = root.AddComponent<PlayerController>();
        Wire(body, "visual", visual);
        return body;
    }

    static PlayerCameraRig BuildCamera(Transform target)
    {
        var main = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = main.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;
        main.AddComponent<AudioListener>();
        var brain = main.AddComponent<CinemachineBrain>();
        // The player moves in Update, so the camera follows in LateUpdate of the same frame.
        // SmartUpdate guesses the target's update loop and can pick the wrong one (jitter).
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
        brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;

        var go = new GameObject("PlayerCamera");
        var vcam = go.AddComponent<CinemachineCamera>();
        vcam.Target.TrackingTarget = target;
        vcam.Lens = LensSettings.Default;
        vcam.Lens.FieldOfView = 70f;
        vcam.Lens.NearClipPlane = 0.1f;
        vcam.Lens.FarClipPlane = 500f;

        var orbit = go.AddComponent<CinemachineOrbitalFollow>();
        orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
        orbit.Radius = 4.5f;
        orbit.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
        orbit.TrackerSettings.PositionDamping = new Vector3(0.1f, 0.1f, 0.1f);
        orbit.HorizontalAxis.Recentering.Enabled = false;
        orbit.VerticalAxis.Range = new Vector2(-40f, 65f);
        orbit.VerticalAxis.Center = 15f;
        orbit.VerticalAxis.Value = 15f;
        orbit.VerticalAxis.Recentering.Enabled = false;

        var composer = go.AddComponent<CinemachineRotationComposer>();
        composer.Damping = Vector2.zero;

        var deoccluder = go.AddComponent<CinemachineDeoccluder>();
        deoccluder.IgnoreTag = "Player";
        deoccluder.MinimumDistanceFromTarget = 0.3f;
        deoccluder.AvoidObstacles.Enabled = true;
        deoccluder.AvoidObstacles.CameraRadius = 0.2f;
        deoccluder.AvoidObstacles.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
        deoccluder.AvoidObstacles.Damping = 0.3f;
        deoccluder.AvoidObstacles.DampingWhenOccluded = 0.05f;

        return go.AddComponent<PlayerCameraRig>();
    }

    // --- Helpers ---------------------------------------------------------------------------

    static PlayerMovementData LoadOrCreateMovementData()
    {
        var data = AssetDatabase.LoadAssetAtPath<PlayerMovementData>(MovementDataPath);
        if (data != null) return data;
        Directory.CreateDirectory(Path.GetDirectoryName(MovementDataPath));
        data = ScriptableObject.CreateInstance<PlayerMovementData>();
        AssetDatabase.CreateAsset(data, MovementDataPath);
        return data;
    }

    static Material MakeMaterial(string name, Color color)
    {
        string path = $"{MaterialDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Directory.CreateDirectory(MaterialDir);
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = Primitive(PrimitiveType.Cube, name, parent, center, size, mat);
        go.isStatic = true;
        return go;
    }

    /// <summary>A ramp rising from <paramref name="foot"/> toward <paramref name="yaw"/> (0 = +Z), <paramref name="length"/> m long on the ground.</summary>
    static void Ramp(Transform parent, string name, Vector3 foot, float angle, Material mat, float length = 6f, float yaw = 0f)
    {
        float rise = Mathf.Tan(angle * Mathf.Deg2Rad) * length;
        float slope = Mathf.Sqrt(length * length + rise * rise);
        const float thickness = 0.4f;
        var go = Box(parent, name, Vector3.zero, new Vector3(3f, thickness, slope), mat);
        go.transform.rotation = Quaternion.Euler(-angle, yaw, 0f);
        // Put the top surface's low edge on the ground at the foot.
        Vector3 up = go.transform.up;
        Vector3 forward = go.transform.forward;
        go.transform.position = foot + forward * (slope / 2f) - up * (thickness / 2f);
    }

    static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        // The player's own parts must not collide with the CharacterController.
        if (parent != null && parent.root.name == "Player") Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static void Wire(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null) throw new System.ArgumentException($"{target.GetType().Name} has no field '{field}'");
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void RegisterScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Boot.unity", true),
            new EditorBuildSettingsScene(ScenePath, true),
        };
    }
}
