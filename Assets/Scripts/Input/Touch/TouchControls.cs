using System;
using Aethvirse.Cameras;
using Aethvirse.Skills;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Aethvirse.Input.Touch
{
    /// <summary>
    /// Touch layout (port of the Godot mobile controls): floating joystick on the left 45% of the
    /// screen, camera drag on the rest, Attack / Heavy / Dash / Jump bottom right and Lock-on top right.
    /// Dash also holds Sprint,
    /// so keeping the thumb on it after a dash keeps sprinting. Built in code at runtime; shown on
    /// phones, or on PC with the <c>-touch-ui</c> command-line flag (for testing).
    /// </summary>
    public sealed class TouchControls : MonoBehaviour
    {
        public const string ForceFlag = "-touch-ui";
        const float ReferenceWidth = 1920f;
        const float ReferenceHeight = 1080f;
        const float JoystickZoneShare = 0.45f;
        const float StickBaseSize = 260f;
        const float StickKnobSize = 120f;
        const float StickTravel = 110f;

        /// <summary>True while touch controls are on screen (the camera then ignores mouse capture / drag).</summary>
        public static bool IsActive { get; private set; }

        public static bool ShouldShow =>
            Application.isMobilePlatform || Array.IndexOf(Environment.GetCommandLineArgs(), ForceFlag) >= 0;

        /// <summary>Creates the controls once per scene when they should show.</summary>
        public static TouchControls CreateIfNeeded(PlayerCameraRig rig)
        {
            if (!ShouldShow) return null;
            var go = new GameObject("TouchControls");
            var controls = go.AddComponent<TouchControls>();
            controls.Build(rig);
            return controls;
        }

        void OnEnable() => IsActive = true;
        void OnDisable() => IsActive = false;

        void Build(PlayerCameraRig rig)
        {
            EnsureEventSystem();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 1f;
            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform safe = Panel("SafeArea", (RectTransform)transform);
            gameObject.AddComponent<SafeAreaFitter>().Target = safe;

            // Order matters: later siblings are on top and get the touch first.
            BuildLookArea(safe, rig);
            BuildJoystick(safe);
            // Thumb arc around the big attack button (bottom right).
            BuildButton(safe, "Attack", "/", new Vector2(-190f, 190f), 230f, "<Gamepad>/buttonWest");
            BuildButton(safe, "Heavy", "//", new Vector2(-440f, 120f), 150f, "<Gamepad>/buttonNorth");
            BuildButton(safe, "Dash", "»", new Vector2(-420f, 330f), 150f, "<Gamepad>/buttonEast", "<Gamepad>/leftStickPress");
            BuildButton(safe, "Jump", "^", new Vector2(-200f, 450f), 150f, "<Gamepad>/buttonSouth");
            RectTransform lockButton = BuildButton(safe, "LockOn", "L", new Vector2(-110f, -110f), 120f, "<Gamepad>/rightStickPress");
            lockButton.anchorMin = lockButton.anchorMax = Vector2.one;

            // Skills in an arc left of the attack cluster; the ultimate above it.
            SkillCaster caster = FindFirstObjectByType<SkillCaster>();
            if (caster != null)
            {
                Vector2[] spots = { new Vector2(-660f, 110f), new Vector2(-660f, 270f), new Vector2(-620f, 430f), new Vector2(-430f, 540f) };
                for (int i = 0; i < spots.Length; i++) BuildSkillButton(safe, caster, i, spots[i], 130f, Color.white);
                BuildSkillButton(safe, caster, 4, new Vector2(-190f, 640f), 140f, new Color(1f, 0.85f, 0.3f));
            }
            RectTransform statusButton = BuildTapButton(safe, "Status", "C", new Vector2(-260f, -110f), 110f, Core.StatusPanel.Toggle);
            statusButton.anchorMin = statusButton.anchorMax = Vector2.one;
            RectTransform bagButton = BuildTapButton(safe, "Bag", "I", new Vector2(-540f, -110f), 110f, UI.InventoryPanel.Toggle);
            bagButton.anchorMin = bagButton.anchorMax = Vector2.one;
            RectTransform pauseButton = BuildTapButton(safe, "Pause", "II", new Vector2(-400f, -110f), 110f, UI.PauseMenu.Toggle);
            pauseButton.anchorMin = pauseButton.anchorMax = Vector2.one;

            // Contextual: only shows while something usable is near.
            var detector = FindFirstObjectByType<Interaction.InteractionDetector>();
            if (detector != null)
            {
                RectTransform use = BuildTapButton(safe, "Interact", "E", new Vector2(-420f, 760f), 150f, detector.UseCurrent);
                use.anchorMin = use.anchorMax = new Vector2(1f, 0f);
                use.gameObject.AddComponent<ShowWhen>().Condition = () => detector.Current != null;
            }
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }

        void BuildLookArea(RectTransform parent, PlayerCameraRig rig)
        {
            RectTransform area = Panel("LookArea", parent);
            area.anchorMin = new Vector2(JoystickZoneShare, 0f);
            area.anchorMax = Vector2.one;
            Hitbox(area);
            area.gameObject.AddComponent<TouchLookArea>().Setup(rig);
        }

        void BuildJoystick(RectTransform parent)
        {
            RectTransform zone = Panel("JoystickZone", parent);
            zone.anchorMin = Vector2.zero;
            zone.anchorMax = new Vector2(JoystickZoneShare, 1f);
            Hitbox(zone);

            RectTransform stickBase = Circle("StickBase", zone, TouchSprites.Ring, StickBaseSize, new Color(1f, 1f, 1f, 0.45f));
            stickBase.anchorMin = stickBase.anchorMax = Vector2.zero;
            RectTransform knob = Circle("Knob", stickBase, TouchSprites.Disc, StickKnobSize, new Color(1f, 1f, 1f, 0.6f));

            // Configure before enabling: OnScreenControl binds its virtual device in OnEnable.
            zone.gameObject.SetActive(false);
            zone.gameObject.AddComponent<FloatingJoystick>().Setup(stickBase, knob, StickTravel);
            zone.gameObject.SetActive(true);
        }

        RectTransform BuildButton(RectTransform parent, string name, string label, Vector2 fromBottomRight, float size, params string[] paths)
        {
            RectTransform button = Circle(name, parent, TouchSprites.Disc, size, Color.white);
            button.anchorMin = button.anchorMax = new Vector2(1f, 0f);
            button.anchoredPosition = fromBottomRight;
            var image = button.GetComponent<Image>();
            image.raycastTarget = true;
            Text(button, label, size * 0.45f);

            button.gameObject.SetActive(false);
            for (int i = 0; i < paths.Length; i++)
            {
                var control = button.gameObject.AddComponent<TouchButton>();
                control.controlPath = paths[i];
                // Only the first one tints the button.
                control.SetGraphic(i == 0 ? image : null);
            }
            button.gameObject.SetActive(true);
            return button;
        }

        void BuildSkillButton(RectTransform parent, SkillCaster caster, int slot, Vector2 fromBottomRight, float size, Color ring)
        {
            RectTransform button = Circle($"Skill{slot + 1}", parent, TouchSprites.Disc, size, Color.white);
            button.anchorMin = button.anchorMax = new Vector2(1f, 0f);
            button.anchoredPosition = fromBottomRight;
            Image face = button.GetComponent<Image>();
            face.raycastTarget = true;
            RectTransform outline = Circle("Ring", button, TouchSprites.Ring, size, ring);
            outline.anchoredPosition = Vector2.zero;
            RectTransform cd = Circle("Cooldown", button, TouchSprites.Disc, size, new Color(0f, 0f, 0f, 0.6f));
            Image cooldown = cd.GetComponent<Image>();
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillAmount = 0f;
            Text(button, "", size * 0.36f);
            Text label = button.GetComponentInChildren<Text>();
            button.gameObject.AddComponent<TouchSkillButton>().Setup(caster, slot, face, cooldown, label);
        }

        RectTransform BuildTapButton(RectTransform parent, string name, string label, Vector2 position, float size, System.Action onTap)
        {
            RectTransform button = Circle(name, parent, TouchSprites.Disc, size, new Color(1f, 1f, 1f, 0.35f));
            button.anchoredPosition = position;
            button.GetComponent<Image>().raycastTarget = true;
            Text(button, label, size * 0.45f);
            button.gameObject.AddComponent<TouchTap>().OnTap = onTap;
            return button;
        }

        // --- UI helpers -------------------------------------------------------------------------

        static RectTransform Panel(string name, RectTransform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Invisible graphic so the panel receives touches.</summary>
        static void Hitbox(RectTransform rt)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
        }

        static RectTransform Circle(string name, RectTransform parent, Sprite sprite, float size, Color color)
        {
            var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        static void Text(RectTransform parent, string label, float fontSize)
        {
            var rt = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var text = rt.GetComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.RoundToInt(fontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.05f, 0.1f, 0.2f, 0.85f);
            text.raycastTarget = false;
        }
    }
}
