using System;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>Game time (§7): 24 real minutes = 1 game day, so 1 real second = 1 game minute. Pure, saved.</summary>
    [Serializable]
    public sealed class WorldClock
    {
        public const int MinutesPerDay = 1440;
        public const float RealSecondsPerGameMinute = 1f;
        public const int NightStartHour = 20;
        public const int NightEndHour = 5;
        /// <summary>New games start on day 1 at 08:00.</summary>
        public const float StartMinutes = 8f * 60f;

        /// <summary>Total game minutes since day 1, 00:00.</summary>
        public float totalMinutes = StartMinutes;

        public void Advance(float realSeconds) => totalMinutes += realSeconds / RealSecondsPerGameMinute;

        public int Day => Mathf.FloorToInt(totalMinutes / MinutesPerDay) + 1;
        public float MinuteOfDay => Mathf.Repeat(totalMinutes, MinutesPerDay);
        public int Hour => Mathf.FloorToInt(MinuteOfDay / 60f);
        public int Minute => Mathf.FloorToInt(MinuteOfDay) % 60;
        public bool IsNight => Hour >= NightStartHour || Hour < NightEndHour;
        /// <summary>0 at midnight, 0.5 at noon (sun rotation).</summary>
        public float DayFraction => MinuteOfDay / MinutesPerDay;

        /// <summary>Jumps forward to the next time the clock shows hh:mm (same day if still ahead).</summary>
        public void SetTimeOfDay(int hh, int mm)
        {
            float target = Mathf.Clamp(hh, 0, 23) * 60f + Mathf.Clamp(mm, 0, 59);
            float dayStart = totalMinutes - MinuteOfDay;
            totalMinutes = target >= MinuteOfDay ? dayStart + target : dayStart + MinutesPerDay + target;
        }

        /// <summary>True if the hour is in [from, to), wrapping past midnight.</summary>
        public bool IsBetween(int fromHour, int toHour)
        {
            int h = Hour;
            return fromHour <= toHour ? h >= fromHour && h < toHour : h >= fromHour || h < toHour;
        }

        public string Format() => $"{Hour:00}:{Minute:00}";
    }
}
