#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Editor
{
    public static class ManorProjectInitializer
    {
        private const string Root = "Assets/_Project";

        [MenuItem("庄园/初始化项目基础场景")]
        public static void Initialize()
        {
            EnsureFolders();
            CreateBootScene();
            CreateMainMenuScene();
            CreateManorDemoScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Manor] Project initialization completed.");
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Art", "Art/Characters", "Art/Environment", "Art/Materials", "Art/Textures", "Art/VFX", "Art/UI",
                "Audio", "Audio/Ambience", "Audio/SFX", "Audio/Voices", "Audio/Music",
                "Data", "Data/Clues", "Data/Story", "Data/Objectives", "Data/Save", "Data/Settings",
                "Prefabs", "Prefabs/Actors", "Prefabs/Interactables", "Prefabs/Environment", "Prefabs/Gameplay", "Prefabs/UI",
                "Scenes", "Scenes/Production", "Scenes/Tests",
                "Scripts", "Scripts/Core", "Scripts/Gameplay", "Scripts/Player", "Scripts/AI", "Scripts/Narrative", "Scripts/UI", "Scripts/Audio", "Scripts/Debug",
                "Settings", "Tests"
            };

            foreach (string folder in folders)
            {
                string path = $"{Root}/{folder}";
                if (!AssetDatabase.IsValidFolder(path))
                {
                    string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                    string name = Path.GetFileName(path);
                    AssetDatabase.CreateFolder(parent, name);
                }
            }
        }

        private static void CreateBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Boot");
            new GameObject("SceneFlowService").transform.SetParent(root.transform);
            SaveScene(scene, "SCN_Boot_启动场景");
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("MainMenu_主菜单");
            GameObject camera = CreateCamera("MainMenuCamera_主菜单摄像机", new Vector3(0f, 1.6f, -6f));
            camera.transform.SetParent(root.transform);
            GameObject light = CreateLight("MainMenuLight_主菜单灯光", new Vector3(0f, 3f, -2f));
            light.transform.SetParent(root.transform);
            SaveScene(scene, "SCN_MainMenu_主菜单");
        }

        private static void CreateManorDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("ManorDemo_庄园Demo");
            CreateChild(root, "Environment_环境");
            CreateChild(root, "Navigation_导航");
            CreateChild(root, "Gameplay_玩法");
            CreateChild(root, "Narrative_叙事");
            CreateChild(root, "Actors_角色");
            CreateChild(root, "Audio_音频");
            CreateChild(root, "Lighting_灯光");
            CreateChild(root, "Debug_调试");

            GameObject camera = CreateCamera("PlayerCamera_玩家摄像机", new Vector3(0f, 1.6f, -6f));
            camera.transform.SetParent(root.transform.Find("Actors_角色"));
            GameObject light = CreateLight("KeyLight_主灯光", new Vector3(0f, 3f, -2f));
            light.transform.SetParent(root.transform.Find("Lighting_灯光"));

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Greybox_Floor_灰盒地面_Prototype";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(5f, 1f, 5f);
            floor.transform.SetParent(root.transform.Find("Environment_环境"));

            GameObject spawn = new GameObject("PlayerSpawn_玩家出生点");
            spawn.transform.SetParent(root.transform.Find("Gameplay_玩法"));
            spawn.transform.position = new Vector3(0f, 0.1f, -4f);

            SaveScene(scene, "SCN_ManorDemo_庄园Demo");
        }

        private static GameObject CreateCamera(string name, Vector3 position)
        {
            GameObject camera = new GameObject(name);
            camera.AddComponent<Camera>();
            camera.transform.position = position;
            camera.transform.rotation = Quaternion.identity;
            return camera;
        }

        private static GameObject CreateLight(string name, Vector3 position)
        {
            GameObject lightObject = new GameObject(name);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.position = position;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            return lightObject;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child;
        }

        private static void SaveScene(Scene scene, string sceneName)
        {
            string path = $"{Root}/Scenes/Production/{sceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void ConfigureBuildSettings()
        {
            string[] scenePaths =
            {
                $"{Root}/Scenes/Production/SCN_Boot_启动场景.unity",
                $"{Root}/Scenes/Production/SCN_MainMenu_主菜单.unity",
                $"{Root}/Scenes/Production/SCN_ManorDemo_庄园Demo.unity"
            };

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenePaths[0], true),
                new EditorBuildSettingsScene(scenePaths[1], true),
                new EditorBuildSettingsScene(scenePaths[2], true)
            };
        }
    }
}
#endif
