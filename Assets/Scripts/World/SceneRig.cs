using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>
    /// Marks a root object of the player rig (player, camera, HUD, services). Scenes are saved with the rig inactive;
    /// <see cref="RigActivator"/> wakes it when the scene is opened on its own and drops it when the scene is only
    /// streamed in next to the current one (the rig then moves between region scenes).
    /// </summary>
    public sealed class SceneRig : MonoBehaviour { }
}
