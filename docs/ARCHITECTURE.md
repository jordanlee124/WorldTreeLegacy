# Worldtree Legacy — Technical Architecture

Source: [Technical Architecture](https://app.notion.com/p/40837083c6b6815f8321dd15ceab29e3)
Source last edited (UTC): 2026-10-29T08:42:29.476Z
Exported: 2026-10-30 (Australia/Sydney)

The content below is a faithful Markdown export of the existing Notion page. Source estimates, illustrative values and unresolved decisions remain unchanged.

## Stack

Unity 6 LTS, C#, URP, Input System, Cinemachine, ScriptableObjects, Addressables, Unity Test Framework, Blender, Git and Git LFS.

## Dependency boundaries

Presentation (UI, animation, VFX, camera) → application (combat orchestration, encounters, inventory) → pure C# domain (damage, stats, skills, boss states, gear) → infrastructure adapters (save/load, logging, future networking). Keep the domain independent of Unity scene objects.

## 2.5D conventions

Movement and collision constrained to XY gameplay plane with constant Z. Orthographic camera looking along Z. Use a consistent 3D collider approach rather than mixing Physics and Physics2D. Fixed simulation step initially 60 Hz; render independent of combat calculations.

## Data design

Stable IDs for skills, items, monsters, bosses and regions. Data-driven definitions with validation. Versioned save schema and safe writes. Explicit combat commands and serializable events to make future server authority feasible without introducing networking during Phase 0.

## Art pipeline

Blender source files in Blender/; exported runtime assets under Assets/\_Game/Art. Define units, axes, origins, naming, textures and rigs. First asset is a simple humanoid and a platform. Prototype geometry before production art.

## Testing

Movement states, jump limits, plane constraints, damage formulas, boss phase transitions, gear enhancement probability simulations, save round trips, scene/prefab integrity, Windows build and performance profiling.

## Key folders

Assets/\_Game/\{Art,Audio,Data,Prefabs,Scenes,Scripts,Tests\}; Scripts/\{Core,Combat,Characters,Bosses,Equipment,Progression,Inventory,UI,Infrastructure\}; Blender/\{Characters,Bosses,Environments,Exports\}; Docs/\{GameDesign,TechnicalDesign,ArtDirection\}.
