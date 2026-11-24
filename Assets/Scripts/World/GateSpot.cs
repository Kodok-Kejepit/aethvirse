using Aethvirse.Gates;
using Aethvirse.Interaction;
using Aethvirse.Save;
using Aethvirse.Stats;
using Aethvirse.UI;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>A place where Gates open: shows a coloured portal while one is open here; entering starts the dungeon.</summary>
    public sealed class GateSpot : MonoBehaviour
    {
        public const string GateScene = "Gate";
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color[] RankColors =
        {
            new Color(0.7f, 0.7f, 0.7f), new Color(0.4f, 0.85f, 0.4f), new Color(0.35f, 0.6f, 1f),
            new Color(0.7f, 0.4f, 1f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.25f, 0.25f),
        };

        [SerializeField] int index;
        GameObject _portal;
        Interactable _interact;
        MaterialPropertyBlock _block;

        void Awake()
        {
            _portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _portal.name = "GatePortal";
            Destroy(_portal.GetComponent<Collider>());
            _portal.transform.SetParent(transform, false);
            _portal.transform.localPosition = Vector3.up * 2.5f;
            _portal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _portal.transform.localScale = new Vector3(4f, 0.15f, 4f);
            _block = new MaterialPropertyBlock();
            _interact = gameObject.AddComponent<Interactable>();
            _interact.Range = 3.5f;
            _interact.Used += _ => Enter();
        }

        OpenGate Gate => GameSession.Instance.Gates.At(GameSession.Instance.Region, index);

        void Update()
        {
            OpenGate g = Gate;
            bool open = g != null;
            if (_portal.activeSelf != open) _portal.SetActive(open);
            _interact.Available = open && !g.broken;
            if (!open) return;
            string kind = g.red ? "RED GATE" : "Gate";
            _interact.Prompt = $"Enter {kind} — rank {WorldGates.RankNames[g.rank]}";
            Color c = g.broken ? new Color(0.5f, 0f, 0f) : g.red ? new Color(0.6f, 0.05f, 0.05f) : RankColors[g.rank];
            _block.SetColor(BaseColorId, c * (0.8f + 0.2f * Mathf.Sin(Time.time * 3f)));
            _portal.GetComponent<Renderer>().SetPropertyBlock(_block);
            _portal.transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.Self);
        }

        void Enter()
        {
            OpenGate g = Gate;
            RegionData r = RegionController.Current;
            if (g == null || r == null) return;
            int level = Mathf.Clamp(r.levelMin + g.rank * Mathf.Max(1, (r.levelMax - r.levelMin) / 6), r.levelMin, r.levelMax);
            GateSession.Begin(g, level, r.enemies.ToArray());
            Codex.Notify(g.red ? "[ RED GATE ] There is no way out but through." : $"Entering a rank {WorldGates.RankNames[g.rank]} Gate.", g.red ? CodexTone.Warning : CodexTone.System);
            SceneTravel.Go(GateScene, null, rememberReturn: true);
        }
    }
}
