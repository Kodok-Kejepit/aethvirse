using System.Collections.Generic;
using Aethvirse.Player;
using Aethvirse.Skills;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Fx
{
    /// <summary>
    /// The look of every skill (owner: each skill must move and look different). Graybox effects from
    /// pooled pieces (rings, arcs, spikes, pillars, afterimages) on a transparent unlit material.
    /// </summary>
    public sealed class SkillFx : MonoBehaviour
    {
        [SerializeField] SkillCaster caster;
        [SerializeField] PlayerCombat combat;
        [SerializeField] PlayerStats stats;
        [SerializeField] Transform visual;
        [Tooltip("Mesh used for afterimages (the body).")]
        [SerializeField] MeshFilter bodyMesh;
        [Tooltip("Blade renderer: glows during Flame Edge.")]
        [SerializeField] Renderer blade;
        [Tooltip("Transparent unlit template (keeps the shader variant in builds).")]
        [SerializeField] Material fxMaterial;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        const int PoolSize = 64;

        sealed class Piece
        {
            public Transform t;
            public MeshFilter filter;
            public MeshRenderer renderer;
            public float age, life;
            public Vector3 startScale, endScale, velocity;
            public Color color;
            public float spin;
            public bool active;
        }

        readonly List<Piece> _pool = new List<Piece>();
        MaterialPropertyBlock _block;
        int _next;
        float _afterimageTime;
        float _afterimageTimer;
        Piece _aura;
        Color _bladeBase;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            // Pieces live in world space: they must not ride along with the player.
            Transform container = new GameObject("SkillFxPool").transform;
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Fx");
                go.transform.SetParent(container, false);
                var piece = new Piece
                {
                    t = go.transform,
                    filter = go.AddComponent<MeshFilter>(),
                    renderer = go.AddComponent<MeshRenderer>(),
                };
                piece.renderer.sharedMaterial = fxMaterial;
                piece.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(false);
                _pool.Add(piece);
            }
            if (blade != null) _bladeBase = blade.sharedMaterial.GetColor(BaseColorId);
        }

        void OnEnable()
        {
            if (caster != null) caster.SkillUsed += OnCast;
            if (combat != null) combat.SkillStrike += OnStrike;
        }

        void OnDisable()
        {
            if (caster != null) caster.SkillUsed -= OnCast;
            if (combat != null) combat.SkillStrike -= OnStrike;
        }

        Vector3 Pos => transform.position;
        Vector3 Fwd => visual != null ? visual.forward : transform.forward;
        Quaternion Facing => Quaternion.LookRotation(Fwd, Vector3.up);

        // --- Cast: what happens the moment the button is pressed -----------------------------------

        void OnCast(SkillData s)
        {
            switch (s.fx)
            {
                case SkillFxKind.Charge:
                    _afterimageTime = 0.45f;
                    break;
                case SkillFxKind.Roar:
                    Spawn(FxMeshes.Ring, Pos + Vector3.up * 1.2f, Quaternion.identity, Vector3.one * 0.6f, Vector3.one * 6f, new Color(1f, 0.3f, 0.2f, 0.8f), 0.5f);
                    Spawn(FxMeshes.Ring, Pos + Vector3.up * 0.1f, Quaternion.identity, Vector3.one * 0.4f, Vector3.one * 4f, new Color(1f, 0.5f, 0.3f, 0.6f), 0.7f);
                    break;
                case SkillFxKind.SkyFall:
                    // A golden column comes down on you while you rise into the blow.
                    Spawn(FxMeshes.Cylinder, Pos + Vector3.up * 12f, Quaternion.identity, new Vector3(0.2f, 12f, 0.2f), new Vector3(2.4f, 12f, 2.4f), new Color(1f, 0.85f, 0.3f, 0.55f), 0.8f);
                    break;
            }
        }

        // --- Strike: when the skill's hit window opens ---------------------------------------------

        void OnStrike(SkillData s)
        {
            Vector3 chest = Pos + Vector3.up * 1.1f;
            switch (s.fx)
            {
                case SkillFxKind.Cleave:
                    Spawn(FxMeshes.Arc, chest + Fwd * 0.3f, Facing, Vector3.one * 1.2f, Vector3.one * 3.4f, new Color(0.5f, 0.8f, 1f, 0.85f), 0.3f);
                    break;
                case SkillFxKind.FlameArc:
                    Spawn(FxMeshes.Arc, chest + Fwd * 0.3f, Facing * Quaternion.Euler(0f, 0f, 35f), Vector3.one * 1f, Vector3.one * 2.8f, new Color(1f, 0.5f, 0.1f, 0.9f), 0.35f);
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 v = Fwd * Random.Range(1f, 3f) + Vector3.up * Random.Range(2f, 4f) + Random.insideUnitSphere;
                        Spawn(FxMeshes.Cube, chest + Fwd, Random.rotation, Vector3.one * 0.12f, Vector3.one * 0.02f, new Color(1f, 0.6f, 0.15f, 1f), 0.6f, v, 360f);
                    }
                    break;
                case SkillFxKind.GroundSplit:
                    Spawn(FxMeshes.Ring, Pos + Vector3.up * 0.05f, Quaternion.identity, Vector3.one * 0.5f, Vector3.one * 4.5f, new Color(0.8f, 0.55f, 0.25f, 0.85f), 0.45f);
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i * 36f * Mathf.Deg2Rad;
                        var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                        Spawn(FxMeshes.Cube, Pos + dir * Random.Range(1.5f, 3.5f), Quaternion.Euler(Random.Range(-20f, 20f), i * 36f, Random.Range(-20f, 20f)),
                            new Vector3(0.4f, 0.1f, 0.4f), new Vector3(0.3f, 1.6f, 0.3f), new Color(0.55f, 0.4f, 0.25f, 1f), 0.6f);
                    }
                    break;
                case SkillFxKind.SkyFall:
                    Spawn(FxMeshes.Ring, Pos + Vector3.up * 0.1f, Quaternion.identity, Vector3.one, Vector3.one * 7.5f, new Color(1f, 0.85f, 0.3f, 0.9f), 0.6f);
                    Spawn(FxMeshes.Ring, Pos + Vector3.up * 0.5f, Quaternion.identity, Vector3.one * 0.5f, Vector3.one * 5f, new Color(1f, 1f, 0.9f, 0.9f), 0.4f);
                    Spawn(FxMeshes.Cylinder, Pos + Vector3.up * 12f, Quaternion.identity, new Vector3(3f, 12f, 3f), new Vector3(0.1f, 12f, 0.1f), new Color(1f, 0.95f, 0.6f, 0.8f), 0.35f);
                    break;
                case SkillFxKind.Charge:
                    Spawn(FxMeshes.Arc, chest + Fwd * 0.8f, Facing, Vector3.one * 0.6f, Vector3.one * 1.4f, new Color(0.85f, 0.9f, 1f, 0.8f), 0.25f);
                    break;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateAfterimages(dt);
            UpdateBlade();
            UpdateAura();
            foreach (Piece p in _pool)
            {
                if (!p.active) continue;
                p.age += dt;
                if (p.age >= p.life)
                {
                    p.active = false;
                    p.t.gameObject.SetActive(false);
                    continue;
                }
                float k = p.age / p.life;
                p.t.localScale = Vector3.Lerp(p.startScale, p.endScale, 1f - (1f - k) * (1f - k));
                if (p.velocity != Vector3.zero)
                {
                    p.t.position += p.velocity * dt;
                    p.velocity += Physics.gravity * (0.5f * dt);
                }
                if (p.spin != 0f) p.t.Rotate(0f, p.spin * dt, 0f, Space.World);
                Color c = p.color;
                c.a *= 1f - k * k;
                _block.SetColor(BaseColorId, c);
                p.renderer.SetPropertyBlock(_block);
            }
        }

        /// <summary>Iron Charge leaves fading copies of the body along the dash.</summary>
        void UpdateAfterimages(float dt)
        {
            if (_afterimageTime <= 0f || bodyMesh == null) return;
            _afterimageTime -= dt;
            _afterimageTimer -= dt;
            if (_afterimageTimer > 0f) return;
            _afterimageTimer = 0.05f;
            Transform body = bodyMesh.transform;
            Spawn(bodyMesh.sharedMesh, body.position, body.rotation, body.lossyScale, body.lossyScale * 1.05f, new Color(0.7f, 0.85f, 1f, 0.5f), 0.3f);
        }

        /// <summary>Flame Edge sets the blade alight for the length of the swing.</summary>
        void UpdateBlade()
        {
            if (blade == null || combat == null) return;
            bool burning = combat.CurrentSkill != null && combat.CurrentSkill.fx == SkillFxKind.FlameArc;
            Color c = burning ? Color.Lerp(new Color(1f, 0.45f, 0.1f), new Color(1f, 0.85f, 0.3f), Mathf.PingPong(Time.time * 6f, 1f)) : _bladeBase;
            _block.SetColor(BaseColorId, c);
            blade.SetPropertyBlock(_block);
        }

        /// <summary>War Cry: a pulsing red aura while its buff lasts.</summary>
        void UpdateAura()
        {
            if (stats == null) return;
            float left = 0f;
            foreach (SkillData s in stats.Class.skills)
                if (s.fx == SkillFxKind.Roar) left = Mathf.Max(left, stats.Buffs.Remaining(s.buffStat, Time.time));
            if (left <= 0f)
            {
                if (_aura != null) { _aura.active = false; _aura.t.gameObject.SetActive(false); _aura = null; }
                return;
            }
            if (_aura == null)
            {
                _aura = Take();
                _aura.filter.sharedMesh = FxMeshes.Cylinder;
                _aura.t.gameObject.SetActive(true);
            }
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.06f;
            _aura.t.position = Pos + Vector3.up * 0.95f;
            _aura.t.rotation = Quaternion.identity;
            _aura.t.localScale = new Vector3(1.1f * pulse, 0.95f, 1.1f * pulse);
            _block.SetColor(BaseColorId, new Color(1f, 0.25f, 0.15f, 0.18f + 0.08f * Mathf.Sin(Time.time * 6f)));
            _aura.renderer.SetPropertyBlock(_block);
        }

        void Spawn(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 startScale, Vector3 endScale, Color color, float life,
            Vector3 velocity = default, float spin = 0f)
        {
            Piece p = Take();
            p.filter.sharedMesh = mesh;
            p.t.SetPositionAndRotation(position, rotation);
            p.t.localScale = startScale;
            p.startScale = startScale;
            p.endScale = endScale;
            p.color = color;
            p.life = life;
            p.age = 0f;
            p.velocity = velocity;
            p.spin = spin;
            p.active = true;
            p.t.gameObject.SetActive(true);
        }

        Piece Take()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                Piece p = _pool[(_next + i) % _pool.Count];
                if (p.active || p == _aura) continue;
                _next = (_next + i + 1) % _pool.Count;
                return p;
            }
            // All busy: recycle the next one (never the aura).
            Piece r = _pool[_next];
            if (r == _aura) { _next = (_next + 1) % _pool.Count; r = _pool[_next]; }
            _next = (_next + 1) % _pool.Count;
            return r;
        }
    }
}
