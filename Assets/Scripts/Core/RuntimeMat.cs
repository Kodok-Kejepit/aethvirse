using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>
    /// Material for objects created while the game runs (projectiles, traps, hazard marks, pickups, portals …).
    /// GameObject.CreatePrimitive gives Unity's built-in default material: the editor swaps in URP's, but a player
    /// build does not, so those objects rendered magenta (missing shader) — found in self-test screenshots.
    /// Colours still come from a MaterialPropertyBlock (_BaseColor).
    /// </summary>
    public static class RuntimeMat
    {
        static Material _lit;
        public static Material Lit => _lit != null ? _lit : _lit = Resources.Load<Material>("RuntimeLit");

        /// <summary>CreatePrimitive with the URP material and no collider unless asked.</summary>
        public static GameObject Primitive(PrimitiveType type, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            if (Lit != null) r.sharedMaterial = Lit;
            return go;
        }
    }
}
