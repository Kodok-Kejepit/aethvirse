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
            { "close", ("Close", "Tutup") },
            { "journal", ("Mystery Journal", "Jurnal Misteri") },
            { "skills", ("Skills", "Skill") },
            { "job", ("Job: ", "Job: ") },
            { "none_yet", ("none yet", "belum ada") },
            { "next_job", ("Next job change at Lv", "Ganti job berikutnya di Lv") },
            { "lines_revealed", ("lines revealed", "baris terungkap") },
            { "shadow_army", ("Shadow army", "Pasukan bayangan") },
            { "stat_points", ("Stat points", "Poin stat") },
            { "skill_points", ("Skill points", "Poin skill") },
            { "buy", ("Buy", "Beli") },
            { "craft", ("Craft", "Racik") },
            { "equipped", ("Equipped", "Dipakai") },
            { "gold", ("Gold", "Emas") },
            { "select_item", ("Select an item.", "Pilih item.") },
            { "sell_hint", ("Sell from the Bag tab.", "Jual dari tab Tas.") },
            { "sold", ("SOLD", "TERJUAL") },
            { "quartermaster", ("Quartermaster", "Quartermaster") },
            { "bag", ("Bag", "Tas") },
            { "cursed", ("Cursed.", "Terkutuk.") },
            { "hidden_nature", ("Its nature is hidden.", "Sifatnya tersembunyi.") },
            { "heals", ("Heals", "Memulihkan") },
            { "codex_shop", ("Codex Shop — day", "Toko Codex — hari") },
            { "new_stock", ("(new stock every day)", "(stok baru tiap hari)") },
            { "shop", ("Shop", "Toko") }, { "codex_shop_tab", ("Codex Shop", "Toko Codex") },
            { "rune", ("Rune", "Rune") }, { "socket", ("Socket", "Pasang") }, { "unsocket", ("Remove", "Lepas") },
            { "stash", ("Stash", "Gudang") }, { "sockets", ("Sockets", "Soket") }, { "enchant", ("Enchant", "Enchant") },
            { "reforge", ("Reforge", "Tempa ulang") }, { "reforged", ("Reforged", "Ditempa ulang") },
            { "titles", ("Titles & achievements", "Title & pencapaian") }, { "worn", ("Worn title", "Title dipakai") },
            { "wear", ("Wear", "Pakai") }, { "show_all", ("Show all", "Tampilkan semua") }, { "hide_locked", ("Hide locked", "Sembunyikan terkunci") },
            { "difficulty", ("Difficulty", "Tingkat kesulitan") }, { "controls", ("Controls", "Kontrol") }, { "reset_keys", ("Reset keys", "Atur ulang tombol") },
            { "fast_travel", ("Fast Travel", "Perjalanan Cepat") },
            { "perfect_dodge", ("PERFECT DODGE", "HINDARAN SEMPURNA") },
        };

        public static string T(string key)
        {
            if (!Table.TryGetValue(key, out var v)) return key;
            return GameSettings.Language == "id" ? v.id : v.en;
        }
    }
}
