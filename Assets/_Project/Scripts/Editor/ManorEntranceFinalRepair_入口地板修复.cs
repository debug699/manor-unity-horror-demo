#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Editor
{
    public static class ManorEntranceFinalRepair
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";
        private const string FloorName = "ManorGroundFloor_主楼一层可见地板";
        private const string SessionKey = "Manor.MainEntrance.FinalRepair.20260818.v1";

        // 仅通过菜单手动执行，避免再次自动移动用户模型。

        [MenuItem("庄园/场景修复/恢复正门并铺设一层地板 %#&r")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var pivot = Find("ManorDoorPivot_HouseEntrance", scene);
            Vector3 entrance = pivot ? pivot.transform.position : FindManor(scene)?.transform.position ?? Vector3.zero;
            Quaternion entranceRot = pivot ? pivot.transform.rotation : Quaternion.identity;
            Renderer doorRenderer = null;
            Transform restoreParent = pivot ? pivot.transform.parent : null;
            if (pivot)
            {
                doorRenderer = pivot.GetComponentsInChildren<Renderer>(true).FirstOrDefault();
                if (doorRenderer)
                {
                    doorRenderer.gameObject.SetActive(true);
                    doorRenderer.transform.SetParent(restoreParent, true);
                    Debug.Log($"[EntranceFinalRepair] 恢复门模型：{Path(doorRenderer.transform)} bounds={doorRenderer.bounds}");
                }
                foreach (var d in pivot.GetComponentsInChildren<MonoBehaviour>(true).Where(x => x.GetType().Name == "DoorInteractable").ToArray()) Undo.DestroyObjectImmediate(d);
                Undo.DestroyObjectImmediate(pivot);
            }

            var oldFloor = Find(FloorName, scene);
            if (oldFloor) Undo.DestroyObjectImmediate(oldFloor);
            float topY = doorRenderer ? doorRenderer.bounds.min.y : entrance.y;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = FloorName;
            Undo.RegisterCreatedObjectUndo(floor, "Create visible main floor");
            floor.transform.SetParent(restoreParent, true);
            floor.transform.position = entrance + Vector3.up * (topY - 0.10f);
            floor.transform.rotation = entranceRot;
            floor.transform.localScale = new Vector3(6.0f, 0.20f, 6.0f);
            var floorRenderer = floor.GetComponent<Renderer>();
            var source = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(r => r.gameObject.scene == scene && (r.name.Contains("Floor", StringComparison.OrdinalIgnoreCase) || r.name.Contains("地面")) && r.sharedMaterial != null);
            if (source) floorRenderer.sharedMaterial = source.sharedMaterial;
            var box = floor.GetComponent<BoxCollider>();
            box.enabled = true; floor.layer = 0;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log($"[EntranceFinalRepair] 已创建可见一层地板：{Path(floor.transform)} topY={topY:F3} pos={floor.transform.position} size={floor.transform.localScale}");
        }

        private static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path != ScenePath) return;
            SessionState.SetBool(SessionKey, true); Repair();
        }
        private static GameObject Find(string name, Scene scene) => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.gameObject.scene == scene && t.name == name)?.gameObject;
        private static GameObject FindManor(Scene scene) => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.gameObject.scene == scene && t.name == "PF_NewUserManor_新版主楼")?.gameObject;
        private static string Path(Transform t){var p=t.name; while(t.parent!=null){t=t.parent;p=t.name+"/"+p;} return p;}
    }
}
#endif
