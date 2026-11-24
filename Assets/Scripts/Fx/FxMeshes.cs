using UnityEngine;

namespace Aethvirse.Fx
{
    /// <summary>Procedural meshes for graybox effects (no art assets): rings, arcs and built-in primitives.</summary>
    public static class FxMeshes
    {
        static Mesh _ring, _arc, _cube, _cylinder;

        /// <summary>Flat annulus in the XZ plane, outer radius 1, inner 0.8.</summary>
        public static Mesh Ring => _ring ??= Annulus(0.8f, 1f, 360f, 48);

        /// <summary>Flat 150° band in the XZ plane facing +Z, outer radius 1, inner 0.7.</summary>
        public static Mesh Arc => _arc ??= Annulus(0.7f, 1f, 150f, 24);

        public static Mesh Cube => _cube ??= Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        public static Mesh Cylinder => _cylinder ??= Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");

        static Mesh Annulus(float inner, float outer, float degrees, int segments)
        {
            var verts = new Vector3[(segments + 1) * 2];
            var tris = new int[segments * 12];
            float start = -degrees * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float a = (start + degrees * i / segments) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                verts[i * 2] = dir * inner;
                verts[i * 2 + 1] = dir * outer;
            }
            for (int i = 0; i < segments; i++)
            {
                int v = i * 2, t = i * 12;
                // Both faces so the band shows from above and below.
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                tris[t + 3] = v + 1; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
                tris[t + 6] = v; tris[t + 7] = v + 2; tris[t + 8] = v + 1;
                tris[t + 9] = v + 1; tris[t + 10] = v + 2; tris[t + 11] = v + 3;
            }
            var mesh = new Mesh { name = "FxAnnulus", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
