# Decisions

Technical decisions for the Unity version of AETHVIRSE. The Godot prototype's decisions stay in
`../aethvirse/docs/DECISIONS.md` as reference.

## Engine (2026-11-22)
- Unity 6000.3.29f1 (Unity 6.3 LTS), C#. Builds run on GitHub Actions (GameCI), never on the dev laptop.

## Render pipeline: URP (2026-11-24, owner approved)
- `com.unity.render-pipelines.universal` 17.3.0. URP runs on both Windows and Android and supports
  custom toon/outline shaders (Shader Graph or HLSL), which the cel-shaded look needs.
- One pipeline asset for now: `Assets/Settings/Rendering/URP_Default.asset` (+ `_Renderer`), used by
  every quality level. Separate PC / Android assets come with the quality presets (master prompt §13).
- Recreate with menu `AETHVIRSE > Setup Render Pipeline` (`ProjectSetup.EnsureRenderPipeline`).

## Input: Input System package (2026-11-24, owner approved)
- `com.unity.inputsystem` 1.20.1. Active input handling = Input System only (old `Input` class is off).
- Actions-based input (no hard-coded keys) covers keyboard/mouse, gamepad and touch, and supports
  rebinding (master prompt §5).

## Camera: Cinemachine (2026-11-24, owner approved)
- `com.unity.cinemachine` 3.1.8 (pulls in `com.unity.splines`). Third-person orbit camera with
  collision, lock-on framing (target group), shake (impulse) and FOV changes come from Cinemachine
  instead of a hand-written spring arm (master prompt §5).

## Camera smoothness (owner, 2026-11-24)
- Owner: the camera must be perfectly smooth, even on fast FPS-style mouse turns (the Godot camera stuttered).
- The player moves in `Update` (CharacterController, no physics step), and the CinemachineBrain updates in
  `LateUpdate` of the same frame, so camera and body never drift by a frame. `SmartUpdate` is off: it guesses
  the target's loop and a wrong guess shows up as jitter.
- Mouse look is raw 1:1 (pixels → degrees, no smoothing, no deltaTime); only the stick is a rate.
- Frame pacing: VSync on PC, 60 fps cap on Android (`FramePacing`). A frame-rate option comes with Settings.
- Rule for later systems: anything the camera follows must move in `Update`, or be a Rigidbody with
  interpolation on; never move a followed object in `FixedUpdate` without interpolation.

## Combo heat instead of letter ranks (owner, 2026-11-24)
- Owner: letter ranks (D … SSS) feel out of place; use a combo flame that grows instead.
- Tiers (same hit thresholds as the master prompt's ranks): Spark 1 · Ember 5 · Flame 10 · Blaze 20 ·
  Inferno 35 · Phoenix 60. The HUD shows a three-layer procedural flame that grows, recolours (Phoenix
  turns blue-white), flickers and pulses per hit; XP/drop bonuses per tier stay as planned.
- Owner also asked that every attack in a combo moves differently: graybox swings now have their own
  kinds (right/left slash, chop, rising, diagonal, spin, double spin, slam, thrust); real animation clips
  replace them with the hero model.

## Stats & skills port (2026-11-24)
- The tank class shows as **Paladin** (id `vanguard`), as in the latest Godot data (hero roster 2026-11-09);
  STORY.md's "Warden" is the older name.
- Starter weapon ATK = `level × weaponAtkPerLevel` (the sheet's average Rare weapon) until items (M6).
- Test cheats (F6 level up, F7 refill + no cooldowns) are on in every build for playtests; they get gated to
  debug builds with the cheat console (M5) / release (M12).
- Skill looks are graybox effects (procedural rings / arcs / spikes / pillars on one transparent unlit material);
  real VFX come with the visual identity milestone (M4) and the hero model.
