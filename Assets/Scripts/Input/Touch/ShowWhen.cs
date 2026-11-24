using System;
using UnityEngine;

namespace Aethvirse.Input.Touch
{
    /// <summary>Shows its UI (via a CanvasGroup, so it keeps updating) only while the condition holds.</summary>
    public sealed class ShowWhen : MonoBehaviour
    {
        public Func<bool> Condition;
        CanvasGroup _group;

        void Awake() => _group = gameObject.AddComponent<CanvasGroup>();

        void Update()
        {
            bool on = Condition != null && Condition();
            _group.alpha = on ? 1f : 0f;
            _group.blocksRaycasts = on;
        }
    }
}
