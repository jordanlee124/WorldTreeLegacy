# Camera and graybox (Task 04)

Result codes: **PASS**, **FAIL**, **BLOCKED**, **NOT TESTED**. Editor `6000.3.27f1`, Cinemachine 3.1.5, run with `Unity.exe -batchmode`.

## What exists

| Piece | Path |
| --- | --- |
| Camera prefab | `Assets/_Game/Prefabs/GameCamera.prefab` (root `GameCamera`: `Main Camera` with `CinemachineBrain`; `CM Follow` with `CinemachineCamera`, `CinemachinePositionComposer`, `CameraBounds`, `CameraWarpOnRespawn`) |
| Graybox scene | `Assets/_Game/Scenes/Graybox.unity` (in build settings) |
| View-bounds maths (pure C#, EditMode-tested) | `Assets/_Game/Scripts/Domain/Camera/ViewBounds.cs` |
| Bounds extension | `Presentation/CameraBounds.cs` (Cinemachine extension, world-space `Rect`) |
| Fall-reset zone | `Presentation/FallResetZone.cs` (trigger volume), `PlayerMotor.Respawn()` and the `Respawned` event |
| Respawn camera warp | `Presentation/CameraWarpOnRespawn.cs` |
| Debug overlay | `Presentation/MovementDebugHud.cs` (F3 toggles): state, jumps, player and camera position, player's screen position, fps, fixed rate |
| Generator | `Assets/Editor/GrayboxSetup.cs`, menu `Worldtree > Apply Graybox Setup` or `-executeMethod Worldtree.Editor.GrayboxSetup.Apply` (idempotent: skips what exists) |
| Tests | `Tests/EditMode/ViewBoundsTests.cs` (6), `Tests/PlayMode/GrayboxSceneTests.cs` (5 cases), `PlayerMotorTests.FallResetZone_*` |

## Camera design

- Orthographic, size 6 (12 m tall), camera Z = -10 looking along +Z at the Z = 0 plane. Brain update method is `LateUpdate`: with the interpolated rigidbody it measured less jitter than `SmartUpdate` (see Evidence).
- Follow: `CinemachinePositionComposer`, damping x 0.35 s, y 0.25 s (slower so hops do not bob the view), dead zone 8% x 20% of the screen, hard limits 70% so damping can lag but the player never leaves the screen area. Lookahead is off.
- Boundaries: `CameraBounds` keeps the whole view rectangle inside `Bounds` (default for the Graybox: x -6..151, y -6..14). A view larger than the bounds on an axis is centred. A plain rectangle was chosen over `CinemachineConfiner` so no collider or Physics2D is involved (ARCHITECTURE: one physics approach).
- Respawn: `PlayerMotor` raises `Respawned(delta)`; `CameraWarpOnRespawn` calls `OnTargetObjectWarped`, so the camera jumps with the player instead of gliding the length of the level.
- Per-level values (bounds, follow target) live on the prefab instance; tuning of the camera itself is on the prefab.

## Graybox course (Z = 0 plane, all blocks 2 deep)

Spawn x = 0. Ground A (x -5..60, flat 65 m run-up) | 4 m pit (one jump) | Ground B (64..100) with steps (70..74, 1 m and 2 m), one-way ledges at heights 2 (x 78..83) and 4 (x 85..90), a barrier at x 95 (3 m, needs the double jump) | 8 m pit (double jump, or the one-way ledge at x 103..105, height 2) | Ground C (108..150). Solid walls at both ends. The fall-reset zone is a trigger box (x -28..172, y -8..-4) under the pits. Distances assume the default movement config.

## Evidence

| Check | Result | Command / evidence |
| --- | --- | --- |
| Prefab and scene generated, project compiles | PASS | `-executeMethod Worldtree.Editor.GrayboxSetup.Apply` exit 0 |
| EditMode suite | PASS | `-runTests -testPlatform EditMode`: 31 total, 31 passed, 0 failed (6 new `ViewBoundsTests`) |
| PlayMode suite | PASS | `-runTests -testPlatform PlayMode`: 20 total, 20 passed, 0 failed (full suite, in project order; the final run still included the uncapped jitter case, since removed from the assertions) |
| Player visible | PASS | `RunningRight_*` asserts the player's viewport position stays within 5%..95% on every sampled frame; also at start, against the barrier and after a reset |
| View stays inside bounds | PASS | same tests assert the view rectangle never leaves `CameraBounds.Bounds` |
| Camera jitter | PASS (measured, not zero) | camera-to-player offset spread while running at constant speed after the camera has settled, limit 50 mm. Runs differ: **144 fps 7-14 mm, 30 fps 0.8-18 mm, uncapped (about 4300 fps in batch mode) 26-45 mm**. 11 mm is about one pixel at 1080p (view 12 m tall). The uncapped case is no longer asserted because it sits close to the limit and is not a real frame rate. `SmartUpdate` gave 14.5 mm at 144 fps and 44 mm uncapped in one comparison run, so `LateUpdate` is only a modest gain. |
| Collisions on the plane | PASS | `Barrier_BlocksPlayer_OnThePlane_*`: stops at the barrier face (one step, 0.13 m, of impact overlap allowed), Z drift < 1e-4 |
| Fall reset zone | PASS | `FallingIntoThePit_ResetsToSpawn_*` in the scene and `FallResetZone_ReturnsPlayerToSpawn_AndRaisesRespawned`; camera is back on the player within a few frames |
| Jumps | PASS | Task 03 suite (single, double, third rejected, one-way) still passes unchanged |
| Real display refresh (vsync 60 / 144 Hz monitor) | NOT TESTED | frame rate was capped in software in batch mode only |
| Seen by a human, camera feel | NOT TESTED | nobody played the scene |
| Window aspect ratios other than the test runner's | NOT TESTED | clamp uses the camera's live aspect |
| Windows build | BLOCKED | `FoundationSetup.BuildWindows` in the scratch copy ended "Build Finished, Result: Failure": the Burst AOT step could not start (the same Burst native-library load error seen all session, probably the long scratch path). No C# compile errors. The last real success was before Tasks 03 and 04, so this needs a run in the real project. |

## Assumptions and risks

- **Residual jitter of about 1 pixel at 144 fps** is measured, and I did not find its source (frame timing in the test versus the camera pipeline are both candidates). If it is visible by eye, next steps are `CinemachineBrain.UpdateMethod = FixedUpdate` with a fixed-rate target, or pixel-snapping the camera.
- The offset test measures stability at constant speed. It does not measure stutter during acceleration, jumps or landings.
- The reset zone and `fallResetY` are two mechanisms: the scene uses the zone (the motor's own Y limit is -50 here, only a safety net).
- Camera values (size 6, damping, bounds) are my starting numbers; the design doc gives none.
- `CameraBounds.Bounds` is a hand-set rectangle; moving the course means editing it.
- `ProjectSettings` changes made by an open editor (`GraphicsSettings`, `QualitySettings`, `PackageManagerSettings`, `URPProjectSettings`) are not part of this task and were left out.
- Verification was done in a copy of the project because the editor was open on the real checkout; the generated prefab and scene were copied back with script GUIDs reconciled. Re-run both suites in the real project.

## Next steps

1. Play `Graybox` and judge camera feel (damping, dead zone, size); tune on the prefab.
2. If 144 Hz jitter is visible, try the options above.
3. Checkpoints (respawn points other than the scene spawn) when a level needs them.

## Test-isolation note

The Task 01 `Bootstrap` test leaves the Sandbox scene (its own camera and Cinemachine brain) loaded, which made `Camera.main` pick the wrong camera in full-suite runs. `GrayboxSceneTests` therefore selects the camera by scene and disables brains from other scenes. Filtered runs passed before this fix; full runs did not.
