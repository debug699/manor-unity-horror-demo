import bpy, sys, json, os
files=sys.argv[sys.argv.index("--")+1:]
res=[]
for p in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
      bpy.ops.import_scene.fbx(filepath=p)
      objs=[o for o in bpy.context.scene.objects if o.type=="MESH"]
      comps=[]
      # count root objs parentless
      roots=[o for o in objs if o.parent is None]
      verts=sum(len(o.data.vertices) for o in objs)
      res.append({"file":p,"mesh_objects":len(objs),"root_meshes":len(roots),"vertices":verts,"names":[o.name for o in objs[:12]]})
    except Exception as e:res.append({"file":p,"error":str(e)})
print(json.dumps(res,ensure_ascii=False,indent=2))
