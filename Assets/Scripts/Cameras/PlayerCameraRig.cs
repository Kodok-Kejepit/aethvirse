using Aethvirse.Input;
using Aethvirse.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aethvirse.Cameras
{
    /// <summary>
    /// Free orbit camera (port of the Godot ThirdPersonCamera) on top of Cinemachine:
    /// drives the OrbitalFollow axes from mouse / right stick (touch comes later), captures the
    /// cursor on click (Esc releases it) and widens the FOV while sprinting.
    /// Wall collision is the CinemachineDeoccluder on the same camera.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera), typeof(CinemachineOrbitalFollow))]
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        // Godot used 0.0025 rad per pixel.
        const float MouseDegreesPerPixel = 0.0025f * Mathf.Rad2Deg;

        [SerializeField] float stickDegreesPerSecond = 200f;
        [SerializeField] float sensitivity = 1f;
        [SerializeField] bool invertY;
        [SerializeField] float distance = 4.5f;
        [SerializeField] float baseFov = 70f;
        [SerializeField] float sprintFov = 78f;
        [SerializeField] float fovSmoothing = 6f;

        CinemachineCamera _camera;
        CinemachineOrbitalFollow _orbit;
        GameInput _input;

        /// <summary>Set by the player while sprinting.</summary>
        public bool FovBoost { get; set; }

        /// <summary>World yaw the camera looks along, in degrees (0 = +Z).</summary>
        public float Yaw => _orbit.HorizontalAxis.Value;

        void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
            _orbit = GetComponent<CinemachineOrbitalFollow>();
            _input = GameInput.Instance;
            _orbit.Radius = distance;
            _camera.Lens.FieldOfView = baseFov;
            Transform target = _camera.Target.TrackingTarget;
            // Start behind the player.
            if (target != null) SnapTo(target.eulerAngles.y);
        }

        /// <summary>Jumps behind the target facing <paramref name="yaw"/> (after a teleport: no swoop).</summary>
        public void SnapTo(float yaw)
        {
            _orbit.HorizontalAxis.Value = yaw;
            _camera.PreviousStateIsValid = false;
        }

        void Update()
        {
            UpdateCursor();
            Vector2 look = Vector2.zero;
            if (Cursor.lockState == CursorLockMode.Locked)
                look += _input.LookPointer.ReadValue<Vector2>() * MouseDegreesPerPixel;
            look += _input.LookStick.ReadValue<Vector2>() * (stickDegreesPerSecond * Time.deltaTime);
            if (look != Vector2.zero) RotateBy(look * sensitivity);

            float fovTarget = FovBoost ? sprintFov : baseFov;
            _camera.Lens.FieldOfView = Mathf.Lerp(_camera.Lens.FieldOfView, fovTarget, MovementMath.SmoothWeight(fovSmoothing, Time.deltaTime));
        }

        /// <summary>x turns right, y looks up (degrees).</summary>
        void RotateBy(Vector2 delta)
        {
            ref InputAxis h = ref _orbit.HorizontalAxis;
            ref InputAxis v = ref _orbit.VerticalAxis;
            h.Value = h.ClampValue(h.Value + delta.x);
            // Looking up lowers the camera on the orbit.
            v.Value = v.ClampValue(v.Value - delta.y * (invertY ? -1f : 1f));
        }

        static void UpdateCursor()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (GameInput.Instance.ReleaseCursor.WasPressedThisFrame())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
