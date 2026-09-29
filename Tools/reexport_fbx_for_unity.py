import bpy
import os
import sys


source, output = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.abspath(source), use_anim=False)

for obj in bpy.context.scene.objects:
    obj.select_set(obj.type == "MESH")

os.makedirs(os.path.dirname(os.path.abspath(output)), exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=os.path.abspath(output),
    use_selection=True,
    object_types={"MESH"},
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    bake_space_transform=False,
    add_leaf_bones=False,
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    use_tspace=True,
    use_triangles=False,
    use_armature_deform_only=True,
    bake_anim=False,
    path_mode="AUTO",
    embed_textures=False,
)

print(f"[Manor][UnityFBX] {source} -> {output}")
