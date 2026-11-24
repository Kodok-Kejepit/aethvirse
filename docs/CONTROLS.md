# Controls

All gameplay input goes through Input System actions defined in `Assets/Scripts/Input/GameInput.cs`
(no hard-coded keys in gameplay code; rebinding comes with the Settings menu).

| Action | PC (KB+M) | Gamepad | Android (touch) | Since |
|---|---|---|---|---|
| Move | W A S D | Left stick (light push = walk) | Floating joystick (touch the left 45% of the screen) | M1 |
| Camera | Mouse (click the game to capture, Esc to release) · hold a mouse/touchpad button and drag · arrow keys | Right stick | Drag on the right side of the screen | M1 |
| Jump | Space (hold = higher) | A / ✕ | Big round button (^) | M1 |
| Sprint | Left Shift (hold while moving, unlimited) | L3 (hold) | Dash button — keep holding after the dash | M1 |
| Dodge | Left Ctrl — dash with i-frames, costs stamina, once in mid-air; its late part can chain into a jump or another dash | B / ○ | Round button (») | M1 |
| Debug overlay | F3 | — | — | M1 |

Desktop testing of the touch UI: `tools/play.sh -- -touch-ui` (mouse clicks act as touches).
