using System;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>
    /// Runtime world state that lives across scenes (Godot GameState): clock, weather, flags, gold,
    /// play time, current region. Created on demand and kept with DontDestroyOnLoad.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        static GameSession _instance;

        public static GameSession Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("GameSession");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<GameSession>();
                return _instance;
            }
        }

        public static bool Exists => _instance != null;

        public WorldClock Clock { get; private set; } = new WorldClock();
        public WeatherState Weather { get; private set; } = new WeatherState();
        public WorldFlags Flags { get; private set; } = new WorldFlags();
        public WeatherTable WeatherTable { get; set; } = new WeatherTable();
        public int Gold { get; private set; }
        public float Playtime { get; private set; }
        public string Region { get; set; } = "";
        /// <summary>The clock runs only while a player is in the world (not on the title screen, not paused).</summary>
        public bool WorldRunning { get; set; }

        public event Action<int> HourChanged;
        public event Action<int> DayChanged;
        public event Action<WeatherKind> WeatherChanged;
        public event Action<int> GoldChanged;

        float _autosaveTimer;

        void Update()
        {
            if (!WorldRunning) return;
            float dt = Time.deltaTime;
            Playtime += dt;
            _autosaveTimer += Time.unscaledDeltaTime;
            if (_autosaveTimer >= Save.SaveManager.AutosaveInterval)
            {
                _autosaveTimer = 0f;
                Save.SaveManager.Save(Save.SaveManager.AutosaveSlot);
            }
            int hour = Clock.Hour, day = Clock.Day;
            Clock.Advance(dt);
            if (Clock.Hour != hour) HourChanged?.Invoke(Clock.Hour);
            if (Clock.Day != day) DayChanged?.Invoke(Clock.Day);
            if (Weather.Update(Clock.totalMinutes, WeatherTable)) WeatherChanged?.Invoke(Weather.current);
        }

        public void AddGold(int amount)
        {
            if (amount == 0) return;
            Gold = Mathf.Max(0, Gold + amount);
            GoldChanged?.Invoke(Gold);
        }

        public bool SpendGold(int amount)
        {
            if (amount < 0 || amount > Gold) return false;
            AddGold(-amount);
            return true;
        }

        public void SetWeather(WeatherKind kind, bool lockIt)
        {
            Weather.Force(kind, Clock.totalMinutes, WeatherTable.maxDuration, lockIt);
            WeatherChanged?.Invoke(kind);
        }

        public void SetTime(int hh, int mm)
        {
            Clock.SetTimeOfDay(hh, mm);
            HourChanged?.Invoke(Clock.Hour);
        }

        /// <summary>New game: fresh clock, weather, flags, gold.</summary>
        public void ResetForNewGame()
        {
            Clock = new WorldClock();
            Weather = new WeatherState();
            Flags = new WorldFlags();
            Gold = 0;
            Playtime = 0f;
            Region = "";
        }

        public void Restore(WorldClock clock, WeatherState weather, WorldFlags flags, int gold, float playtime, string region)
        {
            Clock = clock ?? new WorldClock();
            Weather = weather ?? new WeatherState();
            Flags = flags ?? new WorldFlags();
            Flags.Rebuild();
            Gold = gold;
            Playtime = playtime;
            Region = region ?? "";
        }
    }
}
