# Movement (Task 03): 2.5D player controller

Result codes: **PASS**, **FAIL**, **BLOCKED**, **NOT TESTED**. Editor `6000.3.27f1`, run from the repo root with `Unity.exe -batchmode`.

## What exists

| Piece | Path |
| --- | --- |
| Pure C# model (no Unity types, enforced by `noEngineReferences`) | `Assets/_Game/Scripts/Domain/Movement/MovementModel.cs`, `MovementConfig.cs` |
| Rigidbody driver (Presentation) | `Assets/_Game/Scripts/Presentation/PlayerMotor.cs` |
| Tuning asset (ScriptableObject wrapping `MovementConfig`) | `MovementConfigAsset.cs`, `Assets/_Game/Data/PlayerMovement.asset` |
| Input Actions | `Assets/_Game/Settings/Player.inputactions` (map `Gameplay`: `Move` axis, `Jump` button) |
| Test scene | `Assets/_Game/Scenes/MovementTest.unity` (in build settings), debug readout `MovementDebugHud.cs` |
| Generator for the three assets above | `Assets/Editor/MovementSetup.cs`, menu `Worldtree > Apply Movement Setup` or `-executeMethod Worldtree.Editor.MovementSetup.Apply` |
| Tests | `Tests/EditMode/MovementModelTests.cs` (16), `Tests/PlayMode/PlayerMotorTests.cs` (10), `Tests/PlayMode/MovementSceneTests.cs` (2) |

## How it works

- `PlayerMotor.FixedUpdate` (60 Hz, `SimulationConfig`) copies the Rigidbody velocity into the model, calls `MovementModel.Step(input, grounded, dt, config)`, and writes the velocity back. Because the model starts each step from the *measured* velocity, walls and ceilings stop motion without the model knowing about them.
- Body (decision D3 from the plan, applied by me, **not confirmed by the project owner**): dynamic `Rigidbody`, `useGravity = false` (gravity is the model's), `FreezePositionZ | FreezeRotation`, interpolation on, continuous-dynamic collision, a zero-friction material so wall contact never holds the player up, `CapsuleCollider`.
- Plane: the player is moved onto `planeZ` in `Awake` (before the freeze, because Unity ignores position writes on a frozen axis); afterwards Z is frozen and velocity Z is always 0.
- Ground check: sphere cast under the capsule; surfaces steeper than `maxGroundSlope` (50 degrees) do not count as ground.
- Input: `Update` reads the actions; a jump press is latched until the next fixed step so it cannot be lost. `SetMove` / `QueueJump` let tests and later systems drive the motor without a device.
- One-way platforms: a collider with `OneWayPlatform` is solid only while the player's feet are at or above its top. `PlayerMotor` switches `Physics.IgnoreCollision` for platforms within a step of the player, before every fixed step (casts ignore that flag, so the ground check also skips ignored hits). The "above" test allows 0.1 m plus one step of fall distance: a fast faller sinks that far into the top before contact resolves, and without the allowance the landing is mistaken for a pass from below (found at 17 m/s, see Evidence). Platforms are also passable from the side; there is no drop-through input yet.
- Fall reset: below `fallResetY` the player returns to its spawn point, velocity and model state cleared, `FallResets` incremented.

## Rules in the model (all values in `MovementConfig.Default`)

| Rule | Behaviour |
| --- | --- |
| Run | Speed moves toward `input * MaxRunSpeed` (8 m/s) at 60 / 80 m/s^2 (accelerate / release) on ground, 40 / 30 in air. Input is clamped to -1..1. |
| Jump | Sets vertical speed to `JumpVelocity` (13 m/s); apex about 2.4 m. Falling gravity is x1.3, fall speed capped at 25 m/s. |
| Double jump | `MaxJumps = 2`: one more jump in the air, also *sets* vertical speed. |
| Third jump | Refused. The press stays buffered, so if you land within the buffer it becomes a normal ground jump. |
| Landing | Resets the jump count. |
| Coyote time | 0.1 s after leaving ground a jump still counts as the first (ground) jump. |
| Walking off a ledge | Once coyote time has run out the ground jump is spent, so only the double jump remains. |
| Jump buffer | A press is remembered 0.1 s. |
| States | `Idle, Run, Jump, DoubleJump, Fall`. |

## Evidence

| Check | Result | Command / evidence |
| --- | --- | --- |
| Project compiles, new assets import | PASS | `Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod Worldtree.Editor.MovementSetup.Apply` exit 0, no `error CS` |
| EditMode suite | PASS | `-runTests -testPlatform EditMode`: 25 total, 25 passed, 0 failed (16 new movement tests, the rest are Task 01 tests) |
| PlayMode suite | PASS | `-runTests -testPlatform PlayMode`: 13 total, 13 passed, 0 failed (run in a copy of the project, see note under Assumptions) |
| Run / stop on the plane | PASS | `Runs_ToMaxSpeed_OnThePlane`: reaches 8 +/- 0.2 m/s, returns to 0, Z drift < 1e-4 |
| Single jump height | PASS | `SingleJump_ReachesExpectedHeight`: within 15% of 2.41 m (a probe run measured 2.52 m; the model integrates one step behind the body) |
| Double jump | PASS | `DoubleJump_GoesHigherThanSingle_AndThirdIsRejected`: peak > 1.4x a single apex |
| **Third jump rejected** | PASS | Same test (rigidbody: jump count stays 2, vertical speed does not increase, peak < 2.15x apex) and EditMode `ThirdJump_IsRejected`, `Coyote_JumpAfterWindow_LeavesOnlyOneJump` |
| Coyote time, buffer, landing reset, speed clamp, fall clamp | PASS | EditMode `Coyote_*`, `Buffer_*`, `Landing_ResetsJumpCount`, `Falling_IsClampedToMaxFallSpeed` |
| **Plane constraint** | PASS | Pressing into a wall while jumping for 4 s: max Z drift < 1e-4, wall stops the player at x <= 9.06 (face at 9.5, radius 0.5); `SpawningOffThePlane_SnapsToPlane_AndFreezesZ` (spawn at Z = 0.7 ends at Z = 0 with Z and rotation frozen) |
| Fall reset | PASS | `FallingIntoThePit_ResetsToSpawn` |
| Input Actions wired in the scene, real device path | PASS | `MovementSceneTests`: scene loads, ortho camera, player rests on ground; a virtual keyboard sends D (runs right) and Space (jumps) through `Player.inputactions` |
| Windows build still succeeds | PASS | `FoundationSetup.BuildWindows`: "Build Finished, Result: Success." |
| Built player launched | NOT TESTED | exe not run |
| Played by a human with a real keyboard | NOT TESTED | only virtual-device input was sent; the scene was never viewed |
| Gamepad bindings (`leftStick/x`, `buttonSouth`) | NOT TESTED | defined, no gamepad test |
| One-way platforms | PASS | `OneWayPlatform_CanBeJumpedUpThrough_ThenStoodOn`, `..._TooLowAJump_FallsBackThrough`, `..._FallingOntoItFromAbove_Lands` (from 5 m up, about 17 m/s), `SolidBlock_StillBlocksFromBelow`, and the scene check that all three ledges are one-way |
| Slopes, moving platforms, platform drop (down + jump) | NOT TESTED / not built | slope limit is coded but unexercised; drop-through is Phase 1 per the audit proposal |
| Camera follow and jitter at 60/144 Hz | NOT TESTED | camera is Task 04; this scene uses a static orthographic camera |

## Test scene layout (`MovementTest`, Z = 0 plane, all blocks 2 deep)

Ground left (x -12..3) and right (x 6..18) with a 3 m pit; wall at x = 15; one-way ledges (jump up through, stand on top) at height 1.5 (one jump), 3.8 (needs the double jump) and 6.5 (unreachable: a working third jump would reach it). Spawn at x = -10.5; falling below y = -8 resets. The on-screen readout shows state, jump count and velocity.

## Assumptions and risks

- **Assumptions:** D3 (dynamic Rigidbody) as above; tuning numbers are my starting values, not from the spec (GAME_DESIGN gives none); a jump spent by walking off a ledge is intentional; capsule is unit-scale and unrotated (primitive capsule is 2 m tall, not the 1.8 m in the plan).
- `DoubleJump` state also appears for the single air jump after walking off a ledge (the count is 2 there). Harmless now; revisit when animation consumes it.
- Variable jump height (release to cut), drop-through (down + jump) and a respawn-point system (checkpoints) are **not** built; the reset always returns to the scene spawn.
- Moving-platform and slope behaviour will need the model to learn ground velocity; deliberately not designed yet.
- `Physics.SyncTransforms()` runs once in `Awake`; if many players are ever spawned per frame this should move to a spawn helper.
- Tests rely on `Player.inputactions` being enabled by `PlayerMotor`; two motors sharing one asset would share enabled state (single player only for now).
- Test-only: PlayMode keyboard test overrides `backgroundBehavior` and `editorInputBehaviorInPlayMode` and restores them; without that, batch mode drops all keyboard input.
- Edits to Task 01 files: `FoundationSetup.CreateIfMissing` is now `public`, and it no longer calls `RestoreSceneManagerSetup` with an empty list (this threw "Invalid SceneManagerSetup" when setup ran in batch mode with no scene open). Task 01 tests still pass.

## Next steps

1. Task 04: Cinemachine follow camera and jitter check at several refresh rates.
2. Owner decision on D3 and the tuning values; play the scene and adjust `PlayerMovement.asset`.
3. Phase 1: drop-through (down + jump) when combat movement needs it.

## Verification note (one-way platforms)

The one-way change was tested in a copy of the project, because the Unity editor was open on the real checkout and a batch run on the same folder would conflict. The copy's `MovementTest.unity` was regenerated there and copied back with the real `OneWayPlatform` script GUID substituted; the real checkout's scene was not itself run through the test suites. Re-run both suites in the real project before relying on it.
