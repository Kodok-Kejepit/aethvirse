# Progress (Unity)

The Godot prototype (`../aethvirse/docs/PROGRESS.md`) is the reference for what each system should do.

## M0 — Setup (2026-11-22 → 11-24)
- Unity 6.3 LTS project, boot scene, GameCI builds Windows + Android on push to `main`.
- Packages: URP 17.3, Input System 1.20, Cinemachine 3.1 (see DECISIONS.md).
- CI runs the EditMode tests before building.

## M1 — Core movement (branch `m1-core-movement`) — in progress

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
- F3 overlay: FPS, state, speed, stamina, position.
- 19 EditMode tests (movement math, stamina).

### How to test
Download the Windows build from the run's Artifacts (or open the project and press Play in `Boot`):
1. Click to capture the mouse. Walk/run/sprint around; push the stick lightly on a gamepad to walk.
2. Steps: 0.2 / 0.35 m walk up, 0.5+ need a jump; 1.5 m needs a full jump, 2.5 m is out of reach.
3. Ramps: 20° and 40° walkable, 55° not.
4. Gaps: 2 m run-jump, 4 m sprint-jump, 6 m sprint-jump + air dash (Ctrl in the air).
5. Camera: back into the wall / pillars / tunnel — the camera must not go through walls.
6. Fall recovery: walk off the edge of the 120 m ground; you land back on the last safe spot.
7. Climb the tower ramp and jump down 8 m (no fall damage yet; health comes in M2).

### Not yet / next
- Touch controls (floating joystick, camera drag, buttons) for Android.
- Hero model + animations (the body is a capsule with a visor that shows facing).
- Camera options (sensitivity, invert Y, distance) are fields on `PlayerCameraRig`; the Settings menu comes later.
- Camera shake and lock-on come with combat (M2).
