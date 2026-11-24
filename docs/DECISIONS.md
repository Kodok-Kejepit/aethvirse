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
