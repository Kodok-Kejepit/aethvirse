using Aethvirse.Core;
using Aethvirse.Player;
using Aethvirse.Stats;
using Aethvirse.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.World
{
    /// <summary>Puts a scene into its region: session region + weather table, entry message, level warning.</summary>
    public sealed class RegionController : MonoBehaviour
    {
        [SerializeField] RegionData region;

        public RegionData Region => region;
        public static RegionData Current { get; private set; }

        bool _streamed;

        void Awake()
        {
            // Streamed in next to the current region: wait until the player walks in.
            _streamed = WorldStreamer.IsStreaming(gameObject.scene.name);
            if (!_streamed) Apply();
        }

        void Start()
        {
            if (!_streamed) Announce();
        }

        /// <summary>The player crossed into this region's square: it becomes the current region and takes the rig.</summary>
        void Update()
        {
            if (Current == region) return;
            Transform p = PlayerTarget.Current;
            if (p == null || WorldStreamer.DistanceTo(region, p.position) > 0f) return;
            Apply();
            SceneManager.SetActiveScene(gameObject.scene);
            foreach (SceneRig rig in FindObjectsByType<SceneRig>(FindObjectsSortMode.None))
                if (rig.transform.parent == null && rig.gameObject.scene != gameObject.scene) SceneManager.MoveGameObjectToScene(rig.gameObject, gameObject.scene);
            Announce();
        }

        void Apply()
        {
            Current = region;
            GameSession s = GameSession.Instance;
            s.WeatherTable = region.weather;
            s.Region = region.id;
            s.Weather.FitTo(region.weather, s.Clock.totalMinutes);
        }

        void Announce()
        {
            Codex.Notify($"{region.displayName}  ·  Lv {region.levelMin}–{region.levelMax}", CodexTone.System);
            GameEvents.RaiseAreaEntered(region.id);
            int level = PlayerStats.Current != null ? PlayerStats.Current.Progression.level : 1;
            if (level < region.levelMin - 10) Codex.Notify("[ WARNING ] The monsters here are far stronger than you.", CodexTone.Warning);
        }

        /// <summary>The region of the scene an object lives in (streamed scenes keep their own).</summary>
        public static RegionData In(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var rc = root.GetComponent<RegionController>();
                if (rc != null) return rc.region;
            }
            return null;
        }

        void OnDestroy()
        {
            if (Current == region) Current = null;
        }
    }
}
