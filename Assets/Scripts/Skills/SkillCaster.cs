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
            if (!stats.Mana.CanPay(skill.mpCost)) return CastResult.NoMp;
            if (player.IsDead || !player.CanCastSkill) return CastResult.Busy;
            Cast(skill, level);
            return CastResult.Ok;
        }

        void Cast(SkillData skill, int level)
        {
            stats.Mana.TryPay(skill.mpCost);
            _cooldowns[skill.id] = skill.cooldown * (1f - stats.Derived.cooldownReduction);
            float now = Time.time;
            SkillComboData combo = stats.Class.FindCombo(_lastSkill, now - _lastSkillTime, skill.id);
            _lastSkill = skill.id;
            _lastSkillTime = now;
            if (combo != null) ComboTriggered?.Invoke(combo);
            SkillUsed?.Invoke(skill);

            if (skill.kind == SkillKind.Buff)
            {
                stats.AddBuff(skill.buffStat, skill.BuffAt(level), skill.buffDuration);
                return;
            }
            if (!skill.IsDamaging) return;
            combat.QueueSkill(skill, level, combo);
            player.StartQueuedSkill();
        }
    }
}
