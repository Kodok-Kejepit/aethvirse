// Builds Assets/Scenes/Playground.unity: a graybox course for movement and camera tests (M1).
// Re-run from the AETHVIRSE menu after changing it; the scene is regenerated from scratch.
using System.IO;
using Aethvirse.Cameras;
using Aethvirse.Combat;
using Aethvirse.Enemies;
using Aethvirse.Fx;
using Aethvirse.Interaction;
using Aethvirse.UI;
using Aethvirse.World;
using Aethvirse.Skills;
using Aethvirse.Stats;
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
    const string GreatswordComboPath = "Assets/Data/Combat/Greatsword/GreatswordCombo.asset";
    const string PaladinPath = "Assets/Data/Classes/Paladin.asset";
    const string TuningPath = "Assets/Data/Balancing/StatTuning.asset";
    const string FxMaterialPath = "Assets/Art/Materials/Fx/FxTransparent.mat";

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
        Material blade = MakeMaterial("Blade", new Color(0.80f, 0.85f, 0.95f));
        Material dummyMat = MakeMaterial("Dummy", new Color(0.72f, 0.55f, 0.36f));
        Material markerMat = MakeMaterial("LockMarker", new Color(1f, 0.25f, 0.25f));

        Light sun = BuildLight();
        BuildCourse(ground, block, ramp);
        PlayerController body = BuildPlayer(player, accent, out Transform cameraTarget);
        PlayerCameraRig rig = BuildCamera(cameraTarget);
        Wire(body, "cameraRig", rig);
        Wire(body, "movement", LoadOrCreateMovementData());
        BuildPlayerCombat(body, rig, blade, markerMat);
        BuildPlayerSkills(body);
        BuildDummies(dummyMat);
        BuildEnemies();
        BuildHud(body);
        BuildServices(body, sun);
        BuildWorldObjects();

        var overlay = new GameObject("DebugOverlay").AddComponent<DebugOverlay>();
        Wire(overlay, "player", body);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        RegisterScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("AETHVIRSE: playground scene built");
    }

    static Light BuildLight()
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
        return sun;
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
        // Hit-stop / slow motion change Time.timeScale; the camera keeps real-time smoothing.
        brain.IgnoreTimeScale = true;

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

        // Receives camera shake from heavy hits (CinemachineImpulseSource on the player).
        go.AddComponent<CinemachineImpulseListener>();
        return go.AddComponent<PlayerCameraRig>();
    }

    static void BuildPlayerCombat(PlayerController body, PlayerCameraRig rig, Material bladeMat, Material markerMat)
    {
        GameObject root = body.gameObject;
        Transform visual = root.transform.Find("Visual");

        var damageable = root.AddComponent<Damageable>();
        damageable.Configure(300, new CombatStats { attack = 24f, defense = 6f, critChance = 0.15f, critDamage = 1.6f }, side: Team.Player);

        // Graybox greatsword held at the right side; PlayerCombat swings the pivot per attack.
        var pivot = new GameObject("WeaponPivot").transform;
        pivot.SetParent(visual, false);
        pivot.localPosition = new Vector3(0.38f, 1.1f, 0.1f);
        Primitive(PrimitiveType.Cube, "Blade", pivot, new Vector3(0f, 0f, 0.75f), new Vector3(0.08f, 0.16f, 1.3f), bladeMat).tag = "Player";
        Primitive(PrimitiveType.Cube, "Guard", pivot, new Vector3(0f, 0f, 0.08f), new Vector3(0.36f, 0.08f, 0.08f), bladeMat).tag = "Player";

        var impulse = root.AddComponent<CinemachineImpulseSource>();
        var combat = root.AddComponent<PlayerCombat>();
        Wire(combat, "combo", AssetDatabase.LoadAssetAtPath<ComboData>(GreatswordComboPath));
        Wire(combat, "weaponPivot", pivot);
        Wire(combat, "impulse", impulse);

        // Lock-on marker: a spinning red diamond above the target (not parented to the player).
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "LockMarker";
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.transform.localScale = Vector3.one * 0.22f;
        marker.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
        marker.GetComponent<Renderer>().sharedMaterial = markerMat;
        var lockOn = root.AddComponent<LockOnTargeter>();
        Wire(lockOn, "cameraRig", rig);
        Wire(lockOn, "marker", marker.transform);

        Wire(body, "combat", combat);
        Wire(body, "lockOn", lockOn);
        Wire(body, "damageable", damageable);
    }

    const string AshHoundPath = "Assets/Data/Enemies/AshHound.asset";

    /// <summary>Three Ash Hounds in an arena north-east of the spawn (respawn 6 s after death).</summary>
    static void BuildEnemies()
    {
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>(AshHoundPath);
        if (data == null) throw new System.InvalidOperationException($"Missing {AshHoundPath}: run AETHVIRSE > Build Combat Data first");
        Material bodyMat = MakeMaterial("AshHound", data.bodyColor);
        Material eyeMat = MakeMaterial("EnemyEye", new Color(1f, 0.3f, 0.2f));
        Material telegraphMat = MakeMaterial("Telegraph", new Color(0.9f, 0.1f, 0.1f));
        Material barBack = MakeMaterial("BarBack", new Color(0.08f, 0.08f, 0.1f));
        Material barFill = MakeMaterial("BarFill", new Color(0.9f, 0.2f, 0.2f));

        var parent = new GameObject("Enemies").transform;
        Vector3[] spots = { new Vector3(26f, 0f, 26f), new Vector3(31f, 0f, 23f), new Vector3(29f, 0f, 30f) };
        Box(parent, "ArenaMarker", new Vector3(29f, 0.01f, 26f), new Vector3(14f, 0.02f, 14f), MakeMaterial("Arena", new Color(0.36f, 0.33f, 0.33f)));
        for (int i = 0; i < spots.Length; i++)
        {
            var root = new GameObject($"AshHound_{i}");
            root.transform.SetParent(parent, false);
            root.transform.position = spots[i];
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.2f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0f, 0.6f, 0f);
            cc.stepOffset = 0.3f;
            root.AddComponent<Damageable>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            visual.rotation = Quaternion.Euler(0f, 225f, 0f);
            GameObject bodyGo = NoCollider(Primitive(PrimitiveType.Cube, "Body", visual, new Vector3(0f, 0.6f, 0f), new Vector3(0.7f, 0.55f, 1.2f), bodyMat));
            NoCollider(Primitive(PrimitiveType.Cube, "Head", visual, new Vector3(0f, 0.85f, 0.7f), new Vector3(0.45f, 0.4f, 0.45f), bodyMat));
            NoCollider(Primitive(PrimitiveType.Cube, "EyeL", visual, new Vector3(-0.12f, 0.92f, 0.93f), new Vector3(0.08f, 0.06f, 0.02f), eyeMat));
            NoCollider(Primitive(PrimitiveType.Cube, "EyeR", visual, new Vector3(0.12f, 0.92f, 0.93f), new Vector3(0.08f, 0.06f, 0.02f), eyeMat));
            for (int leg = 0; leg < 4; leg++)
            {
                float lx = leg % 2 == 0 ? -0.25f : 0.25f;
                float lz = leg < 2 ? 0.4f : -0.4f;
                NoCollider(Primitive(PrimitiveType.Cube, $"Leg{leg}", visual, new Vector3(lx, 0.17f, lz), new Vector3(0.14f, 0.34f, 0.14f), bodyMat));
            }

            GameObject telegraph = NoCollider(Primitive(PrimitiveType.Cylinder, "Telegraph", root.transform, Vector3.zero, Vector3.one, telegraphMat));
            telegraph.SetActive(false);

            var bar = new GameObject("HealthBar").transform;
            bar.SetParent(root.transform, false);
            bar.localPosition = new Vector3(0f, 1.7f, 0f);
            NoCollider(Primitive(PrimitiveType.Cube, "Back", bar, new Vector3(0f, 0f, 0.005f), new Vector3(1.04f, 0.14f, 0.01f), barBack));
            GameObject fill = NoCollider(Primitive(PrimitiveType.Cube, "Fill", bar, Vector3.zero, new Vector3(1f, 0.1f, 0.01f), barFill));
            var healthBar = bar.gameObject.AddComponent<WorldHealthBar>();
            Wire(healthBar, "fill", fill.transform);

            var enemy = root.AddComponent<Enemy>();
            Wire(enemy, "data", data);
            Wire(enemy, "visual", visual);
            Wire(enemy, "bodyRenderer", bodyGo.GetComponent<Renderer>());
            Wire(enemy, "telegraph", telegraph.transform);
            Wire(enemy, "healthBar", healthBar);
            Wire(enemy, "scalingClass", AssetDatabase.LoadAssetAtPath<ClassData>(PaladinPath));
            Wire(enemy, "tuning", AssetDatabase.LoadAssetAtPath<StatTuning>(TuningPath));
        }
    }

    static void BuildHud(PlayerController body)
    {
        var hud = new GameObject("PlayerHud").AddComponent<PlayerHud>();
        Wire(hud, "player", body);
        Wire(hud, "combat", body.GetComponent<PlayerCombat>());
        Wire(hud, "lockOn", body.GetComponent<LockOnTargeter>());
        Wire(hud, "health", body.GetComponent<Damageable>());
        Wire(hud, "stats", body.GetComponent<PlayerStats>());
        Wire(hud, "caster", body.GetComponent<SkillCaster>());

        var panel = new GameObject("StatusPanel").AddComponent<StatusPanel>();
        Wire(panel, "stats", body.GetComponent<PlayerStats>());

        var flame = new GameObject("ComboFlame").AddComponent<ComboFlameHud>();
        Wire(flame, "combat", body.GetComponent<PlayerCombat>());
    }

    static GameObject NoCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        return go;
    }

    /// <summary>Stats (Paladin), skill caster and the skill effects on the player.</summary>
    static void BuildPlayerSkills(PlayerController body)
    {
        GameObject root = body.gameObject;
        var classData = AssetDatabase.LoadAssetAtPath<ClassData>(PaladinPath);
        var tuning = AssetDatabase.LoadAssetAtPath<StatTuning>(TuningPath);
        if (classData == null || tuning == null)
            throw new System.InvalidOperationException("Missing class data: run AETHVIRSE > Build Class Data first");

        var stats = root.AddComponent<PlayerStats>();
        Wire(stats, "classData", classData);
        Wire(stats, "tuning", tuning);
        Wire(stats, "player", body);
        Wire(stats, "combat", root.GetComponent<PlayerCombat>());
        Wire(stats, "damageable", root.GetComponent<Damageable>());

        var caster = root.AddComponent<SkillCaster>();
        Wire(caster, "stats", stats);
        Wire(caster, "player", body);
        Wire(caster, "combat", root.GetComponent<PlayerCombat>());

        Transform visual = root.transform.Find("Visual");
        var fx = root.AddComponent<SkillFx>();
        Wire(fx, "caster", caster);
        Wire(fx, "combat", root.GetComponent<PlayerCombat>());
        Wire(fx, "stats", stats);
        Wire(fx, "visual", visual);
        Wire(fx, "bodyMesh", visual.Find("Body").GetComponent<MeshFilter>());
        Wire(fx, "blade", visual.Find("WeaponPivot/Blade").GetComponent<Renderer>());
        Wire(fx, "fxMaterial", MakeFxMaterial());
    }

    /// <summary>Transparent, unlit, double-sided URP material for effects (alpha via _BaseColor).</summary>
    static Material MakeFxMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(FxMaterialPath);
        if (mat == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FxMaterialPath));
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, FxMaterialPath);
        }
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_Cull", 0f);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void BuildDummies(Material mat)
    {
        var parent = new GameObject("TrainingDummies").transform;
        Vector3[] spots = { new Vector3(4f, 0f, 3.5f), new Vector3(6.5f, 0f, 2f), new Vector3(6.5f, 0f, 5f) };
        for (int i = 0; i < spots.Length; i++)
        {
            var root = new GameObject($"Dummy_{i}");
            root.transform.SetParent(parent, false);
            root.transform.position = spots[i];
            root.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.height = 2f;
            col.radius = 0.4f;
            root.AddComponent<Damageable>();

            var wobble = new GameObject("Wobble").transform;
            wobble.SetParent(root.transform, false);
            GameObject bodyGo = Primitive(PrimitiveType.Capsule, "Body", wobble, new Vector3(0f, 1f, 0f), new Vector3(0.75f, 0.75f, 0.75f), mat);
            Object.DestroyImmediate(bodyGo.GetComponent<Collider>());
            GameObject arms = Primitive(PrimitiveType.Cube, "Arms", wobble, new Vector3(0f, 1.25f, 0f), new Vector3(1.3f, 0.14f, 0.14f), mat);
            Object.DestroyImmediate(arms.GetComponent<Collider>());

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            var label = labelGo.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelGo.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.fontSize = 48;
            label.characterSize = 0.05f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;

            var dummy = root.AddComponent<TrainingDummy>();
            Wire(dummy, "bodyRenderer", bodyGo.GetComponent<Renderer>());
            Wire(dummy, "wobblePivot", wobble);
            Wire(dummy, "label", label);
        }
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

    static void RegisterScenes() => TitleSceneBuilder.RegisterScenes();

    /// <summary>Day/night + weather, pause menu, Codex hooks, interaction detector.</summary>
    static void BuildServices(PlayerController body, Light sun)
    {
        var env = new GameObject("Environment").AddComponent<EnvironmentController>();
        Wire(env, "sun", sun);
        var weather = new GameObject("RegionWeather").AddComponent<RegionWeather>();
        new GameObject("PauseMenu").AddComponent<PauseMenu>();
        new GameObject("CodexHooks").AddComponent<CodexHooks>();
        var detector = body.gameObject.AddComponent<InteractionDetector>();
        Wire(detector, "facing", body.transform.Find("Visual"));
        Wire(Object.FindFirstObjectByType<PlayerHud>(), "interaction", detector);
    }

    /// <summary>Sample interactables next to the spawn: save crystal, notice board, rest bench.</summary>
    static void BuildWorldObjects()
    {
        var parent = new GameObject("WorldObjects").transform;
        Material crystal = MakeMaterial("SaveCrystal", new Color(0.4f, 0.85f, 1f));
        Material wood = MakeMaterial("Wood", new Color(0.5f, 0.36f, 0.22f));

        GameObject c = Primitive(PrimitiveType.Cube, "SaveCrystal", parent, new Vector3(-3f, 1f, 2f), new Vector3(0.6f, 1.6f, 0.6f), crystal);
        c.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        Interactable ci = c.AddComponent<Interactable>();
        ci.Prompt = "Touch the crystal (save)";
        c.AddComponent<SaveCrystal>();

        GameObject board = Primitive(PrimitiveType.Cube, "NoticeBoard", parent, new Vector3(-5.5f, 1.2f, -1f), new Vector3(1.6f, 1.2f, 0.12f), wood);
        board.transform.rotation = Quaternion.Euler(0f, 70f, 0f);
        Interactable bi = board.AddComponent<Interactable>();
        bi.Prompt = "Read the notice";
        var notice = board.AddComponent<NoticeBoard>();
        var so = new SerializedObject(notice);
        so.FindProperty("text").stringValue = "Movement test ground. Dummies east, Ash Hounds north-east.";
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject bench = Primitive(PrimitiveType.Cube, "RestBench", parent, new Vector3(-5f, 0.25f, 3.5f), new Vector3(1.8f, 0.5f, 0.6f), wood);
        Interactable ri = bench.AddComponent<Interactable>();
        ri.Prompt = "Rest until morning";
        bench.AddComponent<RestSpot>();
    }
}
