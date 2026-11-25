using System.Collections.Generic;
using Aethvirse.Combat;
using Aethvirse.Player;
using UnityEngine;

namespace Aethvirse.Enemies
{
    public enum EliteAffix { Berserk, Shielded, Vampiric, Splitter, Teleporter }

    /// <summary>
    /// Elite enemies (§6): tougher, named, with one or two random affixes — Berserk (enrages at half health),
    /// Shielded (takes 60 % less until eight hits break the shield), Vampiric (heals from its blows), Splitter (splits
    /// in two when it dies), Teleporter (blinks behind the hero).
    /// </summary>
    public sealed class EliteAffixes : MonoBehaviour
    {
        public const float Chance = 0.08f;
        const int ShieldHits = 8;

        readonly List<EliteAffix> _affixes = new List<EliteAffix>();
        Enemy _enemy;
        Damageable _self;
        bool _berserk;
        int _shieldHits;
        float _blinkTimer = 4f;

        public IReadOnlyList<EliteAffix> Affixes => _affixes;

        public static string Prefix(IReadOnlyList<EliteAffix> list)
        {
            if (list == null || list.Count == 0) return "";
            var words = new List<string>();
            foreach (EliteAffix a in list) words.Add(a.ToString());
            return string.Join(" ", words) + " ";
        }

        /// <summary>Picks 1–2 different affixes.</summary>
        public static List<EliteAffix> Roll(System.Random rng)
        {
            var all = new List<EliteAffix>((EliteAffix[])System.Enum.GetValues(typeof(EliteAffix)));
            var picked = new List<EliteAffix>();
            int n = rng.NextDouble() < 0.35 ? 2 : 1;
            for (int i = 0; i < n; i++)
            {
                int k = rng.Next(all.Count);
                picked.Add(all[k]);
                all.RemoveAt(k);
            }
            return picked;
        }

        /// <summary>Makes an enemy elite with the given affixes.</summary>
        public static EliteAffixes Make(Enemy e, List<EliteAffix> affixes)
        {
            var c = e.gameObject.AddComponent<EliteAffixes>();
            c._affixes.AddRange(affixes);
            c._enemy = e;
            c._self = e.GetComponent<Damageable>();
            e.MakeBoss(2.5f, 1.3f, 1.15f);
            e.Elite = c;
            if (affixes.Contains(EliteAffix.Shielded))
            {
                c._self.DamageTakenMultiplier = 0.4f;
                c._self.HitReceived += (info, applied) =>
                {
                    if (!applied || c._shieldHits >= ShieldHits) return;
                    if (++c._shieldHits >= ShieldHits)
                    {
                        c._self.DamageTakenMultiplier = 1f;
                        DamageNumbers.Label(e.transform.position + Vector3.up * 2f, "SHIELD BROKEN", new Color(0.6f, 0.85f, 1f));
                    }
                };
            }
            if (affixes.Contains(EliteAffix.Vampiric))
                e.DealtDamage += amount => c._self.Health.Heal(Mathf.Max(1, amount / 4));
            if (affixes.Contains(EliteAffix.Splitter))
                c._self.Health.Died += c.Split;
            return c;
        }

        void Update()
        {
            if (_enemy == null || _enemy.State == EnemyState.Dead) return;
            if (_affixes.Contains(EliteAffix.Berserk) && !_berserk && _enemy.HealthRatio <= 0.5f)
            {
                _berserk = true;
                _enemy.Enrage(1.35f, 0.6f);
                DamageNumbers.Label(transform.position + Vector3.up * 2f, "BERSERK", new Color(1f, 0.3f, 0.2f));
            }
            if (_affixes.Contains(EliteAffix.Teleporter) && _enemy.State == EnemyState.Chase)
            {
                _blinkTimer -= Time.deltaTime;
                Transform p = PlayerTarget.Visible;
                if (_blinkTimer <= 0f && p != null && (p.position - transform.position).magnitude > 4f)
                {
                    _blinkTimer = 6f;
                    Vector3 behind = p.position - p.forward * 2.5f;
                    _enemy.Teleport(behind);
                }
            }
        }

        void Split()
        {
            if (EnemyCatalog.Instance == null) return;
            for (int i = 0; i < 2; i++)
            {
                Vector3 at = transform.position + new Vector3(i == 0 ? -1.2f : 1.2f, 0.1f, 0f);
                Enemy small = EnemyCatalog.Instance.Spawn(_enemy.Data.id, at, _enemy.CurrentLevel, 20f, false);
                if (small == null) continue;
                small.RespawnAtHome = false;
                small.Shrink(0.65f);
            }
        }
    }
}
