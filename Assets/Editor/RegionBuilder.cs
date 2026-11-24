// Builds the world: enemy types per region, region data, the atlas (galaxy → planet → region) and one graybox
// scene per region (town, village, landmark, camps, waystones, Gate spots, roads). Re-run after changes.
using System.Collections.Generic;
using System.IO;
using Aethvirse.Combat;
using Aethvirse.Enemies;
using Aethvirse.Interaction;
using Aethvirse.Stats;
using Aethvirse.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RegionBuilder
{
    const string EnemyDir = "Assets/Data/Enemies";
    const string RegionDir = "Assets/Data/Regions";
    public const string SceneDir = "Assets/Scenes/Regions";

    struct Def
    {
        public string id, name, scene, blurb;
        public int min, max, rank;
        public Color palette;
        public string[] enemies;
        public WeatherKind[] weather;
        public bool hidden;
    }

    static readonly Def[] Regions =
    {
        new Def { id = "r1_ashfall", name = "Ashfall Outskirts", scene = "R1_Ashfall", min = 1, max = 50, rank = 0, palette = new Color(0.42f, 0.4f, 0.4f),
            blurb = "Outskirts buried in ash. A dead city that still breathes.", enemies = new[] { "ash_hound", "ember_husk" }, weather = new[] { WeatherKind.Clear, WeatherKind.Rain, WeatherKind.Fog, WeatherKind.Ashfall, WeatherKind.Storm } },
        new Def { id = "r2_verdant", name = "Verdant Hollow", scene = "R2_Verdant", min = 50, max = 100, rank = 1, palette = new Color(0.32f, 0.5f, 0.3f),
            blurb = "A green valley. Too quiet to trust.", enemies = new[] { "thorn_stalker", "moss_brute" }, weather = new[] { WeatherKind.Clear, WeatherKind.Rain, WeatherKind.Fog, WeatherKind.Storm } },
        new Def { id = "r3_dunes", name = "Crimson Dunes", scene = "R3_Dunes", min = 100, max = 150, rank = 2, palette = new Color(0.7f, 0.4f, 0.3f),
            blurb = "Red sand over old ruins.", enemies = new[] { "dune_scuttler", "sand_wraith" }, weather = new[] { WeatherKind.Clear, WeatherKind.Sandstorm } },
        new Def { id = "r4_frostveil", name = "Frostveil Peaks", scene = "R4_Frostveil", min = 150, max = 200, rank = 3, palette = new Color(0.8f, 0.85f, 0.9f),
            blurb = "Frozen peaks under a veil of snow.", enemies = new[] { "frost_yeti", "ice_wyvernling" }, weather = new[] { WeatherKind.Clear, WeatherKind.Snow, WeatherKind.Fog, WeatherKind.Storm } },
        new Def { id = "r5_archive", name = "Sunken Archive", scene = "R5_Archive", min = 200, max = 250, rank = 4, palette = new Color(0.3f, 0.45f, 0.55f),
            blurb = "An archive under the tide, of things meant to be forgotten.", enemies = new[] { "drowned_scribe", "ink_wraith" }, weather = new[] { WeatherKind.Rain, WeatherKind.Fog, WeatherKind.Clear } },
        new Def { id = "r6_fracture", name = "The Fracture", scene = "R6_Fracture", min = 250, max = 300, rank = 5, palette = new Color(0.35f, 0.3f, 0.45f),
            blurb = "Broken land. The world split here once.", enemies = new[] { "void_hollow", "rift_stalker" }, weather = new[] { WeatherKind.Clear, WeatherKind.Storm, WeatherKind.Fog } },
        new Def { id = "r7_throne", name = "Hollow Throne", scene = "R7_Throne", min = 300, max = 350, rank = 5, palette = new Color(0.2f, 0.18f, 0.22f),
            blurb = "An empty throne at the end of the known map.", enemies = new[] { "hollow_echo", "gravewarden" }, weather = new[] { WeatherKind.Clear, WeatherKind.Fog, WeatherKind.Storm } },
        new Def { id = "r0_unknown", name = "[ ??? ]", scene = "", min = 350, max = 400, rank = 5, palette = Color.black, hidden = true,
            blurb = "", enemies = new string[0], weather = new[] { WeatherKind.Clear } },
    };

    [MenuItem("AETHVIRSE/Build World (regions)")]
    public static void Build()
    {
        Directory.CreateDirectory(EnemyDir);
        Directory.CreateDirectory(RegionDir);
        Directory.CreateDirectory(SceneDir);
        BuildEnemies();
        var regions = new List<RegionData>();
        foreach (Def d in Regions) regions.Add(RegionAsset(d));
        for (int i = 0; i < regions.Count - 1; i++)
        {
            regions[i].neighbours = new List<string>();
            if (i > 0) regions[i].neighbours.Add(regions[i - 1].id);
            if (i < regions.Count - 2) regions[i].neighbours.Add(regions[i + 1].id);
        }
        var atlas = LoadOrCreate<WorldAtlas>("Assets/Resources/WorldAtlas.asset");
        atlas.galaxies = new List<GalaxyInfo>
        {
            new GalaxyInfo
            {
                id = "home", displayName = "The Known Sky",
                planets = new List<PlanetInfo>
                {
                    new PlanetInfo { id = "aethvirse", displayName = "Aethvirse", regions = regions },
                    // Part 2 / 3 (STORY §6.5): further worlds open after the last seal; regions get added as they are built.
                    new PlanetInfo { id = "planet_2", displayName = "[ unknown world ]", unlockFlag = "last_seal_opened" },
                },
            },
            new GalaxyInfo { id = "galaxy_2", displayName = "[ unknown galaxy ]", unlockFlag = "galaxy_2_opened" },
        };
        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
        for (int i = 0; i < Regions.Length; i++) if (!Regions[i].hidden) BuildScene(Regions[i], regions[i], i);
        BuildGateScene();
        BuildUnknownScene();
        RegisterScenes();
        AssetDatabase.SaveAssets();
        Debug.Log($"AETHVIRSE: world built ({regions.Count} regions)");
    }

    public static string[] SceneNames()
    {
        var list = new List<string>();
        foreach (Def d in Regions) if (!d.hidden) list.Add($"{SceneDir}/{d.scene}.unity");
        return list.ToArray();
    }

    static void RegisterScenes() => TitleSceneBuilder.RegisterScenes();

    // --- Enemy types ------------------------------------------------------------------------------

    static void BuildEnemies()
    {
        var catalog = LoadOrCreate<EnemyCatalog>("Assets/Resources/EnemyCatalog.asset");
        catalog.basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlaygroundBuilder.AshHoundPrefabPath);
        catalog.enemies = new List<EnemyData> { AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemyDir}/AshHound.asset") };
        // id, name, colour, size, hp, atk, chase, windup, cooldown, knockback, launch, range, poise
        catalog.enemies.Add(E("ember_husk", "Ember Husk", new Color(0.55f, 0.3f, 0.2f), 1.3f, 1.6f, 1.2f, 2.6f, 0.9f, 1.6f, 7f, 0f, 2.4f, 45f));
        catalog.enemies.Add(E("thorn_stalker", "Thorn Stalker", new Color(0.3f, 0.45f, 0.25f), 0.9f, 0.8f, 1.1f, 6f, 0.45f, 0.8f, 4f, 0f, 2f, 20f));
        catalog.enemies.Add(E("moss_brute", "Moss Brute", new Color(0.25f, 0.4f, 0.2f), 1.5f, 2f, 1.3f, 2.4f, 1f, 1.8f, 9f, 4f, 2.6f, 60f));
        catalog.enemies.Add(E("dune_scuttler", "Dune Scuttler", new Color(0.75f, 0.55f, 0.35f), 0.8f, 0.7f, 1f, 6.5f, 0.4f, 0.7f, 3f, 0f, 1.8f, 18f));
        catalog.enemies.Add(E("sand_wraith", "Sand Wraith", new Color(0.85f, 0.7f, 0.5f), 1.1f, 1f, 1.3f, 4.5f, 0.6f, 1.1f, 5f, 0f, 2.2f, 25f));
        catalog.enemies.Add(E("frost_yeti", "Frost Yeti", new Color(0.85f, 0.9f, 0.95f), 1.6f, 2.1f, 1.4f, 3f, 0.95f, 1.7f, 10f, 5f, 2.6f, 65f));
        catalog.enemies.Add(E("ice_wyvernling", "Ice Wyvernling", new Color(0.55f, 0.75f, 0.95f), 1f, 0.9f, 1.2f, 6f, 0.5f, 0.9f, 4f, 0f, 2.2f, 22f));
        catalog.enemies.Add(E("drowned_scribe", "Drowned Scribe", new Color(0.3f, 0.45f, 0.5f), 1f, 1f, 1.2f, 3.6f, 0.7f, 1.2f, 4f, 0f, 2.2f, 30f));
        catalog.enemies.Add(E("ink_wraith", "Ink Wraith", new Color(0.12f, 0.12f, 0.18f), 1.1f, 0.9f, 1.4f, 5.2f, 0.55f, 1f, 5f, 0f, 2.2f, 24f));
        catalog.enemies.Add(E("void_hollow", "Void Hollow", new Color(0.2f, 0.15f, 0.3f), 1.2f, 1.3f, 1.4f, 4.4f, 0.6f, 1.1f, 6f, 2f, 2.4f, 35f));
        catalog.enemies.Add(E("rift_stalker", "Rift Stalker", new Color(0.5f, 0.3f, 0.7f), 0.95f, 0.9f, 1.5f, 7f, 0.4f, 0.8f, 5f, 0f, 2f, 22f));
        catalog.enemies.Add(E("hollow_echo", "Hollow Echo", new Color(0.7f, 0.7f, 0.75f), 1.1f, 1.2f, 1.5f, 5f, 0.5f, 1f, 6f, 0f, 2.2f, 30f));
        catalog.enemies.Add(E("gravewarden", "Gravewarden", new Color(0.25f, 0.22f, 0.2f), 1.7f, 2.4f, 1.6f, 2.8f, 1f, 1.6f, 11f, 6f, 2.8f, 80f));
        catalog.enemies.Add(E("the_unwritten", "The Unwritten", new Color(0.05f, 0.05f, 0.05f), 1.4f, 3f, 1.8f, 5f, 0.5f, 0.9f, 12f, 8f, 3f, 200f));
        EditorUtility.SetDirty(catalog);
    }

    static EnemyData E(string id, string name, Color color, float size, float hp, float atk, float chase, float windup, float cooldown,
        float knockback, float launch, float range, float poise)
    {
        var e = LoadOrCreate<EnemyData>($"{EnemyDir}/{id}.asset");
        EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemyDir}/AshHound.asset"), e);
        e.name = id;
        e.id = id;
        e.displayName = name;
        e.bodyColor = color;
        e.size = size;
        e.hpScale = hp;
        e.attackScale = atk;
        e.chaseSpeed = chase;
        e.attackWindup = windup;
        e.attackCooldown = cooldown;
        e.attackKnockback = knockback;
        e.attackLaunch = launch;
        e.attackRange = range * Mathf.Max(1f, size * 0.8f);
        e.poise = poise;
        e.telegraphRadius = 1.6f * size;
        e.attackHitboxSize = new Vector3(1.8f, 1.4f, 1.6f) * Mathf.Max(1f, size * 0.9f);
        EditorUtility.SetDirty(e);
        return e;
    }

    // --- Regions --------------------------------------------------------------------------------------

    static RegionData RegionAsset(Def d)
    {
        var r = LoadOrCreate<RegionData>($"{RegionDir}/{d.id}.asset");
        r.id = d.id;
        r.displayName = d.name;
        r.blurb = d.blurb;
        r.scene = d.scene;
        r.levelMin = d.min;
        r.levelMax = d.max;
        r.requiredRank = d.rank;
        r.hidden = d.hidden;
        r.palette = d.palette;
        r.enemies = new List<string>(d.enemies);
        r.weather = new WeatherTable { entries = new List<WeatherTable.Entry>() };
        for (int i = 0; i < d.weather.Length; i++) r.weather.entries.Add(new WeatherTable.Entry { kind = d.weather[i], weight = i == 0 ? 4f : 1.5f });
        r.waystones = new List<WaystoneInfo>
        {
            new WaystoneInfo { id = d.id + "_gate", displayName = d.name + " — Entry", position = new Vector3(6f, 0f, -100f) },
            new WaystoneInfo { id = d.id + "_town", displayName = d.name + " — Town", position = new Vector3(-50f, 0f, -30f) },
            new WaystoneInfo { id = d.id + "_landmark", displayName = d.name + " — Landmark", position = new Vector3(0f, 0f, 55f) },
        };
        r.gateSpots = new List<Vector3> { new Vector3(70f, 0f, -70f), new Vector3(-80f, 0f, 40f), new Vector3(85f, 0f, 70f), new Vector3(-30f, 0f, 95f) };
        EditorUtility.SetDirty(r);
        return r;
    }

    static void BuildScene(Def d, RegionData region, int index)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rng = new System.Random(index * 9973 + 17);
        Material ground = PlaygroundBuilder.MakeMaterial($"Ground_{d.id}", d.palette);
        Material wall = PlaygroundBuilder.MakeMaterial($"Wall_{d.id}", Color.Lerp(d.palette, Color.white, 0.35f));
        Material dark = PlaygroundBuilder.MakeMaterial($"Dark_{d.id}", Color.Lerp(d.palette, Color.black, 0.45f));
        Material stone = PlaygroundBuilder.MakeMaterial("WaystoneMat", new Color(0.4f, 0.8f, 1f));
        var world = new GameObject(d.name).transform;

        PlaygroundBuilder.Box(world, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(260f, 1f, 260f), ground);
        // Town: a walled block of buildings; village: scattered huts; landmark: one big structure.
        Walls(world, new Vector3(-50f, 0f, -30f), 44f, wall);
        for (int i = 0; i < 10; i++)
            PlaygroundBuilder.Box(world, $"House{i}", new Vector3(-66f + (i % 4) * 11f, 0f, -44f + (i / 4) * 12f) + Vector3.up * 3f, new Vector3(7f, 6f + rng.Next(5), 7f), wall);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            PlaygroundBuilder.Box(world, $"Hut{i}", new Vector3(60f + Mathf.Cos(a) * 12f, 1.6f, 25f + Mathf.Sin(a) * 12f), new Vector3(4f, 3.2f, 4f), dark);
        }
        PlaygroundBuilder.Box(world, "Landmark", new Vector3(0f, 9f, 75f), new Vector3(16f, 18f, 16f), dark);
        for (int i = 0; i < 40; i++)
        {
            Vector3 p = new Vector3(rng.Next(-120, 120), 0f, rng.Next(-120, 120));
            if (p.magnitude < 20f || (p - new Vector3(-50f, 0f, -30f)).magnitude < 30f) continue;
            float h = 1.5f + (float)rng.NextDouble() * 5f;
            PlaygroundBuilder.Box(world, $"Rock{i}", p + Vector3.up * h * 0.5f, new Vector3(2f + rng.Next(4), h, 2f + rng.Next(4)), dark);
        }

        PlaygroundBuilder.BuildPlayerRig(new Vector3(0f, 0.05f, -105f), d.id);
        RegionWeather rw = Object.FindFirstObjectByType<RegionWeather>();
        if (rw != null) Object.DestroyImmediate(rw.gameObject);
        var rc = new GameObject("Region").AddComponent<RegionController>();
        PlaygroundBuilder.Wire(rc, "region", region);

        foreach (WaystoneInfo w in region.waystones)
        {
            GameObject ws = PlaygroundBuilder.Primitive(PrimitiveType.Cylinder, "Waystone_" + w.id, world, w.position + Vector3.up * 1.5f, new Vector3(0.8f, 1.5f, 0.8f), stone);
            ws.AddComponent<Interactable>();
            var wsc = ws.AddComponent<Waystone>();
            var so = new SerializedObject(wsc);
            so.FindProperty("waystoneId").stringValue = w.id;
            so.FindProperty("displayName").stringValue = w.displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Camps: further from the entry = higher level.
        Vector3[] camps = { new Vector3(30f, 0f, -70f), new Vector3(-20f, 0f, -60f), new Vector3(70f, 0f, -20f), new Vector3(-90f, 0f, 10f),
                            new Vector3(40f, 0f, 30f), new Vector3(-40f, 0f, 60f), new Vector3(80f, 0f, 90f), new Vector3(-80f, 0f, 100f) };
        foreach (Vector3 c in camps)
        {
            float t = Mathf.InverseLerp(-105f, 120f, c.z);
            var spawner = new GameObject("Camp").AddComponent<EnemySpawner>();
            spawner.transform.SetParent(world, false);
            spawner.transform.position = c;
            var so = new SerializedObject(spawner);
            SerializedProperty types = so.FindProperty("types");
            types.arraySize = region.enemies.Count;
            for (int k = 0; k < region.enemies.Count; k++) types.GetArrayElementAtIndex(k).stringValue = region.enemies[k];
            so.FindProperty("count").intValue = 3;
            so.FindProperty("level").intValue = region.LevelAt(t * 0.6f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Roads: back (south) and forward (north); Ashfall's back road is the academy.
        if (index == 0)
        {
            GameObject back = PlaygroundBuilder.Primitive(PrimitiveType.Cylinder, "AcademyGate", world, new Vector3(-6f, 2f, -112f), new Vector3(3f, 0.12f, 3f), stone);
            back.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            back.AddComponent<Interactable>().Prompt = "Return to the academy";
            AcademyBuilder.SetPortal(back.AddComponent<Portal>(), "Academy", false, null);
        }
        foreach (string n in region.neighbours)
        {
            bool forward = System.Array.FindIndex(Regions, r => r.id == n) > index;
            GameObject road = PlaygroundBuilder.Primitive(PrimitiveType.Cube, "Road_" + n, world, new Vector3(forward ? 0f : 8f, 1.2f, forward ? 118f : -112f), new Vector3(1.2f, 2.4f, 0.4f), stone);
            road.AddComponent<Interactable>();
            var rs = road.AddComponent<RoadStone>();
            var so = new SerializedObject(rs);
            so.FindProperty("targetRegion").stringValue = n;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var spots = new GameObject("GateSpots").transform;
        for (int i = 0; i < region.gateSpots.Count; i++)
        {
            var spot = new GameObject($"GateSpot_{i}").AddComponent<GateSpot>();
            spot.transform.SetParent(spots, false);
            spot.transform.position = region.gateSpots[i];
            var so = new SerializedObject(spot);
            so.FindProperty("index").intValue = i;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        new GameObject("WorldGates").AddComponent<WorldGatesDriver>();
        if (d.id == "r7_throne")
        {
            PlaygroundBuilder.Box(world, "Throne", new Vector3(0f, 1.5f, 66f), new Vector3(4f, 3f, 4f), dark);
            var boss = new GameObject("ThroneBoss").AddComponent<Aethvirse.Endgame.ThroneBoss>();
            boss.transform.position = new Vector3(0f, 0.2f, 58f);
        }
        if (d.id == "r1_ashfall")
        {
            // Where the small gate opened eight years ago: the white ring of 03:xx.
            GameObject ring = PlaygroundBuilder.Primitive(PrimitiveType.Cylinder, "WhiteRing", world, new Vector3(-6f, 2.5f, 64f), new Vector3(3.5f, 0.1f, 3.5f), PlaygroundBuilder.MakeMaterial("WhiteRing", Color.white));
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var gateGo = new GameObject("HollowGate");
            gateGo.transform.position = new Vector3(-6f, 0f, 64f);
            gateGo.AddComponent<Interactable>();
            var hg = gateGo.AddComponent<Aethvirse.Endgame.HollowGate>();
            PlaygroundBuilder.Wire(hg, "ring", ring);
        }

        EditorSceneManager.SaveScene(scene, $"{SceneDir}/{d.scene}.unity");
    }

    public const string GatePath = "Assets/Scenes/Gate.unity";

    /// <summary>The Gate dungeon scene: the player rig and a DungeonRun that builds the map from the Gate's seed.</summary>
    static void BuildGateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PlaygroundBuilder.BuildPlayerRig(new Vector3(0f, 0.2f, 0f), "gate");
        var run = new GameObject("DungeonRun").AddComponent<Aethvirse.Gates.DungeonRun>();
        PlaygroundBuilder.Wire(run, "floorMat", PlaygroundBuilder.MakeMaterial("GateFloor", new Color(0.25f, 0.22f, 0.3f)));
        PlaygroundBuilder.Wire(run, "bridgeMat", PlaygroundBuilder.MakeMaterial("GateBridge", new Color(0.35f, 0.32f, 0.4f)));
        PlaygroundBuilder.Wire(run, "pillarMat", PlaygroundBuilder.MakeMaterial("GatePillar", new Color(0.45f, 0.4f, 0.5f)));
        PlaygroundBuilder.Wire(run, "secretMat", PlaygroundBuilder.MakeMaterial("GateSecret", new Color(0.5f, 0.3f, 0.7f)));
        PlaygroundBuilder.Wire(run, "portalMat", PlaygroundBuilder.MakeMaterial("ExpeditionGate", new Color(0.4f, 0.75f, 1f)));
        EditorSceneManager.SaveScene(scene, GatePath);
    }

    public const string UnknownPath = "Assets/Scenes/Regions/R0_Unknown.unity";

    /// <summary>[ ??? ]: a white void with one page to read and the way back.</summary>
    static void BuildUnknownScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Unknown").transform;
        PlaygroundBuilder.Box(root, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 60f), PlaygroundBuilder.MakeMaterial("Void", new Color(0.92f, 0.92f, 0.95f)));
        PlaygroundBuilder.BuildPlayerRig(new Vector3(0f, 0.05f, -20f), "r0_unknown");
        GameObject page = PlaygroundBuilder.Primitive(PrimitiveType.Cube, "LastPage", root, new Vector3(0f, 1.2f, 10f), new Vector3(1.2f, 1.6f, 0.1f), PlaygroundBuilder.MakeMaterial("Page", new Color(0.1f, 0.1f, 0.12f)));
        page.AddComponent<Interactable>();
        page.AddComponent<Aethvirse.Endgame.LastSeal>();
        GameObject back = PlaygroundBuilder.Primitive(PrimitiveType.Cylinder, "WayBack", root, new Vector3(0f, 2f, -26f), new Vector3(3f, 0.1f, 3f), PlaygroundBuilder.MakeMaterial("ExpeditionGate", new Color(0.4f, 0.75f, 1f)));
        back.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        back.AddComponent<Interactable>().Prompt = "Go back";
        AcademyBuilder.SetPortal(back.AddComponent<Portal>(), "", true, null);
        EditorSceneManager.SaveScene(scene, UnknownPath);
    }

    static void Walls(Transform parent, Vector3 center, float size, Material mat)
    {
        float h = 5f, t = 1f, half = size * 0.5f;
        PlaygroundBuilder.Box(parent, "TownWallN", center + new Vector3(0f, h * 0.5f, half), new Vector3(size, h, t), mat);
        PlaygroundBuilder.Box(parent, "TownWallW", center + new Vector3(-half, h * 0.5f, 0f), new Vector3(t, h, size), mat);
        PlaygroundBuilder.Box(parent, "TownWallE", center + new Vector3(half, h * 0.5f, 0f), new Vector3(t, h, size), mat);
        // South wall with a gate gap.
        PlaygroundBuilder.Box(parent, "TownWallS1", center + new Vector3(-half * 0.55f, h * 0.5f, -half), new Vector3(size * 0.45f, h, t), mat);
        PlaygroundBuilder.Box(parent, "TownWallS2", center + new Vector3(half * 0.55f, h * 0.5f, -half), new Vector3(size * 0.45f, h, t), mat);
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
