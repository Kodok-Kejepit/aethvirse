using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Combat
{
    /// <summary>Floating damage numbers: pooled 3D texts that rise, face the camera and fade.</summary>
    public sealed class DamageNumbers : MonoBehaviour
    {
        const int PoolSize = 24;
        const float Lifetime = 0.8f;
        const float RiseSpeed = 1.6f;
        const float BaseCharSize = 0.06f;

        sealed class Entry
        {
            public TextMesh text;
            public float age;
            public Color color;
            public float scale;
        }

        static DamageNumbers _instance;
        readonly List<Entry> _pool = new List<Entry>();
        int _next;

        /// <summary><paramref name="onPlayer"/> = damage the player took (shown red).</summary>
        public static void Spawn(Vector3 position, int amount, bool crit, ElementType element, bool onPlayer)
        {
            if (_instance == null)
            {
                var go = new GameObject("DamageNumbers");
                _instance = go.AddComponent<DamageNumbers>();
            }
            _instance.Show(position, amount, crit, element, onPlayer);
        }

        void Awake()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Number");
                go.transform.SetParent(transform, false);
                var text = go.AddComponent<TextMesh>();
                text.font = font;
                text.fontSize = 64;
                text.characterSize = BaseCharSize;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                go.SetActive(false);
                _pool.Add(new Entry { text = text });
            }
        }

        void Show(Vector3 position, int amount, bool crit, ElementType element, bool onPlayer)
        {
            Entry e = _pool[_next];
            _next = (_next + 1) % _pool.Count;
            e.age = 0f;
            e.scale = crit ? 1.6f : 1f;
            e.color = onPlayer ? new Color(1f, 0.3f, 0.3f) : crit ? new Color(1f, 0.85f, 0.2f) : Element.ColorOf(element);
            e.text.text = crit ? amount + "!" : amount.ToString();
            // A little sideways scatter so quick hits do not stack on one spot.
            e.text.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            e.text.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            Camera cam = Camera.main;
            float dt = Time.unscaledDeltaTime;
            foreach (Entry e in _pool)
            {
                if (!e.text.gameObject.activeSelf) continue;
                e.age += dt;
                if (e.age >= Lifetime)
                {
                    e.text.gameObject.SetActive(false);
                    continue;
                }
                Transform t = e.text.transform;
                t.position += Vector3.up * (RiseSpeed * dt);
                if (cam != null) t.rotation = cam.transform.rotation;
                // Pop in, then fade out.
                float pop = e.age < 0.1f ? Mathf.Lerp(1.4f, 1f, e.age / 0.1f) : 1f;
                t.localScale = Vector3.one * (e.scale * pop);
                Color c = e.color;
                c.a = 1f - Mathf.Clamp01((e.age - Lifetime * 0.5f) / (Lifetime * 0.5f));
                e.text.color = c;
            }
        }
    }
}
