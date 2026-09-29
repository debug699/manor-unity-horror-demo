import bpy
import json
import math
import os
import sys
from mathutils import Vector


ASSETS = {
    "ManorHouse": ("庄园主楼", "SM_ManorHouse.fbx", 10.5, False, 1),
    "WorkshopHut": ("加工木屋", "SM_WorkshopHut.fbx", 4.2, False, 1),
    "AbandonedHut": ("废弃木屋", "SM_AbandonedHut.fbx", 4.0, False, 1),
    "StoneWorkshop": ("木石头工坊", "SM_StoneWorkshop.fbx", 4.2, False, 1),
    "GateKit": ("大门围墙", "SM_GateKit.fbx", 4.0, True, 12),
    "CourtyardPropsA": ("庭院杂物1", "SM_CourtyardProps_A.fbx", 2.5, True, 14),
    "CourtyardPropsB": ("庭院杂物2", "SM_CourtyardProps_B.fbx", 2.5, True, 16),
    "WoodDoorKit": ("木门", "SM_WoodDoorKit.fbx", 2.25, True, 4),
    "DeadTree": ("树1", "SM_DeadTree.fbx", 9.0, False, 1),
}


def source_fbx(source_root, folder):
    matches = [name for name in os.listdir(os.path.join(source_root, folder)) if name.lower().endswith(".fbx")]
    if len(matches) != 1:
        raise RuntimeError(f"Expected one FBX in {folder}, found {len(matches)}")
    return os.path.join(source_root, folder, matches[0])


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)


def mesh_bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return minimum, maximum


def normalize_scene(target_height):
    objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    minimum, maximum = mesh_bounds(objects)
    height = maximum.z - minimum.z
    if height <= 1e-5:
        raise RuntimeError("Imported asset has no usable height")
    scale = target_height / height
    for obj in objects:
        obj.scale *= scale
    bpy.context.view_layer.update()
    minimum, maximum = mesh_bounds(objects)
    center = Vector(((minimum.x + maximum.x) * 0.5, (minimum.y + maximum.y) * 0.5, minimum.z))
    for obj in objects:
        obj.location -= center
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


def split_loose_parts():
    original = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    for obj in original:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.mesh.separate(type="LOOSE")
        obj.select_set(False)
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def center_xy(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return Vector((sum(p.x for p in points) / 8.0, sum(p.y for p in points) / 8.0))


def cluster_parts(parts, cluster_count):
    cluster_count = min(cluster_count, len(parts))
    ordered = sorted(parts, key=lambda obj: (center_xy(obj).x, center_xy(obj).y))
    seeds = [center_xy(ordered[round(i * (len(ordered) - 1) / max(1, cluster_count - 1))]) for i in range(cluster_count)]
    assignments = [0] * len(parts)
    for _ in range(32):
        for index, obj in enumerate(parts):
            point = center_xy(obj)
            assignments[index] = min(range(cluster_count), key=lambda c: (point - seeds[c]).length_squared)
        changed = False
        for cluster in range(cluster_count):
            members = [center_xy(parts[i]) for i, value in enumerate(assignments) if value == cluster]
            if not members:
                continue
            next_seed = sum(members, Vector((0.0, 0.0))) / len(members)
            changed |= (next_seed - seeds[cluster]).length > 1e-5
            seeds[cluster] = next_seed
        if not changed:
            break
    return assignments


def join_clusters(parts, assignments, prefix):
    results = []
    for cluster in sorted(set(assignments)):
        members = [parts[i] for i, value in enumerate(assignments) if value == cluster]
        bpy.ops.object.select_all(action="DESELECT")
        for obj in members:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = max(members, key=lambda obj: len(obj.data.vertices))
        bpy.ops.object.join()
        joined = bpy.context.object
        joined.name = f"{prefix}_{len(results) + 1:02d}"
        bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
        minimum, _ = mesh_bounds([joined])
        joined.location.z -= minimum.z
        results.append(joined)
    return results


def export_asset(source_root, output_root, key, config):
    folder, filename, target_height, should_cluster, cluster_count = config
    reset_scene()
    bpy.ops.import_scene.fbx(filepath=source_fbx(source_root, folder), use_anim=False)
    normalize_scene(target_height)
    objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if should_cluster:
        parts = split_loose_parts()
        objects = join_clusters(parts, cluster_parts(parts, cluster_count), key)
    else:
        for index, obj in enumerate(objects):
            obj.name = key if len(objects) == 1 else f"{key}_{index + 1:02d}"
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    os.makedirs(output_root, exist_ok=True)
    output_path = os.path.join(output_root, filename)
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True,
        add_leaf_bones=False,
        path_mode="COPY",
        embed_textures=False,
    )
    minimum, maximum = mesh_bounds(objects)
    return {
        "key": key,
        "source": source_fbx(source_root, folder),
        "output": output_path,
        "objectCount": len(objects),
        "boundsMeters": [round(maximum.x - minimum.x, 3), round(maximum.y - minimum.y, 3), round(maximum.z - minimum.z, 3)],
        "objects": [obj.name for obj in objects],
    }


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 3:
        raise RuntimeError("Usage: blender --background --python prepare_manor_assets.py -- SOURCE OUTPUT REPORT")
    source_root, output_root, report_path = map(os.path.abspath, args)
    report = [export_asset(source_root, output_root, key, config) for key, config in ASSETS.items()]
    with open(report_path, "w", encoding="utf-8") as handle:
        json.dump(report, handle, ensure_ascii=False, indent=2)
    print(f"[Manor][Blender] Prepared {len(report)} assets: {output_root}")


if __name__ == "__main__":
    main()
