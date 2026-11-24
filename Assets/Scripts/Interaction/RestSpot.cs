using Aethvirse.Combat;
using Aethvirse.Save;
using Aethvirse.Stats;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Interaction
{
    /// <summary>Rest until the next morning (or the next given hour).</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class RestSpot : MonoBehaviour
    {
        [SerializeField] int wakeHour = 8;

        void Awake() => GetComponent<Interactable>().Used += _ =>
        {
            GameSession.Instance.SetTime(wakeHour, 0);
            Codex.Notify($"You rest until {wakeHour:00}:00.", CodexTone.Info);
        };
    }
}
