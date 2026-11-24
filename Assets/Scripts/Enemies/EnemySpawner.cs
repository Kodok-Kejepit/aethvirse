using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Enemies
{
    /// <summary>A camp: keeps up to <see cref="count"/> enemies of the listed types alive around it, respawning slowly.</summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] List<string> types = new List<string>();
        [SerializeField] int count = 3;
        [SerializeField] int level = 1;
        [SerializeField] float radius = 6f;
        [SerializeField] float respawnDelay = 45f;
        [Tooltip("Sleeps beyond this distance from the player (performance).")]
        [SerializeField] float activeRange = 90f;

        readonly List<Enemy> _alive = new List<Enemy>();
        readonly System.Random _rng = new System.Random();
        float _timer;

        public void Configure(List<string> enemyTypes, int amount, int enemyLevel, float spread)
        {
            types = enemyTypes;
            count = amount;
            level = enemyLevel;
            radius = spread;
        }

        void Start()
        {
            for (int i = 0; i < count; i++) SpawnOne();
        }

        void Update()
        {
            _alive.RemoveAll(e => e == null || e.State == EnemyState.Dead);
            if (_alive.Count >= count) return;
            Transform p = Player.PlayerTarget.Current;
            if (p != null && (p.position - transform.position).sqrMagnitude > activeRange * activeRange) return;
            _timer += Time.deltaTime;
            if (_timer < respawnDelay) return;
            _timer = 0f;
            SpawnOne();
        }

        void SpawnOne()
        {
            EnemyCatalog catalog = EnemyCatalog.Instance;
            if (catalog == null || types.Count == 0) return;
            float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
            Vector3 at = transform.position + new Vector3(Mathf.Cos(a), 0.1f, Mathf.Sin(a)) * ((float)_rng.NextDouble() * radius);
            Enemy e = catalog.Spawn(types[_rng.Next(types.Count)], at, level);
            if (e != null) _alive.Add(e);
        }
    }
}
