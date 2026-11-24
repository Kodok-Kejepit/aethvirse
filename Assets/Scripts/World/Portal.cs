using Aethvirse.Interaction;
using Aethvirse.Save;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>Interact to travel: to another scene (with an optional spawn position) or back to where you came from.</summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class Portal : MonoBehaviour
    {
        [SerializeField] string targetScene = "";
        [SerializeField] bool returnToPrevious;
        [SerializeField] bool useSpawnPosition;
        [SerializeField] Vector3 spawnPosition;

        void Awake() => GetComponent<Interactable>().Used += _ => Travel();

        public void Travel()
        {
            if (returnToPrevious) SceneTravel.Return();
            else SceneTravel.Go(targetScene, useSpawnPosition ? spawnPosition : (Vector3?)null);
        }
    }
}
