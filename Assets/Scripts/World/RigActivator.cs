using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>Runs before everything else in a scene: wakes the saved-inactive player rig, or removes it when streamed.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class RigActivator : MonoBehaviour
    {
        void Awake()
        {
            bool streamed = WorldStreamer.IsStreaming(gameObject.scene.name);
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                if (root.GetComponent<SceneRig>() == null) continue;
                if (streamed) Destroy(root);
                else root.SetActive(true);
            }
        }
    }
}
