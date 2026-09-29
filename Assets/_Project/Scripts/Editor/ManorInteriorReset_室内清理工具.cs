#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Editor
{
    /// <summary>
    /// Removes only the generated interior layout and generated interior dressing from the production
    /// scene. The exterior manor model, user bridge, gate, terrain, and exterior dressing are not targets.
    /// </summary>
    public static class ManorInteriorReset
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";
        private const string InteriorLayoutRoot = "ManagedInteriorV2_连续主楼布局";
        private const string InteriorArtRoot = "InteriorArt_v2_连续主楼陈设";

        [MenuItem("庄园/自动化/备份并清理当前室内布局")]
        public static void BackupAndClearGeneratedInterior()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string directory = Path.GetDirectoryName(ScenePath)?.Replace('\\', '/') ?? "Assets";
            string fileName = Path.GetFileNameWithoutExtension(ScenePath);
            string backupPath = $"{directory}/{fileName}_清理室内前备份_{DateTime.Now:yyyyMMdd_HHmmss}.unity";

            if (!AssetDatabase.CopyAsset(ScenePath, backupPath))
                throw new IOException($"Unable to create scene backup: {backupPath}");

            int removed = 0;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                removed += RemoveNamedChildren(rootObject.transform, InteriorLayoutRoot);
                removed += RemoveNamedChildren(rootObject.transform, InteriorArtRoot);
            }

            if (removed == 0)
                Debug.LogWarning("[Manor][InteriorReset] 未找到可清理的生成室内节点；场景保持不变。");
            else
                Debug.Log($"[Manor][InteriorReset] 已清理 {removed} 个生成室内根节点。外壳、木桥和大门未被作为目标。备份：{backupPath}");

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static int RemoveNamedChildren(Transform parent, string targetName)
        {
            int removed = 0;
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (child.name == targetName)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    removed++;
                    continue;
                }

                removed += RemoveNamedChildren(child, targetName);
            }

            return removed;
        }
    }
}
#endif
