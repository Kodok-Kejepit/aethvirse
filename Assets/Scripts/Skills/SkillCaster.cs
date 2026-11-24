using System;
using System.Collections.Generic;
using Aethvirse.Input;
using Aethvirse.Player;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Skills
{
    public enum CastResult { Ok, EmptySlot, Locked, Cooldown, NoMp, Busy }

    /// <summary>
    /// Casts the slotted skills (port of the Godot SkillCaster): keys 1–4 / Q R F G + T, gamepad LB + face
    /// buttons and LT + RT, touch buttons; MP, cooldowns (reduced by CDR), skill combos. Damaging skills
    /// run through the player's Attack state; buffs resolve instantly.
    /// </summary>
    public sealed class SkillCaster : MonoBehaviour
    {
        const float InputBuffer = 0.3f;
        const float TriggerThreshold = 0.6f;

        [SerializeField] PlayerStats stats;
        [SerializeField] PlayerController player;
        [SerializeField] PlayerCombat combat;

        readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        GameInput _input;
        int _bufferedSlot = -1;
        float _bufferTimer;
        string _lastSkill = "";
        float _lastSkillTime = float.NegativeInfinity;
        bool _triggersWereDown;

        public PlayerStats Stats => stats;
        /// <summary>A skill went off (its look plays from PlayerCombat.SkillStrike / here for buffs).</summary>
        public event Action<SkillData> SkillUsed;
        public event Action<SkillComboData> ComboTriggered;
        /// <summary>A cast was refused (HUD feedback: no MP, cooldown).</summary>
        public event Action<int, CastResult> CastFailed;

        void Awake() => _input = GameInput.Instance;

        void Update()
        {
            float dt = Time.deltaTime;
            if (_cooldowns.Count > 0)
            {
                var keys = new List<string>(_cooldowns.Keys);
                foreach (string k in keys) _cooldowns[k] = Mathf.Max(_cooldowns[k] - dt, 0f);
            }
            ReadInput();
            _bufferTimer = Mathf.Max(_bufferTimer - dt, 0f);
            if (_bufferedSlot >= 0 && _bufferTimer > 0f)
            {
                CastResult result = TryCastSlot(_bufferedSlot);
                if (result != CastResult.Busy)
                {
                    if (result != CastResult.Ok) CastFailed?.Invoke(_bufferedSlot, result);
                    _bufferedSlot = -1;
                }
            }
        }

        void ReadInput()
        {
            for (int i = 0; i < _input.Skills.Length; i++)
                if (_input.Skills[i].WasPressedThisFrame()) Buffer(i);
            if (_input.Ultimate.WasPressedThisFrame()) Buffer(PlayerStats.UltimateSlot);
            if (_input.SkillModifier.IsPressed())
            {
                // X, Y, B, A → skills 1–4.
                if (_input.LightAttack.WasPressedThisFrame()) Buffer(0);
                if (_input.HeavyAttack.WasPressedThisFrame()) Buffer(1);
                if (_input.Dodge.WasPressedThisFrame()) Buffer(2);
                if (_input.Jump.WasPressedThisFrame()) Buffer(3);
            }
            bool triggers = _input.TriggerLeft.ReadValue<float>() > TriggerThreshold && _input.TriggerRight.ReadValue<float>() > TriggerThreshold;
            if (triggers && !_triggersWereDown) Buffer(PlayerStats.UltimateSlot);
            _triggersWereDown = triggers;
        }

        /// <summary>Touch buttons and other UI call this.</summary>
        public void Buffer(int slot)
        {
            _bufferedSlot = slot;
            _bufferTimer = InputBuffer;
        }

        public float CooldownRemaining(SkillData skill) =>
            skill != null && _cooldowns.TryGetValue(skill.id, out float t) ? t : 0f;

        public float CooldownRatio(SkillData skill)
        {
            if (skill == null) return 0f;
            float total = skill.cooldown * (1f - stats.Derived.cooldownReduction);
            return total > 0f ? CooldownRemaining(skill) / total : 0f;
        }

        public void ResetCooldowns() => _cooldowns.Clear();

        public CastResult TryCastSlot(int slot)
        {
            SkillData skill = stats.SlotSkill(slot);
            if (skill == null) return CastResult.EmptySlot;
            int level = stats.SkillLevel(skill.id);
            if (level <= 0 || skill.IsPassive) return CastResult.Locked;
            if (CooldownRemaining(skill) > 0f) return CastResult.Cooldown;
            if (!stats.Mana.CanPay(stats.MpCost(skill))) return CastResult.NoMp;
            if (player.IsDead || !player.CanCastSkill) return CastResult.Busy;
            Cast(skill, level);
            return CastResult.Ok;
        }

        readonly List<Jobs.Minion> _familiars = new List<Jobs.Minion>();
        readonly List<Jobs.Minion> _shadows = new List<Jobs.Minion>();

        void Summon(SkillData skill, int level)
        {
            _familiars.RemoveAll(m => m == null || !m.Alive);
            int cap = skill.summonCount + level / 4;
            var cat = Enemies.EnemyCatalog.Instance;
            for (int i = _familiars.Count; i < cap && cat != null; i++)
            {
                Vector3 at = transform.position + Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 2f;
                Enemies.Enemy body = cat.Spawn(skill.summonType, at, stats.Progression.level, 0f, false);
                if (body != null) _familiars.Add(Jobs.Minion.Convert(body, 0.35f, 1f + 0.05f * level, null));
            }
            UI.Codex.Notify("Come forth.", UI.CodexTone.System);
        }

        void Extract(int level)
        {
            const float range = 8f, window = 30f;
            for (int i = Enemies.Enemy.Fallen.Count - 1; i >= 0; i--)
            {
                var f = Enemies.Enemy.Fallen[i];
                if (Time.time - f.time > window || (f.at - transform.position).sqrMagnitude > range * range) continue;
                Enemies.Enemy.Fallen.RemoveAt(i);
                stats.Shadows.Bind(f.type, f.level);
                UI.Codex.Notify("\"Rise, Echo.\"  A shadow answers.", UI.CodexTone.Anomaly);
                return;
            }
            UI.Codex.Notify("No fallen shadow answers here.", UI.CodexTone.Info);
        }

        void CallShadows(SkillData skill, int level)
        {
            _shadows.RemoveAll(m => m == null || !m.Alive);
            int cap = skill.summonCount + level / 3;
            var cat = Enemies.EnemyCatalog.Instance;
            int called = 0;
            foreach (Jobs.ShadowArmy.Soldier s in stats.Shadows.soldiers)
            {
                if (_shadows.Count >= cap || cat == null) break;
                if (_shadows.Exists(m => m.Soldier == s)) continue;
                Vector3 at = transform.position + Quaternion.Euler(0f, _shadows.Count * 60f, 0f) * Vector3.back * 2.5f;
                Enemies.Enemy body = cat.Spawn(s.enemyType, at, s.level, 0f, false);
                if (body == null) continue;
                _shadows.Add(Jobs.Minion.Convert(body, 0.4f, Jobs.ShadowArmy.RankPower[s.rank], s));
                called++;
            }
            UI.Codex.Notify(called > 0 ? "\"Answer me.\"" : "Your army is empty.", UI.CodexTone.Anomaly);
        }

        void Cast(SkillData skill, int level)
        {
            stats.Mana.TryPay(stats.MpCost(skill));
            _cooldowns[skill.id] = skill.cooldown * (1f - stats.Derived.cooldownReduction);
            float now = Time.time;
            SkillComboData combo = stats.Class.FindCombo(_lastSkill, now - _lastSkillTime, skill.id);
            _lastSkill = skill.id;
            _lastSkillTime = now;
            if (combo != null) ComboTriggered?.Invoke(combo);
            SkillUsed?.Invoke(skill);
            Core.GameEvents.RaiseSkillUsed(skill.id);

            switch (skill.kind)
            {
                case SkillKind.Buff:
                    stats.AddBuff(skill.buffStat, skill.BuffAt(level), skill.buffDuration);
                    return;
                case SkillKind.Heal:
                    var health = GetComponent<Combat.Damageable>();
                    if (health != null) health.Health.Heal(Mathf.RoundToInt(health.Health.maxHealth * (skill.healShare + skill.multiplierPerLevel * (level - 1))));
                    return;
                case SkillKind.Stealth:
                    PlayerTarget.HiddenUntil = Time.time + skill.stealthDuration;
                    combat.GrantCounter();
                    return;
                case SkillKind.Trap:
                    combat.PlaceTrap(skill, level, transform.position);
                    return;
                case SkillKind.Summon:
                    Summon(skill, level);
                    return;
                case SkillKind.ShadowExtract:
                    Extract(level);
                    return;
                case SkillKind.ShadowCall:
                    CallShadows(skill, level);
                    return;
            }
            if (!skill.IsDamaging) return;
            combat.QueueSkill(skill, level, combo);
            player.StartQueuedSkill();
        }
    }
}
