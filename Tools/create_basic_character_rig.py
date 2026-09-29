"""Creates a provisional humanoid rig on copied character FBX assets; source assets stay untouched."""
import bpy
import os
import sys
from mathutils import Vector


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def bounds(mesh):
    points = [mesh.matrix_world @ Vector(v) for v in mesh.bound_box]
    return (Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points))),
            Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points))))


def add_bone(edit_bones, name, head, tail, parent=None):
    bone = edit_bones.new(name); bone.head = head; bone.tail = tail; bone.parent = parent; return bone


def rig_mesh(mesh, output):
    low, high = bounds(mesh); center = (low + high) * .5; h = max(.01, high.z - low.z); w = max(.01, high.x - low.x)
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    armature = bpy.context.object; armature.name = mesh.name + "_ProvisionalRig"
    bones = armature.data.edit_bones; bones.remove(bones[0])
    root = add_bone(bones, "Root", Vector((center.x, center.y, low.z)), Vector((center.x, center.y, low.z + h*.12)))
    hips = add_bone(bones, "Hips", root.tail, Vector((center.x, center.y, low.z + h*.45)), root)
    spine = add_bone(bones, "Spine", hips.tail, Vector((center.x, center.y, low.z + h*.72)), hips)
    add_bone(bones, "Head", spine.tail, Vector((center.x, center.y, high.z)), spine)
    for side, sign in (("L", -1), ("R", 1)):
        shoulder = add_bone(bones, side + "_UpperArm", spine.tail, Vector((center.x + sign*w*.45, center.y, low.z + h*.68)), spine)
        add_bone(bones, side + "_Forearm", shoulder.tail, Vector((center.x + sign*w*.72, center.y, low.z + h*.55)), shoulder)
        thigh = add_bone(bones, side + "_Thigh", hips.head, Vector((center.x + sign*w*.2, center.y, low.z + h*.25)), hips)
        add_bone(bones, side + "_Calf", thigh.tail, Vector((center.x + sign*w*.2, center.y, low.z + h*.03)), thigh)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT"); mesh.select_set(True); armature.select_set(True); bpy.context.view_layer.objects.active = armature
    try:
        bpy.ops.object.parent_set(type="ARMATURE_AUTO")
        status = "automatic_weights"
    except RuntimeError as exc:
        mesh.parent = armature; modifier = mesh.modifiers.new("Armature", "ARMATURE"); modifier.object = armature
        status = "armature_parent_only:" + str(exc)
    bpy.ops.object.select_all(action="DESELECT"); mesh.select_set(True); armature.select_set(True); bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(filepath=output, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", add_leaf_bones=False, bake_anim=False)
    return status


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 2: raise RuntimeError("Usage: blender --background --python create_basic_character_rig.py -- SOURCE_ROOT OUTPUT")
    root, output = map(os.path.abspath, args); os.makedirs(output, exist_ok=True)
    for folder, name in (("屠夫", "Butcher_ProvisionalRig.fbx"), ("怨鬼小孩", "GhostChild_ProvisionalRig.fbx")):
        reset(); source = os.path.join(root, folder, "不用拆.fbx"); bpy.ops.import_scene.fbx(filepath=source, use_anim=False)
        mesh = next(o for o in bpy.context.scene.objects if o.type == "MESH")
        print("[Manor][Rig] " + folder + "=" + rig_mesh(mesh, os.path.join(output, name)))


if __name__ == "__main__": main()
