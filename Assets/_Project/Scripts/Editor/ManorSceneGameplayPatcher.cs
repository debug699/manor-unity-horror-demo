#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Manor.Gameplay;

namespace Manor.Editor
{
    /// <summary>Applies small gameplay additions to the user-edited outdoor scene without rebuilding its art hierarchy.</summary>
    public static class ManorSceneGameplayPatcher
    {
        private const string OutdoorScene = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";

        [MenuItem("庄园/自动化/移除旧主楼瞬移入口")]
        public static void RemoveLegacyIndoorEntrance()
        {
            Scene scene = EditorSceneManager.OpenScene(OutdoorScene, OpenSceneMode.Single);
            Transform legacy = Find(scene, "ENT_进入主楼室内");
            if (legacy != null) Object.DestroyImmediate(legacy.gameObject);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Manor][Patcher] Legacy teleport entrance removed; the manor remains continuous.");
        }

        private static Transform Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) { Transform found = Find(root.transform, name); if (found != null) return found; }
            return null;
        }
        private static Transform Find(Transform current, string name)
        {
            if (current.name == name) return current;
            foreach (Transform child in current) { Transform found = Find(child, name); if (found != null) return found; }
            return null;
        }
    }
}
#endif
