"""Builds the Worldtree Legacy proxy models, validates them against docs/BlenderPipeline.md, saves the .blend
sources and exports FBX for Unity. Headless:

    blender -b --python Blender/build_proxies.py

Outputs (all relative to the repository root):
    Blender/Characters/humanoid_proxy.blend      Blender/Exports/SK_HumanoidProxy.fbx
    Blender/Environments/platform_proxy.blend    Blender/Exports/SM_PlatformProxy.fbx
The FBX files are also copied to Assets/_Game/Art/Models/. Exits non-zero if a convention check fails.
"""
import shutil
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
EXPORTS = ROOT / "Blender" / "Exports"
UNITY_MODELS = ROOT / "Assets" / "_Game" / "Art" / "Models"

# --- Conventions (keep in sync with docs/BlenderPipeline.md) -------------------------------------------------
HUMANOID_HEIGHT = 1.8  # metres; origin at the soles of the feet
TOLERANCE = 1e-4

MATERIALS = {  # name -> base colour (placeholder graybox colours, not final art)
    "M_Graybox_Body": (0.55, 0.58, 0.62, 1.0),
    "M_Graybox_Accent": (0.90, 0.45, 0.10, 1.0),
    "M_Graybox_Platform": (0.35, 0.40, 0.35, 1.0),
    "M_Graybox_PlatformTop": (0.55, 0.65, 0.45, 1.0),
}


class ConventionError(Exception):
    pass


def check(condition, message):
    if not condition:
        raise ConventionError(message)


# --- Scene helpers ---------------------------------------------------------------------------------------------
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    units = bpy.context.scene.unit_settings
    units.system = "METRIC"
    units.scale_length = 1.0  # 1 Blender unit = 1 m = 1 Unity unit
    units.length_unit = "METERS"


def make_materials(names):
    out = []
    for name in names:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")  # by type: node names are localized
        bsdf.inputs["Base Color"].default_value = MATERIALS[name]
        mat.diffuse_color = MATERIALS[name]
        out.append(mat)
    return out


def add_box(bm, size, center, material_index):
    """Axis-aligned box. Returns its vertices; faces get material_index."""
    verts = bmesh.ops.create_cube(bm, size=1.0)["verts"]
    bmesh.ops.scale(bm, vec=Vector(size), verts=verts)
    bmesh.ops.translate(bm, vec=Vector(center), verts=verts)
    for f in {f for v in verts for f in v.link_faces}:
        f.material_index = material_index
    return verts


def new_object(name, bm, materials, collection):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for mat in materials:
        mesh.materials.append(mat)
    mesh.polygons.foreach_set("use_smooth", [False] * len(mesh.polygons))  # flat shading: hard-edged blockout
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    return obj


# --- Humanoid ----------------------------------------------------------------------------------------------------
# Character faces -X in Blender, which the Unity importer maps to +X (gameplay "right"): Blender (x, y, z) -> Unity (-x, z, -y).
# Gameplay plane is XZ in Blender, Y is depth. Facing -X with Z up, the left side (.L) is -Y (-> Unity +Z, the far side from the camera).
# part -> (bone, size, centre, material slot). Slot 0 = body grey, 1 = accent orange.
def humanoid_parts():
    # Pelvis first: Unity orders material slots by first use, so the first face must use slot 0 to keep Blender's order.
    parts = {"pelvis": ("pelvis", (0.26, 0.34, 0.16), (0, 0, 1.00), 0)}
    for side, y in (("L", -0.10), ("R", 0.10)):
        parts[f"foot.{side}"] = (f"foot.{side}", (0.28, 0.10, 0.08), (-0.04, y, 0.04), 1)
        parts[f"shin.{side}"] = (f"lower_leg.{side}", (0.10, 0.10, 0.42), (0, y, 0.29), 0)
        parts[f"thigh.{side}"] = (f"upper_leg.{side}", (0.12, 0.12, 0.42), (0, y, 0.71), 0)
    parts["torso"] = ("spine", (0.24, 0.38, 0.40), (0, 0, 1.28), 0)
    parts["neck"] = ("neck", (0.08, 0.08, 0.08), (0, 0, 1.52), 0)
    parts["head"] = ("head", (0.22, 0.22, 0.24), (0, 0, 1.68), 1)
    for side, y in (("L", -0.25), ("R", 0.25)):
        parts[f"upper_arm.{side}"] = (f"upper_arm.{side}", (0.09, 0.09, 0.30), (0, y, 1.33), 0)
        parts[f"lower_arm.{side}"] = (f"lower_arm.{side}", (0.08, 0.08, 0.30), (0, y, 1.03), 0)
    return parts


# bone -> (parent, head, tail)
def humanoid_bones():
    bones = {
        "root": (None, (0, 0, 0), (0, 0, 0.10)),
        "pelvis": ("root", (0, 0, 0.92), (0, 0, 1.08)),
        "spine": ("pelvis", (0, 0, 1.08), (0, 0, 1.48)),
        "neck": ("spine", (0, 0, 1.48), (0, 0, 1.56)),
        "head": ("neck", (0, 0, 1.56), (0, 0, 1.80)),
    }
    for side, y in (("L", -0.10), ("R", 0.10)):
        bones[f"upper_leg.{side}"] = ("pelvis", (0, y, 0.92), (0, y, 0.50))
        bones[f"lower_leg.{side}"] = (f"upper_leg.{side}", (0, y, 0.50), (0, y, 0.08))
        bones[f"foot.{side}"] = (f"lower_leg.{side}", (0, y, 0.08), (-0.18, y, 0.04))
    for side, y in (("L", -0.25), ("R", 0.25)):
        bones[f"upper_arm.{side}"] = ("spine", (0, y, 1.48), (0, y, 1.18))
        bones[f"lower_arm.{side}"] = (f"upper_arm.{side}", (0, y, 1.18), (0, y, 0.88))
    return bones


def build_humanoid():
    reset_scene()
    collection = bpy.context.scene.collection
    body, accent = make_materials(["M_Graybox_Body", "M_Graybox_Accent"])

    bm = bmesh.new()
    owners = []  # (vertex, bone)
    for bone, size, center, slot in humanoid_parts().values():
        owners += [(v, bone) for v in add_box(bm, size, center, slot)]
    bm.verts.index_update()
    index_of_bone = [(v.index, bone) for v, bone in owners]
    mesh_obj = new_object("SK_HumanoidProxy", bm, [body, accent], collection)

    arm_data = bpy.data.armatures.new("Rig")
    rig = bpy.data.objects.new("Rig", arm_data)
    collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    for name, (parent, head, tail) in humanoid_bones().items():
        eb = arm_data.edit_bones.new(name)
        eb.head, eb.tail = head, tail
        if parent:
            eb.parent = arm_data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")

    for bone in humanoid_bones():
        mesh_obj.vertex_groups.new(name=bone)
    for index, bone in index_of_bone:
        mesh_obj.vertex_groups[bone].add([index], 1.0, "REPLACE")  # rigid weights: blockout, not final skinning
    mesh_obj.parent = rig  # both objects sit at the origin, so the parent inverse stays identity
    mod = mesh_obj.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    return mesh_obj, rig


# --- Platform ------------------------------------------------------------------------------------------------------
PLATFORM_SIZE = (4.0, 2.0, 0.5)  # x width, y depth, z thickness; origin at the centre of the TOP face


def build_platform():
    reset_scene()
    collection = bpy.context.scene.collection
    sides, top = make_materials(["M_Graybox_Platform", "M_Graybox_PlatformTop"])
    bm = bmesh.new()
    verts = add_box(bm, PLATFORM_SIZE, (0, 0, -PLATFORM_SIZE[2] / 2), 0)
    for f in {f for v in verts for f in v.link_faces}:
        f.normal_update()
        if f.normal.z > 0.5:
            f.material_index = 1  # top face gets its own slot, to verify slot order in Unity
    return new_object("SM_PlatformProxy", bm, [sides, top], collection)


# --- Validation (Blender side; Unity side is Tests/EditMode/BlenderImportTests.cs) ------------------------------------
def world_bounds(obj):
    pts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    return (Vector((min(p[i] for p in pts) for i in range(3))), Vector((max(p[i] for p in pts) for i in range(3))))


def validate_common(obj, expected_materials):
    check(obj.scale == Vector((1, 1, 1)), f"{obj.name}: scale must be applied (1,1,1), is {tuple(obj.scale)}")
    check(all(abs(a) < TOLERANCE for a in obj.rotation_euler), f"{obj.name}: rotation must be applied")
    check(obj.location.length < TOLERANCE, f"{obj.name}: object origin must sit at the world origin")
    names = [m.name for m in obj.data.materials]
    check(names == expected_materials, f"{obj.name}: materials {names} != {expected_materials}")
    check(all(m.name.startswith("M_") for m in obj.data.materials), "materials must be prefixed M_")
    for poly in obj.data.polygons:
        check(poly.material_index < len(names), f"{obj.name}: face with missing material slot")
    centre = sum((v.co for v in obj.data.vertices), Vector()) / len(obj.data.vertices)
    flat = [p for p in obj.data.polygons if abs(p.normal.length - 1.0) < 1e-4]
    check(len(flat) == len(obj.data.polygons), f"{obj.name}: degenerate face normals")
    return centre


def validate_humanoid(mesh_obj, rig):
    lo, hi = world_bounds(mesh_obj)
    check(abs(lo.z) < TOLERANCE, f"humanoid feet must be at z = 0, min z = {lo.z}")
    check(abs(hi.z - HUMANOID_HEIGHT) < TOLERANCE, f"humanoid height {hi.z} != {HUMANOID_HEIGHT}")
    centre = validate_common(mesh_obj, ["M_Graybox_Body", "M_Graybox_Accent"])
    # Facing -X: the foot (toe) vertices reach further towards -X than back towards +X.
    feet = [v.co for v in mesh_obj.data.vertices if v.co.z < 0.09]
    check(-min(v.x for v in feet) > max(v.x for v in feet), "humanoid must face -X in Blender (toes forward)")
    check(rig.data.bones["foot.L"].head_local.y < 0 < rig.data.bones["foot.R"].head_local.y, "left side (.L) must be on -Y")
    check(mesh_obj.parent == rig and rig.type == "ARMATURE", "humanoid mesh must be parented to the armature")
    bones = {b.name for b in rig.data.bones}
    check(bones == set(humanoid_bones()), f"bone set mismatch: {sorted(bones ^ set(humanoid_bones()))}")
    check(rig.data.bones["root"].parent is None, "single root bone named 'root'")
    check(mesh_obj.data.polygons[0].material_index == 0, "first face must use material slot 0 (Unity orders slots by first use)")
    unweighted = [v.index for v in mesh_obj.data.vertices if not v.groups]
    check(not unweighted, f"{len(unweighted)} vertices without a bone weight")
    return centre


def validate_platform(obj):
    lo, hi = world_bounds(obj)
    check((hi - lo - Vector(PLATFORM_SIZE)).length < TOLERANCE, f"platform size {tuple(hi - lo)} != {PLATFORM_SIZE}")
    check(abs(hi.z) < TOLERANCE and abs(lo.x + hi.x) < TOLERANCE and abs(lo.y + hi.y) < TOLERANCE,
          "platform origin must be the centre of the top face")
    centre = validate_common(obj, ["M_Graybox_Platform", "M_Graybox_PlatformTop"])
    for poly in obj.data.polygons:  # convex box: every normal points away from the centre
        check(poly.normal.dot(poly.center - centre) > 0, "platform face normal points inward")
    check(obj.data.polygons[0].material_index == 0, "first face must use material slot 0 (Unity orders slots by first use)")
    tops = [p for p in obj.data.polygons if p.material_index == 1]
    check(len(tops) == 1 and tops[0].normal.z > 0.99, "exactly one top face on material slot 1")


# --- Export ---------------------------------------------------------------------------------------------------------
def export_fbx(objects, path):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"MESH", "ARMATURE"},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        use_mesh_modifiers=False,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        bake_anim=False,
        path_mode="STRIP",
    )


def save_and_export(blend_path, fbx_name, objects):
    blend_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    fbx = EXPORTS / fbx_name
    export_fbx(objects, fbx)
    UNITY_MODELS.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(fbx, UNITY_MODELS / fbx_name)
    print(f"[pipeline] wrote {blend_path.relative_to(ROOT)} and {fbx_name} ({fbx.stat().st_size} bytes)")


def main():
    mesh_obj, rig = build_humanoid()
    validate_humanoid(mesh_obj, rig)
    save_and_export(ROOT / "Blender" / "Characters" / "humanoid_proxy.blend", "SK_HumanoidProxy.fbx", [rig, mesh_obj])

    platform = build_platform()
    validate_platform(platform)
    save_and_export(ROOT / "Blender" / "Environments" / "platform_proxy.blend", "SM_PlatformProxy.fbx", [platform])
    print("[pipeline] all convention checks passed")


if __name__ == "__main__":
    try:
        main()
    except ConventionError as e:
        print(f"[pipeline] CONVENTION CHECK FAILED: {e}", file=sys.stderr)
        sys.exit(1)
