# Validation Log: Task 01 Unity foundation (2026-10-30)

Editor: 6000.3.27f1, run via `Start-Process Unity.exe -Wait` from the repo root. First attempt was BLOCKED (no licence, exit 198); after the licence was activated every step below was rerun.

| Check | Result | Evidence |
| --- | --- | --- |
| Unity 6 editor installed | PASS | 6000.3.27f1 and 6000.6.7f1 under `C:\Program Files\Unity\Hub\Editor` |
| Git LFS enabled | PASS | `git lfs install --local`; `git check-attr -a -- test.fbx` shows `filter: lfs` |
| Project opens and compiles | PASS | `-batchmode -nographics -quit` exit 0; no `error CS` in log |
| Packages resolve | PASS | URP 17.3.0, Input System 1.20.1, Test Framework 1.6.0, Cinemachine 3.1.5 (from registry) in `Library/PackageCache`; no resolve errors |
| URP asset, scenes generated | PASS | `FoundationSetup.Apply` run twice, exit 0; `Bootstrap.unity`, `Sandbox.unity`, `URP_Asset.asset` created |
| Input System only, force-text, 1/60 timestep | PASS | `activeInputHandler: 1`, `m_SerializationMode: 2`, Fixed Timestep rational ≈ 1/60 in the settings assets |
| EditMode tests | PASS | `-runTests -testPlatform EditMode`: 1 total, 1 passed, 0 failed |
| PlayMode test (Bootstrap loads Sandbox, ortho camera, 60 Hz step) | PASS | `-runTests -testPlatform PlayMode`: 1 total, 1 passed, 0 failed |
| Windows build | PASS | `BuildWindows`: "Build Finished, Result: Success."; `Builds/Windows/WorldtreeLegacy.exe` exists (Win64 Mono, development) |
| Built player launched | NOT TESTED | exe not run |
| Domain rejects `using UnityEngine;` | NOT TESTED | manual check from plan M2 |
| Unity Editor MCP | NOT TESTED | no Pipeline package installed |

## Notes
- Warnings, not errors: empty Application and Infrastructure assemblies "will not be compiled, because it has no scripts"; "Native extension for iOS/WindowsStandalone/WebGL not found".
- "Visible Meta Files" is no longer a setting in this editor's `EditorSettings.asset` (the first setup run logged the property as not found), so that line was removed from the script. `.meta` files are generated regardless.
- The first `&`-style call returned instantly with exit 0 and no log; ignore it. Use `Start-Process -Wait`.
