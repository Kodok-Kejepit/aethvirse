using Aethvirse.Core;
using Aethvirse.Stats;
using Aethvirse.UI;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>Puts a scene into its region: session region + weather table, entry message, level warning.</summary>
    public sealed class RegionController : MonoBehaviour
    {
        [SerializeField] RegionData region;

        public RegionData Region => region;
        public static RegionData Current { get; private set; }

        void Awake()
        {
            Current = region;
            GameSession s = GameSession.Instance;
            s.WeatherTable = region.weather;
            s.Region = region.id;
            s.Weather.FitTo(region.weather, s.Clock.totalMinutes);
        }

        void Start()
        {
            Codex.Notify($"{region.displayName}  ·  Lv {region.levelMin}–{region.levelMax}", CodexTone.System);
            GameEvents.RaiseAreaEntered(region.id);
            int level = PlayerStats.Current != null ? PlayerStats.Current.Progression.level : 1;
            if (level < region.levelMin - 10) Codex.Notify("[ WARNING ] The monsters here are far stronger than you.", CodexTone.Warning);
        }

        void OnDestroy()
        {
            if (Current == region) Current = null;
        }
    }
}
