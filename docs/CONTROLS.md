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
| Interact | E | D-pad up | Contextual button (appears near something usable) | P1-4 |
| Inventory (bag, shops, craft) | I | — | "I" button top right | P1-5 |
| Quick potion | H | D-pad down | — (Bag → Use) | P1-5 |
| Pause (save / load / title) | P · Esc when the cursor is free | Start | ⏸ button top right | P1-4 |
| Debug overlay | F3 | — | — | M1 |
| Test cheats | F6 level up · F7 full HP/MP, no cooldowns · F8 +1 hour · F9 next weather · F10 reveal the region's secrets · F11 open every world | — | — | M3 |

Desktop testing of the touch UI: `tools/play.sh -- -touch-ui` (mouse clicks act as touches).

Combos (greatsword): `L L L L L` chain · `H` Overhead Smash · `L H` Rising Cleave · `L L H` Whirlwind ·
`L L L H` Ground Slam · `L L L L H` Piercing Thrust. Dodge cancels attacks after their cancel point; a dodge
that makes an attack miss in its first 0.15 s is a **Perfect Dodge** → slow motion, and the next hit is a
guaranteed crit with +50% damage.

Skill combos (Paladin): Iron Charge → Rending Slash = **Breaker** (guaranteed crit) · War Cry → Earthsplitter =
**Fury Quake** (+50%) · Flame Edge → Earthsplitter = **Molten Rift** (+30%, extra stagger).

Weapons: greatsword (Paladin) · twin daggers (Assassin, 6-hit chain) · staff (Mage: light = homing bolts,
heavy = orb / lance / fan) · bow (Marksman: light = arrows that curve to the lock-on, heavy = piercing / spread).

## Aerial (P1-18)

- **Launcher:** lock on (Tab / middle mouse / R3), then heavy from neutral.
- **Air chain:** jump right after the launcher, then light attacks (three hits hang you in the air).
- **Plunge:** heavy while in the air.

## Map (P1-20)

- **M** (keyboard) or the **Map** touch button: region map. The minimap is always on (top left).

## Movement (P1-23)

- **Glide:** hold Jump while falling (uses stamina).
- **Ledge climb:** while in the air, push toward a wall up to about 2 m high.
- **Rebind keys:** Pause → Settings → Controls (Esc cancels a change).
