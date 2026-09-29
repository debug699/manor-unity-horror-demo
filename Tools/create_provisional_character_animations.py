"""Adds simple idle and walk actions to staged rigged character FBX copies for Unity smoke testing."""
import bpy
import math
import os
import sys


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def key(pose_bone, frame, rotation):
    pose_bone.rotation_mode = "XYZ"; pose_bone.rotation_euler = rotation; pose_bone.keyframe_insert("rotation_euler", frame=frame)


def create_actions(armature):
    bpy.context.view_layer.objects.active = armature; armature.select_set(True); bpy.ops.object.mode_set(mode="POSE")
    spine = armature.pose.bones.get("Spine")
    left_arm = armature.pose.bones.get("L_UpperArm")
    right_arm = armature.pose.bones.get("R_UpperArm")
    left_leg = armature.pose.bones.get("L_Thigh")
    right_leg = armature.pose.bones.get("R_Thigh")

    idle = bpy.data.actions.new("Idle"); armature.animation_data_create(); armature.animation_data.action = idle
    for frame, tilt in ((1, 0.0), (20, .025), (40, 0.0)):
        if spine: key(spine, frame, (tilt, 0, 0))
        if left_arm: key(left_arm, frame, (0, 0, .03))
        if right_arm: key(right_arm, frame, (0, 0, -.03))
    idle.frame_range = (1, 40)

    walk = bpy.data.actions.new("Walk"); armature.animation_data.action = walk
    for frame, swing in ((1, .38), (13, -.38), (25, .38)):
        if left_arm: key(left_arm, frame, (swing, 0, 0))
        if right_arm: key(right_arm, frame, (-swing, 0, 0))
        if left_leg: key(left_leg, frame, (-swing, 0, 0))
        if right_leg: key(right_leg, frame, (swing, 0, 0))
    walk.frame_range = (1, 25)
    bpy.ops.object.mode_set(mode="OBJECT")


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 2: raise RuntimeError("Usage: blender --background --python create_provisional_character_animations.py -- INPUT_DIR OUTPUT_DIR")
    inp, out = map(os.path.abspath, args); os.makedirs(out, exist_ok=True)
    for filename in ("Butcher_ProvisionalRig.fbx", "GhostChild_ProvisionalRig.fbx"):
        reset(); bpy.ops.import_scene.fbx(filepath=os.path.join(inp, filename), use_anim=False)
        armature = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
        create_actions(armature)
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.export_scene.fbx(filepath=os.path.join(out, filename.replace(".fbx", "_Animations.fbx")), use_selection=True,
            apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True)
        print("[Manor][Animations] exported=" + filename)


if __name__ == "__main__": main()
