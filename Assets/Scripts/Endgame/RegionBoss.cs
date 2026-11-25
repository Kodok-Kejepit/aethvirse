using Aethvirse.Enemies;
using Aethvirse.Player;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Endgame
{
    /// <summary>The guardian at the end of a world: wakes when you come near, grows angrier per phase, sets a flag when it falls.</summary>
    public sealed class RegionBoss : MonoBehaviour
    {
        [SerializeField] string bossType = "";
        [SerializeField] string defeatedFlag = "";
        [SerializeField] string wakeLine = "Something old opens its eyes.";
        [SerializeField] string fallLine = "[ SYSTEM UPDATE ]  The way onward is open.";
        [SerializeField] float hpMultiplier = 40f;
        [SerializeField] float wakeRange = 25f;

        Enemy _boss;
        int _phase;

        void Update()
        {
            WorldFlags flags = GameSession.Instance.Flags;
            if (flags.Has(defeatedFlag)) return;
            Transform p = PlayerTarget.Current;
            if (_boss == null)
            {
                if (p == null || EnemyCatalog.Instance == null || (p.position - transform.position).magnitude > wakeRange) return;
                RegionData r = RegionController.Current;
                _boss = EnemyCatalog.Instance.Spawn(bossType, transform.position, r != null ? r.levelMax : 100, 60f, true);
                if (_boss == null) return;
                _boss.MakeBoss(hpMultiplier, 2f, 2f);
                Codex.Notify(wakeLine, CodexTone.Anomaly);
                return;
            }
            if (_boss.State == EnemyState.Dead)
            {
                flags.SetFlag(defeatedFlag);
                Codex.Notify(fallLine, CodexTone.System);
                return;
            }
            int phase = BossPhases.For(_boss.HealthRatio);
            if (phase > _phase) { _phase = phase; _boss.Enrage(1.15f, 0.8f); Codex.Notify("It is angry now.", CodexTone.Anomaly); }
        }
    }
}
