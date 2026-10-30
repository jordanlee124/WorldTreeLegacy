# Core architecture (Task 06): layers, stable IDs, events, profile saves

Result codes: **PASS**, **FAIL**, **BLOCKED**, **NOT TESTED**. Editor `6000.3.27f1`, run from the repo root with `Unity.exe -batchmode`. Branch `claude/worldtree-core-architecture`, based on Task 05 (`d6e779e`).

## What exists

| Piece | Layer | Path |
| --- | --- | --- |
| `StableId`: validated content ID | Domain | `Assets/_Game/Scripts/Domain/Ids/StableId.cs` |
| `CharacterProfile` + `Validate()` | Domain | `Domain/Profiles/CharacterProfile.cs` |
| `IProfileStore` port, `ProfileLoadResult`, `ProfileLoadStatus` | Domain | `Domain/Profiles/IProfileStore.cs` |
| `EventBus`: typed publish/subscribe | Application | `Application/Events/EventBus.cs` |
| `ProfileService` + events `ProfileLoaded`, `ProfileLoadFailed`, `ProfileSaved` | Application | `Application/Profiles/` |
| `JsonProfileStore`: versioned JSON, atomic writes, backup | Infrastructure | `Infrastructure/Saves/JsonProfileStore.cs` |
| Tests (76 new, all EditMode, no scenes) | Tests | `Tests/EditMode/{StableId,CharacterProfile,EventBus,ProfileService,JsonProfileStore,LayerBoundary}Tests.cs` |

Nothing in Presentation was added. No scene, prefab, package or project setting changed.

## Layers

```
Presentation (MonoBehaviours, input, camera)  ──► Application ──► Domain
Infrastructure (files, JSON)  ─────────────────────────────────► Domain
```

| Assembly | References | Engine | Enforced by |
| --- | --- | --- | --- |
| `Worldtree.Domain` | none | no (`noEngineReferences`) | asmdef + `LayerBoundaryTests` |
| `Worldtree.Application` | Domain | **no** (`noEngineReferences`, new in this task) | asmdef + `LayerBoundaryTests` |
| `Worldtree.Infrastructure` | Domain | yes (uses `JsonUtility`, `System.IO`) | `LayerBoundaryTests` |
| `Worldtree.Presentation` | Domain, Application, Cinemachine, Input System | yes | unchanged |

- Ports live in Domain (`IProfileStore`) and Infrastructure implements them, so Infrastructure never needs Application or Presentation. This is decision D6 from the Phase 0 plan, applied here; the project owner has **not confirmed** it.
- Composition (choosing `JsonProfileStore` with `UnityEngine.Application.persistentDataPath`) belongs in Presentation, for example `Bootstrap`. It is **not wired yet**: Presentation would need an Infrastructure reference, and nothing in the game reads a profile yet.
- **Gotcha:** inside any `Worldtree.*` namespace that can see the Application assembly, `Application.` now means the `Worldtree.Application` namespace, not `UnityEngine.Application`. Write `UnityEngine.Application.` there. `GrayboxSceneTests` was changed for this (3 lines); without it the PlayMode tests stop compiling.

## Stable IDs

Format `kind.name[.more]`: lowercase ASCII letters, digits and `_`, each dot-separated part starting with a letter, at most 64 characters (`class.ironwarden`, `region.rootbound_haven`, `boss.thornroot_sentinel.hard`). Comparison is ordinal. `default(StableId)` is empty and fails profile validation. Saves store the ID string, never a display name or asset path.

The `kind.` prefix and the 64-character limit are my proposal; the spec only says "stable IDs for skills, items, monsters, bosses and regions". IDs are checked for **format only**. There is no content catalog yet to check that `class.ironwarden` actually exists.

## Events

`EventBus` is synchronous and single-threaded. Handlers run in subscription order. `Subscribe<T>` returns an `IDisposable` that unsubscribes. A handler may unsubscribe during a publish. A handler's exception propagates to the publisher. Events are `readonly struct`s with plain fields, which keeps them serializable for later server authority (ARCHITECTURE "serializable events"). Only the three profile events exist; combat commands and events are Phase 1.

## Character profile, save schema v1

| Field | Rule |
| --- | --- |
| `name` | 1–16 characters, no leading/trailing whitespace, no control characters |
| `classId`, `regionId` | valid `StableId` |
| `level` | 1–300 (GAME_DESIGN level bands end at 300) |
| `experience`, `lumen` | `long`, ≥ 0 |

On disk (`profile.json`, UTF-8, pretty-printed):

```json
{
    "schemaVersion": 1,
    "name": "Aren",
    "classId": "class.ironwarden",
    "level": 12,
    "experience": 3400,
    "lumen": 125000,
    "regionId": "region.rootbound_haven"
}
```

The field set and the 16-character name limit are my proposal for the prototype (one class, one region, levels 1–30). Gear, inventory, skills and settings are not in the profile; they arrive with their phases, each as a schema bump.

## Save and load behaviour

**Save** checks the profile and refuses an invalid one (`ArgumentException`). It writes `profile.json.tmp`, flushes it to disk, then `File.Replace` swaps it in and keeps the old save as `profile.json.bak`. The first save uses `File.Move`. A crash at any point leaves either the old save or the new one, never a partial file. Save also refuses (`InvalidOperationException`) to overwrite a save whose `schemaVersion` is newer than this build understands.

**Load** never throws for bad data:

| File state | Status | Notes |
| --- | --- | --- |
| Valid v1 | `Loaded` | Unknown extra fields are ignored |
| Missing | `NotFound` | The backup is **not** used: deleting the save means "no save" |
| Unparseable, empty, truncated, `null`, an array, over 64 KB, unreadable | `Corrupt` | Falls back to `.bak` if that loads (`FromBackup = true`) |
| Parses but fails validation, or `schemaVersion` missing | `Invalid` | Same fallback, with the reason in `Error` |
| `schemaVersion` > 1 | `UnsupportedVersion` | **No** fallback, because playing on the old backup would later overwrite the newer save |

Migrations: none yet (v1 is the first schema). The comment in `JsonProfileStore.Read` marks where v1→v2 goes.

## Evidence

| Check | Result | Command / evidence |
| --- | --- | --- |
| Project imports and compiles | PASS | `Unity.exe -batchmode -nographics -quit -projectPath .` exit 0; `error CS` count 0 in all three Task 06 logs |
| EditMode suite | PASS | `-runTests -testPlatform EditMode -testResults Logs/task06-EditMode.xml`: exit 0, **118 total, 118 passed, 0 failed, 0 skipped** (76 new + 42 existing), start 2026-10-30 03:11:42Z |
| PlayMode suite | PASS | `-runTests -testPlatform PlayMode -testResults Logs/task06-PlayMode.xml`: exit 0, **19 total, 19 passed**, including the 7 `GrayboxSceneTests` after the namespace fix |
| **Profile save/load round trip** | PASS | `SaveThenLoad_RoundTripsEveryField` (includes `long` values above 2^31) |
| **Invalid data handled** | PASS | `Load_UnreadableOrIncomplete_*` (7 cases), `Load_BadField_IsInvalid_WithReason` (8), `Load_MissingSchemaVersion_IsInvalid`, `Load_OversizedFile_IsCorrupt`, `Save_InvalidProfile_*`, `CharacterProfileTests` (14), `StableIdTests` (20) |
| Backup and atomic write | PASS | `SecondSave_KeepsThePreviousAsBackup`, `Save_WritesVersionedJson_AndLeavesNoTempFile`, `Load_CorruptMain_FallsBackToBackup` |
| Newer save never overwritten | PASS | `NewerSchema_IsUnsupported_DoesNotUseBackup_AndIsNeverOverwritten` |
| **Core tests independent of scenes** | PASS | All 76 new tests are plain NUnit in EditMode. They load no scene or asset; the store tests use a temp directory under `%TEMP%` and delete it |
| Layer rules | PASS | `LayerBoundaryTests` (3) read the asmdefs; Domain and Application compile with `noEngineReferences` |
| Crash in the middle of a write | NOT TESTED | The design relies on `File.Replace`; no process was killed mid-save |
| Disk full / read-only folder / locked file | NOT TESTED | Would surface as `IOException` from `Save`, or `Corrupt` on load |
| Real `persistentDataPath`, built player | NOT TESTED | Not wired into the game yet |
| Windows build | NOT TESTED | No runtime scene or Presentation code changed; not re-run |

Exact invocation (sequential, `Start-Process -Wait -PassThru`, `ExitCode` and XML counts read afterwards):

```powershell
$u = 'C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe'
foreach ($mode in 'EditMode','PlayMode') {
  Start-Process $u -WindowStyle Hidden -Wait -PassThru -ArgumentList '-batchmode','-projectPath',$root,'-runTests','-testPlatform',$mode,'-testResults',"Logs/task06-$mode.xml",'-logFile',"Logs/task06-$mode.log"
}
```

## Assumptions

- D6 (ports in Domain, Infrastructure → Domain only) is applied without owner confirmation.
- `JsonUtility` instead of Newtonsoft or System.Text.Json: it is built in, so there is no new package. It has two limits: missing fields arrive as defaults, so validation has to catch them (it does), and it cannot express nullable values or dictionaries. Revisit when the save grows nested gear or inventory.
- One save slot (`profile.json`). The spec does not mention slots.
- The profile fields and limits listed above.

## Risks

- **No migration has been exercised.** The first schema bump must add a v1 fixture test before changing the field set.
- `File.Replace` atomicity relies on NTFS/Windows (the target platform). Other platforms are untested.
- The backup is one generation deep. If both files are damaged the player gets `Corrupt`, and nothing is overwritten until the next save.
- Saves are plain-text JSON, so players can edit them. That is acceptable offline. Multiplayer needs server-side authority, not save validation.
- `EventBus.Publish` copies the handler list on every call. That is fine for profile events; profile it before using the bus for per-frame combat events.

## Next steps

1. Wire composition in Presentation: `Bootstrap` builds `EventBus`, `JsonProfileStore(UnityEngine.Application.persistentDataPath)` and `ProfileService`, and loads at startup. That needs a Presentation → Infrastructure asmdef reference and a "new character" flow.
2. A content catalog (ScriptableObject definitions keyed by `StableId`) so loaded IDs are checked against real content.
3. Owner review of D6, the profile fields and the ID format.
