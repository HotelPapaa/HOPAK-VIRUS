# Hopak Torso Motion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the existing Torso dip and lean with each lifted leg, formalize the headless five-Cube player, and prevent builds from overwriting the user's edited scene.

**Architecture:** Extend the pure pose result with normalized Torso motion, then let `h980220_PlayerRhythmController` apply Inspector-scaled offsets relative to the user's existing Torso transform. Update future scene generation to five Cubes, but never invoke it against the current dirty scene; Windows builds consume the saved scene as-is.

**Tech Stack:** Unity 6000.3, C#, NUnit EditMode tests, Legacy Input Manager.

## Global Constraints

- Preserve every existing user modification unless it directly conflicts with this feature.
- Do not regenerate, revert, stage, or commit `Assets/HOPAK VIRUS/Scenes/HOPAK VIRUS.unity`.
- Every C# filename and declared class/interface/enum starts with `h980220_`.
- Legacy Input only; no external art, audio, particles, trails, lines, rings, or range indicators.
- Player visual is exactly five Cubes: Torso and four leg segments; no Head.

---

### Task 1: Torso pose and controller

**Files:**
- Modify: `Assets/HOPAK VIRUS/Tests/EditMode/h980220_HopakPoseTests.cs`
- Modify: `Assets/HOPAK VIRUS/Runtime/h980220_HopakPose.cs`
- Modify: `Assets/HOPAK VIRUS/Runtime/h980220_PlayerRhythmController.cs`

**Interfaces:**
- Consumes: `h980220_HopakPose.Evaluate(h980220_Leg, float)` and existing `ProcessFrame`.
- Produces: `h980220_LegPose.TorsoDip`, `h980220_LegPose.TorsoLean`, serialized `torso`, `torsoBobHeight`, and `torsoLeanDegrees`.

- [ ] **Step 1: Write failing pose tests**

Add assertions that left mid-step returns `TorsoDip = 1`, `TorsoLean = 1`, right mid-step returns the same dip and `TorsoLean = -1`, and neutral returns both zero. Add controller tests using a Torso with non-zero baseline position/rotation to require a `0.18m` dip, `12 degree` left/right lean, exact reset, and name-based automatic lookup.

- [ ] **Step 2: Run focused RED**

Run Unity EditMode with `-testFilter h980220_HopakPoseTests` and save a new XML. Expected: compile failure or assertions fail because Torso pose fields do not exist.

- [ ] **Step 3: Implement minimal Torso motion**

Extend the pose result with normalized dip/lean. In the controller, cache the existing Torso baseline, apply `baselinePosition + Vector3.down * pose.TorsoDip * torsoBobHeight`, and apply `baselineRotation * Quaternion.Euler(0, 0, pose.TorsoLean * torsoLeanDegrees)`. Find a child named exactly `Torso` when the serialized reference is empty, and restore the baseline on neutral/input disable.

- [ ] **Step 4: Run focused GREEN**

Run the combined `h980220_HopakPoseTests` fixture. Expected: every pose and controller test passes with zero Unity errors.

### Task 2: Five-Cube future builder and scene-safe builds

**Files:**
- Modify: `Assets/HOPAK VIRUS/Editor/h980220_GameSceneBuilder.cs`
- Modify: `Assets/HOPAK VIRUS/Editor/h980220_BuildAutomation.cs`
- Modify: `Assets/HOPAK VIRUS/Tests/EditMode/h980220_GameSceneBuilderTests.cs`
- Modify: `Assets/HOPAK VIRUS/Tests/EditMode/h980220_ProjectComplianceTests.cs`

**Interfaces:**
- Consumes: current saved `h980220_GameSceneBuilder.ScenePath`.
- Produces: future generated five-Cube player and `BuildWindows()` that never calls `BuildScene()`.

- [ ] **Step 1: Write failing source/contract tests**

Change the builder contract to require `Torso`, `LeftThigh`, `LeftShin`, `RightThigh`, and `RightShin`, length 5, and no Head. Add a compliance assertion that the source body of `BuildWindows()` contains no `BuildScene()` call.

- [ ] **Step 2: Run safe RED without invoking the builder**

Run only the new compliance/source contract test in the live project. Expected: failure because `BuildWindows()` currently calls `BuildScene()` and builder source still creates Head/six visuals. Do not run `h980220_GameSceneBuilderTests` because its setup rebuilds the user's scene.

- [ ] **Step 3: Implement minimal builder/build changes**

Remove Head creation and its renderer, assign the Torso reference, require five visuals, and remove the `BuildScene()` call from `BuildWindows()`. Throw a clear error if the saved scene asset is missing before building.

- [ ] **Step 4: Run safe GREEN and regressions**

Run the Torso fixture and project compliance tests in the live project. Run PlayMode tests that do not rebuild the scene. Confirm `git diff -- Assets/HOPAK VIRUS/Scenes/HOPAK VIRUS.unity` is byte-for-byte unchanged from the user's pre-feature diff and remains unstaged.

- [ ] **Step 5: Commit source and tests only**

Stage the runtime, editor, tests, spec, and plan changes. Explicitly exclude the scene. Commit with `feat: add Hopak torso bounce`.
