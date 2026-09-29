import bpy
import os
import sys


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 2:
        raise RuntimeError("Usage: blender --background --python create_o03_doorway_variant.py -- SOURCE OUTPUT")
    source_path, output_path = map(os.path.abspath, args)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=source_path)
    target = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")

    # The authored O03 has no ground-level doorway. Cut only the +X exterior wall at the
    # existing northern facade; dimensions map to an approximately 1.80 x 2.20 metre opening
    # after the builder's uniform 7.158788 scale. The original FBX remains untouched.
    bpy.ops.mesh.primitive_cube_add(location=(0.455, 0.0, 0.15365))
    cutter = bpy.context.object
    cutter.name = "O03_DoorwayCutter_UnityDerived"
    cutter.dimensions = (0.12, 0.252, 0.3073)
    bpy.context.view_layer.objects.active = cutter
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    modifier = target.modifiers.new(name="O03_GroundDoorway_UnityDerived", type="BOOLEAN")
    modifier.operation = "DIFFERENCE"
    modifier.solver = "EXACT"
    modifier.object = cutter
    bpy.context.view_layer.objects.active = target
    target.select_set(True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.data.objects.remove(cutter, do_unlink=True)

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    target.select_set(True)
    bpy.context.view_layer.objects.active = target
    bpy.ops.export_scene.fbx(
        filepath=output_path,
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
        bake_anim=False,
        path_mode="AUTO",
    )
    print(f"[Manor][O03Derived] exported {output_path}")


if __name__ == "__main__":
    main()
