using System.Collections.Generic;
using Aethvirse.Combat;
using Aethvirse.Core;
using Aethvirse.Enemies;
using Aethvirse.Interaction;
using Aethvirse.Items;
using Aethvirse.Player;
using Aethvirse.Save;
using Aethvirse.Stats;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Gates
{
    /// <summary>
    /// Runs a Gate dungeon (§7): builds the layout as floating platforms and bridges over a pit, wakes each room's
    /// monsters when you enter, runs the boss with phases, the treasure and secret rooms, and the exit.
    /// Red Gates have no way out until the boss falls.
    /// </summary>
    public sealed class DungeonRun : MonoBehaviour
    {
        [SerializeField] Material floorMat;
        [SerializeField] Material bridgeMat;
        [SerializeField] Material pillarMat;
        [SerializeField] Material secretMat;
        [SerializeField] Material portalMat;

        DungeonLayout _layout;
        OpenGate _gate;
        int _level;
        string[] _types;
        readonly HashSet<int> _woken = new HashSet<int>();
        Enemy _boss;
        int _phase;
        bool _cleared;
        GameObject _exit;
        readonly System.Random _rng = new System.Random();

        void Start()
        {
            _gate = GateSession.Current ?? new OpenGate { uid = -1, rank = 0, seed = Random.Range(0, 99999), doubleGate = true };
            _level = GateSession.Current != null ? GateSession.Level : (PlayerStats.Current != null ? PlayerStats.Current.Progression.level : 1);
            _types = GateSession.EnemyTypes.Length > 0 ? GateSession.EnemyTypes : new[] { "ash_hound", "ember_husk" };
            _layout = DungeonLayout.Generate(_gate.seed, _gate.rank, _gate.doubleGate);
            BuildGeometry();
            Transform player = PlayerTarget.Current;
            if (player != null) player.GetComponent<PlayerController>().Teleport(_layout.Start.Center + Vector3.up * 0.2f);
            _exit = Portal("Exit", _layout.Start.Center + new Vector3(0f, 2f, -4f), "Leave the Gate");
            if (_gate.red) _exit.SetActive(false);
            Codex.Notify($"Rank {WorldGates.RankNames[_gate.rank]} Gate · {_layout.rooms.Count} rooms · {_layout.shape}", CodexTone.System);
        }

        // --- Geometry ----------------------------------------------------------------------------------

        void BuildGeometry()
        {
            var root = new GameObject("Dungeon").transform;
            foreach (DungeonLayout.Room r in _layout.rooms) BuildRoom(root, r);
            foreach (DungeonLayout.Corridor c in _layout.corridors)
                for (int i = 1; i < c.points.Count; i++) Bridge(root, c.points[i - 1], c.points[i], c.hidden);
        }

        void BuildRoom(Transform root, DungeonLayout.Room r)
        {
            Material m = r.kind == RoomKind.Secret ? secretMat : floorMat;
            Vector3 c = r.Center;
            float w = r.size.x, d = r.size.y;
            switch (r.shape)
            {
                case RoomShape.Cross:
                    Slab(root, c, new Vector3(w, 1f, d * 0.4f), m);
                    Slab(root, c, new Vector3(w * 0.4f, 1f, d), m);
                    break;
                case RoomShape.LShape:
                    Slab(root, c + new Vector3(-w * 0.2f, 0f, 0f), new Vector3(w * 0.6f, 1f, d), m);
                    Slab(root, c + new Vector3(0f, 0f, -d * 0.25f), new Vector3(w, 1f, d * 0.5f), m);
                    break;
                case RoomShape.Round:
                    for (int i = 0; i < 6; i++)
                        Slab(root, c, new Vector3(w * 0.75f, 1f, d * 0.75f), m).transform.rotation = Quaternion.Euler(0f, i * 15f, 0f);
                    break;
                case RoomShape.Pillared:
                    Slab(root, c, new Vector3(w, 1f, d), m);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 p = c + new Vector3((i % 2 == 0 ? -1 : 1) * w * 0.28f, 2f, (i < 2 ? -1 : 1) * d * 0.28f);
                        Block(root, p, new Vector3(1.2f, 4f, 1.2f), pillarMat);
                    }
                    break;
                default:
                    Slab(root, c, new Vector3(w, 1f, d), m);
                    break;
            }
            // A little cover in normal rooms.
            if (r.kind == RoomKind.Normal && r.shape != RoomShape.Pillared)
                Block(root, c + new Vector3(w * 0.2f, 1f, d * 0.15f), new Vector3(2f, 2f, 2f), pillarMat);
            if (r.kind == RoomKind.Treasure) Chest(c, "treasure");
            if (r.kind == RoomKind.Secret) Chest(c, "secret");
        }

        GameObject Slab(Transform root, Vector3 center, Vector3 size, Material m) => Block(root, center + Vector3.down * 0.5f, size, m);

        static GameObject Block(Transform root, Vector3 center, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(root, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        void Bridge(Transform root, Vector3 a, Vector3 b, bool hidden)
        {
            Vector3 mid = (a + b) * 0.5f;
            Vector3 dir = b - a;
            float len = dir.magnitude;
            if (len < 0.1f) return;
            GameObject bridge = Block(root, mid + Vector3.down * 0.5f, new Vector3(4.5f, 1f, len), bridgeMat);
            bridge.transform.rotation = Quaternion.LookRotation(dir);
            if (!hidden) return;
            // The secret room sits behind a cracked wall on its bridge: break it with attacks.
            GameObject wall = Block(root, mid + Vector3.up * 1.5f, new Vector3(4.5f, 3f, 0.6f), secretMat);
            wall.transform.rotation = Quaternion.LookRotation(dir);
            wall.name = "CrackedWall";
            var dmg = wall.AddComponent<Damageable>();
            dmg.Configure(1, new CombatStats { defense = 0f });
            dmg.Health.Died += () =>
            {
                Codex.Notify("The wall crumbles. A hidden room!", CodexTone.Anomaly);
                GameEvents.RaiseAreaEntered("gate_secret_room");
                Destroy(wall);
            };
        }

        void Chest(Vector3 at, string kind)
        {
            GameObject chest = Block(transform, at + new Vector3(0f, 0.45f, 2f), new Vector3(1.4f, 0.9f, 1f), kind == "secret" ? secretMat : pillarMat);
            chest.name = "Chest_" + kind;
            var i = chest.AddComponent<Interactable>();
            i.Prompt = kind == "secret" ? "Open the sealed chest" : "Open the chest";
            i.Used += _ =>
            {
                i.Available = false;
                PlayerInventory inv = PlayerInventory.Current;
                if (inv == null) return;
                EnemyTier tier = kind == "secret" ? EnemyTier.Boss : EnemyTier.Elite;
                foreach (ItemStack s in ItemActions.RollDrops(_level, tier, inv.Database, inv.Rules, _rng)) WorldPickup.Spawn(at + Vector3.up, s);
                WorldPickup.Spawn(at + Vector3.up, new ItemStack { itemId = "gold", count = 40 * (_gate.rank + 1) });
                if (kind == "secret") Codex.Notify("Something in the dark approves of you.", CodexTone.Anomaly);
                chest.transform.localScale = new Vector3(1.4f, 0.3f, 1f);
            };
        }

        GameObject Portal(string name, Vector3 at, string prompt)
        {
            GameObject p = Block(transform, at, new Vector3(3f, 3f, 0.2f), portalMat);
            p.name = name;
            Destroy(p.GetComponent<Collider>());
            var i = p.AddComponent<Interactable>();
            i.Prompt = prompt;
            i.Range = 3.5f;
            i.Used += _ => Leave();
            return p;
        }

        // --- Rooms, boss, clear -------------------------------------------------------------------------

        void Update()
        {
            Transform player = PlayerTarget.Current;
            if (player == null || _layout == null) return;
            foreach (DungeonLayout.Room r in _layout.rooms)
            {
                if (_woken.Contains(r.index) || r.kind == RoomKind.Start || r.kind == RoomKind.Secret) continue;
                Vector3 to = player.position - r.Center;
                if (Mathf.Abs(to.x) > r.size.x * 0.5f || Mathf.Abs(to.z) > r.size.y * 0.5f) continue;
                _woken.Add(r.index);
                Wake(r);
            }
            UpdateBoss();
        }

        void Wake(DungeonLayout.Room r)
        {
            EnemyCatalog cat = EnemyCatalog.Instance;
            if (cat == null) return;
            if (r.kind == RoomKind.Boss)
            {
                _boss = cat.Spawn(_types[_rng.Next(_types.Length)], r.Center + Vector3.forward * 6f, _level + 2, 60f);
                if (_boss != null) _boss.MakeBoss(8f + _gate.rank * 2f, 1.6f, 1.7f);
                Codex.Notify("[ BOSS ] The master of this Gate wakes.", CodexTone.Warning);
                return;
            }
            int count = 2 + _gate.rank / 2 + (_gate.red ? 2 : 0);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = r.Center + new Vector3((float)_rng.NextDouble() * 8f - 4f, 0.2f, (float)_rng.NextDouble() * 8f - 4f);
                cat.Spawn(_types[_rng.Next(_types.Length)], p, _level + (_gate.red ? 5 : 0), 30f);
            }
        }

        void UpdateBoss()
        {
            if (_boss == null || _cleared) return;
            if (_boss.State == EnemyState.Dead) { Clear(); return; }
            float hp = _boss.HealthRatio;
            int phase = hp <= 0.15f ? 3 : hp <= 0.4f ? 2 : hp <= 0.7f ? 1 : 0;
            if (phase <= _phase) return;
            _phase = phase;
            _boss.Enrage(1.15f, 0.8f);
            Codex.Notify($"The boss changes — phase {phase + 1}!", CodexTone.Warning);
            if (phase == 2 && EnemyCatalog.Instance != null)
                for (int i = 0; i < 2 + _gate.rank / 2; i++)
                    EnemyCatalog.Instance.Spawn(_types[_rng.Next(_types.Length)], _boss.transform.position + Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward * 5f, _level, 40f);
        }

        void Clear()
        {
            _cleared = true;
            Codex.Notify("[ GATE CLEARED ]", CodexTone.Reward);
            GameEvents.RaiseGateCleared(WorldGates.RankNames[_gate.rank]);
            GameSession.Instance.AddGold(80 * (_gate.rank + 1) * (_gate.red ? 3 : 1));
            Quests.QuestSystem.Instance.AddAcademyPoints(15 + 10 * _gate.rank);
            if (_gate.uid >= 0) GameSession.Instance.Gates.Close(_gate.uid);
            PlayerInventory inv = PlayerInventory.Current;
            if (inv != null)
                foreach (ItemStack s in ItemActions.RollDrops(_level, EnemyTier.Boss, inv.Database, inv.Rules, _rng))
                    WorldPickup.Spawn(_boss.transform.position, s);
            _exit.SetActive(true);
            _exit.transform.position = _layout.Boss.Center + new Vector3(0f, 2f, -8f);
        }

        void Leave()
        {
            GateSession.End();
            SceneTravel.Return();
        }
    }
}
