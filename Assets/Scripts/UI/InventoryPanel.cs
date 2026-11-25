using System.Collections.Generic;
using Aethvirse.Input;
using Aethvirse.Items;
using Aethvirse.Stats;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.UI
{
    /// <summary>
    /// Graybox inventory window (I / touch bag): Bag (equip, use, sell, salvage, upgrade), Shop (from the
    /// Quartermaster), Codex Shop (daily rare gear) and Craft. The Codex-styled UI replaces it in Phase 2.
    /// </summary>
    public sealed class InventoryPanel : MonoBehaviour
    {
        enum Tab { Bag, Shop, CodexShop, Craft, Stash }

        static InventoryPanel _instance;
        bool _open;
        bool _shopAvailable;
        Tab _tab;
        ItemStack _selected;
        Vector2 _listScroll, _detailScroll;
        CursorLockMode _cursorBefore;
        GUIStyle _title, _text, _button, _item;
        string _status = "";

        public static bool IsOpen => _instance != null && _instance._open;
        public static void Toggle() { if (_instance != null) _instance.SetOpen(!_instance._open, false); }
        /// <summary>The Quartermaster opens the window on its Shop tab.</summary>
        public static void OpenShop() { if (_instance != null) _instance.SetOpen(true, true); }
        /// <summary>Save crystals open the vault.</summary>
        public static void OpenStash()
        {
            if (_instance == null) return;
            _instance.SetOpen(true, false);
            _instance._stashAvailable = true;
            _instance._tab = Tab.Stash;
        }
        bool _stashAvailable;

        void Awake() => _instance = this;

        void Update()
        {
            if (GameInput.Instance.InventoryPanel.WasPressedThisFrame()) SetOpen(!_open, false);
            if (_open && GameInput.Instance.ReleaseCursor.WasPressedThisFrame()) SetOpen(false, false);
        }

        void SetOpen(bool open, bool shop)
        {
            _shopAvailable = shop;
            _stashAvailable = false;
            _tab = shop ? Tab.Shop : Tab.Bag;
            _status = "";
            if (open == _open) return;
            _open = open;
            if (open)
            {
                _cursorBefore = Cursor.lockState;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (_cursorBefore == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnGUI()
        {
            GUI.depth = -10;
            PlayerInventory inv = PlayerInventory.Current;
            if (!_open || inv == null) return;
            float s = Screen.height / 1080f;
            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _text ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            _button ??= new GUIStyle(GUI.skin.button);
            _item ??= new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            _title.fontSize = Mathf.RoundToInt(28 * s);
            _text.fontSize = Mathf.RoundToInt(20 * s);
            _button.fontSize = Mathf.RoundToInt(20 * s);
            _item.fontSize = Mathf.RoundToInt(20 * s);

            float w = 1280 * s, h = 860 * s;
            var area = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.color = new Color(0.02f, 0.06f, 0.14f, 0.94f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(area.x + 20 * s, area.y + 14 * s, w - 40 * s, h - 28 * s));

            GUILayout.BeginHorizontal();
            TabButton(Tab.Bag, Core.Loc.T("bag"), s);
            if (_shopAvailable) TabButton(Tab.Shop, Core.Loc.T("shop"), s);
            TabButton(Tab.CodexShop, Core.Loc.T("codex_shop_tab"), s);
            TabButton(Tab.Craft, Core.Loc.T("craft"), s);
            if (_stashAvailable) TabButton(Tab.Stash, Core.Loc.T("stash"), s);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{Core.Loc.T("gold")} {inv.Gold}", _title);
            if (GUILayout.Button(Core.Loc.T("close") + " (I)", _button, GUILayout.Width(150 * s), GUILayout.Height(44 * s))) SetOpen(false, false);
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status, _text);
            GUILayout.Space(6 * s);

            switch (_tab)
            {
                case Tab.Bag: DrawBag(inv, s, w); break;
                case Tab.Shop: DrawShop(inv, s); break;
                case Tab.CodexShop: DrawCodexShop(inv, s); break;
                case Tab.Craft: DrawCraft(inv, s); break;
                case Tab.Stash: DrawStash(inv, s, w); break;
            }
            GUILayout.EndArea();
        }

        void TabButton(Tab tab, string label, float s)
        {
            GUI.color = _tab == tab ? new Color(0.5f, 0.8f, 1f) : Color.white;
            if (GUILayout.Button(label, _button, GUILayout.Width(170 * s), GUILayout.Height(44 * s))) { _tab = tab; _status = ""; }
            GUI.color = Color.white;
        }

        // --- Bag --------------------------------------------------------------------------------

        void DrawBag(PlayerInventory inv, float s, float w)
        {
            Inventory bag = inv.Bag;
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(w * 0.5f));
            GUILayout.Label(Core.Loc.T("equipped"), _title);
            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            {
                if (slot == EquipSlot.None) continue;
                ItemStack e = bag.GetEquipped(slot);
                if (e == null) continue;
                ItemRow(inv, e, $"[{slot}] ", s);
            }
            GUILayout.Space(8 * s);
            GUILayout.Label($"{Core.Loc.T("bag")} {bag.stacks.Count}/{bag.Cap}", _title);
            _listScroll = GUILayout.BeginScrollView(_listScroll);
            foreach (ItemStack st in new List<ItemStack>(SortedBag(inv))) ItemRow(inv, st, "", s);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            _detailScroll = GUILayout.BeginScrollView(_detailScroll);
            if (_selected != null) DrawDetail(inv, _selected, s);
            else GUILayout.Label(Core.Loc.T("select_item"), _text);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        static IEnumerable<ItemStack> SortedBag(PlayerInventory inv)
        {
            var list = new List<ItemStack>(inv.Bag.stacks);
            list.Sort((a, b) =>
            {
                ItemData ia = inv.Database.Get(a.itemId), ib = inv.Database.Get(b.itemId);
                int ra = ia != null ? (int)ItemStats.RarityOf(a, ia) : 0, rb = ib != null ? (int)ItemStats.RarityOf(b, ib) : 0;
                return rb != ra ? rb.CompareTo(ra) : string.CompareOrdinal(a.itemId, b.itemId);
            });
            return list;
        }

        void ItemRow(PlayerInventory inv, ItemStack st, string prefix, float s)
        {
            ItemData item = inv.Database.Get(st.itemId);
            Color c = item != null ? ItemData.RarityColor(st.identified ? ItemStats.RarityOf(st, item) : Rarity.Unknown) : Color.white;
            GUI.color = st == _selected ? Color.Lerp(c, Color.white, 0.5f) : c;
            string count = st.count > 1 ? $"  ×{st.count}" : "";
            if (GUILayout.Button(prefix + inv.Bag.DisplayName(st) + count, _item, GUILayout.Height(40 * s))) _selected = st;
            GUI.color = Color.white;
        }

        void DrawDetail(PlayerInventory inv, ItemStack st, float s)
        {
            Inventory bag = inv.Bag;
            ItemData item = inv.Database.Get(st.itemId);
            if (item == null) return;
            bool equipped = !bag.stacks.Contains(st);
            if (equipped && bag.equipped.Find(e => e.stack == st) == null) { _selected = null; return; }
            Rarity rarity = st.identified ? ItemStats.RarityOf(st, item) : Rarity.Unknown;
            GUI.color = ItemData.RarityColor(rarity);
            GUILayout.Label(bag.DisplayName(st), _title);
            GUI.color = Color.white;
            GUILayout.Label($"{rarity} {item.kind}" + (item.IsEquipment ? $" · {item.slot}" : "") + (st.itemLevel > 0 ? $" · item Lv {st.itemLevel}" : ""), _text);
            if (st.identified)
            {
                int level = PlayerStats.Current != null ? PlayerStats.Current.Progression.level : 1;
                if (item.slot == EquipSlot.Weapon) GUILayout.Label($"ATK {ItemStats.Attack(st, item, level, inv.Rules):0}", _text);
                float def = ItemStats.Defense(st, item, inv.Rules);
                if (def > 0.01f) GUILayout.Label($"DEF {def:0}", _text);
                foreach (string key in StatKeys.All)
                {
                    float v = ItemStats.Bonus(st, item, key, inv.Rules, inv.Database);
                    if (Mathf.Abs(v) < 0.0001f) continue;
                    GUILayout.Label(StatKeys.IsPercent(key) ? $"{StatLabel(key)} +{v * 100f:0.0}%" : $"{StatLabel(key)} +{v:0}", _text);
                }
                if (st.sockets > 0)
                {
                    var gems = new List<string>();
                    foreach (string g in st.gems) gems.Add(inv.Database.Get(g)?.displayName ?? g);
                    for (int k = st.gems.Count; k < st.sockets; k++) gems.Add("◯");
                    GUILayout.Label($"{Core.Loc.T("sockets")}: {string.Join("  ", gems)}", _text);
                }
                ItemData ench = inv.Database.Get(st.enchant);
                if (ench != null) { GUI.color = new Color(0.7f, 0.9f, 1f); GUILayout.Label($"{Core.Loc.T("enchant")}: {ench.displayName}", _text); GUI.color = Color.white; }
                DrawSetInfo(inv, item);
                if (item.healAmount > 0) GUILayout.Label($"{Core.Loc.T("heals")} {item.healAmount} HP", _text);
                if (item.evolveKills > 0) GUILayout.Label($"Kills {st.kills} / ???", _text);
                if (item.cursed) GUILayout.Label(Core.Loc.T("cursed"), _text);
                GUILayout.Label(item.description, _text);
            }
            else GUILayout.Label(item.unidentifiedDescription.Length > 0 ? item.unidentifiedDescription : Core.Loc.T("hidden_nature"), _text);

            GUILayout.Space(10 * s);
            GUILayout.BeginHorizontal();
            if (item.IsEquipment && !equipped && Btn("Equip", s)) inv.Equip(st);
            if (item.IsEquipment && equipped)
            {
                EquipSlot slot = bag.equipped.Find(e => e.stack == st).slot;
                if (Btn("Unequip", s)) inv.Unequip(slot);
            }
            if (item.IsUsable && !equipped && Btn("Use", s) && inv.Use(st) && !bag.stacks.Contains(st)) _selected = null;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            int price = ItemActions.SellPrice(bag, st, inv.Rules);
            if (!equipped && price > 0 && Btn($"Sell ({price})", s))
            {
                ItemActions.Sell(bag, st, inv.Rules, inv);
                if (!bag.stacks.Contains(st)) _selected = null;
            }
            if (!equipped && item.IsEquipment && item.canSell && Btn("Salvage", s))
            {
                _status = $"Salvaged into {ItemActions.Salvage(bag, st)} scrap.";
                _selected = null;
            }
            if (item.IsEquipment && st.upgrade < inv.Rules.upgradeMax)
            {
                int target = st.upgrade + 1;
                if (Btn($"Upgrade +{target} ({inv.Rules.UpgradeCost(target)}g, {inv.Rules.UpgradeChance(target) * 100f:0}%)", s))
                {
                    UpgradeResult r = ItemActions.Upgrade(st, item, inv.Rules, inv, inv.Rng);
                    _status = r switch
                    {
                        UpgradeResult.Success => $"Upgrade succeeded: +{st.upgrade}",
                        UpgradeResult.FailedDowngraded => $"Upgrade failed — dropped to +{st.upgrade}.",
                        UpgradeResult.Failed => "Upgrade failed.",
                        UpgradeResult.NoGold => "Not enough gold.",
                        _ => "",
                    };
                    if (r == UpgradeResult.Success || r == UpgradeResult.FailedDowngraded) PlayerStats.Current?.Recompute(false);
                }
            }
            GUILayout.EndHorizontal();
            if (item.IsEquipment && st.identified) DrawCrafting(inv, st, item, s);
        }

        static string StatLabel(string key) => key switch
        {
            StatKeys.AtkPct => "ATK", StatKeys.DefPct => "DEF", StatKeys.HpPct => "HP", StatKeys.Cdr => "Cooldown −",
            StatKeys.Aspd => "Attack speed", StatKeys.Mspd => "Move speed", StatKeys.CritDmg => "Crit damage",
            _ => key.ToUpperInvariant(),
        };

        void DrawSetInfo(PlayerInventory inv, ItemData item)
        {
            GearSet set = string.IsNullOrEmpty(item.setId) || GearSetDatabase.Instance == null ? null : GearSetDatabase.Instance.Get(item.setId);
            if (set == null) return;
            inv.SetCounts().TryGetValue(set.id, out int worn);
            GUI.color = new Color(0.55f, 1f, 0.6f);
            GUILayout.Label($"{set.displayName} ({worn}/6)", _title);
            foreach (SetTier t in set.tiers)
            {
                GUI.color = worn >= t.pieces ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 1f, 1f, 0.45f);
                GUILayout.Label($"  ({t.pieces}) {t.text}", _text);
            }
            GUI.color = Color.white;
            GUILayout.Label(set.lore, _text);
        }

        /// <summary>Gems, enchant scrolls and reforging for the selected gear.</summary>
        void DrawCrafting(PlayerInventory inv, ItemStack st, ItemData item, float s)
        {
            Inventory bag = inv.Bag;
            var seen = new HashSet<string>();
            if (st.gems.Count < st.sockets)
            {
                foreach (ItemStack g in new List<ItemStack>(bag.stacks))
                {
                    ItemData gi = inv.Database.Get(g.itemId);
                    if (gi == null || gi.kind != ItemKind.Gem || !seen.Add(gi.id)) continue;
                    if (Btn($"{Core.Loc.T("socket")}: {gi.displayName}", s) && ItemActions.SocketGem(bag, st, gi.id))
                    {
                        _status = $"{gi.displayName} → {bag.DisplayName(st)}";
                        PlayerStats.Current?.Recompute(false);
                    }
                }
            }
            if (st.gems.Count > 0 && Btn($"{Core.Loc.T("unsocket")} ({ItemActions.UnsocketCost(st)}g)", s) && ItemActions.UnsocketAll(bag, st, inv))
                PlayerStats.Current?.Recompute(false);
            foreach (ItemStack sc in new List<ItemStack>(bag.stacks))
            {
                ItemData si = inv.Database.Get(sc.itemId);
                if (si == null || si.kind != ItemKind.EnchantScroll || si.enchantWeapon != (item.slot == EquipSlot.Weapon) || !seen.Add(si.id)) continue;
                if (Btn($"{Core.Loc.T("enchant")}: {si.displayName}", s) && ItemActions.Enchant(bag, st, item, si.id))
                {
                    _status = $"{bag.DisplayName(st)} — {si.displayName}";
                    PlayerStats.Current?.Recompute(false);
                }
            }
            if (st.rarity >= 0 && Btn($"{Core.Loc.T("reforge")} ({ItemActions.ReforgeCost(st)}g + 2 scrap)", s))
            {
                _status = ItemActions.Reforge(bag, st, inv.Rules, inv, inv.Rng) ? $"{Core.Loc.T("reforged")}: {bag.DisplayName(st)}" : "Not enough gold or scrap.";
                PlayerStats.Current?.Recompute(false);
            }
        }

        // --- Stash ----------------------------------------------------------------------------------

        void DrawStash(PlayerInventory inv, float s, float w)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(w * 0.48f));
            GUILayout.Label($"{Core.Loc.T("bag")} {inv.Bag.stacks.Count}/{inv.Bag.Cap}", _title);
            _listScroll = GUILayout.BeginScrollView(_listScroll);
            foreach (ItemStack st in new List<ItemStack>(inv.Bag.stacks))
                if (GUILayout.Button(inv.Bag.DisplayName(st) + (st.count > 1 ? $"  ×{st.count}" : "") + "   →", _item, GUILayout.Height(38 * s)) && inv.Stash.AddStack(st))
                {
                    inv.Bag.stacks.Remove(st);
                    inv.Bag.NotifyChanged();
                }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label($"{Core.Loc.T("stash")} {inv.Stash.stacks.Count}/{inv.Stash.Cap}", _title);
            _detailScroll = GUILayout.BeginScrollView(_detailScroll);
            foreach (ItemStack st in new List<ItemStack>(inv.Stash.stacks))
                if (GUILayout.Button("←   " + inv.Bag.DisplayName(st) + (st.count > 1 ? $"  ×{st.count}" : ""), _item, GUILayout.Height(38 * s)) && inv.Bag.AddStack(st))
                {
                    inv.Stash.stacks.Remove(st);
                    inv.Stash.NotifyChanged();
                }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        bool Btn(string label, float s) => GUILayout.Button(label, _button, GUILayout.Height(46 * s));

        // --- Shops and crafting -------------------------------------------------------------------

        void DrawShop(PlayerInventory inv, float s)
        {
            GUILayout.Label(Core.Loc.T("quartermaster"), _title);
            foreach (ShopEntry e in inv.QuartermasterStock)
            {
                ItemData item = inv.Database.Get(e.itemId);
                if (item == null) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{item.displayName} — {e.price}g", _text, GUILayout.Width(520 * s));
                GUI.enabled = inv.Gold >= e.price;
                if (GUILayout.Button(Core.Loc.T("buy"), _button, GUILayout.Width(120 * s), GUILayout.Height(40 * s)) && inv.SpendGold(e.price))
                {
                    if (inv.Bag.Add(e.itemId) > 0) { inv.AddGold(e.price); _status = "Bag full."; }
                    else _status = $"Bought {item.displayName}.";
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(Core.Loc.T("sell_hint"), _text);
        }

        void DrawCodexShop(PlayerInventory inv, float s)
        {
            GameSession session = GameSession.Instance;
            int level = PlayerStats.Current != null ? PlayerStats.Current.Progression.level : 1;
            GUILayout.Label($"{Core.Loc.T("codex_shop")} {session.Clock.Day} {Core.Loc.T("new_stock")}", _title);
            List<CodexShop.Offer> offers = CodexShop.List(session.Clock.Day, level, inv.Database, inv.Rules, session.Flags);
            for (int i = 0; i < offers.Count; i++)
            {
                CodexShop.Offer o = offers[i];
                if (o.stack == null) continue;
                ItemData item = inv.Database.Get(o.stack.itemId);
                GUILayout.BeginHorizontal();
                GUI.color = ItemData.RarityColor(ItemStats.RarityOf(o.stack, item));
                GUILayout.Label(inv.Bag.DisplayName(o.stack), _text, GUILayout.Width(560 * s));
                GUI.color = Color.white;
                GUILayout.Label(o.sold ? Core.Loc.T("sold") : $"{o.price}g", _text, GUILayout.Width(140 * s));
                GUI.enabled = !o.sold && inv.Gold >= o.price;
                if (GUILayout.Button(Core.Loc.T("buy"), _button, GUILayout.Width(120 * s), GUILayout.Height(40 * s)) && inv.SpendGold(o.price))
                {
                    if (!inv.Bag.AddStack(o.stack)) { inv.AddGold(o.price); _status = "Bag full."; }
                    else
                    {
                        session.Flags.SetFlag(CodexShop.SoldFlag(session.Clock.Day, i));
                        _status = $"Bought {inv.Bag.DisplayName(o.stack)}.";
                    }
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        void DrawCraft(PlayerInventory inv, float s)
        {
            GUILayout.Label(Core.Loc.T("craft"), _title);
            foreach (Recipe r in inv.Recipes)
            {
                ItemData output = inv.Database.Get(r.output);
                if (output == null) continue;
                var parts = new List<string>();
                for (int i = 0; i < r.inputIds.Count; i++)
                {
                    ItemData input = inv.Database.Get(r.inputIds[i]);
                    parts.Add($"{(input != null ? input.displayName : r.inputIds[i])} {inv.Bag.CountOf(r.inputIds[i])}/{r.inputCounts[i]}");
                }
                if (r.goldCost > 0) parts.Add($"{r.goldCost}g");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{output.displayName} ×{r.outputCount}  ←  {string.Join(", ", parts)}", _text, GUILayout.Width(900 * s));
                GUI.enabled = ItemActions.CanCraft(inv.Bag, r, inv);
                if (GUILayout.Button(Core.Loc.T("craft"), _button, GUILayout.Width(120 * s), GUILayout.Height(40 * s)) && ItemActions.Craft(inv.Bag, r, inv))
                    _status = $"Crafted {output.displayName}.";
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }
    }
}
