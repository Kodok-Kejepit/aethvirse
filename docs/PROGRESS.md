# Progress (Unity)

The Godot prototype (`../aethvirse/docs/PROGRESS.md`) is the reference for what each system should do.

## M0 — Setup (2026-11-22 → 11-24)
- Unity 6.3 LTS project, boot scene, GameCI builds Windows + Android on push to `main`.
- Packages: URP 17.3, Input System 1.20, Cinemachine 3.1 (see DECISIONS.md).
- CI runs the EditMode tests before building.

## M2 — Combat core (branch `m2-combat`) — done 2026-11-24, approved by owner

### Done
- **Combat core** (unit tested): damage formula, 9 elements, combo rank D→SSS, health pool, attack tokens,
  lock-on scoring. Greatsword combo graph as assets (`AETHVIRSE > Build Combat Data`, numbers from Godot).
- **Player attacks:** 5-hit light chain + 5 heavy branches, lunge, steering while attacking, cancels into
  dodge / jump / next attack, camera-forward aim with soft assist (28°, 9 m), hit-stop, damage numbers,
  heavy-hit camera shake (Cinemachine impulse), perfect dodge → slow motion + guaranteed crit counter.
- **Lock-on:** best target near the view, switch with mouse wheel / stick flick, camera frames player + target.
- **Training dummies** (3, next to the spawn): immortal, wobble + flash, last hit and DPS.
- **Ash Hound** (3 in the arena north-east, 6 s respawn): patrol → chase → windup with a red ground
  telegraph → bite with lunge → cooldown; max 2 attackers at once; staggered by enough poise damage;
  leash back home (heals); dies and respawns. HP bar after the first hit.
- **Player health:** HP 300, hit reactions (stagger ≥ 15 interrupts unless in a heavy attack; dash breaks
  out of it), death → respawn at the spawn after 2 s, fall damage 10%.
- **HUD (graybox):** HP and stamina bars, lock-on target name, PERFECT DODGE callout.
- **Combo flame** (owner request, replaces D…SSS): Spark → Ember → Flame → Blaze → Inferno → Phoenix, the flame
  grows and recolours with the hit count. Each attack in the chain swings differently.

### How to test
1. Dummies: J/K combos (`L L L L L`, `L L H` …), watch DPS and the combo counter; Tab to lock on.
2. Arena (north-east, beyond the pillars): fight 3 Ash Hounds. Dash just as the red circle fills → PERFECT DODGE.
3. Let them hit you: stagger, dash out of it; die → back at the spawn.

## M1 — Core movement (branch `m1-core-movement`) — done 2026-11-24, approved by owner (PC + phone)

### Done
- **Player** (`PlayerController`, CharacterController): camera-relative walk / run / sprint with eased
  acceleration, buffered + coyote jump with variable height (release early = lower), dash with
  i-frames and stamina (one air dash per jump; late part cancels into jump / dash), fall recovery
  to the last safe ground. Same tuning numbers as Godot (`Assets/Data/Player/DefaultMovement.asset`).
- **Camera** (`PlayerCameraRig` + Cinemachine OrbitalFollow / RotationComposer / Deoccluder): orbit with
  mouse or right stick, wall collision, wider FOV while sprinting.
- **Playground scene** (built by `AETHVIRSE > Build Playground Scene`): steps 0.2–2.5 m, ramps 20° / 40° /
  55°, jump gaps 2 / 4 / 6 m, wall + pillars + low tunnel for the camera, an 8 m tower with a ramp.
  Boot loads it automatically.
- F3 overlay: FPS, state, speed, stamina, position (dark backing, readable over the sky).
- Camera without a mouse: arrow keys (eased) and drag-to-look; CI also builds native Linux.
- 19 EditMode tests (movement math, stamina).

### How to test
On the Kali laptop: `tools/play.sh` (or menu "AETHVIRSE (Unity)") downloads the newest successful CI
build for Linux and starts it — nothing is built locally. `tools/play.sh m1-core-movement` picks a branch.
Windows: the run's Artifacts. Note: under Wine the captured mouse sends no movement on Hyprland — use the
native Linux build.
1. Click to capture the mouse, or hold a touchpad button and drag, or use the arrow keys. Walk/run/sprint around; push the stick lightly on a gamepad to walk.
2. Steps: 0.2 / 0.35 m walk up, 0.5+ need a jump; 1.5 m needs a full jump, 2.5 m is out of reach.
3. Ramps: 20° and 40° walkable, 55° not.
4. Gaps: 2 m run-jump, 4 m sprint-jump, 6 m sprint-jump + air dash (Ctrl in the air).
5. Camera: back into the wall / pillars / tunnel — the camera must not go through walls.
6. Fall recovery: walk off the edge of the 120 m ground; you land back on the last safe spot.
7. Climb the tower ramp and jump down 8 m (no fall damage yet; health comes in M2).

- **Touch controls** (phones, or `-touch-ui` on PC): floating joystick on the left 45%, camera drag on the
  rest, Jump and Dash buttons (holding Dash after the dash keeps sprinting), safe-area aware. They feed a
  virtual gamepad, so gameplay code has no touch special cases.

- Phones are landscape only (left or right side up).

### Not yet / next
- Hero model + animations (the body is a capsule with a visor that shows facing).
- Camera options (sensitivity, invert Y, distance) are fields on `PlayerCameraRig`; the Settings menu comes later.
- Camera shake and lock-on come with combat (M2).
