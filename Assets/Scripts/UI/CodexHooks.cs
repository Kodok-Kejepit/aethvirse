using Aethvirse.Save;
using Aethvirse.Skills;
using Aethvirse.Stats;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.UI
{
    /// <summary>Turns game events into Codex messages (level up, new skills, saves, new day, weather).</summary>
    public sealed class CodexHooks : MonoBehaviour
    {
        PlayerStats _stats;
        bool _ready;

        void Start()
        {
            _stats = PlayerStats.Current;
            if (_stats != null)
            {
                _stats.LeveledUp += OnLevelUp;
                _stats.SkillLearned += OnSkill;
            }
            SaveManager.Saved += OnSaved;
            GameSession.Instance.DayChanged += OnDay;
            GameSession.Instance.WeatherChanged += OnWeather;
            Combat.Damageable.AnyReaction += OnReaction;
            _ready = true;
        }

        void OnDestroy()
        {
            if (!_ready) return;
            if (_stats != null)
            {
                _stats.LeveledUp -= OnLevelUp;
                _stats.SkillLearned -= OnSkill;
            }
            SaveManager.Saved -= OnSaved;
            Combat.Damageable.AnyReaction -= OnReaction;
            if (GameSession.Exists)
            {
                GameSession.Instance.DayChanged -= OnDay;
                GameSession.Instance.WeatherChanged -= OnWeather;
            }
        }

        void OnLevelUp(int level) =>
            Codex.Notify($"[ LEVEL UP ]  Lv {level}  ·  +{_stats.Tuning.freePointsPerLevel} stat points", CodexTone.Reward);

        void OnSkill(SkillData skill) => Codex.Notify($"New skill: {skill.displayName}", CodexTone.Reward);

        void OnSaved(int slot)
        {
            if (slot == SaveManager.AutosaveSlot) Codex.Notify("Autosaved.", CodexTone.System);
        }

        void OnDay(int day) => Codex.Notify($"Day {day} begins.", CodexTone.Info);

        void OnWeather(WeatherKind kind) => Codex.Notify($"Weather: {kind}.", CodexTone.Info);

        /// <summary>First time each reaction happens, the Codex names it.</summary>
        void OnReaction(Combat.ReactionType r)
        {
            string flag = "reaction_seen_" + r;
            if (GameSession.Instance.Flags.Has(flag)) return;
            GameSession.Instance.Flags.SetFlag(flag);
            Codex.Notify($"Reaction discovered: {Combat.Reactions.Name(r)}", CodexTone.System);
        }
    }
}
