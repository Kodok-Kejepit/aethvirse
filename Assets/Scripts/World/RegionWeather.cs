using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>Gives the session this scene's weather table and region id (region data takes over in P1-8).</summary>
    public sealed class RegionWeather : MonoBehaviour
    {
        [SerializeField] string regionId = "r1_ashfall";
        [SerializeField] WeatherTable table = new WeatherTable
        {
            entries = new System.Collections.Generic.List<WeatherTable.Entry>
            {
                new WeatherTable.Entry { kind = WeatherKind.Clear, weight = 5f },
                new WeatherTable.Entry { kind = WeatherKind.Rain, weight = 2f },
                new WeatherTable.Entry { kind = WeatherKind.Fog, weight = 1.5f },
                new WeatherTable.Entry { kind = WeatherKind.Storm, weight = 0.7f },
                new WeatherTable.Entry { kind = WeatherKind.Ashfall, weight = 1f },
            },
        };

        void Awake()
        {
            GameSession s = GameSession.Instance;
            s.WeatherTable = table;
            s.Region = regionId;
            s.Weather.FitTo(table, s.Clock.totalMinutes);
        }
    }
}
