using Aethvirse.Items;
using Aethvirse.Quests;
using Aethvirse.Stats;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Secrets
{
    /// <summary>
    /// Finds secrets, pays them, writes the Mystery Journal and opens the road once a region's page is full
    /// ("every line has been written" → [ SYSTEM UPDATE ]). Lives across scenes.
    /// </summary>
    public sealed class SecretSystem : MonoBehaviour
    {
        static SecretSystem _instance;
        public static SecretSystem Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("SecretSystem");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SecretSystem>();
                return _instance;
            }
        }

        public SecretState State { get; private set; } = new SecretState();
        public event System.Action Changed;

        void Awake() => GameSession.Instance.FlagSet += OnFlag;

        public void ResetForNewGame() { State = new SecretState(); Changed?.Invoke(); }
        public void Restore(SecretState s) { State = s ?? new SecretState(); Changed?.Invoke(); }

        void OnFlag(string flag)
        {
            SecretDatabase db = SecretDatabase.Instance;
            if (db == null || flag.StartsWith("stage_") || flag.StartsWith("journal_")) return;
            foreach (SecretData s in db.secrets)
                if (s != null && s.foundByFlag == flag) Find(s.id);
        }

        /// <summary>Marks a hint as read (signs, NPC lines, notes). Shows in the journal as ◇.</summary>
        public void Hint(string secretId, int index)
        {
            if (State.HintSeen(secretId, index)) return;
            State.SeeHint(secretId, index);
            Codex.Notify("A clue is written in the Mystery Journal.", CodexTone.Anomaly);
            Changed?.Invoke();
        }

        /// <summary>Finds a secret (once). Pays rewards, writes the journal, checks the page.</summary>
        public bool Find(string id)
        {
            SecretDatabase db = SecretDatabase.Instance;
            SecretData s = db != null ? db.Get(id) : null;
            if (s == null || !State.MarkFound(id)) return false;
            Codex.Notify(s.category == SecretCategory.SealedMission ? "[ ??? ]  Something answered." : $"Journal: {s.title}", CodexTone.Anomaly);
            Pay(s);
            Core.GameEvents.RaiseAreaEntered("secret_" + id);
            var (found, total) = db.Progress(s.region, State);
            if (found >= total && total > 0) PageFull(s.region);
            Changed?.Invoke();
            return true;
        }

        void PageFull(string region)
        {
            GameSession.Instance.Flags.SetFlag(SecretDatabase.PageFullFlag(region));
            Codex.Notify("Every line has been written.", CodexTone.Anomaly);
            Codex.Notify("[ SYSTEM UPDATE ]", CodexTone.System);
        }

        static void Pay(SecretData s)
        {
            GameSession session = GameSession.Instance;
            foreach (Reward r in s.rewards)
            {
                switch (r.type)
                {
                    case RewardType.Item: PlayerInventory.Current?.Bag.Add(r.key, r.amount); break;
                    case RewardType.Gold: session.AddGold(r.amount); break;
                    case RewardType.Xp: PlayerStats.Current?.AddXp(r.amount); break;
                    case RewardType.Flag: session.Flags.SetFlag(r.key); break;
                    case RewardType.AcademyPoints: QuestSystem.Instance.AddAcademyPoints(r.amount); break;
                    case RewardType.CodexPoints: session.CodexPoints += r.amount; break;
                    case RewardType.CodexMessage: Codex.Notify(r.text, CodexTone.Anomaly); break;
                    case RewardType.StartQuest: QuestSystem.Instance.Start(r.key); break;
                }
            }
        }
    }
}
