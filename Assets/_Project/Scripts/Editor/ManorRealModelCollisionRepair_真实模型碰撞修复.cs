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
    [InitializeOnLoad]
    public static class ManorRealModelCollisionRepair
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";
        // Bump this only when a scene-repair migration changes.  Unity's SessionState keeps
        // the previous run across recompiles, so a new migration must use a new key.
        private const string SessionKey = "Manor.RealModelCollisionRepair.20260818.v6.no-supplemental-door-wall";

        static ManorRealModelCollisionRepair()
        {
            EditorApplication.delayCall += RunAutomaticPassOnce;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("庄园/场景修复/彻底删除空气墙并给导入模型添加真实碰撞")]
        public static void RunManualPass()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int debrisRoots = RemoveScatteredWhiteProps(scene);
            int proxiesRemoved = RemoveAllNonModelSolidColliders(scene);
            int meshCollidersAdded = AddImportedModelMeshColliders(scene);
            int doorMeshes = AddRealDoorMeshColliders(scene);
            // Never manufacture a solid panel in a doorway. The user requires only collision
            // authored by their real models; an invisible supplemental panel is an air wall.
            int completeDoorLeaves = RemoveSupplementalDoorLeafSurfaces(scene);
            // Do not merge the whole imported house into one static collider.  The user has
            // separated the real door/window pieces; a whole-building collider would silently
            // put their old closed geometry back into every doorway/window opening and prevent
            // an opened leaf or a vault from ever creating a passage.
            int combinedManor = RemoveLegacyCombinedManorCollider(scene);
            int manorMeshes = CountMeshCollidersUnder("PF_NewUserManor_新版主楼");
            int remainingProxySolids = CountRemainingProxySolids(scene);
            int remainingWhiteProps = CountRemainingScatteredProps(scene);
            int legacyNodes = RemoveLegacyManorScaffolding(scene);
            LogMainBuildingParts(scene);

            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[庄园][物理修复完成] 删除白色散落物根节点={debrisRoots}，删除旧空气墙/方块碰撞={proxiesRemoved}，新增导入模型网格碰撞={meshCollidersAdded}，实体门扇网格碰撞={doorMeshes}，删除额外门板空气墙={completeDoorLeaves}，清理主楼周边旧构建节点={legacyNodes}，主楼固定模型网格碰撞={manorMeshes}，移除旧整楼合并碰撞={combinedManor}，剩余非门实体代理={remainingProxySolids}，剩余散落白色碎片={remainingWhiteProps}。 ");
        }

        private static int RemoveLegacyManorScaffolding(Scene scene)
        {
            string[] legacyRoots =
            {
                "ManorEntranceThreshold_主入口门槛", "ManorDoorPivot_HouseEntrance", "G07_可翻越实体窗",
                "ManagedInteriorV2_连续主楼布局", "InteriorArt_v2_连续主楼陈设", "ENT_进入主楼室内"
            };
            int removed = 0;
            foreach (Transform item in UObj.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(item => item.gameObject.scene == scene && legacyRoots.Contains(item.name))
                         .OrderByDescending(GetDepth).ToArray())
            {
                Undo.DestroyObjectImmediate(item.gameObject);
                removed++;
            }
            foreach (MonoBehaviour controller in UObj.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(item => item.gameObject.scene == scene && item.GetType().Name == "ManorExteriorVisibilityController").ToArray())
            {
                Undo.DestroyObjectImmediate(controller);
                removed++;
            }
            return removed;
        }

        // This is deliberately an editor-side audit.  It records the actual independently
        // imported pieces and their bounds, so door/window interaction is attached only to a
        // real split leaf rather than guessed from a whole-building mesh.
        private static void LogMainBuildingParts(Scene scene)
        {
            Transform manor = UObj.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.gameObject.scene == scene && item.name == "PF_NewUserManor_新版主楼");
            if (manor == null) return;
            foreach (MeshFilter filter in manor.GetComponentsInChildren<MeshFilter>(true)
                         .Where(filter => filter.sharedMesh != null && filter.GetComponent<Renderer>() != null)
                         .OrderBy(filter => filter.transform.name))
            {
                Bounds bounds = filter.GetComponent<Renderer>().bounds;
                Debug.Log($"[庄园][主楼拆分件] name={filter.transform.name} center={bounds.center} size={bounds.size} path={GetPath(filter.transform)}");
            }
        }

        private static int RemoveScatteredWhiteProps(Scene scene)
        {
            int removed = 0;
            foreach (Transform root in UObj.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(item => item.gameObject.scene == scene)
                         .Where(item => item.name == "UserDecoration_用户装饰" || item.name.StartsWith("UserProp_", StringComparison.Ordinal))
                         .OrderByDescending(GetDepth))
            {
                if (root == null) continue;
                if (root.parent != null && root.parent.name == "UserDecoration_用户装饰") continue;
                Undo.DestroyObjectImmediate(root.gameObject);
                removed++;
            }
            return removed;
        }

        private static int RemoveAllNonModelSolidColliders(Scene scene)
        {
            int removed = 0;
            foreach (Collider collider in UObj.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
            {
                if (collider == null || collider.gameObject.scene != scene || collider.isTrigger) continue;
                if (collider is CharacterController || collider.GetComponentInParent<CharacterController>() != null) continue;
                if (collider.GetComponentInParent<DoorInteractable>() != null) continue;
                Undo.DestroyObjectImmediate(collider);
                removed++;
            }
            return removed;
        }

        private static int AddImportedModelMeshColliders(Scene scene)
        {
            int added = 0;
            foreach (MeshFilter filter in UObj.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (filter == null || filter.gameObject.scene != scene || filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0) continue;
                if (filter.GetComponentInParent<CharacterController>() != null || filter.GetComponentInParent<DoorInteractable>() != null) continue;
                // Imported FBX pieces and visible authored Unity surfaces (floors, treads, risers,
                // walls and furniture) all collide with their own mesh. Hidden objects do not.
                if (!IsImportedUserModel(filter) && !IsVisibleSceneMesh(filter.gameObject)) continue;
                MeshCollider collider = filter.GetComponent<MeshCollider>();
                if (collider == null) { collider = Undo.AddComponent<MeshCollider>(filter.gameObject); added++; }
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
                collider.isTrigger = false;
                collider.enabled = true;
                filter.gameObject.isStatic = true;
                // Imported Rhino objects can carry IgnoreRaycast/visual-only layers from the
                // source file. Put physical world geometry on Default so CharacterController
                // collision is guaranteed by the project layer matrix.
                filter.gameObject.layer = 0;
            }
            return added;
        }

        private static int RemoveLegacyCombinedManorCollider(Scene scene)
        {
            Transform manor = UObj.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.gameObject.scene == scene && item.name == "PF_NewUserManor_新版主楼");
            if (manor == null) return 0;

            Transform old = manor.Find("REAL_COLLISION_主楼完整模型");
            if (old == null) return 0;
            Undo.DestroyObjectImmediate(old.gameObject);
            return 1;
        }

        private static int AddRealDoorMeshColliders(Scene scene)
        {
            int count = 0;
            foreach (DoorInteractable door in UObj.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (door.gameObject.scene != scene) continue;
                foreach (MeshFilter filter in door.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0) continue;
                    MeshCollider collider = filter.GetComponent<MeshCollider>();
                    if (collider == null) { collider = Undo.AddComponent<MeshCollider>(filter.gameObject); count++; }
                    collider.sharedMesh = filter.sharedMesh;
                    collider.convex = false;
                    collider.isTrigger = false;
                    collider.enabled = true;
                    filter.gameObject.layer = 0;
                    foreach (BoxCollider proxy in filter.GetComponents<BoxCollider>()) proxy.enabled = false;
                }
            }
            return count;
        }

        private static int RemoveSupplementalDoorLeafSurfaces(Scene scene)
        {
            Transform pivot = UObj.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.gameObject.scene == scene && item.name == "ManorDoorPivot_HouseEntrance");
            if (pivot == null) return 0;
            Transform old = pivot.Find("REAL_COLLISION_主楼正门完整门扇");
            if (old == null) return 0;
            Undo.DestroyObjectImmediate(old.gameObject);
            return 1;
        }

        private static bool IsImportedUserModel(MeshFilter filter)
        {
            string meshPath = AssetDatabase.GetAssetPath(filter.sharedMesh);
            if (meshPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return true;
            return HasAncestor(filter.transform, "PF_NewUserManor_新版主楼");
        }

        private static bool IsVisibleGroundSurface(string name) =>
            name.StartsWith("Terrain", StringComparison.Ordinal) || name.StartsWith("MainPath", StringComparison.Ordinal) ||
            name.StartsWith("CourtyardCrossPath", StringComparison.Ordinal) || name.StartsWith("Cliff", StringComparison.Ordinal);

        private static bool IsVisibleSceneMesh(GameObject gameObject)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null || !renderer.enabled || !gameObject.activeInHierarchy) return false;
            string name = gameObject.name;
            return !name.Contains("Marker", StringComparison.OrdinalIgnoreCase) &&
                   !name.Contains("Debug", StringComparison.OrdinalIgnoreCase) &&
                   !name.StartsWith("UserProp_", StringComparison.Ordinal);
        }

        private static int CountMeshCollidersUnder(string rootName)
        {
            Transform root = UObj.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.name == rootName);
            return root == null ? 0 : root.GetComponentsInChildren<MeshCollider>(true).Count(item => item.enabled && item.sharedMesh != null);
        }

        private static int CountRemainingProxySolids(Scene scene) => UObj.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(collider => collider != null && collider.gameObject.scene == scene && !collider.isTrigger &&
                !(collider is CharacterController) && collider.GetComponentInParent<CharacterController>() == null &&
                collider.GetComponentInParent<DoorInteractable>() == null && !(collider is MeshCollider));

        private static int CountRemainingScatteredProps(Scene scene) => UObj.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(item => item.gameObject.scene == scene &&
                (item.name == "UserDecoration_用户装饰" || item.name.StartsWith("UserProp_", StringComparison.Ordinal)));

        private static bool HasAncestor(Transform transform, string name)
        {
            for (Transform current = transform; current != null; current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static int GetDepth(Transform transform)
        {
            int depth = 0;
            for (; transform.parent != null; transform = transform.parent) depth++;
            return depth;
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            for (Transform current = transform.parent; current != null; current = current.parent)
                path = current.name + "/" + path;
            return path;
        }

        private static void RunAutomaticPassOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path != ScenePath) return;
            SessionState.SetBool(SessionKey, true);
            RunManualPass();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            SessionState.EraseBool(SessionKey);
            EditorApplication.delayCall += RunAutomaticPassOnce;
        }
    }
}
#endif
