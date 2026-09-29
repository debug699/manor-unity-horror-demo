import bpy,sys,json
out=[]
for p in sys.argv[sys.argv.index("--")+1:]:
 bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=p)
 ob=bpy.context.scene.objects
 arms=[o for o in ob if o.type=="ARMATURE"]
 out.append({"p":p,"armatures":len(arms),"bones":sum(len(a.data.bones) for a in arms),"actions":len(bpy.data.actions),"object_types":{t:sum(o.type==t for o in ob) for t in set(o.type for o in ob)}})
print(json.dumps(out,ensure_ascii=False))
