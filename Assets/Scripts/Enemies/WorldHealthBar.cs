using UnityEngine;

namespace Aethvirse.Enemies
{
    /// <summary>Small HP bar above an enemy: a dark back and a fill that shrinks to the left; faces the camera.</summary>
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] Transform fill;
        [Tooltip("Only shown after the first hit, until it heals back to full.")]
        [SerializeField] bool hideWhenFull = true;

        float _width = 1f;

        void Awake()
        {
            if (fill != null) _width = fill.localScale.x;
            if (hideWhenFull) gameObject.SetActive(false);
        }

        public void Set(int current, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (fill != null)
            {
                Vector3 s = fill.localScale;
                s.x = _width * ratio;
                fill.localScale = s;
                // Keep the left edge in place.
                fill.localPosition = new Vector3(-_width * (1f - ratio) * 0.5f, fill.localPosition.y, fill.localPosition.z);
            }
            bool show = !hideWhenFull || (ratio < 1f && ratio > 0f);
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
        }

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
