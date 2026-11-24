# Controls

All gameplay input goes through Input System actions defined in `Assets/Scripts/Input/GameInput.cs`
(no hard-coded keys in gameplay code; rebinding comes with the Settings menu).

| Action | PC (KB+M) | Gamepad | Android (touch) | Since |
|---|---|---|---|---|
| Move | W A S D | Left stick (light push = walk) | — (next M1 step) | M1 |
| Camera | Mouse (click the game to capture, Esc to release) | Right stick | — (next M1 step) | M1 |
| Jump | Space (hold = higher) | A / ✕ | — | M1 |
| Sprint | Left Shift (hold while moving, unlimited) | L3 (hold) | — | M1 |
| Dodge | Left Ctrl — dash with i-frames, costs stamina, once in mid-air; its late part can chain into a jump or another dash | B / ○ | — | M1 |
| Debug overlay | F3 | — | — | M1 |
