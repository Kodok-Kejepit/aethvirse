using System.Collections;
using Aethvirse.Combat;
using Aethvirse.Enemies;
using Aethvirse.Player;
using UnityEngine;

namespace Aethvirse.Endgame
{
    /// <summary>
    /// Boss presentation and arena mechanics (§6 boss): a short intro (slow motion + name card), a big health bar with
    /// name and title, and per phase the arena turns on you — stones fall on marked circles (from phase 2), more of
    /// them and faster with adds joining (phase 3). Every hazard is telegraphed for a fair dodge.
    /// </summary>
    public sealed class BossArena : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static BossArena Active { get; private set; }

        Enemy _boss;
        string _name, _title, _addType;
        float _introUntil;
        float _hazardTimer = 6f;
        GUIStyle _big, _small;

        public static BossArena Begin(Enemy boss, string name, string title, string addType = "")
        {
            var a = boss.gameObject.AddComponent<BossArena>();
            a._boss = boss;
            a._name = name;
            a._title = title;
            a._addType = addType;
            a._introUntil = Time.unscaledTime + 2.6f;
            Active = a;
            TimeFx.SlowMotion(0.35f, 1.2f);
            return a;
        }

        static readonly System.Collections.Generic.Dictionary<string, (string title, string add)> Titles = new System.Collections.Generic.Dictionary<string, (string, string)>
        {
            { "the_unwritten", ("The Throne That Was Never Filled", "hollow_echo") },
            { "the_conductor", ("Last of the Silent Choir", "bell_wraith") },
            { "kethra_warden", ("Keeper of the Last City", "hunted_sentinel") },
            { "the_god", ("The One Who Wrote You", "seraph_husk") },
        };

        /// <summary>Begins with the boss's own name and its title (unknown bosses: "Master of the Gate").</summary>
        public static BossArena Begin(Enemy boss)
        {
            string id = boss.Data != null ? boss.Data.id : "";
            var (title, add) = Titles.TryGetValue(id, out var t) ? t : ("Master of the Gate", id);
            return Begin(boss, boss.Data != null ? boss.Data.displayName : "???", title, add);
        }

        /// <summary>Seconds between falling-stone volleys and how many stones, by phase (pure; tested).</summary>
        public static (float every, int stones) Hazard(int phase) => phase switch
        {
            0 => (0f, 0), 1 => (0f, 0), 2 => (6f, 3), _ => (4f, 5),
        };

        void OnDestroy() { if (Active == this) Active = null; }

        void Update()
        {
            if (_boss == null || _boss.State == EnemyState.Dead) { if (Active == this) Active = null; return; }
            int phase = BossPhases.For(_boss.HealthRatio);
            var (every, stones) = Hazard(phase);
            if (every <= 0f) return;
            _hazardTimer -= Time.deltaTime;
            if (_hazardTimer > 0f) return;
            _hazardTimer = every;
            Transform p = PlayerTarget.Current;
            if (p == null) return;
            for (int i = 0; i < stones; i++)
            {
                Vector2 r = Random.insideUnitCircle * (i == 0 ? 0.5f : 7f);
                StartCoroutine(Stone(p.position + new Vector3(r.x, 0f, r.y)));
            }
            if (phase >= 3 && !string.IsNullOrEmpty(_addType) && EnemyCatalog.Instance != null)
            {
                Enemy add = EnemyCatalog.Instance.Spawn(_addType, _boss.transform.position + Random.onUnitSphere.normalized * 6f, Mathf.Max(1, _boss.CurrentLevel - 5), 40f, false);
                if (add != null) add.RespawnAtHome = false;
            }
        }

        /// <summary>A red circle for 1.1 s, then a stone lands there.</summary>
        IEnumerator Stone(Vector3 at)
        {
            at.y = 0.05f;
            var mark = Aethvirse.Core.RuntimeMat.Primitive(PrimitiveType.Cylinder);
            mark.transform.position = at;
            mark.transform.localScale = new Vector3(3.2f, 0.02f, 3.2f);
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, new Color(0.9f, 0.15f, 0.1f));
            mark.GetComponent<Renderer>().SetPropertyBlock(block);
            yield return new WaitForSeconds(1.1f);
            Destroy(mark);
            var rock = Aethvirse.Core.RuntimeMat.Primitive(PrimitiveType.Cube);
            rock.transform.position = at + Vector3.up * 0.6f;
            rock.transform.localScale = Vector3.one * 1.4f;
            rock.transform.rotation = Random.rotation;
            Destroy(rock, 1.2f);
            Transform p = PlayerTarget.Current;
            if (p == null || _boss == null) yield break;
            Vector3 d = p.position - at;
            d.y = 0f;
            if (d.magnitude > 1.7f) yield break;
            var target = p.GetComponent<Damageable>();
            var self = _boss.GetComponent<Damageable>();
            if (target == null || self == null) yield break;
            DamageInfo info = DamageCalculator.Resolve(self.Stats, target.Stats, 1.2f, ElementType.Earth, new System.Random());
            info.stagger = 50f;
            info.launch = 4f;
            info.hitPosition = p.position;
            target.Receive(info);
        }

        void OnGUI()
        {
            if (_boss == null || _boss.State == EnemyState.Dead) return;
            float s = Screen.height / 1080f;
            _big ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _small ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            // Intro card.
            if (Time.unscaledTime < _introUntil)
            {
                float a = Mathf.Clamp01((_introUntil - Time.unscaledTime) / 0.6f);
                GUI.color = new Color(0f, 0f, 0f, 0.55f * a);
                GUI.DrawTexture(new Rect(0, Screen.height * 0.36f, Screen.width, Screen.height * 0.2f), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.85f, 0.6f, a);
                _big.fontSize = Mathf.RoundToInt(64 * s);
                GUI.Label(new Rect(0, Screen.height * 0.37f, Screen.width, 90 * s), _name, _big);
                GUI.color = new Color(1f, 1f, 1f, a);
                _small.fontSize = Mathf.RoundToInt(28 * s);
                GUI.Label(new Rect(0, Screen.height * 0.37f + 90 * s, Screen.width, 40 * s), _title, _small);
                GUI.color = Color.white;
            }
            // Health bar.
            float w = Screen.width * 0.5f, x = (Screen.width - w) * 0.5f, y = Core.DebugOverlay.Shown ? 176f : 70 * s;
            _small.fontSize = Mathf.RoundToInt(22 * s);
            GUI.Label(new Rect(x, y - 32 * s, w, 30 * s), $"{_name}  —  {_title}", _small);
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(x, y, w, 18 * s), Texture2D.whiteTexture);
            GUI.color = new Color(0.85f, 0.15f, 0.2f);
            GUI.DrawTexture(new Rect(x + 2, y + 2, (w - 4) * Mathf.Clamp01(_boss.HealthRatio), 18 * s - 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
            // Phase marks at 70 / 40 / 15 %.
            foreach (float m in new[] { 0.7f, 0.4f, 0.15f }) GUI.DrawTexture(new Rect(x + w * m, y, 2, 18 * s), Texture2D.whiteTexture);
        }
    }
}
