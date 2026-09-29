"""Non-destructively normalize the separately authored room FBX copies for Unity staging."""
import bpy
import json
import os
import sys
from mathutils import Vector


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)


def mesh_bounds():
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    if not points:
        raise RuntimeError("No mesh imported")
    low = Vector((min(v.x for v in points), min(v.y for v in points), min(v.z for v in points)))
    high = Vector((max(v.x for v in points), max(v.y for v in points), max(v.z for v in points)))
    return low, high


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 2:
        raise RuntimeError("Usage: blender --background --python stage_room_assets_for_unity.py -- INPUT OUTPUT")
    input_dir, output_dir = map(os.path.abspath, args)
    os.makedirs(output_dir, exist_ok=True)
    report = []
    for filename in sorted(os.listdir(input_dir)):
        if not filename.lower().endswith(".fbx"):
            continue
        reset()
        source = os.path.join(input_dir, filename)
        bpy.ops.import_scene.fbx(filepath=source, use_anim=False, automatic_bone_orientation=False)
        low, high = mesh_bounds()
        dimensions = high - low
        # These room exports are authored in sub-metre units.  3m floor-to-ceiling is the target.
        vertical = dimensions.z
        if vertical < 0.001:
            raise RuntimeError("Invalid vertical dimension: " + filename)
        factor = 3.0 / vertical
        for obj in bpy.context.scene.objects:
            if obj.type in {"MESH", "EMPTY", "ARMATURE"}:
                obj.scale *= factor
        bpy.context.view_layer.update()
        low, high = mesh_bounds()
        for obj in bpy.context.scene.objects:
            if obj.type in {"MESH", "EMPTY", "ARMATURE"}:
                obj.location.z -= low.z
                if obj.type == "MESH":
                    obj.select_set(True)
        bpy.context.view_layer.update()
        output = os.path.join(output_dir, "STG_" + filename)
        bpy.ops.export_scene.fbx(filepath=output, use_selection=False, apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_UNITS", bake_space_transform=True,
            add_leaf_bones=False, path_mode="AUTO", embed_textures=False)
        final_low, final_high = mesh_bounds()
        final_size = final_high - final_low
        report.append({"source": source, "output": output, "scale_factor": round(factor, 5),
                       "final_dimensions_m": [round(x, 3) for x in final_size], "status": "staged_copy"})
    with open(os.path.join(output_dir, "room_staging_report.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, ensure_ascii=False, indent=2)
    print("[Manor][StageRooms] staged=" + str(len(report)))


if __name__ == "__main__":
    main()
