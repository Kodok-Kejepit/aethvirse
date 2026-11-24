using UnityEngine.InputSystem;

namespace Aethvirse.Input
{
    /// <summary>
    /// All gameplay actions, defined in code so bindings live in one place and can be rebound later
    /// (InputActionMap.SaveBindingOverridesAsJson). Touch controls feed the same actions through
    /// on-screen controls bound to gamepad paths. See docs/CONTROLS.md.
    /// </summary>
    public sealed class GameInput
    {
        static GameInput _instance;
        public static GameInput Instance => _instance ??= new GameInput();

        public readonly InputActionMap Player;
        public readonly InputAction Move;
        /// <summary>Pointer delta in pixels (mouse / touchpad). Applied while the cursor is captured or a button is held.</summary>
        public readonly InputAction LookPointer;
        /// <summary>Look as a rate, -1..1: gamepad right stick or the arrow keys (laptops without a mouse).</summary>
        public readonly InputAction LookStick;
        /// <summary>Held mouse button / touchpad press: drag to look without capturing the cursor.</summary>
        public readonly InputAction LookDrag;
        public readonly InputAction Jump;
        public readonly InputAction Sprint;
        public readonly InputAction Dodge;
        public readonly InputAction ReleaseCursor;
        /// <summary>Keys / gamepad / touch. Always active.</summary>
        public readonly InputAction LightAttack;
        public readonly InputAction HeavyAttack;
        /// <summary>Mouse buttons attack only while the cursor is captured (a free click captures it first).</summary>
        public readonly InputAction LightAttackMouse;
        public readonly InputAction HeavyAttackMouse;
        public readonly InputAction LockOn;
        public readonly InputAction LockNext;
        public readonly InputAction LockPrev;

        GameInput()
        {
            Player = new InputActionMap("Player");

            Move = Player.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            LookPointer = Player.AddAction("LookPointer", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            LookStick = Player.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            LookStick.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            LookDrag = Player.AddAction("LookDrag", InputActionType.Button, "<Mouse>/leftButton");
            LookDrag.AddBinding("<Mouse>/rightButton");

            Jump = Player.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            Jump.AddBinding("<Gamepad>/buttonSouth");

            Sprint = Player.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            Sprint.AddBinding("<Gamepad>/leftStickPress");

            Dodge = Player.AddAction("Dodge", InputActionType.Button, "<Keyboard>/leftCtrl");
            Dodge.AddBinding("<Gamepad>/buttonEast");

            ReleaseCursor = Player.AddAction("ReleaseCursor", InputActionType.Button, "<Keyboard>/escape");

            LightAttack = Player.AddAction("LightAttack", InputActionType.Button, "<Keyboard>/j");
            LightAttack.AddBinding("<Gamepad>/buttonWest");
            HeavyAttack = Player.AddAction("HeavyAttack", InputActionType.Button, "<Keyboard>/k");
            HeavyAttack.AddBinding("<Gamepad>/buttonNorth");
            LightAttackMouse = Player.AddAction("LightAttackMouse", InputActionType.Button, "<Mouse>/leftButton");
            HeavyAttackMouse = Player.AddAction("HeavyAttackMouse", InputActionType.Button, "<Mouse>/rightButton");

            LockOn = Player.AddAction("LockOn", InputActionType.Button, "<Keyboard>/tab");
            LockOn.AddBinding("<Mouse>/middleButton");
            LockOn.AddBinding("<Gamepad>/rightStickPress");
            LockNext = Player.AddAction("LockNext", InputActionType.Button, "<Mouse>/scroll/down");
            LockPrev = Player.AddAction("LockPrev", InputActionType.Button, "<Mouse>/scroll/up");

            Player.Enable();
        }
    }
}
