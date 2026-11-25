using Aethvirse.Combat;
using Aethvirse.Save;
using Aethvirse.Stats;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Interaction
{
    /// <summary>Saves to the autosave slot and restores HP / MP (the academy dorm bed works the same).</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class SaveCrystal : MonoBehaviour
    {
        void Awake() => GetComponent<Interactable>().Used += OnUsed;

        void OnUsed(GameObject user)
        {
            if (PlayerStats.Current != null) PlayerStats.Current.Recompute(refill: true);
            Damageable d = user.GetComponent<Damageable>();
            if (d != null) d.Health.Heal(d.Health.maxHealth);
            Codex.Notify(SaveManager.Save(SaveManager.AutosaveSlot) ? "The crystal hums. HP and MP restored · saved." : "The crystal is silent.", CodexTone.System);
            InventoryPanel.OpenStash();
        }
    }
}
