# Phase 0 — Implementation Plan

Status: **plan only.** Nothing here has been executed. Inputs: [Audit](Audit.md), [ARCHITECTURE](../ARCHITECTURE.md), [ROADMAP](../ROADMAP.md), [GAME_DESIGN](../GAME_DESIGN.md).

Phase 0 goal (Source, ROADMAP): Unity, Blender pipeline, movement, camera, architecture — closed by the Phase 0 gate. The roadmap's 1–2 week estimate is not a commitment; milestones below are sized S/M/L, not dated.

Out of scope for Phase 0: combat, enemies, bosses, gear, saves, UI beyond debug text, Addressables (Proposal, see Audit §5.6), networking, final art.

## 1. Prerequisite checklist

Decisions (owner: user). Each blocks the milestone shown.

| # | Decision | Proposal | Blocks |
| --- | --- | --- | --- |
| D1 | Unity editor version | 6000.3.27f1 (installed, has IL2CPP) | M1 |
| D2 | `docs/` vs `Docs/` | Keep `docs/`; update CLAUDE.md and ARCHITECTURE folder list to match | M0 |
| D3 | Player physics body | Dynamic `Rigidbody`, freeze Z position and X/Y/Z rotation, interpolation on, velocity set from a pure C# movement model | M3 |
| D4 | Blender conventions | 1 Blender unit = 1 m = 1 Unity unit; character origin at feet; FBX export with "Apply Transform"; prefixes `SM_` static, `SK_` skinned | M5 |
| D5 | Repo visibility and canonical Task 1 branch | Decide before first push | M0 |
| D6 | Layer dependency direction | Infrastructure references Domain (implements domain interfaces), never the reverse | M2 |

Environment (owner: user or agent with approval):

- [ ] Open Unity Hub once and confirm licence is active for the chosen editor.
- [ ] VS 2026 "Game development with Unity" workload, or VS Code + Unity extension.
- [ ] `git lfs install` inside the repo.
- [ ] Optional: start Blender with the MCP addon; install the Unity Pipeline package if agent-driven editor checks are wanted.

## 2. Milestones

### M0 — Repo hygiene (S)
- Add `.gitattributes`: LFS for `*.blend *.fbx *.glb *.png *.psd *.tga *.exr *.wav *.ogg *.mp3`; `*.unity *.prefab *.asset *.mat *.anim *.controller merge=unityyamlmerge eol=lf`.
- Extend `.gitignore`: `*.blend1`, `[Mm]emoryCaptures/`, `[Rr]ecordings/`, crash dumps; anchor `/[Bb]uild/` and `/[Bb]uilds/` to the root.
- Resolve D2 docs casing.
- **Accept:** `git check-attr -a -- test.fbx` shows `filter: lfs`; `git lfs track` lists patterns.

### M1 — Unity project skeleton (M)
- Create the project at repo root with editor D1, URP 3D template.
- Packages: URP, Input System, Cinemachine, Test Framework. Record exact versions from `Packages/manifest.json` in the PR.
- Project settings: Active Input Handling = Input System; Fixed Timestep = 1/60; Force Text serialization and Visible Meta Files (confirm defaults).
- Folders (Source): `Assets/_Game/{Art,Audio,Data,Prefabs,Scenes,Scripts,Tests}`, `Blender/{Characters,Bosses,Environments,Exports}`. Create only folders that get content in Phase 0; Git does not track empty ones.
- **Accept:** gate items 1 and 2 — batch-mode open exits 0 with no compile errors in the log.

### M2 — Assembly definitions (S)
| Assembly | References | Notes |
| --- | --- | --- |
| `Worldtree.Domain` | none | `noEngineReferences: true` enforces the "no Unity" rule at compile time |
| `Worldtree.Application` | Domain | |
| `Worldtree.Infrastructure` | Domain | per D6 |
| `Worldtree.Presentation` | Application, Domain, Unity packages | MonoBehaviours, camera, input |
| `Worldtree.Tests.EditMode` | Domain, Application | pure logic tests |
| `Worldtree.Tests.PlayMode` | all | scene/prefab and integration tests |

Script subfolders (`Core, Combat, Characters, …` from ARCHITECTURE) sit inside these assemblies as they gain code; no empty asmdefs for Phase 1+ areas.
- **Accept:** gate item 3; an EditMode test that tries `using UnityEngine;` from Domain fails to compile (manual check once).

### M3 — Movement (M)
- Domain: `MovementModel` — plain C# state (grounded, jumps used, velocity) advanced by `Step(input, dt, config)`. Config is a plain struct filled from a ScriptableObject in Presentation (data-driven, Source).
- Presentation: `PlayerMotor` MonoBehaviour reads Input System actions, calls the model in `FixedUpdate`, applies velocity to the Rigidbody (D3).
- Gameplay logic does not read animation events (Source).
- EditMode tests: move left/right reaches configured speed; jump only when grounded; second jump allowed once airborne; third refused; landing resets count; Z never changes.
- PlayMode test: player on a platform, simulated input, asserts double jump height and Z constant after N fixed steps.
- **Accept:** gate item 5.

### M4 — Camera (S)
- Cinemachine camera, orthographic, looking along +Z, follows the player with damping; player Rigidbody interpolation on.
- PlayMode test: camera stays orthographic and at fixed Z while following.
- Jitter: manual check at 60, 144 Hz and uncapped frame rate; record result.
- **Accept:** gate item 6.

### M5 — Blender pipeline (M)
- Write `Blender/README.md` with D4 conventions.
- Source files: `Blender/Characters/humanoid_proxy.blend` (≈1.8 m blockout), `Blender/Environments/platform_proxy.blend` (e.g. 4 × 0.5 × 2 m).
- Headless export: `blender -b <file>.blend --python Blender/export_fbx.py -- <out>` into `Blender/Exports/`, copied to `Assets/_Game/Art/`.
- EditMode test: imported humanoid bounds height within ±5% of 1.8 and feet at y≈0.
- **Accept:** gate item 4.

### M6 — Tests, build, QA (S)
- Editor build script (`-executeMethod`) for a Win64 Mono development build into `Builds/` (ignored).
- Short profiler capture in the test scene; record frame time.
- Independent QA pass against the gate; report PASS / FAIL / BLOCKED per item.
- **Accept:** gate items 7 and 8.

Order: M0 → M1 → M2 → (M3, M5 in parallel) → M4 → M6. M5 only needs M1.

## 3. Commands planned for verification (not yet run)

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath . -executeMethod Worldtree.Editor.BuildScript.BuildWindows -logFile Logs/build.log
```

```bash
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b Blender/Characters/humanoid_proxy.blend --python Blender/export_fbx.py -- Blender/Exports/SK_HumanoidProxy.fbx
```

The editor path assumes D1 = 6000.3.27f1. `Worldtree.Editor.BuildScript` would live in an editor-only asmdef (not listed above because it is a tooling assembly).

## 4. Risks

| Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| Wrong Unity stream chosen (non-LTS) | Medium | High — upgrades churn assets and packages | D1 before M1; pin version in `ProjectSettings/ProjectVersion.txt` |
| Licence not active / Hub-CLI beta issues | Low | Blocks all Unity work | Open the editor manually first; Unity CLI is beta, fall back to `Unity.exe` direct calls |
| Rigidbody jitter with ortho follow camera | Medium | Gate item 6 fails | Interpolation on; camera updates in LateUpdate/SmartUpdate; test at several refresh rates |
| Domain leaks Unity types | Medium | Breaks future server authority and fast tests | `noEngineReferences` on Domain asmdef |
| Blender axis/scale mismatch | Medium | Rework of every asset later | D4 written down + automated bounds test in M5 |
| Large binaries committed without LFS | Medium | Bloated history, hard to undo | M0 before any asset lands; review staged files |
| Public repo leaks unreleased art/design | Medium | Irreversible once pushed | D5 before first push |
| LF/CRLF churn on YAML (`core.autocrlf=true`) | Medium | Noisy diffs, merge conflicts | `eol=lf` for Unity YAML in `.gitattributes` |
| Two parallel Task 1 branches diverge | Medium | Duplicate/conflicting plans | D5: pick one canonical branch |
| Scope creep into combat/gear in Phase 0 | Medium | Phase 0 never closes | Hold to the gate list; platform drop and Addressables deferred unless agreed |
| Content too close to the inspiring game | Low–Medium | IP risk | Originality check before committing names, data and art |
| Solo pace vs roadmap estimates | High | Schedule pressure | Estimates treated as non-binding (Source) |

## 5. Assumptions

- Unity project lives at the repo root (ARCHITECTURE folder list starts at `Assets/`).
- 6000.3 is an LTS stream (not verified by a tool in the audit).
- Phase 0 player is a proxy capsule/blockout; no rig or animation beyond what the gate needs.
- Windows x64 Mono development build is enough for the Phase 0 gate; IL2CPP release builds come later.
