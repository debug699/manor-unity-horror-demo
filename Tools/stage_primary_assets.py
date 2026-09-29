import bpy
import json
import os
import sys


ASSETS = [
    ("主楼主用", "ai查看修改.fbx", "SM_ManorHouse_Primary_Combined.fbx", 10.5),
    ("加工木屋主用", "已拆开，可能需要重做.fbx", "SM_WorkshopHut_Primary_Combined.fbx", 4.2),
]


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)


def combine_meshes():
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError("No mesh objects imported")
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    combined = bpy.context.object
    combined.name = "CombinedExterior"
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return combined, len(meshes)


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 2:
        raise RuntimeError("Usage: blender --background --python stage_primary_assets.py -- SOURCE_ROOT OUTPUT_ROOT")
    source_root, output_root = map(os.path.abspath, args)
    os.makedirs(output_root, exist_ok=True)
    report = []
    for folder, filename, output_name, target_height in ASSETS:
        reset()
        source = os.path.join(source_root, folder, filename)
        if not os.path.isfile(source):
            report.append({"folder": folder, "source": source, "status": "missing"})
            continue
        bpy.ops.import_scene.fbx(filepath=source, use_anim=False, automatic_bone_orientation=False)
        combined, before = combine_meshes()
        current_height = combined.dimensions.z
        if current_height <= 1e-5:
            raise RuntimeError(f"No usable height in {source}")
        combined.scale *= target_height / current_height
        bpy.context.view_layer.update()
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        minimum_z = min((combined.matrix_world @ corner).z for corner in combined.bound_box)
        combined.location.z -= minimum_z
        bpy.context.view_layer.update()
        output = os.path.join(output_root, output_name)
        bpy.ops.object.select_all(action="DESELECT")
        combined.select_set(True)
        bpy.context.view_layer.objects.active = combined
        bpy.ops.export_scene.fbx(
            filepath=output,
            use_selection=True,
            apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_UNITS",
            bake_space_transform=True,
            add_leaf_bones=False,
            path_mode="COPY",
            embed_textures=False,
        )
        report.append({
            "folder": folder,
            "source": source,
            "output": output,
            "objects_before": before,
            "objects_after": 1,
            "target_height_m": target_height,
            "final_dimensions_m": [round(value, 4) for value in combined.dimensions],
            "status": "staged_exterior_only",
            "note": "Door/window pivots and gameplay colliders must be authored separately; this file is not a final interactive prefab.",
        })
    with open(os.path.join(output_root, "primary_staging_report.json"), "w", encoding="utf-8") as handle:
        json.dump(report, handle, ensure_ascii=False, indent=2)
    print(f"[Manor][Stage] staged={sum(item['status'] == 'staged_exterior_only' for item in report)}")


if __name__ == "__main__":
    main()
