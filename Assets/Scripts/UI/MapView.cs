using System.Collections.Generic;
using Aethvirse.Combat;
using Aethvirse.Input;
using Aethvirse.Player;
using Aethvirse.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.UI
{
    /// <summary>
    /// Minimap (top left, under the bars) and the full region map (M / the Map button) with fog of war. The terrain
    /// image is baked once per scene by looking straight down on a grid; landmarks (waystones, roads, gates, NPCs,
    /// chests) show once their cell has been seen. Secrets never show — the map only tracks how many you found.
    /// </summary>
    public sealed class MapView : MonoBehaviour
    {
        const float MiniRadius = 70f;
        const float RevealRadius = 28f;
        static readonly string[] NoMap = { "Boot", "Title" };

        static MapView _instance;
        Texture2D _terrain, _fog;
        string _key = "";
        bool _ready, _open;
        float _revealTimer;
        CursorLockMode _cursorBefore;
        GUIStyle _label, _title;
        readonly List<(Vector3 pos, Color color, string name)> _marks = new List<(Vector3, Color, string)>();

        public static bool IsOpen => _instance != null && _instance._open;
        public static void Toggle() { if (_instance != null) _instance.SetOpen(!_instance._open); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("MapView");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MapView>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnScene;
        void OnDisable() => SceneManager.sceneLoaded -= OnScene;

        void Start() => Invoke(nameof(Bake), 0.6f);

        void OnScene(Scene scene, LoadSceneMode mode)
        {
            _ready = false;
            SetOpen(false);
            CancelInvoke(nameof(Bake));
            Invoke(nameof(Bake), 0.6f);
        }

        string Key => RegionController.Current != null ? RegionController.Current.id : SceneManager.GetActiveScene().name;

        void Bake()
        {
            if (System.Array.IndexOf(NoMap, SceneManager.GetActiveScene().name) >= 0 || PlayerTarget.Current == null) return;
            _key = Key;
            int n = MapMemory.Grid;
            float cell = MapMemory.CellSize, half = MapMemory.Size * 0.5f;
            _terrain ??= new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            _fog ??= new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color ground = RegionController.Current != null ? RegionController.Current.palette : new Color(0.4f, 0.45f, 0.4f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var origin = new Vector3(-half + (x + 0.5f) * cell, 80f, -half + (y + 0.5f) * cell);
                    Color c = new Color(0.04f, 0.05f, 0.08f);
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        float h = hit.point.y;
                        c = h > 1.2f ? Color.Lerp(ground, Color.black, 0.55f) : h < -2f ? new Color(0.05f, 0.05f, 0.1f) : Color.Lerp(ground, Color.white, 0.1f);
                    }
                    _terrain.SetPixel(x, y, c);
                }
            _terrain.Apply();
            RefreshFog();
            _marks.Clear();
            foreach (var w in FindObjectsByType<Waystone>(FindObjectsSortMode.None)) _marks.Add((w.transform.position, new Color(0.4f, 0.9f, 1f), "Waystone"));
            foreach (var r in FindObjectsByType<RoadStone>(FindObjectsSortMode.None)) _marks.Add((r.transform.position, new Color(0.5f, 1f, 0.5f), "Road"));
            foreach (var p in FindObjectsByType<Portal>(FindObjectsSortMode.None)) _marks.Add((p.transform.position, new Color(0.7f, 0.5f, 1f), "Gate"));
            foreach (var g in FindObjectsByType<Endgame.StarGate>(FindObjectsSortMode.None)) _marks.Add((g.transform.position, new Color(0.95f, 0.85f, 1f), "Star gate"));
            foreach (var npc in FindObjectsByType<Dialog.Npc>(FindObjectsSortMode.None)) _marks.Add((npc.transform.position, new Color(1f, 0.9f, 0.4f), "Person"));
            foreach (var ch in FindObjectsByType<Items.Chest>(FindObjectsSortMode.None)) _marks.Add((ch.transform.position, new Color(0.8f, 0.6f, 0.3f), "Chest"));
            _ready = true;
        }

        void RefreshFog()
        {
            MapMemory mem = GameSession.Instance.Map;
            int n = MapMemory.Grid;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    _fog.SetPixel(x, y, mem.IsRevealed(_key, x, y) ? Color.clear : new Color(0.02f, 0.03f, 0.06f, 0.92f));
            _fog.Apply();
        }

        void Update()
        {
            if (!_ready) return;
            if (GameInput.Instance.Map.WasPressedThisFrame()) SetOpen(!_open);
            if (_open && GameInput.Instance.ReleaseCursor.WasPressedThisFrame()) SetOpen(false);
            _revealTimer -= Time.unscaledDeltaTime;
            Transform p = PlayerTarget.Current;
            if (_revealTimer <= 0f && p != null)
            {
                _revealTimer = 0.25f;
                if (GameSession.Instance.Map.Reveal(_key, p.position, RevealRadius) > 0) RefreshFog();
            }
        }

        void SetOpen(bool open)
        {
            if (open == _open || (open && !_ready)) return;
            _open = open;
            if (open)
            {
                _cursorBefore = Cursor.lockState;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (_cursorBefore == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // --- Drawing ----------------------------------------------------------------------------------

        void OnGUI()
        {
            Transform p = PlayerTarget.Current;
            if (!_ready || p == null) return;
            if (Core.StatusPanel.IsOpen || InventoryPanel.IsOpen || PauseMenu.IsOpen || Dialog.DialogBox.IsOpen) return;
            float s = Screen.height / 1080f;
            _label ??= new GUIStyle(GUI.skin.label);
            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _label.fontSize = Mathf.RoundToInt(20 * s);
            _title.fontSize = Mathf.RoundToInt(30 * s);
            if (_open) DrawFull(p, s);
            else DrawMini(p, s);
        }

        /// <summary>World position → 0..1 map coordinates (x right, y up).</summary>
        static Vector2 Uv(Vector3 w) => new Vector2((w.x + MapMemory.Size * 0.5f) / MapMemory.Size, (w.z + MapMemory.Size * 0.5f) / MapMemory.Size);

        void DrawMini(Transform p, float s)
        {
            float size = 220 * s;
            var r = new Rect(20 * s, 132 * s, size, size);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Texture2D.whiteTexture);
            GUI.color = Color.white;
            Vector2 c = Uv(p.position);
            float span = MiniRadius * 2f / MapMemory.Size;
            // GUI's y runs down; the map's v runs up.
            var uv = new Rect(c.x - span * 0.5f, c.y - span * 0.5f, span, span);
            GUI.DrawTextureWithTexCoords(r, _terrain, uv);
            GUI.DrawTextureWithTexCoords(r, _fog, uv);
            foreach (var m in _marks)
            {
                Vector2 d = Uv(m.pos) - c;
                if (Mathf.Abs(d.x) > span * 0.5f || Mathf.Abs(d.y) > span * 0.5f || !Seen(m.pos)) continue;
                Dot(r.center + new Vector2(d.x / span * size, -d.y / span * size), 8 * s, m.color);
            }
            foreach (ILockable e in LockTargets.All)
            {
                if (e == null || (e is Object o && o == null) || !e.IsLockable) continue;
                Vector2 d = Uv(e.transform.position) - c;
                if (Mathf.Abs(d.x) > span * 0.5f || Mathf.Abs(d.y) > span * 0.5f) continue;
                Dot(r.center + new Vector2(d.x / span * size, -d.y / span * size), 6 * s, new Color(1f, 0.3f, 0.3f));
            }
            Arrow(r.center, p, 12 * s);
            _label.alignment = TextAnchor.UpperCenter;
            GUI.Label(new Rect(r.x, r.yMax + 2 * s, r.width, 26 * s), "M — map", _label);
            _label.alignment = TextAnchor.UpperLeft;
        }

        void DrawFull(Transform p, float s)
        {
            GUI.color = new Color(0.01f, 0.02f, 0.05f, 0.94f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float size = Screen.height * 0.82f;
            var r = new Rect((Screen.width - size) * 0.5f - 160 * s, Screen.height * 0.09f, size, size);
            GUI.DrawTexture(r, _terrain);
            GUI.DrawTexture(r, _fog);
            foreach (var m in _marks)
            {
                if (!Seen(m.pos)) continue;
                Vector2 u = Uv(m.pos);
                Dot(new Vector2(r.x + u.x * size, r.yMax - u.y * size), 12 * s, m.color);
            }
            Vector2 pu = Uv(p.position);
            Arrow(new Vector2(r.x + pu.x * size, r.yMax - pu.y * size), p, 16 * s);

            float x = r.xMax + 30 * s, y = r.y;
            RegionData region = RegionController.Current;
            GUI.Label(new Rect(x, y, 520 * s, 40 * s), region != null ? region.displayName : SceneManager.GetActiveScene().name, _title);
            y += 48 * s;
            GUI.Label(new Rect(x, y, 520 * s, 30 * s), $"Explored {GameSession.Instance.Map.Explored(_key) * 100f:0}%", _label);
            y += 32 * s;
            var db = Secrets.SecretDatabase.Instance;
            if (db != null && region != null)
            {
                var (found, total) = db.Progress(region.id, Secrets.SecretSystem.Instance.State);
                GUI.Label(new Rect(x, y, 520 * s, 30 * s), $"Secrets {found}/{total}", _label);
                y += 32 * s;
            }
            int sealedDone = 0, sealedActive = 0;
            var qs = Quests.QuestSystem.Instance;
            foreach (Quests.QuestData q in qs.ActiveQuests()) if (q.kind == Quests.QuestKind.Sealed) sealedActive++;
            foreach (string id in qs.Log.completed) if (id.StartsWith("sm_")) sealedDone++;
            GUI.Label(new Rect(x, y, 520 * s, 30 * s), $"Sealed missions: {sealedActive} open · {sealedDone} done · ??? unknown", _label);
            y += 52 * s;
            foreach (var (name, color) in new[] { ("You", Color.white), ("Waystone", new Color(0.4f, 0.9f, 1f)), ("Road", new Color(0.5f, 1f, 0.5f)), ("Gate / portal", new Color(0.7f, 0.5f, 1f)),
                         ("Star gate", new Color(0.95f, 0.85f, 1f)), ("Person", new Color(1f, 0.9f, 0.4f)), ("Chest", new Color(0.8f, 0.6f, 0.3f)) })
            {
                Dot(new Vector2(x + 10 * s, y + 14 * s), 12 * s, color);
                GUI.Label(new Rect(x + 28 * s, y, 400 * s, 30 * s), name, _label);
                y += 32 * s;
            }
            if (GUI.Button(new Rect(x, r.yMax - 60 * s, 220 * s, 56 * s), "Close (M)")) SetOpen(false);
        }

        bool Seen(Vector3 pos)
        {
            Vector2Int c = MapMemory.Cell(pos);
            return GameSession.Instance.Map.IsRevealed(_key, c.x, c.y);
        }

        static void Dot(Vector2 centre, float size, Color color)
        {
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(centre.x - size * 0.5f - 1, centre.y - size * 0.5f - 1, size + 2, size + 2), Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(centre.x - size * 0.5f, centre.y - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        /// <summary>The player: a dot with a short line showing where you face.</summary>
        static void Arrow(Vector2 centre, Transform p, float size)
        {
            Vector3 f = p.forward;
            var visual = p.Find("Visual");
            if (visual != null) f = visual.forward;
            Vector2 dir = new Vector2(f.x, -f.z).normalized;
            for (int i = 1; i <= 4; i++) Dot(centre + dir * (size * 0.35f * i), size * 0.35f, Color.white);
            Dot(centre, size, Color.white);
        }
    }
}
