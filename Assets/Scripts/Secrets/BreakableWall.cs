using Aethvirse.Combat;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Secrets
{
    /// <summary>A cracked wall (§9 secret rooms): heavy hits break it; breaking it sets a flag (which finds its secret).</summary>
    public sealed class BreakableWall : MonoBehaviour
    {
        [SerializeField] string brokenFlag = "";
        [SerializeField] int hitPoints = 60;

        void Start()
        {
            if (GameSession.Instance.Flags.Has(brokenFlag)) { gameObject.SetActive(false); return; }
            var d = gameObject.AddComponent<Damageable>();
            d.Configure(hitPoints, new CombatStats { defense = 0f });
            // Only heavy blows count.
            d.HitReceived += (info, applied) => { if (!info.isHeavy) d.Health.Heal(info.amount); };
            d.Health.Died += () =>
            {
                GameSession.Instance.Flags.SetFlag(brokenFlag);
                Codex.Notify("The wall gives way.", CodexTone.Anomaly);
                gameObject.SetActive(false);
            };
        }
    }
}
