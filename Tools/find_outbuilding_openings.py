import bpy
import json
import os
import sys
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    lo = Vector(tuple(min(point[index] for point in points) for index in range(3)))
    hi = Vector(tuple(max(point[index] for point in points) for index in range(3)))
    return lo, hi


def components(grid, width, height):
    unseen = {(x, z) for z in range(height) for x in range(width) if grid[z][x]}
    found = []
    while unseen:
        seed = unseen.pop()
        stack = [seed]
        cells = [seed]
        while stack:
            x, z = stack.pop()
            for neighbour in ((x - 1, z), (x + 1, z), (x, z - 1), (x, z + 1)):
                if neighbour in unseen:
                    unseen.remove(neighbour)
                    stack.append(neighbour)
                    cells.append(neighbour)
        found.append(cells)
    return found


def scan_face(obj, bvh, lo, hi, face, resolution=160):
    if face in ("neg_y", "pos_y"):
        horizontal_axis, depth_axis = 0, 1
    else:
        horizontal_axis, depth_axis = 1, 0
    horizontal_min, horizontal_max = lo[horizontal_axis], hi[horizontal_axis]
    vertical_min, vertical_max = lo.z, hi.z
    depth_min, depth_max = lo[depth_axis], hi[depth_axis]
    depth_size = depth_max - depth_min
    near_limit = max(0.015, depth_size * 0.13)
    grid = []
    for z_index in range(resolution):
        z = vertical_min + (z_index + 0.5) / resolution * (vertical_max - vertical_min)
        row = []
        for x_index in range(resolution):
            horizontal = horizontal_min + (x_index + 0.5) / resolution * (horizontal_max - horizontal_min)
            origin = Vector((0.0, 0.0, z))
            if horizontal_axis == 0:
                origin.x = horizontal
            else:
                origin.y = horizontal
            direction = Vector((0.0, 0.0, 0.0))
            if face == "neg_y":
                origin.y = depth_min - depth_size * 0.05
                direction.y = 1.0
            elif face == "pos_y":
                origin.y = depth_max + depth_size * 0.05
                direction.y = -1.0
            elif face == "neg_x":
                origin.x = depth_min - depth_size * 0.05
                direction.x = 1.0
            else:
                origin.x = depth_max + depth_size * 0.05
                direction.x = -1.0
            _, _, _, distance = bvh.ray_cast(origin, direction)
            distance_to_boundary = depth_size * 0.05
            is_open_near_face = distance is None or distance > distance_to_boundary + near_limit
            row.append(is_open_near_face)
        grid.append(row)

    candidates = []
    for cells in components(grid, resolution, resolution):
        xs = [cell[0] for cell in cells]
        zs = [cell[1] for cell in cells]
        x0, x1 = min(xs), max(xs) + 1
        z0, z1 = min(zs), max(zs) + 1
        horizontal0 = horizontal_min + x0 / resolution * (horizontal_max - horizontal_min)
        horizontal1 = horizontal_min + x1 / resolution * (horizontal_max - horizontal_min)
        vertical0 = vertical_min + z0 / resolution * (vertical_max - vertical_min)
        vertical1 = vertical_min + z1 / resolution * (vertical_max - vertical_min)
        width = horizontal1 - horizontal0
        height = vertical1 - vertical0
        if len(cells) < 30 or width < (horizontal_max - horizontal_min) * 0.06:
            continue
        candidates.append({
            "pixels": len(cells),
            "horizontal_min": round(horizontal0, 6),
            "horizontal_max": round(horizontal1, 6),
            "vertical_min": round(vertical0, 6),
            "vertical_max": round(vertical1, 6),
            "width": round(width, 6),
            "height": round(height, 6),
            "touches_ground": z0 <= 3,
        })
    return sorted(candidates, key=lambda item: item["pixels"], reverse=True)[:20]


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    report_path, *fbx_paths = args
    records = []
    for path in fbx_paths:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=path)
        obj = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
        lo, hi = bounds(obj)
        bvh = BVHTree.FromObject(obj, bpy.context.evaluated_depsgraph_get())
        records.append({
            "file": path,
            "bounds_min": list(lo),
            "bounds_max": list(hi),
            "faces": {face: scan_face(obj, bvh, lo, hi, face) for face in ("neg_y", "pos_y", "neg_x", "pos_x")},
        })
    os.makedirs(os.path.dirname(os.path.abspath(report_path)), exist_ok=True)
    with open(report_path, "w", encoding="utf-8") as handle:
        json.dump(records, handle, ensure_ascii=False, indent=2)
    print(f"[Manor][OpeningScan] wrote {report_path}")


if __name__ == "__main__":
    main()
