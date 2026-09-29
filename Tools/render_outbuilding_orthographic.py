import bpy
import math
import os
import sys
from mathutils import Vector


def scene_bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    lo = Vector(tuple(min(point[index] for point in points) for index in range(3)))
    hi = Vector(tuple(max(point[index] for point in points) for index in range(3)))
    return lo, hi


def look_at(camera, target):
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()


def render_asset(fbx_path, output_root):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx_path)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    lo, hi = scene_bounds(meshes)
    center = (lo + hi) * 0.5
    size = hi - lo

    material = bpy.data.materials.new("InspectionClay")
    material.diffuse_color = (0.48, 0.55, 0.58, 1.0)
    material.metallic = 0.0
    material.roughness = 0.72
    for obj in meshes:
        obj.data.materials.clear()
        obj.data.materials.append(material)

    bpy.ops.object.light_add(type="AREA", location=center + Vector((2.0, -2.0, 3.0)))
    bpy.context.object.data.energy = 900
    bpy.context.object.data.shape = "DISK"
    bpy.context.object.data.size = 5.0
    bpy.ops.object.light_add(type="AREA", location=center + Vector((-2.0, 2.0, 1.5)))
    bpy.context.object.data.energy = 500
    bpy.context.object.data.size = 4.0

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = max(size.x, size.y, size.z) * 1.22
    bpy.context.scene.camera = camera

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("InspectionWorld")
    scene.world.color = (0.025, 0.025, 0.025)

    distance = max(size.x, size.y, size.z) * 3.0
    views = {
        "front_neg_y": Vector((0.0, -distance, 0.0)),
        "back_pos_y": Vector((0.0, distance, 0.0)),
        "left_neg_x": Vector((-distance, 0.0, 0.0)),
        "right_pos_x": Vector((distance, 0.0, 0.0)),
    }
    stem = os.path.splitext(os.path.basename(fbx_path))[0]
    folder = os.path.join(output_root, os.path.basename(os.path.dirname(fbx_path)) + "_" + stem)
    os.makedirs(folder, exist_ok=True)
    for name, offset in views.items():
        camera.location = center + offset
        look_at(camera, center)
        scene.render.filepath = os.path.join(folder, name + ".png")
        bpy.ops.render.render(write_still=True)


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    output_root, *fbx_paths = args
    os.makedirs(output_root, exist_ok=True)
    for fbx_path in fbx_paths:
        render_asset(fbx_path, output_root)


if __name__ == "__main__":
    main()
