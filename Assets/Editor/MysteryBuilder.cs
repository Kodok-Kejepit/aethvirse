// Places the cipher lock and its mural into every region scene (after RegionBuilder and SecretDataBuilder).
using Aethvirse.Interaction;
using Aethvirse.Secrets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MysteryBuilder
{
    [MenuItem("AETHVIRSE/Build Mysteries (cipher locks)")]
    public static void Build()
    {
        var atlas = AssetDatabase.LoadAssetAtPath<Aethvirse.World.WorldAtlas>("Assets/Resources/WorldAtlas.asset");
        int placed = 0;
        // Scene names first: opening a scene unloads the loose atlas objects.
        var scenes = new System.Collections.Generic.Dictionary<string, string>();
        var origins = new System.Collections.Generic.Dictionary<string, Vector3>();
        foreach (MysteryContent.CipherDef c in MysteryContent.Ciphers)
        {
            var region = atlas.Region(c.region);
            if (region != null && !string.IsNullOrEmpty(region.scene)) { scenes[c.region] = region.scene; origins[c.region] = region.origin; }
        }
        foreach (MysteryContent.CipherDef c in MysteryContent.Ciphers)
        {
            if (!scenes.TryGetValue(c.region, out string sceneName)) continue;
            var scene = EditorSceneManager.OpenScene($"{RegionBuilder.SceneDir}/{sceneName}.unity", OpenSceneMode.Single);
            GameObject old = GameObject.Find("Mysteries");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Mysteries").transform;
            root.position = origins[c.region];

            GameObject lockGo = PlaygroundBuilder.Primitive(PrimitiveType.Cube, "CipherLock", root, c.lockAt + Vector3.up * 1f, new Vector3(1.4f, 2f, 0.6f),
                PlaygroundBuilder.MakeMaterial("CipherLock", new Color(0.3f, 0.32f, 0.4f)));
            lockGo.AddComponent<Interactable>();
            var lk = lockGo.AddComponent<CipherLock>();
            var so = new SerializedObject(lk);
            so.FindProperty("lockId").stringValue = c.region;
            so.FindProperty("answer").stringValue = c.answer;
            so.FindProperty("inscription").stringValue = MysteryContent.Inscription(c);
            so.FindProperty("rewardItem").stringValue = c.reward;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject mural = PlaygroundBuilder.Primitive(PrimitiveType.Cube, "Mural", root, c.muralAt + new Vector3(0f, 2f, -1.2f), new Vector3(4f, 2.6f, 0.1f),
                PlaygroundBuilder.MakeMaterial("Mural", new Color(0.55f, 0.45f, 0.4f)));
            mural.AddComponent<Interactable>().Prompt = "Look at the mural";
            var nb = mural.AddComponent<NoticeBoard>();
            var mso = new SerializedObject(nb);
            mso.FindProperty("text").stringValue = c.mural;
            mso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene);
            placed++;
        }
        Debug.Log($"AETHVIRSE: mysteries built ({placed} cipher locks)");
    }
}
