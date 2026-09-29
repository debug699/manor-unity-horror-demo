import bpy
import json
import os
import sys
from mathutils import Vector


def world_bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    lo = Vector(tuple(min(point[index] for point in points) for index in range(3)))
    hi = Vector(tuple(max(point[index] for point in points) for index in range(3)))
    return lo, hi


def component_bounds(obj):
    mesh = obj.data
    adjacency = {vertex.index: set() for vertex in mesh.vertices}
    for edge in mesh.edges:
        a, b = edge.vertices
        adjacency[a].add(b)
        adjacency[b].add(a)
    unseen = set(adjacency)
    components = []
    while unseen:
        seed = unseen.pop()
        stack = [seed]
        indices = [seed]
        while stack:
            current = stack.pop()
            for neighbour in adjacency[current]:
                if neighbour in unseen:
                    unseen.remove(neighbour)
                    stack.append(neighbour)
                    indices.append(neighbour)
        points = [obj.matrix_world @ mesh.vertices[index].co for index in indices]
        lo = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
        hi = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
        components.append({
            "vertices": len(indices),
            "min": [round(value, 6) for value in lo],
            "max": [round(value, 6) for value in hi],
            "center": [round(value, 6) for value in (lo + hi) * 0.5],
            "size": [round(value, 6) for value in hi - lo],
        })
    return sorted(components, key=lambda item: item["vertices"], reverse=True)


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) < 2:
        raise RuntimeError("Usage: blender --background --python audit_outbuilding_geometry.py -- REPORT FBX...")
    report_path, *fbx_paths = args
    records = []
    for fbx_path in fbx_paths:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=fbx_path)
        meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
        record = {"file": fbx_path, "meshes": []}
        for obj in meshes:
            lo, hi = world_bounds(obj)
            record["meshes"].append({
                "name": obj.name,
                "vertices": len(obj.data.vertices),
                "min": [round(value, 6) for value in lo],
                "max": [round(value, 6) for value in hi],
                "size": [round(value, 6) for value in hi - lo],
                "components": component_bounds(obj),
            })
        records.append(record)
    os.makedirs(os.path.dirname(os.path.abspath(report_path)), exist_ok=True)
    with open(report_path, "w", encoding="utf-8") as handle:
        json.dump(records, handle, ensure_ascii=False, indent=2)
    print(f"[Manor][OutbuildingAudit] wrote {report_path}")


if __name__ == "__main__":
    main()
