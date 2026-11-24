using Aethvirse.Interaction;
using Aethvirse.Quests;
using Aethvirse.Save;
using Aethvirse.UI;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>The road to a neighbouring region. Shrouded in mist until the region is open (rank, journal).</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class RoadStone : MonoBehaviour
    {
        [SerializeField] string targetRegion = "";

        RegionData Target => WorldAtlas.Instance != null ? WorldAtlas.Instance.Region(targetRegion) : null;

        void Start()
        {
            var i = GetComponent<Interactable>();
            RegionData r = Target;
            i.Prompt = r != null ? $"Road to {r.displayName}" : "Road";
            i.Used += _ => Travel();
        }

        void Travel()
        {
            RegionData r = Target;
            if (r == null) return;
            GameSession s = GameSession.Instance;
            if (!WorldAtlas.IsOpen(r, AcademyRank.For(s.AcademyPoints), s.Flags))
            {
                bool rankOk = AcademyRank.For(s.AcademyPoints) >= r.requiredRank;
                Codex.Notify(rankOk ? "Mist covers the road. The journal of this land is not yet complete." : $"Mist covers the road. (Academy rank {AcademyRank.Name(r.requiredRank)} needed)", CodexTone.Warning);
                return;
            }
            SceneTravel.Go(r.scene, null);
        }
    }
}
