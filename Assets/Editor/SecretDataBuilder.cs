// Writes the Mystery Journal (Resources/SecretDatabase): 5 secrets per region with two hints each, and places their
// spots, puzzle pieces, cracked walls and hint notes into the region scenes. Text stays cryptic (public repo).
using System.Collections.Generic;
using System.IO;
using Aethvirse.Combat;
using Aethvirse.Interaction;
using Aethvirse.Quests;
using Aethvirse.Secrets;
using Aethvirse.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SecretDataBuilder
{
    const string Dir = "Assets/Data/Secrets";

    enum Kind { Night, Weather, Puzzle, Wall, Bring, Dawn, StormNight }

    struct S
    {
        public string id, title, hint1, hint2;
        public Kind kind;
        public Vector3 at;
        public WeatherKind weather;
        public string item;
        public SecretCategory category;
        /// <summary>Puzzle bell order (0 low, 1 middle, 2 high).</summary>
        public int[] order;
    }

    [MenuItem("AETHVIRSE/Build Secrets (journal)")]
    public static void Build()
    {
        Directory.CreateDirectory(Dir);
        var db = LoadOrCreate<SecretDatabase>("Assets/Resources/SecretDatabase.asset");
        db.secrets = new List<SecretData>();
        _bookIndex = 0;
        _runeIndex = 0;
        db.pages = new List<SecretDatabase.Page>();
        var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>("Assets/Resources/WorldAtlas.asset");
        var regions = new List<RegionData>(atlas.AllRegions());
        var placements = new List<(string scene, List<S> list)>();
        for (int i = 0; i < regions.Count; i++)
        {
            RegionData r = regions[i];
            if (r.hidden || string.IsNullOrEmpty(r.scene)) continue;
            List<S> list = i == 0 ? Ashfall() : Generic(r, i);
            list.AddRange(Extras(r));
            db.pages.Add(new SecretDatabase.Page { region = r.id, synopsis = Synopsis(r.id) });
            foreach (S s in list) db.secrets.Add(Secret(r.id, s));
            // The road on (same world) needs this page full (STORY: mist until the journal page is complete).
            if (i + 1 < regions.Count && regions[i + 1].neighbours.Contains(r.id))
            {
                regions[i + 1].requiredFlag = SecretDatabase.PageFullFlag(r.id);
                EditorUtility.SetDirty(regions[i + 1]);
            }
            placements.Add((r.scene, list));
        }
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        // Opening scenes unloads loose asset objects, so placing comes after everything is saved.
        foreach (var (scene, list) in placements) Place(scene, list);
        Debug.Log($"AETHVIRSE: secrets built ({db.secrets.Count})");
    }

    static string Synopsis(string region) => region switch
    {
        "r1_ashfall" => "The ash remembers what the city forgot. Something here is still breathing.",
        "r2_verdant" => "Green, quiet, patient. The valley is waiting for someone to notice.",
        "r3_dunes" => "The sand moves when no wind blows. Old doors sleep under it.",
        "r4_frostveil" => "Cold keeps things. Ice keeps them longest.",
        "r5_archive" => "Written, then drowned. The tide reads what the people forgot.",
        "r6_fracture" => "The land broke here once. The pieces have not stopped listening.",
        "r7_throne" => "An empty seat. A name nobody says.",
        "s1_glasswood" => "Every tree is a window. Some of them look back.",
        "s2_mirrorsea" => "The water shows a sky that is not this one.",
        "s3_choir" => "They sang until something told them to stop.",
        "k1_cinder" => "Another world. The same hand burned it.",
        "k2_hollow_moon" => "They hid inside their moon. It was not deep enough.",
        "k3_last_city" => "The last ones wrote on the walls. They wrote your name.",
        "z1_seat" => "Every line ends here. Or starts.",
        _ => "A page with no title yet.",
    };

    static List<S> Ashfall() => new List<S>
    {
        new S { id = "r1_drain_breath", category = SecretCategory.Lore, kind = Kind.Weather, weather = WeatherKind.Rain, at = new Vector3(-38f, 0f, -20f),
            title = "The drain breathes when it rains.", hint1 = "Town gossip: 'Don't stand by the drain in the rain. It breathes.'", hint2 = "A child's note: 'The big drain sighs when the sky cries.'" },
        new S { id = "r1_bread_seller", category = SecretCategory.SealedMission, kind = Kind.Bring, item = "bread", at = new Vector3(58f, 0f, 20f),
            title = "The bread seller was waiting for you.", hint1 = "A villager: 'The old stall by the huts — someone still leaves bread there.'", hint2 = "Scratched on the stall: 'Bring one back. Just one.'" },
        new S { id = "r1_small_gate", category = SecretCategory.Room, kind = Kind.Night, at = new Vector3(0f, 0f, 62f),
            title = "A small gate opened here eight years ago.", hint1 = "An old report: 'Eight years ago a small gate opened by the big ruin. Gone by morning.'", hint2 = "A survivor: 'It only feels warm there at night.'" },
        new S { id = "r1_fading_seal", category = SecretCategory.Anomaly, kind = Kind.Puzzle, at = new Vector3(10f, 0f, 48f),
            title = "Something hums quieter every second.", hint1 = "Three old bells by the ruin. A rhyme: 'Low, then high, then the one between.'", hint2 = "The Codex flickers: 'L... H... M...'" },
        new S { id = "r1_ash_room", category = SecretCategory.Chest, kind = Kind.Wall, at = new Vector3(-72f, 0f, -30f),
            title = "A room the ash closed off.", hint1 = "A crack in the town's west wall whistles.", hint2 = "A guard: 'That wall's hollow. Hit it hard enough and you'll hear.'" },
    };

    static List<S> Generic(RegionData r, int i)
    {
        string n = r.displayName;
        WeatherKind w = r.weather.entries.Count > 1 ? r.weather.entries[1].kind : WeatherKind.Fog;
        return new List<S>
        {
            new S { id = r.id + "_night", category = SecretCategory.Room, kind = Kind.Night, at = new Vector3(-30f + i * 3f, 0f, 70f),
                title = $"What {n} shows only at night.", hint1 = $"'In {n} the landmark's shadow points somewhere after dark.'", hint2 = "'Wait for the moon. Then look again.'" },
            new S { id = r.id + "_weather", category = SecretCategory.Lore, kind = Kind.Weather, weather = w, at = new Vector3(45f, 0f, -45f),
                title = $"What the {w} uncovers.", hint1 = $"'When {w} comes, the stones east of the entry change.'", hint2 = "'Some doors only open for bad weather.'" },
            new S { id = r.id + "_bells", category = SecretCategory.Puzzle, kind = Kind.Puzzle, at = new Vector3(-10f, 0f, 30f),
                title = "Three notes, in the right order.", hint1 = "'High, then low, then the middle one.'", hint2 = "Marks under the bells: H · L · M." },
            new S { id = r.id + "_wall", category = SecretCategory.Chest, kind = Kind.Wall, at = new Vector3(-72f, 0f, -30f),
                title = "Behind the hollow wall.", hint1 = "'The town's west wall sounds hollow.'", hint2 = "'Heavy blows open what keys cannot.'" },
            new S { id = r.id + "_offering", category = SecretCategory.SealedMission, kind = Kind.Bring, item = "herb", at = new Vector3(64f, 0f, 30f),
                title = "An offering was accepted.", hint1 = "'The villagers leave herbs at the circle of huts.'", hint2 = "'Three herbs. No more, no less.'" },
        };
    }

    /// <summary>Four more per region (dawn, storm at night, a second bell puzzle, a second hollow wall).</summary>
    static List<S> Extras(RegionData r)
    {
        bool storm = r.weather.entries.Exists(e => e.kind == WeatherKind.Storm);
        WeatherKind w = storm ? WeatherKind.Storm : r.weather.entries[r.weather.entries.Count - 1].kind;
        return new List<S>
        {
            new S { id = r.id + "_dawn", category = SecretCategory.Lore, kind = Kind.Dawn, at = new Vector3(-95f, 0f, -80f),
                title = "What the first light shows.", hint1 = "'At dawn the far south-west corner glows for a moment.'", hint2 = "'Five o'clock. Not four, not seven.'" },
            new S { id = r.id + "_stormnight", category = SecretCategory.Anomaly, kind = Kind.StormNight, weather = w, at = new Vector3(95f, 0f, 0f),
                title = "It only walks in the worst nights.", hint1 = $"'On a {w} night, something stands by the east edge.'", hint2 = "'Night and bad weather together. Never one alone.'" },
            new S { id = r.id + "_bells2", category = SecretCategory.Puzzle, kind = Kind.Puzzle, order = new[] { 1, 2, 0 }, at = new Vector3(30f, 0f, 55f),
                title = "A second song, near the landmark.", hint1 = "'Middle, high, low — the old song.'", hint2 = "Scratches by the landmark: M · H · L." },
            new S { id = r.id + "_wall2", category = SecretCategory.Chest, kind = Kind.Wall, at = new Vector3(100f, 0f, -100f),
                title = "A lonely wall in the south-east.", hint1 = "'Why is there a wall in the middle of nowhere, south-east?'", hint2 = "'It cracks if you swing heavy.'" },
        };
    }

    static int _bookIndex, _runeIndex;

    /// <summary>
    /// Puzzles and storm-night secrets teach the secret skills (in world order, so the rarest books are deepest);
    /// hollow walls hold runes; the rest give an identification scroll or a mystery box.
    /// </summary>
    static string RewardItem(S s)
    {
        var db = AssetDatabase.LoadAssetAtPath<Aethvirse.Skills.SecretSkillDatabase>("Assets/Resources/SecretSkillDatabase.asset");
        if (db != null && (s.kind == Kind.Puzzle || s.kind == Kind.StormNight) && _bookIndex < db.skills.Count)
            return "book_" + db.skills[_bookIndex++].id;
        if (db != null && s.kind == Kind.Wall && db.runes.Count > 0)
            return db.runes[_runeIndex++ % db.runes.Count].id;
        return s.kind == Kind.Bring || s.kind == Kind.Dawn ? "mystery_box" : "codex_identify_scroll";
    }

    static SecretData Secret(string region, S s)
    {
        var d = LoadOrCreate<SecretData>($"{Dir}/{s.id}.asset");
        d.id = s.id; d.region = region; d.category = s.category; d.title = s.title;
        d.hints = new List<string> { s.hint1, s.hint2 };
        d.conditions = new List<Condition>();
        d.foundByFlag = "";
        switch (s.kind)
        {
            case Kind.Night: d.conditions.Add(new Condition { type = ConditionType.IsNight }); break;
            case Kind.Dawn: d.conditions.Add(new Condition { type = ConditionType.HourRange, value = 5f, value2 = 6f }); break;
            case Kind.StormNight:
                d.conditions.Add(new Condition { type = ConditionType.IsNight });
                d.conditions.Add(new Condition { type = ConditionType.Weather, values = new List<string> { s.weather.ToString() } });
                break;
            case Kind.Weather: d.conditions.Add(new Condition { type = ConditionType.Weather, values = new List<string> { s.weather.ToString() } }); break;
            case Kind.Bring: d.conditions.Add(new Condition { type = ConditionType.HasItem, key = s.item, value = s.item == "herb" ? 3 : 1 }); break;
            case Kind.Puzzle: d.foundByFlag = s.id + "_solved"; break;
            case Kind.Wall: d.foundByFlag = s.id + "_broken"; break;
        }
        d.rewards = new List<Reward>
        {
            new Reward { type = RewardType.AcademyPoints, amount = 15 },
            new Reward { type = RewardType.Item, key = RewardItem(s), amount = 1 },
            new Reward { type = RewardType.Xp, amount = 120 },
        };
        d.tier = s.category == SecretCategory.SealedMission ? 2 : s.kind == Kind.Puzzle ? 1 : 0;
        EditorUtility.SetDirty(d);
        return d;
    }

    /// <summary>Puts the spots, puzzle bells, cracked walls and two hint notes per secret into the region scene.</summary>
    static void Place(string sceneName, List<S> list)
    {
        string path = $"{RegionBuilder.SceneDir}/{sceneName}.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        GameObject old = GameObject.Find("Secrets");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Secrets").transform;
        Material bell = PlaygroundBuilder.MakeMaterial("Bell", new Color(0.8f, 0.7f, 0.3f));
        Material crack = PlaygroundBuilder.MakeMaterial("CrackedWall", new Color(0.35f, 0.3f, 0.28f));
        Material note = PlaygroundBuilder.MakeMaterial("Note", new Color(0.9f, 0.88f, 0.8f));
        int k = 0;
        foreach (S s in list)
        {
            if (s.kind == Kind.Puzzle)
            {
                var puzzle = new GameObject("Puzzle_" + s.id).AddComponent<PuzzleSequence>();
                puzzle.transform.SetParent(root, false);
                var so = new SerializedObject(puzzle);
                SerializedProperty pieces = so.FindProperty("pieces");
                pieces.arraySize = 3;
                string[] names = { "Low", "Middle", "High" };
                for (int b = 0; b < 3; b++)
                {
                    GameObject go = PlaygroundBuilder.Primitive(PrimitiveType.Cylinder, "Bell_" + names[b], root, s.at + new Vector3(b * 3f - 3f, 1f + b * 0.3f, 0f), new Vector3(0.8f, 0.6f + b * 0.2f, 0.8f), bell);
                    var it = go.AddComponent<Interactable>();
                    it.Prompt = $"Ring the {names[b].ToLower()} bell";
                    pieces.GetArrayElementAtIndex(b).objectReferenceValue = it;
                }
                // Ashfall: low, high, middle; elsewhere: high, low, middle (matches the hints).
                int[] order = s.order ?? (s.id.StartsWith("r1_") ? new[] { 0, 2, 1 } : new[] { 2, 0, 1 });
                SerializedProperty ord = so.FindProperty("order");
                ord.arraySize = 3;
                for (int b = 0; b < 3; b++) ord.GetArrayElementAtIndex(b).intValue = order[b];
                so.FindProperty("solvedFlag").stringValue = s.id + "_solved";
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (s.kind == Kind.Wall)
            {
                GameObject wall = PlaygroundBuilder.Box(root, "Cracked_" + s.id, s.at + new Vector3(0f, 1.5f, 0f), new Vector3(1.5f, 3f, 4f), crack);
                var breakable = wall.AddComponent<BreakableWall>();
                var so = new SerializedObject(breakable);
                so.FindProperty("brokenFlag").stringValue = s.id + "_broken";
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                var spot = new GameObject("Spot_" + s.id).AddComponent<SecretSpot>();
                spot.transform.SetParent(root, false);
                spot.transform.position = s.at;
                var so = new SerializedObject(spot);
                so.FindProperty("secretId").stringValue = s.id;
                so.FindProperty("interact").boolValue = s.kind == Kind.Bring;
                so.FindProperty("prompt").stringValue = s.kind == Kind.Bring ? "Leave something here" : "Look closer";
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // Two hint notes somewhere else in the region.
            for (int h = 0; h < 2; h++)
            {
                Vector3 p = new Vector3(-20f + 9f * k, 0f, h == 0 ? -92f : -40f + 6f * k);
                GameObject n = PlaygroundBuilder.Primitive(PrimitiveType.Cube, $"Note_{s.id}_{h}", root, p + Vector3.up * 1f, new Vector3(0.6f, 0.8f, 0.08f), note);
                n.AddComponent<Interactable>().Prompt = "Read the note";
                var nb = n.AddComponent<NoticeBoard>();
                var so = new SerializedObject(nb);
                so.FindProperty("text").stringValue = h == 0 ? s.hint1 : s.hint2;
                so.FindProperty("hintSecret").stringValue = s.id;
                so.FindProperty("hintIndex").intValue = h;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            k++;
        }
        EditorSceneManager.SaveScene(scene);
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
