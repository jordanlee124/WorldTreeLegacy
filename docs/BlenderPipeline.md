# Blender pipeline (Task 05)

Result codes: **PASS**, **FAIL**, **BLOCKED**, **NOT TESTED**. Blender 5.2.3 LTS, Unity 6000.3.27f1 (URP). The two proxy models are blockouts, not final art.

## Conventions

| Topic | Convention |
| --- | --- |
| Units | Metric, scale 1.0. **1 Blender unit = 1 m = 1 Unity unit.** Exported with `FBX_SCALE_UNITS`, Unity Scale Factor 1. |
| Gameplay plane | Blender **XZ** (X along the level, Z up, Y is depth). Unity **XY**, Z is depth. |
| Axis mapping (measured) | Blender (x, y, z) arrives in Unity as **(-x, z, -y)**: up stays up, Blender's front (-Y) becomes Unity +Z, and **Blender X is negated**. |
| Facing | A character that faces Unity **+X** (right) must be modelled facing **-X in Blender**. Its left side (`.L`) is then on **-Y** in Blender and on the far side (+Z) in Unity. |
| Pivot | Characters: origin at the soles of the feet, centred in X and Y. Platforms: origin at the **centre of the top face**, so a platform placed at a point has its walkable surface at that point. |
| Transforms | Scale applied (1,1,1), rotation applied (0,0,0), object origin at the world origin, before export. |
| Naming | `SK_` skinned mesh, `SM_` static mesh, `M_` material (`M_Graybox_*` for placeholders). Bones lower_snake_case with `.L` / `.R` suffix, one root bone named `root`. Armature object `Rig`. |
| Rig | Generic, 15 bones: `root`, `pelvis`, `spine`, `neck`, `head`, `upper_leg/lower_leg/foot .L/.R`, `upper_arm/lower_arm .L/.R`. Blockout uses rigid (weight 1.0) vertex groups. |
| Normals | Authored in Blender (flat for blockouts, outward, consistent winding), imported with "Import" (no recalculation). |
| Materials | One Principled BSDF per material, base colour only (no textures yet). Unity orders material slots by **first use** in the mesh, so the first face must use slot 0; code should still look materials up by name. |
| Export | Selected objects only, mesh + armature, no cameras/lights/animation, `axis_forward=-Z`, `axis_up=Y`, `bake_space_transform=True`, flat smoothing, no leaf bones. |
| Import (enforced) | `Assets/Editor/ModelImportConventions.cs` for everything under `Assets/_Game/Art/Models/`: scale 1, normals Import, no cameras/lights, `SK_` Generic rig, `SM_` no animation. |

Known quirk: the exporter's axis bake does not reach armature objects, so the `Rig` node arrives rotated -90 degrees about X in Unity. The bind pose compensates; the posed character is upright (tested). Do not "fix" it by rotating the armature in Blender.

## Files

| Path | What |
| --- | --- |
| `Blender/build_proxies.py` | Builds both proxies from code, runs the convention checks, saves the `.blend` files, exports FBX, copies to Unity. Exits non-zero if a check fails. |
| `Blender/Characters/humanoid_proxy.blend` | Humanoid source: 1.8 m blockout of boxes, armature, two materials. |
| `Blender/Environments/platform_proxy.blend` | Platform source: 4 x 2 x 0.5 m box, top face on its own material slot. |
| `Blender/Exports/SK_HumanoidProxy.fbx`, `SM_PlatformProxy.fbx` | Exports (also copied to `Assets/_Game/Art/Models/`, where Unity imports them). |
| `Assets/Editor/ModelImportConventions.cs` | Import settings postprocessor. |
| `Assets/_Game/Tests/EditMode/BlenderImportTests.cs` | 11 Unity tests of the imported result. |

Blend and FBX files are Git LFS (`.gitattributes`).

## Re-export

```bash
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python Blender/build_proxies.py
```

Then open the project in Unity (or run the tests) to re-import. Edit the `.blend` by hand if you like, but the script is the reference: re-running it overwrites the `.blend` files.

## Evidence

| Check | Result | Command / evidence |
| --- | --- | --- |
| Blender runs headless, builds both models | PASS | the command above: exit 0, "all convention checks passed" |
| Blender-side checks: height 1.8 m, feet at z = 0, platform 4 x 2 x 0.5 with origin at top centre, transforms applied, bone set, every vertex weighted, materials named and slotted, outward platform normals, facing and left/right side | PASS | assertions in `build_proxies.py` |
| Unity scale | PASS | `Platform_SizeOriginAndAxes` (4 x 0.5 x 2 m), `Humanoid_HeightFeetAndFacing`, posed instance height 1.8 m +/- 0.02 |
| Unity transforms and axes | PASS | root and mesh nodes identity, `Rig` -90 X as documented, posed instance upright, toes +X, left side +Z |
| Unity normals | PASS | unit length, imported (not calculated), equal to the triangle winding normal for every triangle; platform normals outward; head top faces up |
| Unity materials | PASS | names and slot order, URP shader (not pink), base colours within 0.02 of Blender, platform top on submesh 1 |
| Unity import settings | PASS | `ImportSettings_FollowConvention` |
| EditMode suite | PASS | `Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode`: 41 total, 41 passed, 0 failed |
| PlayMode suite and Windows build after this change | NOT TESTED | nothing in the runtime code changed; the new postprocessor only touches `Art/Models` |
| Models placed in a scene or used by the player | NOT TESTED | out of scope; the player is still a capsule |
| Blender MCP add-on route | NOT TESTED | used headless `blender -b` instead |
| Visual inspection (viewport render) | NOT TESTED | verified by numbers, nobody has looked at the models |
| Animation, textures, UVs, collision meshes, LODs | NOT TESTED / not built | out of scope for this task |

## Assumptions and risks

- **Facing -X in Blender is counter-intuitive** (Blender's own "front" is -Y). It follows from the measured mapping and is the price of having Unity +X as "right". Anyone modelling by hand must remember it; the build script's check fails if a character faces the wrong way.
- Rigid weights and box limbs will deform badly when animated; they exist to prove the rig, naming and import path.
- The humanoid is 1.8 m tall; the player's capsule collider is 2 m (Task 03 note). Reconcile when the model replaces the capsule.
- No UVs or textures: the first textured asset may need new conventions (colour space, texture naming, packing).
- Exported files are small (48 KB and 16 KB) but still LFS; `.blend` files are binary and merge poorly, so one person edits a given `.blend` at a time.
- Blender emits a deprecation warning for `Material.use_nodes` (removal planned for Blender 6.0); harmless now.
- Axis mapping was measured on Blender 5.2.3 and Unity 6000.3.27f1. A Blender or Unity upgrade could change it; the Unity tests will catch that.

## Next steps

1. Look at the models in Unity and Blender, then decide whether proportions are acceptable as graybox stand-ins.
2. Swap the capsule for `SK_HumanoidProxy` once an Animator is needed (Phase 1), reconciling the collider height.
3. Add texture conventions with the first textured asset.
