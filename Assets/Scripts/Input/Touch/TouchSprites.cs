using UnityEngine;

namespace Aethvirse.Input.Touch
{
    /// <summary>Procedural round sprites for the touch controls (no image assets needed).</summary>
    public static class TouchSprites
    {
        const int Size = 128;
        static Sprite _disc;
        static Sprite _ring;

        /// <summary>Filled circle with a soft edge.</summary>
        public static Sprite Disc => _disc ??= Make(0f);

        /// <summary>Circle outline (inner radius 78% of the outer).</summary>
        public static Sprite Ring => _ring ??= Make(0.78f);

        static Sprite Make(float innerRatio)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            float r = Size / 2f;
            const float edge = 1.5f;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float outer = Mathf.Clamp01((r - d) / edge);
                float inner = innerRatio > 0f ? Mathf.Clamp01((d - r * innerRatio) / edge) : 1f;
                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(255 * outer * inner));
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
