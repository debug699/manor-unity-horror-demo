#if UNITY_EDITOR
using Manor.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Editor
{
    /// <summary>Places the player at the main building entrance for layout inspection; it does not change geometry.</summary>
    public static class ManorBridgeSpawnFix
    {
        [MenuItem("庄园/场景修复/把出生点移到主楼正门前（布局检查）")]
        public static void MovePlayerToMainBuildingEntrance()
        {
            Scene scene = SceneManager.GetActiveScene();
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Include);
            if (player == null)
            {
                EditorUtility.DisplayDialog("修复出生点", "当前场景没有找到玩家。请打开庄园 Demo 场景后重试。", "知道了");
                return;
            }

            // This clear point sits directly in front of the manor facade entrance, away from
            // the bridge's detailed rail meshes, so the whole estate layout can be inspected.
            Undo.RecordObject(player.transform, "Move player to main building entrance");
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, 16.2f), Quaternion.Euler(0f, 180f, 0f));
            if (controller != null) controller.enabled = true;

            Transform spawn = FindByName(scene, "PlayerSpawn_玩家出生点");
            if (spawn != null)
            {
                Undo.RecordObject(spawn, "Move player spawn to main building entrance");
                spawn.SetPositionAndRotation(player.transform.position, player.transform.rotation);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog("出生点已移动", "玩家已放到主楼正门前，方便检查整体布局。\n\n桥、栅栏和大门都没有移动。", "完成");
        }

        private static Transform FindByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindByName(root.transform, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindByName(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform found = FindByName(parent.GetChild(index), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
