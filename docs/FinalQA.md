# Task 07 — Final QA

Date: 2026-10-30, Australia/Sydney. Independent QA: Codex. Execution window: approximately 14:24–14:27 AEDT.

**Updated verdict: the two remaining Phase 0 checks PASS — clean-checkout import/tests/build and playable Windows launch/controls. QA-01 is fixed and verified.** End-to-end profile persistence remains BLOCKED (QA-02), but saves belong to roadmap Phase 3 and are not a Phase 0 completion requirement unless explicitly brought into scope. No claim is made that the full RPG or the broader persistence acceptance is complete.

## Phase 0 closure verification — 2026-10-30, approximately 15:19–15:28 AEDT

The user reported the earlier editor Graybox play-test looked good. On request to verify the remaining Windows-build and clean-checkout checks, Codex created an isolated local Git clone at `D:/Projects/WorldTreeLegacy-QA-20261030` from branch `claude/worldtree-core-architecture`, baseline f1bc2a8. The clone was initially clean, `Library` did not exist, and `git lfs fsck` passed. No Library/Temp/Logs/build output was copied from the original project. Unity performed a first import there. This is a fresh project cache on the same machine; shared machine-level package caches, installed tools and credentials were still available. It is not a new-machine/offline-download test.

### Reproduction and minimal fix

The startup regression was changed first to require Graybox and a PlayerMotor. On the old implementation it failed as intended: **expected Graybox, actual Sandbox**, 1 test failed, process exit 2. Evidence: `D:/Projects/WorldTreeLegacy-QA-20261030/Logs/entry-red.xml` and `entry-red.log`. This first run also exercised the clean import.

Four existing files were corrected:

- `Assets/Editor/FoundationSetup.cs`: BuildWindows now derives its scene list from enabled EditorBuildSettings entries instead of hard-coding Bootstrap/Sandbox.
- `Assets/_Game/Scripts/Presentation/Bootstrap.cs`: default first scene changed to Graybox.
- `Assets/_Game/Scenes/Bootstrap.unity`: existing serialized firstScene changed to Graybox (changing only the script default would not update the scene).
- `Assets/_Game/Tests/PlayMode/BootstrapTests.cs`: verifies Graybox, 60 Hz, orthographic camera and playable character. Teardown unloads the entry scene into an empty test scene so its player/colliders cannot contaminate later physics tests.

After verification, these four files were copied byte-for-byte to the integrated source worktree. Before copying, each original file was confirmed unchanged from its HEAD version; after copying, SHA-256 comparisons confirmed equality with the tested files. Existing GraphicsSettings/QualitySettings changes and untracked PackageManagerSettings/URPProjectSettings were preserved, not copied into the clean clone or reset. They are outside this clean-build certification. Changes remain uncommitted; no branch merge or push occurred.

### Fresh verification

| Check | Result | Evidence |
| --- | --- | --- |
| Clean project import | **PASS** | Fresh clone with no Library; imported and compiled sufficiently to run the deliberately failing startup regression. Failure was the expected scene assertion, not an import/compile error. |
| Corrected full EditMode suite | **PASS** | 118/118, zero failed/skipped, process exit 0. XML start 2026-10-30 04:21:12Z, end 04:21:15Z. Includes movement, saves, Blender imports and foundation tests. |
| Corrected full PlayMode suite | **PASS** | 19/19, zero failed/skipped, process exit 0. XML start 04:21:29Z, end 04:22:10Z. Includes the corrected playable startup test and movement/camera/scene cases. |
| Windows build | **PASS** | Process exit 0, Build Finished, Result: Success. Log explicitly opens Bootstrap, Sandbox, MovementTest and Graybox for packaging. |
| Standalone launch/rendering | **PASS, observed by Codex** | Launched the new executable in a 1280x720 window. Visible Graybox capsule/ground/debug HUD, grounded at spawn, 60 Hz fixed simulation, about 60 fps in the displayed sample. No visible pink materials in that view. No runtime Exception/NullReferenceException/MissingReferenceException/Failed to load matches in the inspected player log. |
| Standalone movement/double-jump | **PASS, user-observed** | Computer-control key taps produced no visible movement, so that automation attempt was not counted as success. User then tested A/D and Space in the standalone window and replied: "Movement and double-jump work". Attribution is explicit; Codex did not independently automate a full course traversal. |
| Diff hygiene | **PASS** | git diff --check exits 0; only the four intended files changed in the isolated clone. |

The earlier QA-01 build/entry failure is closed. The user's earlier "all looks good" feedback applies to the editor course test; the new input confirmation applies to the standalone player. The build is left open for continued testing.

### Commands and artifacts

```powershell
git clone --no-hardlinks --branch claude/worldtree-core-architecture D:/Projects/WorldTreeLegacy D:/Projects/WorldTreeLegacy-QA-20261030
# In the clone: git lfs fsck => 0; Test-Path Library => False before Unity.
```

Unity invocations used `Start-Process -WindowStyle Hidden -Wait -PassThru` with editor 6000.3.27f1 and `-projectPath D:/Projects/WorldTreeLegacy-QA-20261030`:

- RED: `-batchmode -runTests -testPlatform PlayMode -testFilter Worldtree.Tests.BootstrapTests -testResults Logs/entry-red.xml -logFile Logs/entry-red.log` => exit 2, expected assertion failure.
- GREEN full EditMode: `-batchmode -runTests -testPlatform EditMode -testResults Logs/clean-EditMode.xml -logFile Logs/clean-EditMode.log` => exit 0, 118 passed.
- GREEN full PlayMode: `-batchmode -runTests -testPlatform PlayMode -testResults Logs/clean-PlayMode.xml -logFile Logs/clean-PlayMode.log` => exit 0, 19 passed.
- Build: `-batchmode -nographics -executeMethod Worldtree.Editor.FoundationSetup.BuildWindows -logFile Logs/clean-build.log` => exit 0.

Visible player launch:

```powershell
Start-Process D:/Projects/WorldTreeLegacy-QA-20261030/Builds/Windows/WorldtreeLegacy.exe -PassThru -ArgumentList '-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile','D:/Projects/WorldTreeLegacy-QA-20261030/Logs/player-smoke.log'
```

Tested executable: `D:/Projects/WorldTreeLegacy-QA-20261030/Builds/Windows/WorldtreeLegacy.exe`; keep all companion files in its build folder. Launcher SHA-256: `93043E47ABFCE89AE9D048DBF34108E101EDB367150E6203E0BCEE27CD5A623D` (identifies the launcher only, not the entire game content). XML/log artifacts are under the clone's `Logs/`. The old executable under the original worktree was not replaced; use this tested build or rebuild from the corrected source.

**NOT TESTED:** independent standalone automation of the entire obstacle course, physical gamepad, every resolution/display refresh, prolonged performance/stability, offline first-time dependency downloads, a different Windows machine, release/IL2CPP configuration, player exit/relaunch, and end-to-end saves. Earlier camera tests remain bounded to their stated thresholds. These limits remain explicit; no result was upgraded solely because a build succeeded.

Only the four listed source/test/scene files and this report were changed in the original worktree by this closure task. A copy of this updated report is retained in the isolated clone. No multiplayer, art regeneration, save wiring or unrelated gameplay was implemented. Phase 0's outstanding clean-checkout and playable-build checks are closed for this environment; save wiring remains a separate task.

---

## Historical Task 07 verdict (superseded for QA-01 and Phase 0 closure)

**Original verdict: NOT CERTIFIED for the integrated playable game.** Compile, all 137 automated tests, saved Blender-source validation and the existing Windows build passed. At that time the build did not include or enter the movement/Graybox course, and profile persistence was not connected to gameplay. The original evidence below is retained as history; the closure section above supersedes the old build/launch/clean-checkout statuses.

## Scope and preserved baseline

Reviewed `D:/Projects/WorldTreeLegacy/.claude/worktrees/worldtree-legacy-audit-165e6c`, branch `claude/worldtree-core-architecture`, commit `f1bc2a82f778e9c48278d16fff10300d402241a7`. Despite its historical folder name, this is the integrated Tasks 01–06 worktree: movement, camera, Blender assets and the core/save layer are present. Its tree `4fc4cea9098e62d43613d5ce820716056d4d88a8` exactly matches the latest locally available merged branch `claude/worldtree-camera-graybox` at `8701a4a`. No remote fetch was performed.

The main checkout still contains only specifications. The older player/Blender worktree has unrelated existing settings changes; it was inspected read-only and left untouched. QA ran in the initially clean integrated worktree. No Unity Editor process was found running against it before the batch runs (Unity Hub/CLI processes were present).

Read repository instructions, game design, architecture, roadmap, movement/camera/Blender/core task records, relevant tests, bootstrap/build code, importer and save implementation before execution. Unity is pinned to **6000.3.27f1**; Blender reports **5.2.3 LTS**. No graphify graph is present. No project implementation was changed and no setup/generator method was run.

Status definitions: **PASS** = observed success for the named check; **FAIL** = observed requirement/integration mismatch; **BLOCKED** = a missing prerequisite prevents the requested end-to-end check; **NOT TESTED** = not executed, with no success implied.

## Results

| Area | Status | Evidence and limits |
| --- | --- | --- |
| Integrated import and compile | **PASS** | Fresh batch open/quit exit 0; fresh test and player-build sessions also compile. No `error CS` matches in finalqa logs. Uses the existing Library/package cache, not a clean restore. |
| Full EditMode suite | **PASS** | 118 total, 118 passed, zero failed/skipped; process exit 0. XML starts 2026-10-30 03:24:54Z, ends 03:24:56Z. |
| Full PlayMode suite | **PASS** | 19 total, 19 passed, zero failed/skipped; process exit 0. XML starts 2026-10-30 03:25:12Z, ends 03:25:53Z. Run together, not only individually filtered fixtures. |
| Movement domain | **PASS** | MovementModelTests: 16/16. Acceleration, clamps, ground/air jumps, third-jump rejection, coyote time, buffer and landing state. |
| Player physics | **PASS** | PlayerMotorTests: 11/11. Speed/stop, jump heights/count, wall blocking, XY-plane constraint, off-plane spawn correction, one-way platforms, solid underside collisions and fall resets. |
| Movement scene/input wiring | **PASS** | MovementSceneTests: 2/2; grounded plane state and virtual keyboard through real Input Actions for movement/jump. Does not certify a physical keyboard/gamepad session. |
| Graybox scene/camera | **PASS, bounded** | GrayboxSceneTests: 5/5, including two parameterized frame-rate cases. Checks camera follow, orthographic view/bounds/player visibility, barrier/plane collision and fall-reset camera response. |
| Camera-offset stability | **PASS to test threshold** | At target 144 fps: 865 frames in 6 s, 289 settled samples, offset spread 21.7 mm. At target 30 fps: 180 frames, 60 samples, spread 22.5 mm. Both below the test limit of 50 mm. This is software-capped batch rendering, not zero jitter or a physical display/visual-quality certification. |
| Bootstrap scene | **PASS to existing test; FAIL for integrated entry** | BootstrapTests 1/1 confirms loading Sandbox and 60 Hz. That is the old foundation behavior; it does not validate entering the course. See QA-01. |
| Save store | **PASS, component level** | JsonProfileStoreTests: 29/29, real files in unique temporary directories. Covers every-field round trip including 64-bit values, versioned output, backup creation/recovery, malformed/invalid/oversized data, invalid-save rejection and refusal to overwrite newer schemas. |
| Profiles, IDs, events, service and layers | **PASS** | CharacterProfile 14/14; StableId 20/20; EventBus 6/6; ProfileService 4/4; LayerBoundary 3/3. Domain/Application independence is declared and exercised by compilation plus boundary checks. |
| In-game save/load across restart | **BLOCKED** | ProfileService/JsonProfileStore are not composed or called from Presentation; no playable profile flow exists. See QA-02. |
| Blender saved-source conventions | **PASS** | Blender headless process exit 0. Opened both existing .blend files and ran the project's validate_humanoid / validate_platform functions, plus metric/scale checks. No sources were saved, rebuilt or exported. |
| Unity Blender imports | **PASS** | BlenderImportTests: 11/11. Imported dimensions, pivots/axes, facing, normals, material slots/colors, rig/root, vertex weighting, posed humanoid bounds and importer settings. Tests load the existing imported FBX assets. |
| Export consistency and LFS | **PASS** | Both exported FBX files byte-match their Assets copies; git lfs fsck exits 0. Humanoid SHA-256 b6f00241385df56c4e4105009bd3f31b46fbb14f44f4ad52719a0fad0c80c113; platform 79aa24b70943e2a54a731a63c94b9b9ddfac8cb46d694242cfbd5625911bce98. |
| Metadata and package lock | **PASS, static** | 82 .meta files; zero missing companions/orphans/duplicate GUIDs. No direct manifest/lock version mismatches. This is not an exhaustive serialized-reference audit. |
| Windows build execution | **PASS** | FoundationSetup.BuildWindows exits 0; log says `Build Finished, Result: Success.` Win64 development build produced at Builds/Windows/WorldtreeLegacy.exe (667136-byte launcher plus companion files). Earlier reported Burst build failure did not reproduce here. |
| Windows integrated course packaging | **FAIL** | Build script hard-codes only Bootstrap and Sandbox, despite four enabled scenes in EditorBuildSettings. Build log confirms those two scenes only. See QA-01. |
| Built player launch/manual play | **NOT TESTED** | The executable was not launched; no visual, keyboard, gamepad or audio sign-off. |
| Fresh checkout/cache rebuild | **NOT TESTED** | Existing local caches were used. |
| Destructive save fault injection | **NOT TESTED** | No process termination during writes, disk-full, locked-file or read-only-storage tests. Atomicity is not certified by successful normal saves. |
| Save migration | **NOT TESTED** | Only schema v1 exists; newer-version rejection passed, migration did not occur. |

Remaining EditMode fixtures: FoundationSetup 5/5, ProjectSettings 3/3, SimulationConfig 1/1, ViewBounds 6/6. These and the fixture counts above sum to 118. PlayMode counts sum to 19. Some earlier task prose refers to seven Graybox tests; the current XML contains five cases (the parameterized camera test contributes two). Current results use XML counts, not old prose.

## Findings and reproduction

### QA-01 — High / FAIL: Windows build cannot deliver the implemented movement course

Evidence:

- `Assets/Editor/FoundationSetup.cs:135` supplies only BootstrapScene and SandboxScene to BuildPlayer.
- `ProjectSettings/EditorBuildSettings.asset` enables Bootstrap, Sandbox, MovementTest and Graybox, but the custom build method ignores that list.
- `Assets/_Game/Scenes/Bootstrap.unity:151` has `firstScene: Sandbox`; Bootstrap.cs defaults to the same name.
- finalqa-build.log lines 1048–1065 show opening Bootstrap and Sandbox for packaging. The passing Bootstrap test explicitly asserts Sandbox.

Reproduce: run the BuildWindows command below; inspect its scene list and log. The result excludes MovementTest/Graybox. Launching this build is expected to enter Sandbox via Bootstrap; that runtime launch was NOT TESTED in this QA. For the playable course, opening Graybox directly in the Editor currently bypasses this build/entry gap.

Required next step: agree the intended playable entry, include the needed scenes in the supported build path, route Bootstrap to that entry (or another explicit navigation flow), and add a test that checks the intended entry/build scene list. Rebuild and launch the result. Merely enabling Graybox in Build Settings does not fix the existing hard-coded method. No implementation fix was made during QA.

### QA-02 — High integration blocker / BLOCKED: save functionality is not connected to the game

The save layer passes its isolated and service tests, but `Bootstrap.Awake` only sets fixedDeltaTime and loads a scene. Presentation has no references/calls to ProfileService, JsonProfileStore or IProfileStore. `docs/CoreArchitecture.md` explicitly records composition and the new-character flow as not wired.

Reproduce: search `Assets/_Game/Scripts/Presentation` for those three type names and inspect Bootstrap.cs and Worldtree.Presentation.asmdef. There is no entry point through which a player can create/update/save/load a profile. A restart-persistence scenario cannot currently be exercised through the playable application. This is a missing integration prerequisite, not a failed save-store test; Task 06 intentionally stopped at the component layer.

Required next step before certifying persistence: implement the intended minimal profile composition/lifecycle in a separately scoped implementation task, then verify create/change/save/exit/relaunch/load using an isolated QA save directory. Preserve any existing user saves. No gameplay or save composition was added here.

### Non-blocking observations and limits

- Several session logs report a licensing access-token refresh error near startup, but the editor proceeds and all process exits/tests/build succeed. It was not a blocker on this machine; fresh-machine entitlement remains untested.
- Camera spread is measurable (about 22 mm), so the roadmap wording "without jitter" is not certified visually. Physical 60/144 Hz display checks, acceleration/jump/landing smoothness and other aspect ratios remain NOT TESTED.
- No manual comparison of the Blender model appearance in the playable scene was performed. Successful import/pose/material assertions do not prove artistic quality or that the playable character uses the proxy.
- Developer-proposed movement tuning/body choice, domain-port dependency interpretation and profile schema remain as documented. This QA does not convert proposals into owner approvals.
- No final art, networking, combat progression, release/IL2CPP build, performance profiling or remote Git/LFS transfer was undertaken.

## Commands actually executed

Working directory for Unity, Git and Python checks:
`D:/Projects/WorldTreeLegacy/.claude/worktrees/worldtree-legacy-audit-165e6c`

```powershell
$u = 'C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe'
$root = 'D:/Projects/WorldTreeLegacy/.claude/worktrees/worldtree-legacy-audit-165e6c'
$p = Start-Process $u -WindowStyle Hidden -Wait -PassThru -ArgumentList '-batchmode','-nographics','-quit','-projectPath',$root,'-logFile','Logs/finalqa-compile.log'
$p.ExitCode # 0
foreach ($mode in @('EditMode','PlayMode')) {
    $p = Start-Process $u -WindowStyle Hidden -Wait -PassThru -ArgumentList '-batchmode','-projectPath',$root,'-runTests','-testPlatform',$mode,'-testResults',('Logs/finalqa-'+$mode+'.xml'),'-logFile',('Logs/finalqa-'+$mode+'.log')
    $p.ExitCode # 0 for each
}
$p = Start-Process $u -WindowStyle Hidden -Wait -PassThru -ArgumentList '-batchmode','-nographics','-projectPath',$root,'-executeMethod','Worldtree.Editor.FoundationSetup.BuildWindows','-logFile','Logs/finalqa-build.log'
$p.ExitCode # 0
```

Blender was run with a small QA-only adapter at `Logs/finalqa_blender.py` (ignored, no implementation edits):

```powershell
$p = Start-Process 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -WindowStyle Hidden -Wait -PassThru -ArgumentList '-b','--factory-startup','--python-exit-code','1','--python','Logs/finalqa_blender.py' -RedirectStandardOutput Logs/finalqa-blender.log -RedirectStandardError Logs/finalqa-blender-errors.log
$p.ExitCode # 0
```

The adapter loads `Blender/build_proxies.py` with `runpy.run_path(..., run_name='finalqa_validation')`, so its generating main function does not run. It opens `humanoid_proxy.blend`, checks metric units/scale 1, calls validate_humanoid on SK_HumanoidProxy and Rig; then opens platform_proxy.blend, repeats units checks and calls validate_platform on SM_PlatformProxy. Python exceptions force a nonzero Blender exit. Both sources printed PASS.

Additional checks actually run: `git worktree list --porcelain`, `git status --short --branch`, `git log`, `git diff --stat HEAD claude/worldtree-camera-graybox` (empty), `git rev-parse HEAD` and both tree IDs, `git lfs ls-files`, `git lfs fsck` (0), `git diff --check` (0). Read-only Python parsed XML suite/case counts, checked .meta companion/GUID uniqueness, matched manifest versions against the lock and compared both FBX pairs using SHA-256. `rg` inspected finalqa logs, scene/build entry and Presentation save references.

## Evidence files

- [Compile log](../Logs/finalqa-compile.log)
- [EditMode XML](../Logs/finalqa-EditMode.xml) and [log](../Logs/finalqa-EditMode.log)
- [PlayMode XML](../Logs/finalqa-PlayMode.xml) and [log](../Logs/finalqa-PlayMode.log)
- [Windows build log](../Logs/finalqa-build.log)
- [Blender source validation log](../Logs/finalqa-blender.log), [stderr](../Logs/finalqa-blender-errors.log), [QA adapter](../Logs/finalqa_blender.py)

Logs are ignored local artifacts; preserve them separately if moving/sharing this report. Earlier task reports are context, not substitutes for these fresh runs.

## Changed files, risks and next steps

Only deliverable added: `docs/FinalQA.md`. No tracked implementation/settings/assets were changed. Unity refreshed ignored Library/Logs/Builds content; its transient Assets/Resources test-build artifacts were removed by the build pipeline. Git status was clean after validation, before this report was added. The ignored Blender adapter and logs are retained as evidence. Other worktrees were not modified. No commit, merge or push was performed.

Assumption: the integrated core worktree matching the latest locally available merged tree is the intended QA target. This report does not certify the documentation-only main checkout or unpublished changes elsewhere.

Next steps, in order:
1. Fix QA-01 so the shipped build reaches and includes the intended course; update its entry/build regression tests.
2. Resolve QA-02 in a scoped integration task if end-to-end persistence is required for this acceptance gate.
3. Rerun the integrated suites and build; launch the player, exercise movement/jumps/reset/camera with real input, and perform the restart-save scenario once it exists.
4. Validate a clean checkout/import/build and real-display camera behavior before claiming the full Phase 0 gate passed.
