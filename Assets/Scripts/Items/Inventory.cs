using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Items
{
    /// <summary>
    /// Bag (60 stacks) + equipped slots (port of the Godot Inventory). Pure and serializable; the
    /// <see cref="ItemDatabase"/> is attached after loading.
    /// </summary>
    [Serializable]
    public sealed class Inventory
    {
        public const int Capacity = 60;

        [Serializable]
        public sealed class Equipped
        {
            public EquipSlot slot;
            public ItemStack stack;
        }

        public List<ItemStack> stacks = new List<ItemStack>();
        public List<Equipped> equipped = new List<Equipped>();

        [NonSerialized] ItemDatabase _db;
        public ItemDatabase Db { get => _db; set => _db = value; }

        public event Action Changed;

        public Inventory() { }
        public Inventory(ItemDatabase db) => _db = db;

        public void NotifyChanged() => Changed?.Invoke();

        /// <summary>Adds items; returns how many did not fit.</summary>
        public int Add(string itemId, int amount = 1)
        {
            ItemData item = _db != null ? _db.Get(itemId) : null;
            if (item == null || amount <= 0) return amount;
            int left = amount;
            if (item.IsStackable)
            {
                foreach (ItemStack s in stacks)
                {
                    if (s.itemId != itemId || s.count >= item.maxStack || !s.identified || s.IsUniqueInstance) continue;
                    int moved = Mathf.Min(left, item.maxStack - s.count);
                    s.count += moved;
                    left -= moved;
                    if (left == 0) break;
                }
            }
            while (left > 0 && stacks.Count < Capacity)
            {
                int moved = Mathf.Min(left, Mathf.Max(1, item.maxStack));
                stacks.Add(ItemStack.Make(itemId, moved, item));
                left -= moved;
            }
            Changed?.Invoke();
            return left;
        }

        /// <summary>Adds a unique copy (generated gear). False when the bag is full.</summary>
        public bool AddStack(ItemStack stack)
        {
            if (stacks.Count >= Capacity) return false;
            stacks.Add(stack);
            Changed?.Invoke();
            return true;
        }

        public int CountOf(string itemId)
        {
            int total = 0;
            foreach (ItemStack s in stacks) if (s.itemId == itemId) total += s.count;
            foreach (Equipped e in equipped) if (e.stack.itemId == itemId) total += e.stack.count;
            return total;
        }

        public bool Has(string itemId, int amount = 1) => CountOf(itemId) >= amount;

        /// <summary>Removes from the bag (not equipped items). False if there are not enough.</summary>
        public bool Remove(string itemId, int amount = 1)
        {
            int inBag = 0;
            foreach (ItemStack s in stacks) if (s.itemId == itemId) inBag += s.count;
            if (inBag < amount) return false;
            int left = amount;
            for (int i = stacks.Count - 1; i >= 0 && left > 0; i--)
            {
                ItemStack s = stacks[i];
                if (s.itemId != itemId) continue;
                int taken = Mathf.Min(left, s.count);
                s.count -= taken;
                left -= taken;
                if (s.count <= 0) stacks.RemoveAt(i);
            }
            Changed?.Invoke();
            return true;
        }

        /// <summary>Takes one item out of a specific stack (selling, using).</summary>
        public void TakeOne(ItemStack stack)
        {
            if (stack.count > 1) stack.count--;
            else stacks.Remove(stack);
            Changed?.Invoke();
        }

        public ItemStack GetEquipped(EquipSlot slot)
        {
            foreach (Equipped e in equipped) if (e.slot == slot) return e.stack;
            return null;
        }

        public ItemData GetEquippedItem(EquipSlot slot)
        {
            ItemStack s = GetEquipped(slot);
            return s != null && _db != null ? _db.Get(s.itemId) : null;
        }

        public bool Equip(ItemStack stack)
        {
            ItemData item = _db != null ? _db.Get(stack.itemId) : null;
            if (item == null || !item.IsEquipment || !stacks.Contains(stack)) return false;
            EquipSlot slot = item.slot;
            // Two ring slots: use the second if the first is taken.
            if (slot == EquipSlot.Ring && GetEquipped(EquipSlot.Ring) != null && GetEquipped(EquipSlot.Ring2) == null) slot = EquipSlot.Ring2;
            if (GetEquipped(slot) != null && !Unequip(slot)) return false;
            stacks.Remove(stack);
            equipped.Add(new Equipped { slot = slot, stack = stack });
            Changed?.Invoke();
            return true;
        }

        /// <summary>False if the bag is full or the item is cursed (it will not come off).</summary>
        public bool Unequip(EquipSlot slot)
        {
            Equipped e = equipped.Find(x => x.slot == slot);
            if (e == null || stacks.Count >= Capacity) return false;
            ItemData item = _db != null ? _db.Get(e.stack.itemId) : null;
            if (item != null && item.cursed && !e.stack.identifiedBy.Contains("purified")) return false;
            equipped.Remove(e);
            stacks.Add(e.stack);
            Changed?.Invoke();
            return true;
        }

        public string DisplayName(ItemStack s)
        {
            ItemData item = _db != null ? _db.Get(s.itemId) : null;
            if (item == null) return s.itemId;
            if (!s.identified) return item.unidentifiedName;
            string name = item.displayName;
            foreach (Affix a in s.affixes)
                if (!string.IsNullOrEmpty(a.name)) name = a.suffix ? $"{name} {a.name}" : $"{a.name} {name}";
            return s.upgrade > 0 ? $"{name} +{s.upgrade}" : name;
        }
    }
}
