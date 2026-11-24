using Aethvirse.Quests;
using Aethvirse.Save;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>Fast travel between attuned waystones of open regions (graybox IMGUI list, grouped by region).</summary>
    public sealed class FastTravelMenu : MonoBehaviour
    {
        static FastTravelMenu _instance;
        bool _open;
        Vector2 _scroll;
        GUIStyle _title, _button;
        CursorLockMode _before;

        public static bool IsOpen => _instance != null && _instance._open;

        public static void Open()
        {
            if (_instance == null) _instance = new GameObject("FastTravelMenu").AddComponent<FastTravelMenu>();
            _instance._open = true;
            _instance._before = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Close()
        {
            _open = false;
            if (_before == CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        void Update()
        {
            if (_open && Input.GameInput.Instance.ReleaseCursor.WasPressedThisFrame()) Close();
        }

        void OnGUI()
        {
            if (!_open) return;
            WorldAtlas atlas = WorldAtlas.Instance;
            if (atlas == null) { Close(); return; }
            float s = Screen.height / 1080f;
            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _button ??= new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            _title.fontSize = Mathf.RoundToInt(28 * s);
            _button.fontSize = Mathf.RoundToInt(22 * s);
            float w = 900 * s, h = 820 * s;
            var area = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.color = new Color(0.02f, 0.06f, 0.14f, 0.94f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(area.x + 20 * s, area.y + 14 * s, w - 40 * s, h - 28 * s));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Fast Travel", _title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", _button, GUILayout.Width(140 * s), GUILayout.Height(44 * s))) Close();
            GUILayout.EndHorizontal();
            _scroll = GUILayout.BeginScrollView(_scroll);
            GameSession session = GameSession.Instance;
            int rank = AcademyRank.For(session.AcademyPoints);
            foreach (RegionData r in atlas.AllRegions())
            {
                if (!WorldAtlas.IsOpen(r, rank, session.Flags)) continue;
                bool any = false;
                foreach (WaystoneInfo w2 in r.waystones)
                {
                    if (!session.Flags.Has(Waystone.Flag(w2.id))) continue;
                    if (!any) { GUILayout.Label($"{r.displayName}  (Lv {r.levelMin}–{r.levelMax})", _title); any = true; }
                    if (GUILayout.Button("   " + w2.displayName, _button, GUILayout.Height(46 * s)))
                    {
                        Close();
                        SceneTravel.Go(r.scene, w2.position + Vector3.forward * 2.5f);
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
