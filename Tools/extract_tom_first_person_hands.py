"""Creates non-destructive left/right first-person arm FBX copies from Tom's weighted mesh."""
import bpy
import bmesh
import os
import sys


LEFT = {"L_Clavicle", "L_Upperarm", "L_UpperarmTwist01", "L_UpperarmTwist02", "L_Forearm", "L_ForearmTwist01", "L_ForearmTwist02", "L_Hand"}
RIGHT = {name.replace("L_", "R_") for name in LEFT}


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def keep_arm(mesh, names):
    group_indices = {g.index for g in mesh.vertex_groups if g.name in names}
    bm = bmesh.new(); bm.from_mesh(mesh.data)
    to_delete = []
    for vert in bm.verts:
        source = mesh.data.vertices[vert.index]
        if not any(weight.group in group_indices and weight.weight > .05 for weight in source.groups):
            to_delete.append(vert)
    bmesh.ops.delete(bm, geom=to_delete, context="VERTS")
    bm.to_mesh(mesh.data); bm.free()
    for group in list(mesh.vertex_groups):
        if group.name not in names: mesh.vertex_groups.remove(group)


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 2: raise RuntimeError("Usage: blender --background --python extract_tom_first_person_hands.py -- SOURCE OUTPUT_DIR")
    source, out = map(os.path.abspath, args); os.makedirs(out, exist_ok=True)
    for label, names in (("Left", LEFT), ("Right", RIGHT)):
        reset(); bpy.ops.import_scene.fbx(filepath=source, use_anim=False)
        armature = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
        mesh = next(o for o in bpy.context.scene.objects if o.type == "MESH")
        keep_arm(mesh, names)
        mesh.name = "TomFirstPerson" + label + "Arm"
        bpy.ops.object.select_all(action="DESELECT"); mesh.select_set(True); armature.select_set(True); bpy.context.view_layer.objects.active = mesh
        bpy.ops.export_scene.fbx(filepath=os.path.join(out, "Tom_FirstPerson_" + label + "Arm.fbx"), use_selection=True,
            apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", add_leaf_bones=False, bake_anim=False)
        print("[Manor][TomHands] exported=" + label)


if __name__ == "__main__": main()
