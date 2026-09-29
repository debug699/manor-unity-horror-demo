#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Editor
{
    /// <summary>Reports the exact first physical surface on the central approach to the manor.</summary>
public static class ManorEntranceCollisionAudit
{
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";

        [InitializeOnLoadMethod]
        private static void RunVerificationAfterReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && SceneManager.GetActiveScene().path == ScenePath)
                    VerifyRealModelCollision();
            };
        }

        [MenuItem("庄园/场景修复/审计主楼正门实际碰撞")]
        public static void Audit()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            Vector3 origin = new(0f, 1.1f, 16.2f);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.back, 18f, ~0, QueryTriggerInteraction.Ignore)
                .OrderBy(hit => hit.distance).ToArray();
            foreach (RaycastHit hit in hits)
                Debug.Log($"[EntranceAudit] ray hit {Path(hit.transform)} distance={hit.distance:F3} point={hit.point} type={hit.collider.GetType().Name}");

            for (float z = 16.2f; z >= 6f; z -= .2f)
            {
                Vector3 foot = new(0f, .02f, z);
                Collider[] overlaps = Physics.OverlapCapsule(foot + Vector3.up * .35f, foot + Vector3.up * 1.5f, .3f, ~0, QueryTriggerInteraction.Ignore);
                Collider manor = overlaps.FirstOrDefault(item => Path(item.transform).Contains("PF_NewUserManor_新版主楼"));
                if (manor != null)
                    Debug.Log($"[EntranceAudit] capsule reaches manor at z={z:F2}: {Path(manor.transform)}");
            }
            if (hits.Length == 0) Debug.LogError("[EntranceAudit] No physical model exists on the central approach.");
        }

        [MenuItem("庄园/场景修复/验证主楼实体碰撞与无空气墙")]
        public static void VerifyRealModelCollision()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();

            Transform manor = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.gameObject.scene == scene && item.name == "PF_NewUserManor_新版主楼");
            if (manor == null) { Debug.LogError("[RealCollisionAudit] 主楼不存在。"); return; }

            int fixedModelMeshes = manor.GetComponentsInChildren<MeshFilter>(true)
                .Count(filter => filter.sharedMesh != null && filter.GetComponent<MeshCollider>() != null && filter.GetComponent<MeshCollider>().enabled);
            MeshFilter[] missingFixedModelColliders = manor.GetComponentsInChildren<MeshFilter>(true)
                .Where(filter => filter.sharedMesh != null && filter.GetComponent<Renderer>() != null)
                .Where(filter => filter.GetComponent<MeshCollider>() == null || !filter.GetComponent<MeshCollider>().enabled)
                .ToArray();
            int legacyCombined = manor.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == "REAL_COLLISION_主楼完整模型");
            int supplementalDoorWalls = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(item => item.gameObject.scene == scene && item.name == "REAL_COLLISION_主楼正门完整门扇");
            int legacyManorNodes = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(item => item.gameObject.scene == scene && new[]
                {
                    "ManorEntranceThreshold_主入口门槛", "ManorDoorPivot_HouseEntrance", "G07_可翻越实体窗",
                    "ManagedInteriorV2_连续主楼布局", "InteriorArt_v2_连续主楼陈设", "ENT_进入主楼室内"
                }.Contains(item.name));
            int legacyVaults = Object.FindObjectsByType<Manor.Gameplay.VaultWindowInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(item => item.gameObject.scene == scene);
            int exteriorVisibilityControllers = Object.FindObjectsByType<Manor.Gameplay.ManorExteriorVisibilityController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(item => item.gameObject.scene == scene);
            int nonMeshSolids = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(collider => collider.gameObject.scene == scene && !collider.isTrigger &&
                    !(collider is MeshCollider) && !(collider is CharacterController) &&
                    collider.GetComponentInParent<Manor.Gameplay.DoorInteractable>() == null);
            int debris = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(item => item.gameObject.scene == scene &&
                    (item.name == "UserDecoration_用户装饰" || item.name.StartsWith("UserProp_")));
            CharacterController player = Object.FindFirstObjectByType<CharacterController>(FindObjectsInactive.Include);
            bool playerHitsDefault = player != null && !Physics.GetIgnoreLayerCollision(player.gameObject.layer, 0);

            foreach (MeshFilter missing in missingFixedModelColliders)
                Debug.LogError($"[RealCollisionAudit] 漏碰撞主楼网格：{Path(missing.transform)}");

            Debug.Log($"[RealCollisionAudit] 主楼固定模型MeshCollider={fixedModelMeshes}; 主楼可见网格漏碰撞={missingFixedModelColliders.Length}; 旧整楼合并碰撞={legacyCombined}; 额外门板空气墙={supplementalDoorWalls}; 旧主楼构建节点={legacyManorNodes}; 旧翻窗移动脚本={legacyVaults}; 旧外壳切换脚本={exteriorVisibilityControllers}; 非门非玩家实体代理={nonMeshSolids}; 白色散落物根节点={debris}; 玩家层={player?.gameObject.layer.ToString() ?? "未找到"}; 玩家碰撞Default层={playerHitsDefault}.");
            if (fixedModelMeshes == 0 || missingFixedModelColliders.Length != 0 || legacyCombined != 0 || supplementalDoorWalls != 0 || legacyManorNodes != 0 || legacyVaults != 0 || exteriorVisibilityControllers != 0 || nonMeshSolids != 0 || debris != 0 || !playerHitsDefault)
                Debug.LogError("[RealCollisionAudit] 未通过：请按上述计数修复。");
            else
                Debug.Log("[RealCollisionAudit] 通过：固定主楼为真实模型碰撞，未发现旧整楼碰撞或空气墙代理。");
        }

        private static string Path(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
            return path;
        }
    }
}
#endif
