using System.Collections.Generic;
using Aethvirse.Combat;
using UnityEngine;

namespace Aethvirse.Enemies
{
    /// <summary>Immortal target that shows the last hit and DPS; wobbles and flashes when hit. Never attacks.</summary>
    [RequireComponent(typeof(Damageable))]
    public sealed class TrainingDummy : MonoBehaviour, ILockable
    {
        const float DpsWindow = 4f;
        const float FlashTime = 0.12f;
        const float WobbleTime = 0.35f;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Renderer bodyRenderer;
        [SerializeField] Transform wobblePivot;
        [SerializeField] TextMesh label;
        [SerializeField] float lockHeight = 1.1f;

        Damageable _damageable;
        MaterialPropertyBlock _block;
        Color _baseColor;
        readonly List<(float time, int amount)> _hits = new List<(float, int)>();
        float _flash;
        float _wobble;
        Vector3 _wobbleAxis;
        int _last;
        bool _lastCrit;

        public bool IsLockable => isActiveAndEnabled;
        public Vector3 LockPoint => transform.position + Vector3.up * lockHeight;

        void Awake()
        {
            _damageable = GetComponent<Damageable>();
            _damageable.Configure(10000, new CombatStats { defense = 0f }, immortal: true);
            _damageable.HitReceived += OnHit;
            _block = new MaterialPropertyBlock();
            if (bodyRenderer != null) _baseColor = bodyRenderer.sharedMaterial.GetColor(BaseColorId);
            Refresh();
        }

        void OnEnable() => LockTargets.Register(this);
        void OnDisable() => LockTargets.Unregister(this);

        void OnHit(DamageInfo info, bool applied)
        {
            if (!applied) return;
            _hits.Add((Time.time, info.amount));
            _last = info.amount;
            _lastCrit = info.isCrit;
            _flash = FlashTime;
            _wobble = WobbleTime;
            Vector3 push = info.knockback.sqrMagnitude > 0.0001f ? info.knockback.normalized : -transform.forward;
            _wobbleAxis = Vector3.Cross(Vector3.up, push).normalized;
            Refresh();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (_flash > 0f || _wobble > 0f)
            {
                _flash = Mathf.Max(_flash - dt, 0f);
                _wobble = Mathf.Max(_wobble - dt, 0f);
                if (bodyRenderer != null)
                {
                    _block.SetColor(BaseColorId, Color.Lerp(_baseColor, Color.white, _flash / FlashTime));
                    bodyRenderer.SetPropertyBlock(_block);
                }
                if (wobblePivot != null)
                {
                    float k = _wobble / WobbleTime;
                    float angle = Mathf.Sin((1f - k) * Mathf.PI * 3f) * 14f * k;
                    wobblePivot.localRotation = Quaternion.AngleAxis(angle, _wobbleAxis);
                }
            }
            if (Time.frameCount % 15 == 0) Refresh();
            if (label != null && Camera.main != null) label.transform.rotation = Camera.main.transform.rotation;
        }

        void Refresh()
        {
            _hits.RemoveAll(h => Time.time - h.time > DpsWindow);
            int total = 0;
            foreach (var h in _hits) total += h.amount;
            if (label != null)
                label.text = $"Training Dummy\nlast {_last}{(_lastCrit ? " CRIT" : "")} · DPS {total / DpsWindow:0}";
        }
    }
}
