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
| Light attack | J · left mouse while the mouse is captured | X / □ | Big round button (/) | M2 |
| Heavy attack | K · right mouse while the mouse is captured | Y / △ | Round button (//) | M2 |
| Lock-on | Tab · middle mouse | R3 | Small button top right (L) | M2 |
| Next / previous target | Mouse wheel down / up | Flick the right stick while locked | — | M2 |
| Skills 1–4 | 1–4 (also Q, R, F, G) | Hold LB + X / Y / B / A | 4 round skill buttons (initials, cooldown sweep) | M3 |
| Ultimate | T | LT + RT together | Gold-ringed button | M3 |
| Status (stats, skills, slots) | C | Select / Back | "C" button top right | M3 |
| Debug overlay | F3 | — | — | M1 |
| Test cheats | F6 = level up · F7 = full HP/MP, no cooldowns | — | — | M3 |

Desktop testing of the touch UI: `tools/play.sh -- -touch-ui` (mouse clicks act as touches).

Combos (greatsword): `L L L L L` chain · `H` Overhead Smash · `L H` Rising Cleave · `L L H` Whirlwind ·
`L L L H` Ground Slam · `L L L L H` Piercing Thrust. Dodge cancels attacks after their cancel point; a dodge
that makes an attack miss in its first 0.15 s is a **Perfect Dodge** → slow motion, and the next hit is a
guaranteed crit with +50% damage.

Skill combos (Paladin): Iron Charge → Rending Slash = **Breaker** (guaranteed crit) · War Cry → Earthsplitter =
**Fury Quake** (+50%) · Flame Edge → Earthsplitter = **Molten Rift** (+30%, extra stagger).
