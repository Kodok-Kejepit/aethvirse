using Aethvirse.Combat;
using Aethvirse.Enemies;
using Aethvirse.Player;
using Aethvirse.Quests;
using Aethvirse.Stats;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Interaction
{
    /// <summary>
    /// The academy duel ring: challenge the rivals one by one (each scales to you). The first win over each pays well
    /// and unlocks the next name on the board. After the last, a fourth name appears that nobody wrote.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class DuelRing : MonoBehaviour
    {
        public struct Rival
        {
            public string id, name, intro, half, win, reward;
            public RivalMove[] moves;
            public float evade, hp;
            public ElementType element;
            public Color color;
        }

        public static readonly Rival[] Rivals =
        {
            new Rival { id = "rival_kaelen", name = "Kaelen Dray", reward = "u_kaelens_spare_sword", evade = 0.45f, hp = 7f, element = ElementType.Wind, color = new Color(0.55f, 1f, 0.7f),
                moves = new[] { RivalMove.Dash, RivalMove.Volley }, intro = "Kaelen: \"Cinder Seat. Let's see if you deserve to sit anywhere.\"",
                half = "Kaelen: \"…Fine. No more holding back.\"", win = "Kaelen: \"Tch. Lucky. …Next time I won't lose.\"" },
            new Rival { id = "rival_ilyra", name = "Ilyra Vance, Second Year", reward = "rune_umbral", evade = 0.55f, hp = 8f, element = ElementType.Shadow, color = new Color(0.6f, 0.35f, 0.9f),
                moves = new[] { RivalMove.BlinkStrike, RivalMove.Volley, RivalMove.Dash }, intro = "Ilyra: \"First-years always look at my hands. Look at my shadow instead.\"",
                half = "Ilyra: \"You can see me. Interesting.\"", win = "Ilyra: \"Keep that eye of yours. The academy has more shadows than me.\"" },
            new Rival { id = "rival_torvald", name = "Torvald Ash, Academy Champion", reward = "set_ashwalker_0", evade = 0.15f, hp = 11f, element = ElementType.Earth, color = new Color(0.8f, 0.6f, 0.35f),
                moves = new[] { RivalMove.Nova, RivalMove.Dash }, intro = "Torvald: \"Champions disappear from this academy. I intend to be the first who doesn't.\"",
                half = "Torvald: \"The ground is on my side.\"", win = "Torvald: \"…Then you are the champion now. Be careful who calls your name at night.\"" },
            new Rival { id = "rival_echo", name = "[ Your Name ]", reward = "book_sx_echoing_blade", evade = 0.5f, hp = 12f, element = ElementType.Void, color = new Color(0.9f, 0.9f, 0.95f),
                moves = new[] { RivalMove.Dash, RivalMove.Nova, RivalMove.Volley, RivalMove.BlinkStrike }, intro = "The board shows a fourth name. It is yours. Someone is already standing in the ring.",
                half = "It moves the way you move. A little faster.", win = "The name on the board fades. The Codex is quiet for a long time." },
        };

        public static string WonFlag(string id) => $"duel_{id}_won";

        Interactable _use;
        Enemy _current;
        int _currentIndex = -1;

        /// <summary>Next rival to face: the first one not beaten yet (the echo only after the champion); all beaten = rematch the echo.</summary>
        public static int NextRival(WorldFlags flags)
        {
            for (int i = 0; i < Rivals.Length; i++) if (!flags.Has(WonFlag(Rivals[i].id))) return i;
            return Rivals.Length - 1;
        }

        void Awake()
        {
            _use = GetComponent<Interactable>();
            _use.Used += _ => Challenge();
        }

        void Update()
        {
            int next = NextRival(GameSession.Instance.Flags);
            _use.Prompt = $"Duel: {Rivals[next].name}";
            _use.Available = _current == null;
            if (_current == null) return;
            Transform p = PlayerTarget.Current;
            if (_current.State == EnemyState.Dead) { Won(); return; }
            if (p == null || (p.position - transform.position).magnitude > 30f)
            {
                Destroy(_current.gameObject);
                _current = null;
                Codex.Notify("You left the ring. The duel is forfeit.", CodexTone.Info);
            }
        }

        void Challenge()
        {
            if (EnemyCatalog.Instance == null || PlayerStats.Current == null) return;
            _currentIndex = NextRival(GameSession.Instance.Flags);
            Rival r = Rivals[_currentIndex];
            int level = PlayerStats.Current.Progression.level + _currentIndex * 2;
            _current = EnemyCatalog.Instance.Spawn(r.id, transform.position + Vector3.forward * 5f + Vector3.up * 0.1f, level, 40f, false);
            if (_current == null) return;
            _current.RespawnAtHome = false;
            _current.MakeBoss(r.hp, 1.2f, 1f);
            RivalDuelist.Make(_current, r.moves, r.evade, r.element, r.color, new[] { r.intro, r.half, r.win });
            Codex.Notify(r.intro, CodexTone.Anomaly);
        }

        void Won()
        {
            Rival r = Rivals[_currentIndex];
            _current = null;
            WorldFlags flags = GameSession.Instance.Flags;
            bool first = !flags.Has(WonFlag(r.id));
            flags.SetFlag(WonFlag(r.id));
            Codex.Notify(r.win, CodexTone.Anomaly);
            if (!first) { Items.PlayerInventory.Current?.AddGold(200); return; }
            QuestSystem.Instance?.AddAcademyPoints(40 + 20 * _currentIndex);
            PlayerStats.Current?.AddXp(400 * (1 + _currentIndex));
            var inv = Items.PlayerInventory.Current;
            if (inv != null && inv.Database.Get(r.reward) != null)
            {
                inv.Bag.Add(r.reward);
                Codex.Notify($"[ DUEL WON ]  {inv.Database.Get(r.reward).displayName}", CodexTone.Reward);
            }
            if (_currentIndex == 2) Codex.Notify("A fourth name has been written on the duel board.", CodexTone.Anomaly);
        }
    }
}
