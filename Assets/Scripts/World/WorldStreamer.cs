using System.Collections.Generic;
using Aethvirse.Player;
using Aethvirse.Quests;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.World
{
    /// <summary>
    /// Seamless world (§7): the regions of one world sit side by side. Walking toward a neighbour streams its scene in
    /// next to yours (no loading screen); walking away unloads it again. Regions behind the mist (journal / rank) are
    /// not streamed — the mist wall stops you instead.
    /// </summary>
    public sealed class WorldStreamer : MonoBehaviour
    {
        /// <summary>Distance from a neighbour's border at which it starts streaming in / is dropped again.</summary>
        public const float LoadDistance = 90f;
        public const float UnloadDistance = 160f;
        /// <summary>Region squares: spacing between region centres along the road.</summary>
        public const float Spacing = 300f;

        static WorldStreamer _instance;
        static readonly HashSet<string> Pending = new HashSet<string>();
        float _timer;

        public static bool IsStreaming(string scene) => Pending.Contains(scene);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("WorldStreamer");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<WorldStreamer>();
        }

        /// <summary>Distance from a point to a region's square (0 inside).</summary>
        public static float DistanceTo(RegionData r, Vector3 p)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(p.x - r.origin.x) - Spacing * 0.5f);
            float dz = Mathf.Max(0f, Mathf.Abs(p.z - r.origin.z) - Spacing * 0.5f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 0.4f;
            RegionData current = RegionController.Current;
            Transform p = PlayerTarget.Current;
            WorldAtlas atlas = WorldAtlas.Instance;
            if (current == null || p == null || atlas == null) return;
            GameSession s = GameSession.Instance;
            int rank = AcademyRank.For(s.AcademyPoints);

            foreach (string id in current.neighbours)
            {
                RegionData n = atlas.Region(id);
                if (n == null || string.IsNullOrEmpty(n.scene) || Pending.Contains(n.scene) || SceneManager.GetSceneByName(n.scene).isLoaded) continue;
                if (!WorldAtlas.IsOpen(n, rank, s.Flags) || DistanceTo(n, p.position) > LoadDistance) continue;
                Pending.Add(n.scene);
                AsyncOperation op = SceneManager.LoadSceneAsync(n.scene, LoadSceneMode.Additive);
                if (op == null) { Pending.Remove(n.scene); continue; }
                string name = n.scene;
                op.completed += _ => Pending.Remove(name);
            }

            Scene active = SceneManager.GetActiveScene();
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded || sc == active || Pending.Contains(sc.name)) continue;
                RegionData r = atlas.RegionForScene(sc.name);
                if (r != null && DistanceTo(r, p.position) > UnloadDistance) SceneManager.UnloadSceneAsync(sc);
            }
        }
    }
}
