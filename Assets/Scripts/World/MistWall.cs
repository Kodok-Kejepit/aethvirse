using Aethvirse.Quests;
using Aethvirse.UI;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>Mist across the road to a region that is not open yet (rank or journal). Solid until it opens.</summary>
    public sealed class MistWall : MonoBehaviour
    {
        [SerializeField] string targetRegion = "";

        Collider _wall;
        Renderer _look;
        float _timer, _toldAt = -99f;

        void Awake()
        {
            _wall = GetComponent<Collider>();
            _look = GetComponent<Renderer>();
        }

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 1f;
            RegionData r = WorldAtlas.Instance != null ? WorldAtlas.Instance.Region(targetRegion) : null;
            GameSession s = GameSession.Instance;
            bool open = r != null && WorldAtlas.IsOpen(r, AcademyRank.For(s.AcademyPoints), s.Flags);
            if (_wall != null) _wall.enabled = !open;
            if (_look != null) _look.enabled = !open;
        }

        void LateUpdate()
        {
            Transform p = Player.PlayerTarget.Current;
            if (p == null || _wall == null || !_wall.enabled) return;
            if (_wall.bounds.SqrDistance(p.position) < 9f) Tell();
        }

        void Tell()
        {
            if (Time.time - _toldAt < 4f) return;
            _toldAt = Time.time;
            RegionData r = WorldAtlas.Instance != null ? WorldAtlas.Instance.Region(targetRegion) : null;
            if (r == null) return;
            bool rankOk = AcademyRank.For(GameSession.Instance.AcademyPoints) >= r.requiredRank;
            Codex.Notify(rankOk ? "Mist covers the road. The journal of this land is not yet complete." : $"Mist covers the road. (Academy rank {AcademyRank.Name(r.requiredRank)} needed)", CodexTone.Warning);
        }
    }
}
