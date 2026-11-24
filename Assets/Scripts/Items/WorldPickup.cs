using Aethvirse.Player;
using Aethvirse.UI;
using UnityEngine;

namespace Aethvirse.Items
{
    /// <summary>Loot on the ground: a spinning cube in its rarity colour that flies to the player when close.</summary>
    public sealed class WorldPickup : MonoBehaviour
    {
        const float MagnetRange = 3f;
        const float PickRange = 0.7f;
        const float Lifetime = 300f;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        ItemStack _stack;
        float _age;
        Vector3 _velocity;

        public static void Spawn(Vector3 position, ItemStack stack)
        {
            PlayerInventory inv = PlayerInventory.Current;
            if (inv == null || stack == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Pickup_" + stack.itemId;
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position + Vector3.up * 0.6f;
            ItemData item = inv.Database.Get(stack.itemId);
            Rarity rarity = stack.itemId == "gold" ? Rarity.Legendary : item != null ? ItemStats.RarityOf(stack, item) : Rarity.Common;
            bool gear = item != null && item.IsEquipment;
            go.transform.localScale = Vector3.one * (gear ? 0.35f : stack.itemId == "gold" ? 0.22f : 0.25f);
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, stack.itemId == "gold" ? new Color(1f, 0.85f, 0.2f) : ItemData.RarityColor(rarity));
            go.GetComponent<Renderer>().SetPropertyBlock(block);
            var p = go.AddComponent<WorldPickup>();
            p._stack = stack;
            p._velocity = new Vector3(Random.Range(-1.5f, 1.5f), 3f, Random.Range(-1.5f, 1.5f));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_age > Lifetime) { Destroy(gameObject); return; }
            transform.Rotate(0f, 120f * dt, 0f, Space.World);
            Transform player = PlayerTarget.Current;
            if (player != null && _age > 0.4f)
            {
                Vector3 to = player.position + Vector3.up * 0.9f - transform.position;
                float d = to.magnitude;
                if (d < PickRange) { Collect(); return; }
                if (d < MagnetRange)
                {
                    transform.position += to.normalized * (Mathf.Lerp(12f, 4f, d / MagnetRange) * dt);
                    return;
                }
            }
            // Pop out, fall, then hover with a bob.
            if (_age < 0.6f)
            {
                _velocity += Physics.gravity * dt;
                transform.position += _velocity * dt;
            }
            else transform.position += Vector3.up * (Mathf.Sin(_age * 3f) * 0.15f * dt);
        }

        void Collect()
        {
            PlayerInventory inv = PlayerInventory.Current;
            if (inv == null) return;
            if (!inv.Give(_stack)) return;
            ItemData item = inv.Database.Get(_stack.itemId);
            if (_stack.itemId == "gold") Codex.Notify($"+{_stack.count} gold", CodexTone.Reward);
            else if (item != null && (item.IsEquipment || ItemStats.RarityOf(_stack, item) >= Rarity.Rare || item.opensMysteryBox))
                Codex.Notify($"Got: {inv.Bag.DisplayName(_stack)}", CodexTone.Reward);
            Destroy(gameObject);
        }
    }
}
