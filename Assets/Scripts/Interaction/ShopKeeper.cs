using UnityEngine;

namespace Aethvirse.Interaction
{
    /// <summary>Opens the inventory window on its Shop tab (Quartermaster and other merchants).</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class ShopKeeper : MonoBehaviour
    {
        void Awake() => GetComponent<Interactable>().Used += _ => UI.InventoryPanel.OpenShop();
    }
}
