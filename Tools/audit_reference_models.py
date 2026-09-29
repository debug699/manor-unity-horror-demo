import bpy
import json
import os
import re
import sys
from mathutils import Vector


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)


def bounds(objects):
    meshes = [obj for obj in objects if obj.type == "MESH"]
    if not meshes:
        return None
    points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return {
        "min": [round(v, 4) for v in lo],
        "max": [round(v, 4) for v in hi],
        "size": [round(hi.x - lo.x, 4), round(hi.y - lo.y, 4), round(hi.z - lo.z, 4)],
    }


def object_record(obj):
    data = {"name": obj.name, "type": obj.type}
    if obj.type == "MESH":
        data.update({
            "vertices": len(obj.data.vertices),
            "polygons": len(obj.data.polygons),
            "materials": len(obj.data.materials),
            "dimensions": [round(v, 4) for v in obj.dimensions],
        })
    if obj.type == "ARMATURE":
        data["bones"] = len(obj.data.bones)
    if obj.animation_data and obj.animation_data.action:
        data["action"] = obj.animation_data.action.name
    return data


def classify(folder, filename):
    text = f"{folder} {filename}"
    flags = []
    if "主用" in folder:
        flags.append("primary")
    if "备用" in folder:
        flags.append("backup")
    if "初版" in folder or "初代" in folder:
        flags.append("prototype")
    if "重新生成" in folder:
        flags.append("regenerated")
    if "不用拆" in filename or "不需要改" in filename:
        flags.append("keep_whole")
    if "已拆" in filename:
        flags.append("split_claimed")
    if "ai查看" in filename or "ai查看" in folder or "ai查看编辑" in filename:
        flags.append("ai_review")
    return flags


def audit_fbx(path, folder):
    clear_scene()
    try:
        bpy.ops.import_scene.fbx(filepath=path, use_anim=True, automatic_bone_orientation=False)
        objects = list(bpy.context.scene.objects)
        meshes = [obj for obj in objects if obj.type == "MESH"]
        armatures = [obj for obj in objects if obj.type == "ARMATURE"]
        actions = sorted({obj.animation_data.action.name for obj in objects if obj.animation_data and obj.animation_data.action})
        loose = []
        for obj in meshes:
            connected = 0
            try:
                import bmesh
                bm = bmesh.new()
                bm.from_mesh(obj.data)
                connected = len(bmesh.ops.split_edges(bm, edges=bm.edges)) if False else 0
                # Connected components are estimated from mesh islands without modifying the source mesh.
                adjacency = {v.index: set() for v in bm.verts}
                for edge in bm.edges:
                    a, b = edge.verts
                    adjacency[a.index].add(b.index)
                    adjacency[b.index].add(a.index)
                unseen = set(adjacency)
                while unseen:
                    stack = [unseen.pop()]
                    connected += 1
                    while stack:
                        current = stack.pop()
                        for nxt in adjacency[current]:
                            if nxt in unseen:
                                unseen.remove(nxt)
                                stack.append(nxt)
                bm.free()
            except Exception:
                connected = None
            loose.append({"name": obj.name, "connected_components": connected})
        return {
            "folder": folder,
            "file": os.path.basename(path),
            "path": path,
            "flags": classify(folder, os.path.basename(path)),
            "objects": len(objects),
            "mesh_objects": len(meshes),
            "armatures": len(armatures),
            "bones": sum(len(a.data.bones) for a in armatures),
            "actions": actions,
            "bounds": bounds(objects),
            "object_details": [object_record(obj) for obj in objects],
            "mesh_components": loose,
            "status": "ok",
        }
    except Exception as exc:
        return {
            "folder": folder,
            "file": os.path.basename(path),
            "path": path,
            "flags": classify(folder, os.path.basename(path)),
            "status": "error",
            "error": repr(exc),
        }


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 2:
        raise RuntimeError("Usage: blender --background --python audit_reference_models.py -- SOURCE_ROOT REPORT")
    source_root, report_path = map(os.path.abspath, args)
    records = []
    for root, _, files in os.walk(source_root):
        for filename in sorted(files):
            if filename.lower().endswith(".fbx"):
                folder = os.path.relpath(root, source_root).replace("\\", "/")
                records.append(audit_fbx(os.path.join(root, filename), folder))
    missing_expected = [name for name in ["G01", "G02", "G03", "G04", "G05", "G06", "G07", "G08", "G09", "G10", "G11", "G12"] if not os.path.isdir(os.path.join(source_root, name))]
    report = {
        "source_root": source_root,
        "fbx_count": len(records),
        "fbm_files": [],
        "missing_expected_room_folders": missing_expected,
        "records": records,
    }
    for root, _, files in os.walk(source_root):
        for filename in files:
            if filename.lower().endswith(".fbm"):
                report["fbm_files"].append(os.path.join(root, filename))
    os.makedirs(os.path.dirname(report_path), exist_ok=True)
    with open(report_path, "w", encoding="utf-8") as handle:
        json.dump(report, handle, ensure_ascii=False, indent=2)
    print(f"[Manor][Audit] FBX={len(records)} FBM={len(report['fbm_files'])} missing_rooms={missing_expected}")


if __name__ == "__main__":
    main()
