using System.Collections.Generic;

namespace Aethvirse.Core
{
    /// <summary>
    /// Localization (§12): English + Bahasa Indonesia by key. Menus use it now; the remaining UI strings move here
    /// together with the Phase 2 UI (unknown keys fall back to English, then the key).
    /// </summary>
    public static class Loc
    {
        static readonly Dictionary<string, (string en, string id)> Table = new Dictionary<string, (string, string)>
        {
            { "resume", ("Resume", "Lanjutkan") }, { "save", ("Save", "Simpan") }, { "load", ("Load", "Muat") },
            { "settings", ("Settings", "Pengaturan") }, { "title", ("Return to title", "Kembali ke judul") }, { "quit", ("Quit", "Keluar") },
            { "back", ("Back", "Kembali") }, { "paused", ("PAUSED", "JEDA") }, { "continue", ("Continue", "Lanjutkan") },
            { "new_game", ("New Game", "Permainan Baru") }, { "choose_class", ("Choose your class", "Pilih kelasmu") },
            { "sensitivity", ("Camera sensitivity", "Sensitivitas kamera") }, { "invert", ("Invert Y", "Balik sumbu Y") },
            { "distance", ("Camera distance", "Jarak kamera") }, { "volume", ("Volume", "Volume") }, { "quality", ("Graphics", "Grafis") },
            { "fps", ("FPS limit", "Batas FPS") }, { "language", ("Language", "Bahasa") }, { "on", ("On", "Nyala") }, { "off", ("Off", "Mati") },
            { "monitor", ("Monitor (VSync)", "Monitor (VSync)") },
        };

        public static string T(string key)
        {
            if (!Table.TryGetValue(key, out var v)) return key;
            return GameSettings.Language == "id" ? v.id : v.en;
        }
    }
}
