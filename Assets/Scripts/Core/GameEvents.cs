using System;
using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>
    /// Gameplay events in one place (Godot EventBus): quests, daily quests, the Mystery Journal and achievements
    /// listen here instead of holding references to the player's components.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<string> EnemyKilled;      // enemy id
        public static event Action Attacked;                  // a player attack started
        public static event Action Dodged;
        public static event Action<float> Moved;              // metres on the ground
        public static event Action<string, int> PickedUp;     // item id, count
        public static event Action<string> Crafted;           // output item id
        public static event Action<string> ItemUsed;          // item id
        public static event Action<string> SkillUsed;         // skill id
        public static event Action<string> Talked;            // npc id
        public static event Action<string> AreaEntered;       // area id
        public static event Action<string> GateCleared;       // gate id / rank

        public static void RaiseEnemyKilled(string id) => EnemyKilled?.Invoke(id);
        public static void RaiseAttacked() => Attacked?.Invoke();
        public static void RaiseDodged() => Dodged?.Invoke();
        public static void RaiseMoved(float metres) => Moved?.Invoke(metres);
        public static void RaisePickedUp(string id, int count) => PickedUp?.Invoke(id, count);
        public static void RaiseCrafted(string id) => Crafted?.Invoke(id);
        public static void RaiseItemUsed(string id) => ItemUsed?.Invoke(id);
        public static void RaiseSkillUsed(string id) => SkillUsed?.Invoke(id);
        public static void RaiseTalked(string id) => Talked?.Invoke(id);
        public static void RaiseAreaEntered(string id) => AreaEntered?.Invoke(id);
        public static void RaiseGateCleared(string id) => GateCleared?.Invoke(id);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearOnPlay()
        {
            EnemyKilled = null; Attacked = null; Dodged = null; Moved = null; PickedUp = null; Crafted = null;
            ItemUsed = null; SkillUsed = null; Talked = null; AreaEntered = null; GateCleared = null;
        }
    }
}
