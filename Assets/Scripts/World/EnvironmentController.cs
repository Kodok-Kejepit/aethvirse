using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>
    /// Day/night and weather on the scene (graybox): the sun follows the clock, light and ambient dim at
    /// night, fog thickens with fog / rain / storms. Real skies and weather VFX come in Phase 2.
    /// </summary>
    public sealed class EnvironmentController : MonoBehaviour
    {
        [SerializeField] Light sun;
        [SerializeField] float dayIntensity = 1.3f;
        [SerializeField] float nightIntensity = 0.12f;
        [SerializeField] Color daySky = new Color(0.55f, 0.62f, 0.75f);
        [SerializeField] Color nightSky = new Color(0.08f, 0.1f, 0.2f);
        [SerializeField] float smoothing = 1.5f;

        float _fogTarget;

        void Update()
        {
            GameSession s = GameSession.Instance;
            float f = s.Clock.DayFraction;
            // Sun: below the horizon at midnight, overhead at noon.
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(f * 360f - 90f, -30f, 0f);
                float daylight = Mathf.Clamp01(Mathf.Sin(f * Mathf.PI * 2f - Mathf.PI * 0.5f) * 1.5f + 0.4f);
                float weatherDim = s.Weather.current == WeatherKind.Storm ? 0.45f : s.Weather.IsWet || s.Weather.current == WeatherKind.Fog ? 0.7f : 1f;
                sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, daylight) * weatherDim;
                RenderSettings.ambientSkyColor = Color.Lerp(nightSky, daySky, daylight) * Mathf.Lerp(0.7f, 1f, weatherDim);
            }
            _fogTarget = s.Weather.current switch
            {
                WeatherKind.Fog => 0.045f,
                WeatherKind.Storm => 0.03f,
                WeatherKind.Rain => 0.015f,
                WeatherKind.Sandstorm => 0.05f,
                WeatherKind.Snow => 0.02f,
                WeatherKind.Ashfall => 0.025f,
                _ => 0.004f,
            };
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, _fogTarget, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            RenderSettings.fogColor = Color.Lerp(nightSky, daySky, s.Clock.IsNight ? 0.2f : 0.9f);
        }
    }
}
