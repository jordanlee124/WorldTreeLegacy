# Task 02 — Foundation review

Date: 2026-10-30 (Australia/Sydney). Reviewer: Codex, independent inspection and fresh validation.

**Current verdict: PASS — original findings F1–F4 closed for the independently verified scope.** No new blocking finding identified. This approves the foundation corrections, not the full Phase 0 gameplay/art gate.

## Latest independent review — 2026-10-30, 12:44–12:46 Australia/Sydney

Reviewed the current uncommitted working tree over e401730 in the same foundation worktree. The remaining change sets company identity to **Siregi**, alongside product **Worldtree Legacy**, in both `ProjectSettings/ProjectSettings.asset:15–16` and `FoundationSetup.SetProjectSettings`. `Identity_IsSiregiWorldtreeLegacy` now checks both values. `docs/UnitySetup.md` records Siregi as the owner's choice. **F4 is now PASS.** Earlier BLOCKED/FAIL entries below are historical and superseded by this verdict.

### Verified results

- **PASS F1:** scene-preservation regression cases passed again; no final diff to production scenes or EditorBuildSettings.asset; temporary test directory removed by test teardown.
- **PASS F2:** saved rational remains 2352000 / 141120000, exactly 1/60 second; saved and loaded setting tests passed in the new editor process.
- **PASS F3:** configured driver still matches the previously independently tested command. The successful independent-edit merge and correctly rejected same-field conflict from the preceding review remain the evidence; merge fixtures were not rerun because the driver and attributes are unchanged.
- **PASS F4:** saved Siregi / Worldtree Legacy identity and fresh identity regression test agree with setup code and documentation.
- **PASS:** fresh EditMode **9/9**, zero failures/skips, process exit **0**; XML start 2026-10-30 01:44:38Z.
- **PASS:** fresh PlayMode **1/1**, zero failures/skips, process exit **0**; XML start 2026-10-30 01:44:57Z.
- **PASS:** fresh Windows development build, process exit **0**, `Build Finished, Result: Success.` No `error CS` matches in these logs.
- **PASS:** final `git diff --check` exit **0**. No package, production scene or build-scene configuration diff. The pre-existing six modified tracked files and four untracked test/meta files remain; implementation work was preserved.

Commands: the same sequential `Start-Process -WindowStyle Hidden -Wait -PassThru` invocations recorded in the preceding review, with log/XML prefix changed from `Logs/recheck-` to `Logs/final-review-`. Test platforms were EditMode and PlayMode; build method was `Worldtree.Editor.FoundationSetup.BuildWindows`; editor remained 6000.3.27f1. Process ExitCode and XML counts were read independently.

Fresh evidence: [EditMode XML](../Logs/final-review-EditMode.xml), [PlayMode XML](../Logs/final-review-PlayMode.xml), [EditMode log](../Logs/final-review-EditMode.log), [PlayMode log](../Logs/final-review-PlayMode.log), [build log](../Logs/final-review-build.log).

### Limits and changes

**FAIL:** none among original F1–F4 within tested scope. **BLOCKED:** broader Blender/player movement/follow-camera acceptance still lacks those later-phase implementations. **NOT TESTED:** player launch, clean-cache restoration, interactive/multi-scene and exceptional scene-save paths, and other limitations documented in the preceding review. The optional save-failure restoration observation remains open as non-blocking robustness advice; it was not silently marked fixed.

Only this report was edited by the reviewer. Unity refreshed ignored logs/caches/build output. No implementation changes, commits, merges or pushes. The current fixes are still uncommitted. Next validation work is player launch and clean-checkout restoration; neither is implied by the passing build.

---

## Previous review snapshot (superseded)

**Previous verdict: F1–F3 PASS; F4 PARTIAL / BLOCKED pending company identity.**

## Independent re-review — 2026-10-30, 12:12–12:14 Australia/Sydney

Reviewed the uncommitted fixes over `e401730a3d17adf4fe4937f95a608d9d695eac8f` in the same foundation worktree. Existing modifications were preserved. The sections below this re-review retain the original findings and implementer's evidence as history; their old FAIL labels, test counts, clean-status statements and NOT TESTED merge status describe the earlier snapshot, not the current result.

### Current findings and disposition

| Finding | Status | Independent evidence |
| --- | --- | --- |
| F1 — destructive setup | PASS, scoped | Source now skips existing scene files and appends only absent build entries. Five regression tests passed: existing sentinel/file preservation, missing-scene creation, build ordering/enabled-state preservation, repeatability and refusal to discard dirty scene work. Existing project scenes/build settings have no diff against HEAD. Full interactive menu and exceptional save-failure paths were not exercised. |
| F2 — saved timestep | PASS | Saved count 2352000 / rate 141120000 = exactly 1/60 second. Fresh EditMode process passed both saved-file and loaded-Time.fixedDeltaTime checks before Bootstrap. Setup now writes the rational fields and marks settings dirty. ValidationLog preserves and corrects the historical false PASS. |
| F3 — YAML merge | PASS, scoped | Local driver configuration present and documented. Independently merged copies of Sandbox in a disposable Git repo: Ground scale x=30 and position z=7 both survived, exit 0, no conflict markers. GIT_TRACE confirmed UnityYAMLMerge.exe invocation. Conflicting scale edits exited 1 with UU sample.unity, so conflicts are not silently accepted. Real project Git configuration/branches/scenes were not changed. |
| F4 — identity | PARTIAL / BLOCKED, low severity | Product name is Worldtree Legacy and its regression test passes. ProjectSettings/ProjectSettings.asset:15 still says companyName: DefaultCompany. The implementer correctly left the unspecified publisher identity for the owner. Choose a stable name, apply it and verify before closing F4. |

No additional blocking defect was identified in the reviewed fixes. Optional robustness improvement: `FoundationSetup.CreateIfMissing` (lines 59–63) should restore the prior scene setup in a finally block and check SaveScene's boolean return. Currently an exception can skip restoration, and a false save result can still allow a missing scene to be added to build settings. This is a source-inspection observation; failure injection was NOT TESTED and it does not invalidate the passing normal-path preservation tests.

### Fresh execution results

- **PASS:** EditMode 9 total / 9 passed / 0 failed / 0 skipped, process exit 0. XML start 2026-10-30 01:12:22Z.
- **PASS:** PlayMode 1 total / 1 passed / 0 failed / 0 skipped, process exit 0. XML start 2026-10-30 01:12:40Z.
- **PASS:** Windows development build, process exit 0; log contains `Build Finished, Result: Success.`
- **PASS:** 32 metadata files, zero missing companions, orphans or duplicate GUID groups; no disposable test assets remained.
- **PASS:** git diff --check, exit 0. Existing LF/CRLF notices are not whitespace failures. The same six tracked files remained modified; four new test/meta files and this report remained untracked. No implementation files were edited by this reviewer.
- **Known warnings:** empty Application and Infrastructure assemblies remain uncompiled. No error CS matches in recheck logs.

Actual Unity invocation pattern (executed sequentially, using Start-Process -WindowStyle Hidden -Wait -PassThru and reading ExitCode):

```powershell
$u = 'C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe'
$project = 'D:/Projects/WorldTreeLegacy/.claude/worktrees/worldtree-unity-foundation-ed1116'
foreach ($mode in @('EditMode','PlayMode')) {
    $p = Start-Process $u -WindowStyle Hidden -Wait -PassThru -ArgumentList '-batchmode','-projectPath',$project,'-runTests','-testPlatform',$mode,'-testResults',('Logs/recheck-'+$mode+'.xml'),'-logFile',('Logs/recheck-'+$mode+'.log')
    $p.ExitCode
}
$p = Start-Process $u -WindowStyle Hidden -Wait -PassThru -ArgumentList '-batchmode','-nographics','-projectPath',$project,'-executeMethod','Worldtree.Editor.FoundationSetup.BuildWindows','-logFile','Logs/recheck-build.log'
$p.ExitCode
```

Evidence: [EditMode XML](../Logs/recheck-EditMode.xml), [PlayMode XML](../Logs/recheck-PlayMode.xml), [EditMode log](../Logs/recheck-EditMode.log), [PlayMode log](../Logs/recheck-PlayMode.log), [build log](../Logs/recheck-build.log). Logs remain ignored.

Merge verification used a Python subprocess harness to run git init, local scratch identity/driver configuration, base/ours/theirs commits, and git merge --no-edit. It copied the active driver from `git config --get merge.unityyamlmerge.driver`; it asserted both intended YAML edits and the traced driver invocation, then asserted a same-field conflict returns 1 and UU. Scratch evidence retained at `C:/Users/leejo/AppData/Local/Temp/worldtree-review-merge-vp97nkmc/merge-trace.txt` and `conflict-trace.txt`. Only the disposable repository has a deliberately unresolved conflict.

### Remaining limits and next step

**BLOCKED:** F4 company name needs an owner decision. Blender import and player movement/follow-camera validation still lack their later-phase implementations.

**NOT TESTED:** player launch, visuals/input/performance, clean-cache restoration, full Apply first-run flow, interactive unsaved scene handling, multi-scene restoration, save-failure injection, other merge asset types/platforms and remote LFS transfer. Current tests exercise EnsureScenes rather than the entire Apply orchestration. No new regression tests were added by this reviewer.

Only `docs/FoundationReview.md` was edited during this re-review; Unity regenerated ignored logs/caches/build output, and the disposable merge repository was created outside the project. No source/config edits, commits, project branch merges or pushes. Finish F4's identity decision; treat launch and clean-checkout verification as separate remaining validation work.

---

## Historical initial verdict

**Initial verdict: FAIL — foundation needed corrections before approval.** Existing EditMode and PlayMode tests and the Windows build passed; those results did not cover the original findings. The original reviewer made no implementation fixes.

## Scope and baseline

The user identified `D:/Projects/WorldTreeLegacy`. Its main checkout contains specifications only. The implemented foundation is in `D:/Projects/WorldTreeLegacy/.claude/worktrees/worldtree-unity-foundation-ed1116`, branch `claude/worldtree-unity-foundation-ed1116`, commit `e401730a3d17adf4fe4937f95a608d9d695eac8f`. This report reviews that worktree, not main or the unrelated Unity RPG prototype. Initial Git status was clean (82 tracked files).

Read before validation: `CLAUDE.md`, `docs/GAME_DESIGN.md`, `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, both Phase0 audit/plan documents, `docs/UnitySetup.md`, `docs/ValidationLog.md`, all five C# sources, seven assembly definitions, package manifest/lock, relevant settings and Git configuration. No local AGENTS.md or graphify graph was present. No graph rebuild was needed because no code changed.

PASS means independently verified within the stated scope; FAIL means an observed mismatch; BLOCKED means a prerequisite prevents validation; NOT TESTED means the check was not run. Earlier validation claims are not counted as fresh test results.

## Severity-ranked findings

### F1 — High / FAIL: setup can overwrite authored scenes

`Assets/Editor/FoundationSetup.cs:29–30,74–102` unconditionally creates empty scenes and saves over Bootstrap and Sandbox. `Apply` also replaces the entire build scene list. It is exposed as a menu command and documented as repeatable setup. Running it after editing Sandbox can discard saved scene content; there is no existence check, backup or confirmation. Opening a new scene can also discard the current editor scene state.

Verified by source inspection; destructive execution was deliberately NOT TESTED. Before reuse, make normal setup preserve existing scenes/build entries and separate regeneration into an explicit operation. Validate on disposable scene copies with a sentinel object and an extra build scene. This is a tooling/data-loss risk, not an observed loss during this review.

**F1 fix (2026-10-30, uncommitted) - PASS for the cases below.** `FoundationSetup.EnsureScenes` now creates Bootstrap and Sandbox only if the file is missing, appends them to the build list only if absent, and never rewrites existing entries (order and enabled state kept). If any open scene has unsaved changes and a scene must be created, it throws `InvalidOperationException` before touching anything; otherwise it creates the scene in a scratch scene and restores the open-scene setup. No regeneration feature was added. Other findings (F2-F4) are not addressed.

Evidence (`Start-Process Unity.exe -Wait`, exit codes from the process; logs in ignored `Logs/f1-*`):
- EditMode `-runTests -testPlatform EditMode`: exit 0, 6 total, 6 passed. New `FoundationSetupTests` (disposable scenes under `Assets/_FoundationSetupTestTemp`, build settings restored in TearDown; no leftover files in `git status`): `ExistingScene_KeepsContents` (sentinel object and file bytes unchanged), `MissingScenes_AreCreated`, `BuildList_PreservesExistingEntriesOrderAndState` (extra disabled entry first, disabled Bootstrap stays disabled, Sandbox appended), `RepeatedRun_CreatesNoDuplicatesOrChanges`, `OpenUnsavedScene_IsNotDiscarded` (throws, unsaved object and dirty flag kept, nothing created).
- First test run failed 5 of 6 with "Cannot create a new scene additively with an untitled scene unsaved." That showed an additive approach cannot work on a fresh editor, so the implementation was changed to the abort-or-scratch-scene design above, then retested.
- Real `FoundationSetup.Apply` on the project: exit 0; `git status` shows no change to scenes or build settings.
- PlayMode: exit 0, 1 passed. `BuildWindows`: exit 0, "Build Finished, Result: Success."

NOT TESTED: the interactive menu command with real unsaved edits in the Editor UI; a first-run Apply on a project with no scenes; scenes loaded from a Single-mode editor with multiple open scenes (restore of multi-scene setups); running the built player. `Curl error 42` in the Apply log is an unrelated network message.

### F2 — Medium / FAIL: saved simulation timestep is approximately 50 Hz, not 60 Hz

`ProjectSettings/TimeManager.asset:7–11` stores count 2822399, numerator 141120000, denominator 1: **0.0199999929138322 seconds**, approximately **50.0000177 Hz**. Required 60 Hz is 0.0166666667 seconds. The saved value remained unchanged after fresh tests/build.

`Bootstrap.Awake` assigns 1/60 at runtime, so the PlayMode test passes after loading Bootstrap. Opening Sandbox directly bypasses that assignment and uses the incorrect project default. The EditMode test only checks the constant, not saved project settings. `docs/ValidationLog.md` incorrectly describes the stored rational value as approximately 1/60.

Correct and persist the project timestep, verify it after reopening Unity, and add a check that reads the saved/default setting before Bootstrap overrides it. Investigate the setup script's SerializedProperty write rather than assuming it persisted successfully.

**F2 fix (2026-10-30, uncommitted) - PASS.** Cause: the setup wrote `floatValue` on `Fixed Timestep`, which is a rational struct (`m_Count` ticks over `m_Rate`), so that write never applied; the saved 0.0199999929 s was the editor default. `SetProjectSettings` now writes `Fixed Timestep.m_Count = 2352000`, `m_Rate.m_Numerator = 141120000`, `m_Denominator = 1` (exactly 1/60 s) and marks the asset dirty. `FoundationSetup.Apply` was run once; `ProjectSettings/TimeManager.asset` diff is only `m_Count: 2822399 -> 2352000`. New EditMode tests in `ProjectSettingsTests`: `SavedFixedTimestep_Is60Hz` (parses the saved file, tolerance 1e-9 s) and `LoadedFixedTimestep_Is60Hz` (`Time.fixedDeltaTime` in a fresh editor process, before any Bootstrap). Persistence: they passed in separate Unity processes started after the Apply process had exited (EditMode run exit 0, 9 of 9). The new tests were not run against the old value (the old file would give 0.02 s and fail by arithmetic).

### F3 — Medium / FAIL: Unity YAML merge attribute has no configured driver

`.gitattributes` assigns `merge=unityyamlmerge` to Unity YAML. However, `git config --get merge.unityyamlmerge.driver` and the broader merge configuration query return no setting (exit 1). The attribute therefore does not establish working semantic merges on this machine. Git LFS is separately configured and its pre-push hook exists.

Document/configure the matching driver and validate a disposable three-way scene merge before claiming smart merge support. Actual merge/conflict behavior was NOT TESTED. No Git configuration was changed during review.

**F3 fix (2026-10-30, local config only) - PASS for the cases below.** Local (`.git/config`) settings added; global config untouched, nothing existing replaced:
`merge.unityyamlmerge.name = Unity SmartMerge (UnityYAMLMerge)`
`merge.unityyamlmerge.driver = "C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -h --force --fallback none %O %B %A %A`
The first attempt (`merge -p %O %B %A %A`) did run but failed: Git passes extensionless temp files and the tool answered "Couldn't locate merge tool to handle extension", leaving a text conflict. `--force` (merge unknown extensions) and `--fallback none` (no external tool or dialog) fix that. Setup steps for future checkouts are in `docs/UnitySetup.md`.
Test, in a disposable repo under the scratchpad using this repo's `.gitattributes` and a copy of `Sandbox.unity` (real scenes untouched, scratch repo deleted afterwards): ours changed Ground `m_LocalPosition` z to 7, theirs changed the adjacent `m_LocalScale` x to 30. Without the driver `git merge` exited 1 with a text conflict. With the driver `git merge` exited 0, both edits are in the result, zero conflict markers. `GIT_TRACE` showed Git invoking `UnityYAMLMerge.exe`. A true conflict (both sides change the same scale) exited 1 with `UU`, not silently resolved. NOT TESTED: prefab merges, merges across multiple objects/files, `git mergetool`, and checkouts on other machines.

### F4 — Low / FAIL: application identity still uses the worktree name

`ProjectSettings/ProjectSettings.asset:15–16`: company `DefaultCompany`, product `worldtree-unity-foundation-ed1116`. The executable filename is WorldtreeLegacy, but project identity is not. Establish stable identity before save paths or distribution depend on it; no save migration is needed for this foundation review.

**F4 partial fix (2026-10-30, uncommitted) - product name PASS, company BLOCKED.** `productName` is now `Worldtree Legacy` (set in `FoundationSetup.SetProjectSettings` and saved; `ProductName_IsWorldtreeLegacy` EditMode test passes). The executable stays `WorldtreeLegacy.exe` from the build script. `companyName` is still `DefaultCompany`: no repository document names a company or publisher (searched `docs/*.md` and `CLAUDE.md`), so it needs a decision from the owner. No save migration was added.

**F4 company name resolved (2026-10-30, uncommitted) - PASS.** The owner chose `Siregi`. `FoundationSetup.SetProjectSettings` sets `PlayerSettings.companyName`; `Apply` run (exit 0); `ProjectSettings/ProjectSettings.asset` now has `companyName: Siregi`, `productName: Worldtree Legacy`. The EditMode test was renamed `Identity_IsSiregiWorldtreeLegacy` and checks both. Rerun: EditMode exit 0, 9 of 9; PlayMode exit 0, 1 of 1; `BuildWindows` exit 0, "Build Finished, Result: Success." The verdict and summary table at the top of this file (written by the reviewer) still show F4 as blocked and were not edited; they predate this change. NOT TESTED: player launch.

**Verification of F2-F4 (fresh processes, exit codes from `Start-Process -Wait`):** EditMode exit 0, 9 of 9 passed; PlayMode exit 0, 1 of 1 passed; `BuildWindows` exit 0, "Build Finished, Result: Success." NOT TESTED: launching the built player, and whether the product-name change alters the player's save/persistent-data path (no saves exist).

## Independently verified PASS

| Check | Evidence and limits |
| --- | --- |
| Editor pin | ProjectVersion.txt and installed editor match 6000.3.27f1, revision 672d562a42a7. Unity identifies 6.3 as LTS: [official release overview](https://unity.com/releases/unity-6); [exact editor release](https://unity.com/releases/editor/whats-new/6000.3.27f1). |
| Current package compatibility | All 13 direct manifest versions match packages-lock.json; fresh editor test sessions and build succeed. URP 17.3.0, Input System 1.20.1, Cinemachine 3.1.5, Test Framework 1.6.0, uGUI 2.0.0. Cached package metadata declares minimum editor versions 6000.3, 6000.0, 2022.3 and 6000.0.44f1 respectively for the first four. This validates the installed combination, not every package feature or an empty-cache restore. |
| Assembly graph, static | Seven asmdefs inspected; no project dependency cycle. Domain has no references and noEngineReferences=true. Application and Infrastructure reference Domain; Presentation references Domain/Application and camera/input packages; Editor is Editor-only; tests are constrained by UNITY_INCLUDE_TESTS. Current code compiles. |
| Asset metadata | 30 non-meta asset/folder entries and 30 matching .meta files; zero missing, orphaned or duplicate GUIDs; all 30 GUIDs valid 32-digit lowercase hex. No untracked asset files. This is pairing/GUID uniqueness, not a complete prefab/scene reference audit. |
| Rendering/input settings | Active Input Handling=1; graphics and all six quality presets reference the same existing URP asset GUID. Bootstrap and Sandbox enabled in build settings. PlayMode test confirms Sandbox and an orthographic main camera. |
| Serialization | Force Text (m_SerializationMode=2) and Visible Meta Files in VersionControlSettings.asset. |
| Git hygiene | Initial and post-validation status clean; no generated Library/Temp/Logs/Builds/UserSettings files tracked. git diff --check exits 0. git fsck --no-reflogs exits 0; dangling blobs/tree reported, no corruption reported. |
| Git LFS configuration | git-lfs/3.8.1 installed; FBX probe has filter/diff/merge=lfs and -text. Shared pre-push hook invokes git lfs pre-push. No LFS objects exist yet; remote round-trip is not covered. |
| EditMode | Fresh review-editmode.xml: 1 total, 1 passed, 0 failed/skipped; process exit 0. FixedStep_Is60Hz only. |
| PlayMode | Fresh review-playmode.xml: 1 total, 1 passed, 0 failed/skipped; process exit 0. Bootstrap_LoadsSandbox_AndSetsFixedStep only. |
| Windows development build | BuildWindows process exit 0; review-build.log says Build Finished, Result: Success. Win64 Mono output exists at Builds/Windows/WorldtreeLegacy.exe (667136-byte executable plus companion files). Incremental build; executable timestamp alone is not evidence of regeneration. |

Application and Infrastructure contain no scripts and Unity warns that those assemblies are not compiled. Treat their boundaries as declared structure, not exercised implementations. No compile errors were found in the fresh logs. Stack-frame names containing EmitExceptionAsError belong to those warnings and are not evidence of failed tests.

## BLOCKED

- Blender sample import/scale validation: no Blender source/sample exists in this foundation. Tool installation alone cannot validate the pipeline.
- Movement, double-jump, plane-constrained player physics and follow-camera jitter acceptance: no player implementation exists. These are later Phase 0 work, not requests to implement them in this review.

## NOT TESTED

- Built-player launch, visual correctness, interactive input, audio, performance and offline runtime behavior.
- Clean checkout/empty Library package restore, offline dependency restore, remote Git/LFS transfer and semantic merge conflict resolution.
- IL2CPP/release build, other platforms, negative compile test inserting UnityEngine into Domain, and test execution inside a standalone player.
- Destructive setup rerun, exhaustive serialized reference integrity and package feature integration beyond the current scene/test/build paths.

Addressables is absent, as explicitly proposed for deferral in the Phase0 plan. Its absence is recorded rather than treated as a package compatibility failure. Infrastructure-to-Domain direction follows that plan's proposed interpretation of the ambiguous architecture arrows. Neither proposal is represented here as a newly approved decision. No multiplayer or final-art work was performed.

## Actual commands and outcomes

Commands ran in the reviewed worktree with editor path `$u` below. Each Unity invocation used `Start-Process -WindowStyle Hidden -PassThru -Wait`; exit codes came from the returned process. No other Unity editor process was found before launch. The setup Apply method was not run.

```powershell
$u = 'C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe'
# Arguments passed to Start-Process, sequentially:
# EditMode: exit 0
-batchmode -nographics -projectPath D:/Projects/WorldTreeLegacy/.claude/worktrees/worldtree-unity-foundation-ed1116 -runTests -testPlatform EditMode -testResults Logs/review-editmode.xml -logFile Logs/review-editmode.log
# PlayMode: exit 0
-batchmode -projectPath D:/Projects/WorldTreeLegacy/.claude/worktrees/worldtree-unity-foundation-ed1116 -runTests -testPlatform PlayMode -testResults Logs/review-playmode.xml -logFile Logs/review-playmode.log
# Build: exit 0
-batchmode -nographics -projectPath D:/Projects/WorldTreeLegacy/.claude/worktrees/worldtree-unity-foundation-ed1116 -executeMethod Worldtree.Editor.FoundationSetup.BuildWindows -logFile Logs/review-build.log
```

Additional executed checks:

```powershell
git status --short --branch
git rev-parse HEAD
git ls-files --stage
git diff --check                         # exit 0
git fsck --no-reflogs                    # exit 0, dangling objects only
git lfs version                         # 3.8.1
git lfs ls-files                        # no objects
git check-attr -a -- probe.fbx probe.unity
git config --get merge.unityyamlmerge.driver # exit 1, absent
git config --show-origin --get-regexp 'merge\..*|core\.hooksPath' # exit 1
```

PowerShell enumerated `Get-ChildItem Assets -Recurse -Force`, checked companion paths with Test-Path, parsed `^guid: ([0-9a-f]{32})$`, and grouped GUIDs with Group-Object. Manifest/lock parsed with ConvertFrom-Json: zero direct-version mismatches. The saved timestep check parsed m_Count/m_Numerator/m_Denominator and used:

```powershell
$seconds = $count * $den / $num
if ([Math]::Abs($seconds - 1.0/60) -gt 0.000001) { 'FAIL saved timestep' }
# FAIL: 0.0199999929138322 seconds; expected 0.0166666666666667
```

One initial review-shell comparison mistakenly used `>` (PowerShell redirection), yielding a false PASS and creating `1E-06`. That result was discarded, the review-created file removed, and the corrected `-gt` check above rerun. Initial restricted Git commands also failed with worktree/access errors; successful checks above were rerun with the required filesystem access. These were review execution issues, not project failures.

Fresh evidence remains under ignored `Logs/`: [EditMode XML](../Logs/review-editmode.xml), [PlayMode XML](../Logs/review-playmode.xml), [EditMode log](../Logs/review-editmode.log), [PlayMode log](../Logs/review-playmode.log), [build log](../Logs/review-build.log). Preserve these separately if sharing the report outside this local checkout.

## Changes, assumptions and next steps

Only added deliverable: `docs/FoundationReview.md`. Existing tracked code, packages, scenes, settings and documentation were unchanged. Unity refreshed ignored caches/logs and the existing ignored Windows build output. No commit, merge, push, setup rerun or dependency upgrade was performed.

Review target assumption: the foundation-named worktree is the intended implementation under the user-supplied repository. Main remains a different, documentation-only checkout; this review does not certify main.

Next: protect setup from overwrites; persist and test the 60 Hz default and correct its validation claim; configure/verify YAML merges; stabilize project identity. Then rerun tests/build and launch the player. Passing two smoke tests is useful foundation evidence, but does not close the broader Phase 0 gate.
