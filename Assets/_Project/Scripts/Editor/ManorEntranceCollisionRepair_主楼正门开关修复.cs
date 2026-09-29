#if UNITY_EDITOR
using System;
using System.Linq;
using Manor.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObj = UnityEngine.Object;

namespace Manor.Editor
{
    public static class ManorEntranceCollisionRepair
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";
        private const string PivotName = "ManorDoorPivot_HouseEntrance";
        private const string SessionKey = "Manor.MainEntranceDoorRepair.20260818.v1";

        // 不再在编译/打开场景时自动按尺寸猜测门模型；请通过菜单在确认对象后手动运行。

        [MenuItem("庄园/场景修复/把主楼正门设为可开关门")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform manor = UObj.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.gameObject.scene == scene && item.name == "PF_NewUserManor_新版主楼");
            if (manor == null) { Debug.LogError("[MainDoorRepair] 找不到主楼。"); return; }

            DoorInteractable existing = UObj.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.gameObject.scene == scene && Path(item.transform).Contains(PivotName, StringComparison.Ordinal));
            if (existing != null) { EnsureMeshCollision(existing); EditorSceneManager.SaveScene(scene); return; }

            Renderer visual = FindEntranceDoorRenderer(manor);
            if (visual == null) { Debug.LogError("[MainDoorRepair] 找不到主楼正门可见门模型，未创建任何碰撞物。"); return; }
            GameObject pivotObject = new GameObject(PivotName);
            Undo.RegisterCreatedObjectUndo(pivotObject, "Create main entrance door pivot");
            pivotObject.transform.SetParent(visual.transform.parent, true);
            pivotObject.transform.position = visual.bounds.center - visual.bounds.extents.x * Vector3.right;
            pivotObject.transform.rotation = visual.transform.rotation;
            pivotObject.layer = 0;
            Vector3 worldPosition = visual.transform.position;
            Quaternion worldRotation = visual.transform.rotation;
            Vector3 worldScale = visual.transform.lossyScale;
            visual.transform.SetParent(pivotObject.transform, true);
            visual.transform.SetPositionAndRotation(worldPosition, worldRotation);
            visual.transform.localScale = worldScale;
            DoorInteractable door = visual.gameObject.GetComponent<DoorInteractable>() ?? Undo.AddComponent<DoorInteractable>(visual.gameObject);
            SerializedObject serialized = new SerializedObject(door);
            serialized.FindProperty("_doorPivot").objectReferenceValue = pivotObject.transform;
            serialized.FindProperty("_openAngle").floatValue = 100f;
            serialized.FindProperty("_startsOpen").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EnsureMeshCollision(door);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MainDoorRepair] 已将真实门模型设为可开关门：{Path(visual.transform)}。");
        }

        private static Renderer FindEntranceDoorRenderer(Transform manor) => manor.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
            .Where(renderer => renderer.bounds.center.y > .5f && renderer.bounds.center.y < 4.8f)
            .Where(renderer => renderer.bounds.size.y > 1.5f)
            .Where(renderer => Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z) > 1.5f)
            .OrderBy(renderer => Mathf.Abs(renderer.bounds.center.x))
            .ThenBy(renderer => Mathf.Abs(renderer.bounds.center.z - 10.5f))
            .FirstOrDefault();

        private static void EnsureMeshCollision(DoorInteractable door)
        {
            foreach (MeshFilter filter in door.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0) continue;
                MeshCollider collider = filter.GetComponent<MeshCollider>() ?? Undo.AddComponent<MeshCollider>(filter.gameObject);
                collider.sharedMesh = filter.sharedMesh; collider.convex = false; collider.isTrigger = false; collider.enabled = true;
                filter.gameObject.layer = 0;
                foreach (BoxCollider box in filter.GetComponents<BoxCollider>()) box.enabled = false;
            }
        }

        private static string Path(Transform item)
        {
            string path = item.name;
            while (item.parent != null) { item = item.parent; path = item.name + "/" + path; }
            return path;
        }

        private static void RunOnce() { }
    }
}
#endif
