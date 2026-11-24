using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Aethvirse.Input.Touch
{
    /// <summary>Plain tap button that runs a callback (menus, panels).</summary>
    public sealed class TouchTap : MonoBehaviour, IPointerClickHandler
    {
        public Action OnTap;
        public void OnPointerClick(PointerEventData eventData) => OnTap?.Invoke();
    }
}
