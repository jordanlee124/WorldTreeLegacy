# Phase 0 — Task 00 Audit

Audit date: 2026-10-30. Machine: Windows 11 Pro 10.0.26300. Auditor: Claude Code (read-only inspection).
Branch: `claude/worldtree-legacy-audit-165e6c` at `70e8165`.

Labels: **Observed** = seen in a command output on this machine. **Source** = stated in [GAME_DESIGN](../GAME_DESIGN.md), [ARCHITECTURE](../ARCHITECTURE.md) or [ROADMAP](../ROADMAP.md). **Proposal** / **Assumption** = mine, not yet agreed.
Result codes: **PASS**, **FAIL**, **BLOCKED** (cannot run yet, reason given), **NOT TESTED** (could run, did not).

Nothing outside `docs/Phase0/` was created or changed. No Unity project was created, no package installed, no tool configured.

## 1. Repository (Observed)

| Item | Finding |
| --- | --- |
| Commits | `c792fed` Initial (adds `.gitignore` only); `70e8165` adds `CLAUDE.md` and the three `docs/*.md` spec exports. |
| Tracked files | `.gitignore`, `CLAUDE.md`, `docs/ARCHITECTURE.md`, `docs/GAME_DESIGN.md`, `docs/ROADMAP.md`. Nothing else. |
| Unity project | **Absent.** No `Assets/`, `Packages/`, `ProjectSettings/`. Matches the CLAUDE.md note. |
| Blender sources | **Absent.** No `Blender/` folder, no `.blend` files. |
| Code / tests | **None.** No C#, no asmdefs, no test assemblies, no CI config. |
| `.gitignore` | Unity basics (`Library`, `Temp`, `Obj`, `Build(s)`, `Logs`, `UserSettings`, `.vs`, `.idea`, `*.csproj`, `*.sln`). Missing `*.blend1`, `[Mm]emoryCaptures/`, `[Rr]ecordings/`, `*.pidb.meta`, crash dumps. `[Bb]uild/` is unanchored, so it would also ignore any nested folder named `Build`. |
| `.gitattributes` | **Absent.** No LFS tracking patterns, no Unity YAML merge settings. `git lfs ls-files` lists nothing. |
| Git LFS hooks | Global `filter.lfs.*` is configured; repo hooks dir has no LFS hooks (`git lfs install` not run for this repo). |
| Remote | `origin` = `https://github.com/jordanlee124/WorldTreeLegacy.git`. `gh repo view` reports **PUBLIC** with no default branch; `git ls-remote --heads` returns nothing — nothing has been pushed. |
| Worktrees | Main checkout `D:/Projects/WorldTreeLegacy` (`main`); this worktree; **and a second Task 1 worktree** `C:/Users/leejo/.worldtree-manager/worktrees/task-1` on `feature/wl-1-00-audit-and-planning` (same commit, untracked `WRITE_TEST.txt`). Not touched. |
| Git config | `core.ignorecase=true`, `core.autocrlf=true`, user `Jee Ho (Jordan) Lee`. |

## 2. Installed tooling (Observed)

| Tool | Evidence | Version / detail | Result |
| --- | --- | --- | --- |
| Git | `git --version` | 2.56.0.windows.4 | PASS |
| Git LFS | `git lfs version` | 3.8.1 | PASS (installed; not set up for this repo) |
| GitHub CLI | registry, `gh repo view` | 2.102.0, authenticated | PASS |
| Unity Hub | `Get-AppxPackage` | MSIX `UnityTechnologies.UnityHub` 3.22.5 | PASS |
| Unity CLI | `unity --version` | 1.0.0-beta.14 (beta channel) | PASS |
| Unity editor A | `unity editors list --installed`, `Unity.exe -version` → `6000.3.27f1` exit 0 | 6000.3.27f1 (Unity 6.3). Modules: Windows (Mono + IL2CPP, x64/x86/arm64), WebGL, iOS | PASS |
| Unity editor B | `unity editors list --installed` | 6000.6.7f1 (Unity 6.6). Modules: Windows **Mono only** (no IL2CPP), WebGL | PASS (installed) |
| Unity licence | `%LOCALAPPDATA%\Unity\Editor\Editor.log` (last written 2026-10-29 14:31) | 6000.3.27f1 launched for another project (`D:\Projects\something-cool\unity`); log shows "Successfully resolved entitlement details". Also a warning: licensing client signature check "Code 10 … failed validation; ignoring", after which it connected. No live check today. | NOT TESTED (indirect evidence only) |
| Unity Editor MCP | `unity-cli-status` | "No Unity Editor instances found with reachable Pipeline servers" | BLOCKED (no editor running, no project) |
| Blender | `blender.exe --version` exit 0 | 5.2.3 LTS, build 2026-10-22 | PASS |
| Blender MCP | `get_addon_status` | "Could not connect to Blender" (Blender not running) | BLOCKED (optional tool) |
| .NET SDK | `dotnet --list-sdks` | 10.0.403 (+ .NET Standard 2.1 targeting pack) | PASS |
| IDE | registry | Visual Studio Community 2026 18.11.2, VS Build Tools 2026 18.10.3, VS Code 1.143.1. Rider not found. VS "Game development with Unity" workload not verified. | NOT TESTED (workload) |
| Hardware | CIM | AMD Radeon RX 6600 XT, 31.9 GB RAM; D: 1.9 TB free | PASS |

Commands used (all read-only): `git status/log/remote/worktree/branch/config`, `git lfs version/env/ls-files`, `gh repo view`, `git ls-remote --heads origin`, `dotnet --list-sdks`, `unity --version`, `unity editors list [--installed|--json]`, `"…/6000.3.27f1/Editor/Unity.exe" -version`, `"…/Blender 5.2/blender.exe" --version`, PowerShell `Get-AppxPackage`, registry uninstall keys, `Get-CimInstance`, `Select-String` on `Editor.log`.

## 3. Dependency list

| Dependency | Source requirement | On machine | Gap |
| --- | --- | --- | --- |
| Unity 6 LTS editor | Source: "Unity 6 LTS" | 6000.3.27f1, 6000.6.7f1 | **Decision:** which editor. See §5.1. |
| Windows Standalone build support | Source: Windows PC, Windows build in Phase 0 gate | Mono + IL2CPP on 6000.3; Mono only on 6000.6 | None for 6000.3. |
| URP | Source | Not installed (no project) | Install at project creation. |
| Input System | Source | — | Install at project creation. |
| Cinemachine | Source | — | Install at project creation. |
| Unity Test Framework | Source | — | Bundled with editor templates; confirm at creation. |
| Addressables | Source (stack list) | — | **Decision:** needed in Phase 0? See §5.6. |
| ScriptableObjects | Source | Built-in | — |
| Blender | Source (version unspecified) | 5.2.3 LTS | Pin 5.2.x as project Blender version (Proposal). |
| Git + Git LFS | Source; Phase 0 gate | Installed | `.gitattributes`, `git lfs install` for repo. |
| C# IDE | Implied | VS 2026 Community, VS Code | Verify Unity workload / VS Code Unity extension. |
| .NET SDK | Not required by source | 10.0.403 | Optional, see §5.8. |

## 4. Phase 0 gate status (Source: ROADMAP)

| Gate item | Status | Reason |
| --- | --- | --- |
| Unity opens and compiles without errors | BLOCKED | No Unity project exists; creating one is out of Task 1 scope. |
| URP, Input System, Cinemachine configured | BLOCKED | No project. |
| Git, Git LFS and assembly definitions in place | FAIL (partial) | Git present; LFS not set up for repo (no `.gitattributes`); no asmdefs. |
| Blender sample asset imports at correct scale | BLOCKED | No sample asset, no project, scale/axis convention undefined. |
| Left/right movement, jump, double jump | BLOCKED | Not implemented (Phase 0 work). |
| Orthographic camera follows without jitter | BLOCKED | Not implemented. |
| Tests pass; Windows build produced | BLOCKED | No tests or project to run. |
| Independent QA PASS or blockers documented | NOT TESTED | Happens after Phase 0 implementation. |

Tests executed in this task: none exist in the repo, so no test suite was run. Only the tool version checks in §2 were executed.

## 5. Conflicts, gaps and missing decisions

1. **Unity version.** Source says "Unity 6 LTS" without a version. Two editors are installed. Assumption (from Unity's published release streams, not verified by a tool here): 6000.3 is the Unity 6.3 LTS stream; the LTS status of 6000.6 is unverified. 6000.6.7f1 also lacks IL2CPP. **Proposal:** pin 6000.3.27f1. User decision.
2. **`Docs/` vs `docs/` casing.** CLAUDE.md asks for `Docs/Phase0/…`; ARCHITECTURE lists `Docs/{GameDesign,TechnicalDesign,ArtDirection}`; the committed folder is lowercase `docs/`. Windows treats them as one folder, but Git and non-Windows checkouts do not. This audit is written to the existing `docs/Phase0/`. **Decision:** keep `docs/` (Proposal) or rename to `Docs/` once.
3. **Layer arrow direction.** ARCHITECTURE writes "presentation → application → domain → infrastructure adapters" but also says the domain must stay independent. Read literally, the domain would depend on infrastructure. **Proposal:** the domain defines interfaces (e.g. save port); infrastructure implements them and references the domain, not the reverse. Needs confirmation.
4. **Physics body type.** Source: "consistent 3D collider approach", fixed 60 Hz. Not decided: dynamic `Rigidbody` with Z/rotation constraints, kinematic `Rigidbody` with custom collision, or `CharacterController`. Affects jitter, one-way platforms and future server authority.
5. **Blender → Unity conventions.** Source says "define units, axes, origins, naming, textures and rigs" — none defined yet. Export format (FBX vs glTF) also undefined.
6. **Addressables in Phase 0.** Listed in the stack but nothing in the Phase 0 gate needs it. **Proposal:** defer to Phase 3/4 when content loading is real.
7. **Platform drop.** GAME_DESIGN lists platform drop under combat movement; the Phase 0 gate lists only left/right, jump, double jump. **Proposal:** Phase 1 unless one-way platforms are cheap with the chosen physics approach.
8. **Domain test runner.** Source: Unity Test Framework. The pure C# domain could additionally be tested with `dotnet test` (SDK present) for fast runs outside Unity. **Proposal only**; would need a `netstandard2.1`-compatible domain and duplicate project files, so not recommended for Phase 0.
9. **Public, empty GitHub repo.** Anything pushed is public, including future art and Blender sources. GitHub LFS has storage/bandwidth quotas. **Decision:** confirm visibility before first push.
10. **Parallel Task 1 worktree.** Another tool has a `feature/wl-1-00-audit-and-planning` worktree for the same task. **Decision:** which branch's deliverables are canonical.
11. **IP originality.** GAME_DESIGN names "Modern MapleStory Heroic" as inspiration. Names, UI and art must stay original; worth a content originality check before committing data or art.
12. **Balance values.** Forging rates and boss limits are "illustrative until simulated". Not a Phase 0 concern; noted so they are not treated as fixed.

## 6. Missing prerequisites (summary)

Required before Phase 0 implementation can start:

- [ ] Decide Unity editor version (Proposal: 6000.3.27f1).
- [ ] Decide `docs/` vs `Docs/` casing.
- [ ] Decide physics body approach for the player.
- [ ] Define Blender units/axes/origin/naming and export format.
- [ ] Confirm repo visibility and canonical Task 1 branch.
- [ ] Add `.gitattributes` (LFS + Unity YAML) and run `git lfs install` in the repo.
- [ ] Confirm Unity licence is active today (open the editor once).
- [ ] Verify IDE Unity integration (VS "Game development with Unity" workload or VS Code Unity extension).

Optional: Unity Editor MCP pipeline package and Blender MCP addon running, for agent-driven checks.
