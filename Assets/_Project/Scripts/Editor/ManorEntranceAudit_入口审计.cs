#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ManorEntranceAudit
{
    private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";

    [MenuItem("庄园/场景修复/审计主楼入口")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name.Contains("ManorDoorPivot") || root.name.Contains("地板") || root.name.Contains("GroundFloor") || root.name.Contains("PF_NewUserManor"))
                Dump(root.transform, 0);
        }
        var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.gameObject.scene == scene)
            .Where(t => t.name.Contains("Door", StringComparison.OrdinalIgnoreCase) || t.name.Contains("门") || t.name.Contains("ManorDoorPivot"));
        foreach (var t in all)
        {
            var rs = t.GetComponentsInChildren<Renderer>(true);
            var b = new Bounds(t.position, Vector3.zero); var has=false;
            foreach(var r in rs){ if(!has){b=r.bounds;has=true;} else b.Encapsulate(r.bounds); }
            Debug.Log($"[EntranceAudit] {Path(t)} active={t.gameObject.activeSelf} renderers={rs.Length} bounds={(has?b.ToString():"none")}");
        }
    }

    private static void Dump(Transform t, int depth)
    {
        if (depth > 5) return;
        var r=t.GetComponent<Renderer>(); var mf=t.GetComponent<MeshFilter>(); var mc=t.GetComponent<MeshCollider>();
        Debug.Log($"[EntranceAudit] {new string(' ',depth*2)}{Path(t)} active={t.gameObject.activeSelf} renderer={(r!=null)} mesh={(mf!=null?mf.sharedMesh?.name:"-")} collider={(mc!=null?mc.sharedMesh?.name:"-")} pos={t.position} scale={t.lossyScale}");
        foreach(Transform c in t) Dump(c,depth+1);
    }
    private static string Path(Transform t){var p=t.name; while(t.parent!=null){t=t.parent;p=t.name+"/"+p;} return p;}
}
#endif
