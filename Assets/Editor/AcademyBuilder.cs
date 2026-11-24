// Builds Assets/Scenes/Academy.unity (Aster Vey Academy hub, graybox) and Assets/Scenes/PenaltyZone.unity.
using System.Collections.Generic;
using Aethvirse.Dialog;
using Aethvirse.Interaction;
using Aethvirse.Quests;
using Aethvirse.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AcademyBuilder
{
    public const string AcademyPath = "Assets/Scenes/Academy.unity";
    public const string PenaltyPath = "Assets/Scenes/PenaltyZone.unity";

    [MenuItem("AETHVIRSE/Build Academy + Penalty Zone")]
    public static void Build()
    {
        BuildAcademy();
        BuildPenaltyZone();
        TitleSceneBuilder.RegisterScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("AETHVIRSE: academy and penalty zone built");
    }

    static void BuildAcademy()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Material stone = PlaygroundBuilder.MakeMaterial("AcademyStone", new Color(0.55f, 0.55f, 0.6f));
        Material wall = PlaygroundBuilder.MakeMaterial("AcademyWall", new Color(0.62f, 0.58f, 0.5f));
        Material roof = PlaygroundBuilder.MakeMaterial("AcademyRoof", new Color(0.35f, 0.3f, 0.45f));
        Material grass = PlaygroundBuilder.MakeMaterial("AcademyGrass", new Color(0.35f, 0.5f, 0.32f));
        Material gate = PlaygroundBuilder.MakeMaterial("ExpeditionGate", new Color(0.4f, 0.75f, 1f));
        Material dummy = PlaygroundBuilder.MakeMaterial("Dummy", new Color(0.72f, 0.55f, 0.36f));
        var world = new GameObject("Academy").transform;

        PlaygroundBuilder.Box(world, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(120f, 1f, 120f), grass);
        PlaygroundBuilder.Box(world, "Courtyard", new Vector3(0f, 0.01f, -2f), new Vector3(30f, 0.02f, 26f), stone);
        // Buildings (open front so you can walk in): classroom north, library west, dorm east.
        Building(world, "Classroom", new Vector3(0f, 0f, 16f), new Vector3(16f, 6f, 10f), wall, roof);
        Building(world, "Library", new Vector3(-22f, 0f, 4f), new Vector3(10f, 7f, 14f), wall, roof, openSide: Vector3.right);
        Building(world, "Dormitory", new Vector3(22f, 0f, 4f), new Vector3(10f, 5f, 14f), wall, roof, openSide: Vector3.left);
        // Training ground south with dummies; the expedition gate beyond it.
        PlaygroundBuilder.Box(world, "TrainingGround", new Vector3(0f, 0.02f, -26f), new Vector3(22f, 0.02f, 14f), PlaygroundBuilder.MakeMaterial("Arena", new Color(0.36f, 0.33f, 0.33f)));
        PlaygroundBuilder.BuildDummies(dummy, new Vector3(-4f, 0f, -30f));

        PlaygroundBuilder.BuildPlayerRig(new Vector3(0f, 0.05f, -6f), "academy");

        Npc(world, "horne", new Vector3(0f, 0f, 12.5f), 180f);
        Npc(world, "ilsveth", new Vector3(5f, 0f, 13f), 200f);
        Npc(world, "nyra", new Vector3(-18f, 0f, 4f), 90f);
        Npc(world, "kaelen", new Vector3(6f, 0f, -24f), 270f);
        Npc(world, "quartermaster", new Vector3(10f, 0f, -6f), 240f);

        // Dorm bed: save + rest; the dorm area counts for the first quest.
        GameObject bed = PlaygroundBuilder.Primitive(PrimitiveType.Cube, "DormBed", world, new Vector3(23f, 0.35f, 6f), new Vector3(2f, 0.7f, 1f), PlaygroundBuilder.MakeMaterial("Bed", new Color(0.6f, 0.65f, 0.8f)));
        bed.AddComponent<Interactable>().Prompt = "Sleep (save, rest until 08:00)";
        bed.AddComponent<SaveCrystal>();
        bed.AddComponent<RestSpot>();
        Area(world, "academy_dorm", new Vector3(22f, 2f, 4f), new Vector3(10f, 6f, 14f));
        Area(world, "academy", Vector3.zero, new Vector3(120f, 40f, 120f));

        GameObject board = PlaygroundBuilder.Primitive(PrimitiveType.Cube, "QuestBoard", world, new Vector3(-7f, 1.3f, -8f), new Vector3(2f, 1.4f, 0.15f), PlaygroundBuilder.MakeMaterial("Wood", new Color(0.5f, 0.36f, 0.22f)));
        board.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
        board.AddComponent<Interactable>().Prompt = "Check the quest board";
        var qb = board.AddComponent<QuestBoard>();
        var so = new SerializedObject(qb);
        SerializedProperty list = so.FindProperty("quests");
        string[] ids = { "q_bounty_hounds" };
        list.arraySize = ids.Length;
        for (int i = 0; i < ids.Length; i++) list.GetArrayElementAtIndex(i).stringValue = ids[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject expedition = PlaygroundBuilder.Primitive(PrimitiveType.Cylinder, "ExpeditionGate", world, new Vector3(0f, 2f, -44f), new Vector3(4f, 0.15f, 4f), gate);
        expedition.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        expedition.AddComponent<Interactable>().Prompt = "Expedition Gate: Ashfall Outskirts";
        SetPortal(expedition.AddComponent<Portal>(), "Playground", false, null);

        EditorSceneManager.SaveScene(scene, AcademyPath);
    }

    static void BuildPenaltyZone()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Material floor = PlaygroundBuilder.MakeMaterial("PenaltyFloor", new Color(0.18f, 0.05f, 0.08f));
        Material wall = PlaygroundBuilder.MakeMaterial("PenaltyWall", new Color(0.35f, 0.08f, 0.12f));
        var world = new GameObject("PenaltyZone").transform;
        PlaygroundBuilder.Box(world, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), floor);
        for (int i = 0; i < 4; i++)
        {
            Quaternion r = Quaternion.Euler(0f, i * 90f, 0f);
            GameObject w = PlaygroundBuilder.Box(world, $"Wall{i}", r * new Vector3(0f, 2f, 20f), new Vector3(40f, 4f, 1f), wall);
            w.transform.rotation = r;
        }
        PlaygroundBuilder.BuildPlayerRig(new Vector3(0f, 0.05f, 0f), "penalty_zone");

        GameObject exit = PlaygroundBuilder.Primitive(PrimitiveType.Cylinder, "ExitPortal", world, new Vector3(0f, 1.5f, 8f), new Vector3(3f, 0.1f, 3f), PlaygroundBuilder.MakeMaterial("ExpeditionGate", new Color(0.4f, 0.75f, 1f)));
        exit.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        exit.AddComponent<Interactable>().Prompt = "Leave the Penalty Zone";
        SetPortal(exit.AddComponent<Portal>(), "", true, null);

        var zone = new GameObject("Penalty").AddComponent<PenaltyZone>();
        var so = new SerializedObject(zone);
        SerializedProperty prefabs = so.FindProperty("enemyPrefabs");
        prefabs.arraySize = 1;
        prefabs.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PlaygroundBuilder.AshHoundPrefabPath);
        so.FindProperty("exitPortal").objectReferenceValue = exit;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, PenaltyPath);
    }

    static void Building(Transform parent, string name, Vector3 center, Vector3 size, Material wall, Material roof, Vector3? openSide = null)
    {
        Vector3 open = openSide ?? Vector3.back;
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.position = center;
        float t = 0.4f;
        // Four walls (the open side gets a doorway: two short pieces), then the roof.
        foreach (Vector3 side in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
        {
            bool alongX = side.z != 0f;
            float length = alongX ? size.x : size.z;
            Vector3 offset = new Vector3(side.x * size.x * 0.5f, size.y * 0.5f, side.z * size.z * 0.5f);
            Vector3 wallSize = alongX ? new Vector3(length, size.y, t) : new Vector3(t, size.y, length);
            if (side == open)
            {
                float piece = (length - 4f) * 0.5f;
                Vector3 along = alongX ? Vector3.right : Vector3.forward;
                Vector3 pieceSize = alongX ? new Vector3(piece, size.y, t) : new Vector3(t, size.y, piece);
                PlaygroundBuilder.Box(root, $"{name}_WallA", center + offset + along * (2f + piece * 0.5f), pieceSize, wall);
                PlaygroundBuilder.Box(root, $"{name}_WallB", center + offset - along * (2f + piece * 0.5f), pieceSize, wall);
            }
            else PlaygroundBuilder.Box(root, $"{name}_Wall", center + offset, wallSize, wall);
        }
        PlaygroundBuilder.Box(root, $"{name}_Roof", center + Vector3.up * (size.y + 0.2f), new Vector3(size.x + 1f, 0.4f, size.z + 1f), roof);
    }

    static void Npc(Transform parent, string id, Vector3 at, float yaw)
    {
        NpcData data = QuestDataBuilder.LoadNpc(id);
        var root = new GameObject("Npc_" + id).transform;
        root.SetParent(parent, false);
        root.position = at;
        root.rotation = Quaternion.Euler(0f, yaw, 0f);
        GameObject body = PlaygroundBuilder.Primitive(PrimitiveType.Capsule, "Body", root, new Vector3(0f, 1f, 0f), Vector3.one, PlaygroundBuilder.MakeMaterial("Npc_" + id, data.bodyColor));
        root.gameObject.AddComponent<Interactable>();
        var npc = root.gameObject.AddComponent<Npc>();
        PlaygroundBuilder.Wire(npc, "data", data);
        PlaygroundBuilder.Wire(npc, "body", body);
    }

    static void Area(Transform parent, string id, Vector3 at, Vector3 size)
    {
        var area = new GameObject("Area_" + id).AddComponent<AreaTrigger>();
        area.transform.SetParent(parent, false);
        area.transform.position = at;
        var so = new SerializedObject(area);
        so.FindProperty("areaId").stringValue = id;
        so.FindProperty("size").vector3Value = size;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetPortal(Portal portal, string scene, bool returnToPrevious, Vector3? spawn)
    {
        var so = new SerializedObject(portal);
        so.FindProperty("targetScene").stringValue = scene;
        so.FindProperty("returnToPrevious").boolValue = returnToPrevious;
        so.FindProperty("useSpawnPosition").boolValue = spawn.HasValue;
        if (spawn.HasValue) so.FindProperty("spawnPosition").vector3Value = spawn.Value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
