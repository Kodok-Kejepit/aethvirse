// Writes the quest database (Resources/QuestDatabase) and the academy NPCs. Text stays spoiler-free:
// this repo is public; the story's secrets live in aethvirse-dev only.
using System.Collections.Generic;
using System.IO;
using Aethvirse.Dialog;
using Aethvirse.Quests;
using UnityEditor;
using UnityEngine;

public static class QuestDataBuilder
{
    const string QuestDir = "Assets/Data/Quests";
    const string NpcDir = "Assets/Data/Npcs";
    const string DatabasePath = "Assets/Resources/QuestDatabase.asset";

    [MenuItem("AETHVIRSE/Build Quest Data")]
    public static void Build()
    {
        Directory.CreateDirectory(QuestDir);
        Directory.CreateDirectory(NpcDir);
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath));
        var db = LoadOrCreate<QuestDatabase>(DatabasePath);
        db.quests = new List<QuestData>
        {
            Quest("q_cinder_seat", "The Cinder Seat", QuestKind.Story,
                "You made it into Aster Vey Academy on the seat nobody wanted. Find your instructor and your room.",
                autoStart: true, academy: 20,
                objectives: new[]
                {
                    Obj(ObjectiveType.Talk, "horne", 1, "Report to Instructor Horne (classroom)"),
                    Obj(ObjectiveType.ReachArea, "academy_dorm", 1, "Find your bed in the dormitory"),
                    Obj(ObjectiveType.Talk, "nyra", 1, "Visit the library (optional)", optional: true),
                },
                rewards: new[] { R(RewardType.Flag, "cinder_class"), R(RewardType.StartQuest, "q_first_field") }),
            Quest("q_first_field", "First Field Assignment", QuestKind.Story,
                "Cinder Class gets the jobs nobody else takes: thin out the Ash Hounds in Ashfall Outskirts.",
                academy: 50,
                objectives: new[]
                {
                    Obj(ObjectiveType.ReachArea, "r1_ashfall", 1, "Go to Ashfall Outskirts (expedition gate)"),
                    Obj(ObjectiveType.Kill, "ash_hound", 3, "Defeat 3 Ash Hounds", stage: 1),
                    Obj(ObjectiveType.Talk, "horne", 1, "Report back to Horne", stage: 2),
                },
                rewards: new[] { R(RewardType.Xp, "", 150), R(RewardType.Gold, "", 100), R(RewardType.Item, "potion_small", 3), R(RewardType.StartQuest, "q_rival") }),
            Quest("q_rival", "Prove You Belong", QuestKind.Academy,
                "Kaelen Dray says Cinder Class is a waste of a seat. Show him at the training ground.",
                academy: 40,
                objectives: new[]
                {
                    Obj(ObjectiveType.Talk, "kaelen", 1, "Talk to Kaelen at the arena"),
                    Obj(ObjectiveType.Attack, "", 30, "Land 30 attacks on the training dummies", stage: 1),
                    Obj(ObjectiveType.Skill, "", 5, "Use skills 5 times", stage: 1),
                    Obj(ObjectiveType.Talk, "kaelen", 1, "Talk to Kaelen again", stage: 2),
                },
                rewards: new[] { R(RewardType.Xp, "", 200), R(RewardType.Flag, "kaelen_noticed") }),
            Quest("q_library_herbs", "Ink and Ash", QuestKind.Side,
                "Nyra needs Ashfall Herbs for her ink. Strange request for a librarian.",
                academy: 15,
                objectives: new[]
                {
                    Obj(ObjectiveType.Collect, "herb", 3, "Bring 3 Ashfall Herbs"),
                    Obj(ObjectiveType.Talk, "nyra", 1, "Give them to Nyra", stage: 1),
                },
                rewards: new[] { R(RewardType.Item, "codex_identify_scroll", 1), R(RewardType.Flag, "nyra_trust"), R(RewardType.Gold, "", 40) }),
            Quest("q_bounty_hounds", "Bounty: Ash Hound Cull", QuestKind.Bounty,
                "Posted on the academy board. Pays per pack, no questions asked.",
                academy: 25,
                objectives: new[] { Obj(ObjectiveType.Kill, "ash_hound", 5, "Defeat 5 Ash Hounds") },
                rewards: new[] { R(RewardType.Gold, "", 150), R(RewardType.Xp, "", 100) }),
        };
        // Sealed missions (§9.1): hidden quest chains that start by themselves.
        foreach (MysteryContent.Sealed m in MysteryContent.Missions())
        {
            QuestData q = Quest(m.id, m.title, QuestKind.Sealed, m.desc, m.objectives, m.rewards, autoStart: true);
            q.startConditions = new List<Condition>(m.start);
            EditorUtility.SetDirty(q);
            db.quests.Add(q);
        }
        EditorUtility.SetDirty(db);

        BuildNpcs();
        AssetDatabase.SaveAssets();
        Debug.Log($"AETHVIRSE: quest data built ({db.quests.Count} quests)");
    }

    static void BuildNpcs()
    {
        Npc("horne", "Instructor Horne", new Color(0.45f, 0.4f, 0.35f), -1, -1,
            Entry("report_back", new[] { QuestStage("q_first_field", 2) },
                "Back already? And in one piece.", "Hm. Three hounds. Don't let it go to your head, Cinder."),
            Entry("first_day", new[] { Active("q_cinder_seat") },
                "So you're the Cinder Seat.", "Rank E. Lowest reading this academy has ever logged.",
                "Don't look at me like that. I teach the class nobody else wants. Find your bed. Field work starts tomorrow."),
            Entry("default", null, "Training ground's to the east. The board has work if you want coin.")
                .WithDaily("Rain makes the hounds bolder. Keep your guard up.", "Sleep. You look worse than usual.", "Read the notices. Some of them are even true."));
        Npc("nyra", "Nyra Solace", new Color(0.35f, 0.4f, 0.6f), 22, 6,
            Entry("herbs_done", new[] { Active("q_library_herbs"), new Condition { type = ConditionType.HasItem, key = "herb", value = 3 } },
                "Ashfall Herbs. Thank you.", "They make an ink that does not fade. Some things should not be forgotten."),
            Entry("herbs_ask", new[] { Done("q_cinder_seat"), Not(Done("q_library_herbs")), Not(Active("q_library_herbs")) },
                "You're the one from Ashfall.", "Could you bring me three Ashfall Herbs? I need them for ink.")
                .WithChoice("I'll find some.", StartQuest("q_library_herbs")).WithChoice("Maybe later."),
            Entry("default", null, "The library closes at ten. The lower stacks are closed always.", "…Please don't ask me why."));
        Npc("kaelen", "Kaelen Dray", new Color(0.7f, 0.6f, 0.25f), -1, -1,
            Entry("rival_done", new[] { QuestStage("q_rival", 2) },
                "…Fine. You can swing a sword.", "That doesn't mean you belong here, Cinder. But fine."),
            Entry("rival_start", new[] { QuestStage("q_rival", 0) },
                "Cinder Seat. They let anyone in now.", "Show me you're not dead weight. Hit something. Use whatever tricks you have."),
            Entry("default", null, "Rank S doesn't talk to rank E. Go away."));
        Npc("quartermaster", "Quartermaster", new Color(0.35f, 0.55f, 0.4f), -1, -1,
            Entry("shop", null, "Potions, bread, scrolls. Academy prices — which is to say, not cheap.")
                .WithChoice("Show me your goods.", new DialogEffect { type = DialogEffectType.OpenShop }).WithChoice("Just looking."));
        Npc("ilsveth", "Headmaster Ilsveth", new Color(0.8f, 0.8f, 0.85f), -1, -1,
            Entry("default", null, "Welcome to Aster Vey. Every student here matters to me.", "Even you. Especially you.")
                .WithDaily("Work hard. The academy is watching with great interest.", "Have you been sleeping well? Any… strange dreams?"));
    }

    // --- Helpers -------------------------------------------------------------------------------

    static QuestData Quest(string id, string title, QuestKind kind, string desc, QuestObjective[] objectives, Reward[] rewards,
        bool autoStart = false, int academy = 0)
    {
        var q = LoadOrCreate<QuestData>($"{QuestDir}/{id}.asset");
        q.id = id;
        q.title = title;
        q.kind = kind;
        q.description = desc;
        q.autoStart = autoStart;
        q.academyPoints = academy;
        q.startConditions = new List<Condition>();
        q.objectives = new List<QuestObjective>(objectives);
        q.rewards = new List<Reward>(rewards);
        EditorUtility.SetDirty(q);
        return q;
    }

    static QuestObjective Obj(ObjectiveType t, string target, int count, string text, int stage = 0, bool optional = false) =>
        new QuestObjective { type = t, target = target, count = count, text = text, stage = stage, optional = optional };

    static Reward R(RewardType t, string key = "", int amount = 1) => new Reward { type = t, key = key, amount = amount };

    static Condition Active(string quest) => new Condition { type = ConditionType.QuestActive, key = quest };
    static Condition Done(string quest) => new Condition { type = ConditionType.QuestDone, key = quest };
    static Condition Not(Condition c) { c.negate = !c.negate; return c; }
    /// <summary>Quest active and at this stage (stage is tracked by a flag the QuestSystem sets: quest_stage_id_n).</summary>
    static Condition QuestStage(string quest, int stage) => new Condition { type = ConditionType.Flag, key = $"stage_{quest}_{stage}" };
    static DialogEffect StartQuest(string quest) => new DialogEffect { type = DialogEffectType.StartQuest, key = quest };

    static DialogEntry Entry(string id, Condition[] conditions, params string[] lines) => new DialogEntry
    {
        id = id,
        conditions = conditions != null ? new List<Condition>(conditions) : new List<Condition>(),
        lines = new List<string>(lines),
    };

    static void Npc(string id, string name, Color color, int awayFrom, int awayTo, params DialogEntry[] entries)
    {
        var n = LoadOrCreate<NpcData>($"{NpcDir}/{id}.asset");
        n.id = id;
        n.displayName = name;
        n.bodyColor = color;
        n.awayFromHour = awayFrom;
        n.awayToHour = awayTo;
        n.entries = new List<DialogEntry>(entries);
        EditorUtility.SetDirty(n);
    }

    public static NpcData LoadNpc(string id) => AssetDatabase.LoadAssetAtPath<NpcData>($"{NpcDir}/{id}.asset");

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}

static class DialogEntryExtensions
{
    public static DialogEntry WithDaily(this DialogEntry e, params string[] lines)
    {
        e.dailyVariants.AddRange(lines);
        return e;
    }

    public static DialogEntry WithChoice(this DialogEntry e, string text, params DialogEffect[] effects)
    {
        e.choices.Add(new DialogChoice { text = text, effects = new List<DialogEffect>(effects) });
        return e;
    }
}
