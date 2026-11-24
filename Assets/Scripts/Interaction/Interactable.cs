using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Interaction
{
    /// <summary>
    /// Something the player can use (NPC, chest, door, waystone, Gate …). Systems hook <see cref="Used"/>
    /// or subclass behaviour through it. Registered in a static list (no scene searches).
    /// </summary>
    public sealed class Interactable : MonoBehaviour
    {
        [SerializeField] string prompt = "Use";
        [SerializeField] float range = 2.2f;
        [Tooltip("Optional point the range is measured from (defaults to this transform).")]
        [SerializeField] Transform anchor;

        static readonly List<Interactable> _all = new List<Interactable>();
        public static IReadOnlyList<Interactable> All => _all;

        public string Prompt { get => prompt; set => prompt = value; }
        public float Range { get => range; set => range = value; }
        public Vector3 Point => anchor != null ? anchor.position : transform.position;
        /// <summary>Hidden from the detector while false (e.g. an opened chest).</summary>
        public bool Available { get; set; } = true;

        /// <summary>Raised with the user (the player's GameObject).</summary>
        public event Action<GameObject> Used;

        void OnEnable() => _all.Add(this);
        void OnDisable() => _all.Remove(this);

        public void Use(GameObject user) => Used?.Invoke(user);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearOnPlay() => _all.Clear();
    }
}
