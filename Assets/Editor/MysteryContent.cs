// Sealed missions (hidden quest chains, §9.1) and the cipher locks with their murals (one per region).
// QuestDataBuilder appends the sealed missions; MysteryBuilder places the locks and murals into the region scenes.
using System.Collections.Generic;
using Aethvirse.Quests;
using Aethvirse.Secrets;
using UnityEngine;

public static class MysteryContent
{
    // --- Cipher locks --------------------------------------------------------------------------------

    public struct CipherDef
    {
        public string region, answer, mural, reward;
        public CipherKind kind;
        public int key;
        public Vector3 lockAt, muralAt;
    }

    public static readonly CipherDef[] Ciphers =
    {
        new CipherDef { region = "r1_ashfall", answer = "ASH", kind = CipherKind.Caesar, key = 3, reward = "u_empty_name_tag", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "A mural on the town wall: three crows on a broken sign, all looking back the way they came." },
        new CipherDef { region = "r2_verdant", answer = "BLOOM", kind = CipherKind.Atbash, reward = "gem_diamond_3", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "A mural of the valley reflected in a lake. Every word painted on it is written for the reflection." },
        new CipherDef { region = "r3_dunes", answer = "SAND", kind = CipherKind.Reverse, reward = "rune_hourglass", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "Sand-scoured paint: 'Here the wind writes from the end.'" },
        new CipherDef { region = "r4_frostveil", answer = "FROST", kind = CipherKind.Numbers, reward = "gem_sapphire_3", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "Frozen into the wall, a list of numbers. The first line: '1 is A. Count the rest.'" },
        new CipherDef { region = "r5_archive", answer = "INK", kind = CipherKind.Morse, reward = "rune_echo", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "Drowned clerks painted mid-tap at their desks: long, short, long. They never stopped." },
        new CipherDef { region = "r6_fracture", answer = "SPLIT", kind = CipherKind.Caesar, key = 5, reward = "u_unwritten_page_ring", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "Five cracks run across a painted face. Each one moves the letters one step." },
        new CipherDef { region = "r7_throne", answer = "CROWN", kind = CipherKind.Atbash, reward = "set_hollow_regent_1", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "A royal mural, painted in a mirror by a court that feared to look at its king directly." },
        new CipherDef { region = "s1_glasswood", answer = "GLASS", kind = CipherKind.Reverse, reward = "gem_emerald_3", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "Glass panes where the words show only from the other side." },
        new CipherDef { region = "s2_mirrorsea", answer = "ECHO", kind = CipherKind.Atbash, reward = "rune_twin_moon", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "The shallows show a sky that is the alphabet upside down: Z where A should be." },
        new CipherDef { region = "s3_choir", answer = "BELL", kind = CipherKind.Morse, reward = "u_seraph_halo", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "Sheet music with only two kinds of notes: long and short." },
        new CipherDef { region = "k1_cinder", answer = "EMBER", kind = CipherKind.Caesar, key = 7, reward = "u_cinder_kings_mantle", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "A burned mural of seven kings, each one standing a step further than the last." },
        new CipherDef { region = "k2_hollow_moon", answer = "MOON", kind = CipherKind.Numbers, reward = "gem_topaz_4", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "Survey marks carved by the Kethrans: every letter has its number." },
        new CipherDef { region = "k3_last_city", answer = "NAME", kind = CipherKind.Reverse, reward = "rune_crimson_pact", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "The last defenders wrote their names backwards, so the enemy could not call them." },
        new CipherDef { region = "z1_seat", answer = "FIRST", kind = CipherKind.Caesar, key = 1, reward = "u_first_line_quill", lockAt = new Vector3(-40f, 0f, 30f), muralAt = new Vector3(-60f, 0f, -52f),
            mural = "One line, carved in light: 'Every story is one step from where it began.'" },
    };

    public static string Inscription(CipherDef c) => Cipher.Encode(c.answer, c.kind, c.key);

    // --- Sealed missions -----------------------------------------------------------------------------

    static QuestObjective O(ObjectiveType t, string target, int count, string text, int stage) =>
        new QuestObjective { type = t, target = target, count = count, text = text, stage = stage };
    static Reward R(RewardType t, string key = "", int amount = 1, string text = "") => new Reward { type = t, key = key, amount = amount, text = text };
    static Condition Flag(string f) => new Condition { type = ConditionType.Flag, key = f };

    public struct Sealed
    {
        public string id, title, desc;
        public Condition[] start;
        public QuestObjective[] objectives;
        public Reward[] rewards;
    }

    public static List<Sealed> Missions() => new List<Sealed>
    {
        new Sealed { id = "sm_breathing_drain", title = "Something Breathes Below", desc = "The drain breathed. Something answered from further down.",
            start = new[] { Flag("secret_r1_drain_breath") },
            objectives = new[] { O(ObjectiveType.Kill, "ember_husk", 6, "Silence what crawls up from the drains", 0),
                O(ObjectiveType.ReachArea, "secret_r1_ash_room", 1, "Find the room the ash closed off", 1),
                O(ObjectiveType.Flag, CipherLock.SolvedFlag("r1_ashfall"), 1, "Open the lock the crows look away from", 2) },
            rewards = new[] { R(RewardType.Item, "rune_ember"), R(RewardType.Xp, "", 600), R(RewardType.CodexMessage, text: "Under the city, something stops breathing. Or starts.") } },
        new Sealed { id = "sm_bread_debt", title = "The Debt of Bread", desc = "Someone has been waiting a long time for one loaf.",
            start = new[] { Flag("secret_r1_bread_seller") },
            objectives = new[] { O(ObjectiveType.Collect, "bread", 5, "Carry five loaves", 0),
                O(ObjectiveType.Talk, "quartermaster", 1, "Ask who used to bake for the academy", 1),
                O(ObjectiveType.ReachArea, "secret_r2_verdant_offering", 1, "Leave an offering where the valley is too quiet", 2) },
            rewards = new[] { R(RewardType.Item, "u_horne_whistle"), R(RewardType.AcademyPoints, "", 30) } },
        new Sealed { id = "sm_cinder_names", title = "Names on the Cinder Seat", desc = "Champions vanish from this academy. The seat remembers who sat in it.",
            start = new[] { Flag("duel_rival_torvald_won") },
            objectives = new[] { O(ObjectiveType.Talk, "horne", 1, "Ask Horne about the last champion", 0),
                O(ObjectiveType.ReachArea, "secret_r5_archive_bells", 1, "Find the record the archive drowned", 1),
                O(ObjectiveType.Flag, CipherLock.SolvedFlag("r7_throne"), 1, "Read the mirrored name at the throne", 2),
                O(ObjectiveType.Kill, "hollow_echo", 15, "Quiet the echoes of the champions", 3) },
            rewards = new[] { R(RewardType.Item, "u_blade_of_the_cinder_seat"), R(RewardType.CodexMessage, text: "Every name carved on the seat is a champion who never came home. There is room for one more.") } },
        new Sealed { id = "sm_frozen_tear", title = "A Tear That Did Not Fall", desc = "A queen of ice wept once. It froze before it landed.",
            start = new[] { Flag("secret_r4_frostveil_night") },
            objectives = new[] { O(ObjectiveType.Kill, "frost_yeti", 10, "Break the guardians of the frozen hall", 0),
                O(ObjectiveType.ReachArea, "secret_r4_frostveil_stormnight", 1, "Stand where she stood on the worst night", 1),
                O(ObjectiveType.Flag, CipherLock.SolvedFlag("r4_frostveil"), 1, "Count the ice", 2) },
            rewards = new[] { R(RewardType.Item, "u_frozen_tear"), R(RewardType.Xp, "", 1500) } },
        new Sealed { id = "sm_drowned_index", title = "The Index of Unwritten Books", desc = "A catalogue lists books that were never written. One of them is about you.",
            start = new[] { Flag(CipherLock.SolvedFlag("r5_archive")) },
            objectives = new[] { O(ObjectiveType.Kill, "drowned_scribe", 12, "Stop the scribes still writing under water", 0),
                O(ObjectiveType.ReachArea, "secret_r5_archive_wall2", 1, "Find the shelf behind the lonely wall", 1),
                O(ObjectiveType.UseItem, "codex_identify_scroll", 3, "Read three things that were hidden", 2) },
            rewards = new[] { R(RewardType.Item, "u_drowned_crown"), R(RewardType.CodexMessage, text: "The index has one more entry now. Its title is your name.") } },
        new Sealed { id = "sm_who_swung_first", title = "Who Swung First", desc = "The world split here. Someone held the hammer.",
            start = new[] { Flag("secret_r6_fracture_wall") },
            objectives = new[] { O(ObjectiveType.Kill, "void_hollow", 12, "Clear the hollows from the fault", 0),
                O(ObjectiveType.Kill, "rift_stalker", 8, "Clear the stalkers from the fault", 0),
                O(ObjectiveType.Flag, CipherLock.SolvedFlag("r6_fracture"), 1, "Follow the five cracks", 1),
                O(ObjectiveType.Kill, "gravewarden", 3, "Defeat the wardens of the split", 2) },
            rewards = new[] { R(RewardType.Item, "u_fault_hammer") } },
        new Sealed { id = "sm_lost_courier", title = "The Courier Who Never Arrived", desc = "A message is still on its way. It has been for three hundred years.",
            start = new[] { Flag("secret_r3_dunes_weather") },
            objectives = new[] { O(ObjectiveType.Distance, "", 3000, "Run the courier's route (3 km)", 0),
                O(ObjectiveType.ReachArea, "secret_r3_dunes_dawn", 1, "Arrive when the courier always did", 1),
                O(ObjectiveType.Flag, CipherLock.SolvedFlag("r3_dunes"), 1, "Read the message the wind wrote", 2) },
            rewards = new[] { R(RewardType.Item, "u_sandglass_dagger"), R(RewardType.Item, "set_dunestrider_4") } },
        new Sealed { id = "sm_valley_heart", title = "The Valley's Slow Heartbeat", desc = "Put your ear to the ground. The valley has a pulse.",
            start = new[] { Flag("secret_r2_verdant_night") },
            objectives = new[] { O(ObjectiveType.Kill, "thorn_stalker", 10, "Pull the thorns from the valley", 0),
                O(ObjectiveType.Kill, "moss_brute", 5, "Wake the sleeping brutes", 1),
                O(ObjectiveType.Flag, CipherLock.SolvedFlag("r2_verdant"), 1, "Read the lake's mirrored word", 2) },
            rewards = new[] { R(RewardType.Item, "u_verdant_heart") } },
        new Sealed { id = "sm_last_rehearsal", title = "The Last Rehearsal", desc = "The choir stopped singing mid-note. Someone told them to.",
            start = new[] { Flag("secret_s3_choir_bells") },
            objectives = new[] { O(ObjectiveType.Kill, "bell_wraith", 12, "Silence the wraiths still holding the note", 0),
                O(ObjectiveType.Flag, "galaxy_2_opened", 1, "Face the one who conducted the silence", 1),
                O(ObjectiveType.ReachArea, "secret_s3_choir_bells2", 1, "Finish the song", 2) },
            rewards = new[] { R(RewardType.Item, "u_conductors_baton") } },
        new Sealed { id = "sm_name_on_the_wall", title = "Your Name on the Wall", desc = "The last city wrote a name on its walls before it fell. It is yours.",
            start = new[] { Flag(CipherLock.SolvedFlag("k3_last_city")) },
            objectives = new[] { O(ObjectiveType.Kill, "hunted_sentinel", 10, "Relieve the sentinels of their watch", 0),
                O(ObjectiveType.Kill, "star_archer", 10, "Lower the archers' bows", 0),
                O(ObjectiveType.Flag, "god_war_opened", 1, "Do what they could not", 1) },
            rewards = new[] { R(RewardType.Item, "u_last_city_banner"), R(RewardType.CodexMessage, text: "They knew you would come. They waited a thousand years.") } },
        new Sealed { id = "sm_fourth_name", title = "The Fourth Name", desc = "You fought yourself in the ring. It fought back with your hands.",
            start = new[] { Flag("duel_rival_echo_won") },
            objectives = new[] { O(ObjectiveType.Skill, "sx_echoing_blade", 10, "Strike twice, ten times", 0),
                O(ObjectiveType.Flag, "region_r0_unknown_found", 1, "Go where there is no map", 1),
                O(ObjectiveType.Flag, "last_seal_opened", 1, "Read the last page", 2) },
            rewards = new[] { R(RewardType.Item, "set_first_vanguard_0"), R(RewardType.CodexMessage, text: "Someone carried the Codex before you. Their blade is yours now.") } },
        new Sealed { id = "sm_page_not_found", title = "ERR: page not found", desc = "At three in the morning, the Codex shows a page that does not exist.",
            start = new[] { new Condition { type = ConditionType.HourRange, value = 3f, value2 = 4f }, new Condition { type = ConditionType.LevelAtLeast, value = 80f } },
            objectives = new[] { O(ObjectiveType.Talk, "ilsveth", 1, "Ask the headmaster what the Codex is", 0),
                O(ObjectiveType.UseItem, "book_sx_codex_glitch", 1, "Read the page that is not there", 1),
                O(ObjectiveType.Flag, CipherLock.SolvedFlag("z1_seat"), 1, "Find the first line", 2) },
            rewards = new[] { R(RewardType.Item, "u_codex_bookmark"), R(RewardType.CodexMessage, text: "[ page restored ]  …you were not supposed to find this.") } },
        new Sealed { id = "sm_measure_the_dark", title = "Measure the Dark", desc = "The Kethrans measured the dark inside their moon. It measured them back.",
            start = new[] { Flag("secret_k2_hollow_moon_night") },
            objectives = new[] { O(ObjectiveType.Kill, "moon_stalker", 12, "Drive back what lives in the hollow", 0),
                O(ObjectiveType.Flag, CipherLock.SolvedFlag("k2_hollow_moon"), 1, "Read the survey numbers", 1) },
            rewards = new[] { R(RewardType.Item, "u_hollow_moon_boots") } },
        new Sealed { id = "sm_glass_commission", title = "The Unclaimed Commission", desc = "A glasswright finished a bow for a client who never returned.",
            start = new[] { Flag("secret_s1_glasswood_wall") },
            objectives = new[] { O(ObjectiveType.Collect, "mat_s1_glasswood", 10, "Gather singing glass", 0),
                O(ObjectiveType.Flag, CipherLock.SolvedFlag("s1_glasswood"), 1, "Read the glass from the other side", 1) },
            rewards = new[] { R(RewardType.Item, "u_glass_tree_bow"), R(RewardType.CodexMessage, text: "The commission note is signed. The client's name is yours.") } },
        // Hidden job chains (§8): the only way to the Necromancer, the Shadow Sovereign and the Echo Warden.
        new Sealed { id = "sm_grave_that_answers", title = "A Grave That Answers", desc = "Your familiars keep looking at the ground. Something under it is looking back.",
            start = new[] { Flag("job_summoner"), Flag("secret_r7_throne_night") },
            objectives = new[] { O(ObjectiveType.Kill, "hollow_echo", 25, "Let the echoes fall where you can hear them", 0),
                O(ObjectiveType.ReachArea, "secret_r5_archive_night", 1, "Read the names the archive drowned", 1),
                O(ObjectiveType.Flag, CipherLock.SolvedFlag("r5_archive"), 1, "Tap out the clerks' last word", 2) },
            rewards = new[] { R(RewardType.Flag, "sealed_necromancer"), R(RewardType.CodexMessage, text: "[ HIDDEN JOB ]  The dead have started listening to you. (Necromancer can be chosen.)") } },
        new Sealed { id = "sm_umbra_page", title = "The Umbra Page", desc = "One page of the Codex is written in shadow. Only the dead can read it to you.",
            start = new[] { Flag("job_necromancer") },
            objectives = new[] { O(ObjectiveType.Skill, "necromancer_4", 15, "Raise fifteen echoes", 0),
                O(ObjectiveType.Kill, "the_unwritten", 1, "Silence the thing on the empty throne", 1),
                O(ObjectiveType.Flag, "last_seal_opened", 1, "Read the last page of this world", 2) },
            rewards = new[] { R(RewardType.Flag, "codex_umbra_read"), R(RewardType.CodexMessage, text: "[ HIDDEN JOB ]  Every shadow knows your name now. (Shadow Sovereign can be chosen.)") } },
        new Sealed { id = "sm_echo_king", title = "The King of Echoes", desc = "After you beat yourself in the ring, your footsteps started arriving twice.",
            start = new[] { Flag("duel_rival_echo_won"), new Condition { type = ConditionType.LevelAtLeast, value = 30f } },
            objectives = new[] { O(ObjectiveType.ReachArea, "secret_r1_small_gate", 1, "Go back to where the small gate opened", 0),
                O(ObjectiveType.Kill, "hollow_echo", 40, "Answer forty echoes", 1),
                O(ObjectiveType.Flag, "region_r0_unknown_found", 1, "Follow your own footsteps where there is no map", 2) },
            rewards = new[] { R(RewardType.Flag, "echo_king_answered"), R(RewardType.CodexMessage, text: "[ HIDDEN JOB ]  The echo kneels. It was always you. (Echo Warden can be chosen.)") } },
    };
}
