#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Manor.Core;
using Manor.Gameplay;
using Manor.Player;
using Manor.UI;
using Manor.Runtime;

namespace Manor.Editor
{
    public static class ManorProjectAutomation
    {
        private const string Root = "Assets/_Project";
        private const string ProductionScenes = Root + "/Scenes/Production";
        private const string TestScenes = Root + "/Scenes/Tests";

        private enum DoorSide
        {
            North,
            South,
            East,
            West
        }

        [MenuItem("庄园/自动化/初始化项目")]
        public static void Initialize()
        {
            EnsureFolders();
            EnsureProductionScenes();
            CreateTestScenes();
            ConfigureBuildSettings();
            ValidateProject();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Manor][Automation] 初始化完成 / Initialization completed.");
        }

        [MenuItem("庄园/自动化/创建测试场景")]
        public static void CreateTestScenes()
        {
            EnsureFolders();
            CreateFoundationTestScenes();
            CreateSimpleTestScene("TST_AI_追逐测试", "AITestArea_追逐测试区域");
            CreateSimpleTestScene("TST_UI_界面测试", "UITestArea_界面测试区域");
            CreateSimpleTestScene("TST_Loading_加载测试", "LoadingTestArea_加载测试区域");
            CreateSimpleTestScene("TST_Audio_音频测试", "AudioTestArea_音频测试区域");
            CreateSimpleTestScene("TST_Performance_性能测试", "PerformanceTestArea_性能测试区域");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Manor][Automation] 测试场景创建完成 / Test scenes created.");
        }

        [MenuItem("庄园/自动化/重建庄园 Demo 灰盒场景")]
        public static void RebuildManorDemoGreybox()
        {
            EnsureFolders();
            CreateManorDemoScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Manor][Automation] 庄园 Demo 灰盒场景已重建 / Manor demo greybox rebuilt.");
        }

        [MenuItem("庄园/自动化/导出庄园灰盒俯视图")]
        public static void CaptureManorGreyboxViews()
        {
            Scene scene = EditorSceneManager.OpenScene(ProductionScenes + "/SCN_ManorDemo_庄园Demo.unity", OpenSceneMode.Single);
            Transform environment = FindNamedTransform(scene.GetRootGameObjects()[0].transform, "Environment_环境");
            if (environment == null) throw new InvalidOperationException("找不到环境节点 / Environment root missing.");

            string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../AutomationLogs/GreyboxCaptures"));
            Directory.CreateDirectory(outputDirectory);
            CaptureGreyboxGroup(environment, "GroundLevel_一层区域", new Vector3(0f, 32f, 0.5f), 18f, Path.Combine(outputDirectory, "ManorGreybox_Ground_一层.png"));
            CaptureGreyboxGroup(environment, "UpperLevel_二层区域", new Vector3(0f, 32f, -1f), 18f, Path.Combine(outputDirectory, "ManorGreybox_Upper_二层.png"));
            CaptureGreyboxGroup(environment, "BasementLevel_地下区域", new Vector3(6.5f, 28f, 16f), 14f, Path.Combine(outputDirectory, "ManorGreybox_Basement_地下.png"));
            CaptureGreyboxGroup(environment, "OutdoorRoute_室外路线", new Vector3(-2f, 34f, 22f), 20f, Path.Combine(outputDirectory, "ManorGreybox_Outdoor_室外.png"));
            Debug.Log("[Manor][Automation] 灰盒俯视图已导出 / Greybox captures exported: " + outputDirectory);
        }

        [MenuItem("庄园/自动化/创建基础可玩骨架测试场景")]
        public static void CreateFoundationTestScenes()
        {
            EnsureFolders();
            CreateFoundationScene("TST_Player_玩家测试", false);
            CreateFoundationScene("TST_Interaction_交互测试", true);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Manor][Automation] 基础可玩骨架测试场景已更新 / Foundation test scenes updated.");
        }

        [MenuItem("庄园/自动化/验证项目")]
        public static void ValidateProject()
        {
            List<string> errors = new List<string>();
            string[] requiredFolders =
            {
                Root, Root + "/Scenes/Production", Root + "/Scenes/Tests",
                Root + "/Scripts/Core", Root + "/Scripts/Gameplay", Root + "/Scripts/Player",
                Root + "/Scripts/AI", Root + "/Scripts/Narrative", Root + "/Scripts/UI"
            };

            foreach (string folder in requiredFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) errors.Add("缺少文件夹 / Missing folder: " + folder);
            }

            string[] requiredScenes =
            {
                ProductionScenes + "/SCN_Boot_启动场景.unity",
                ProductionScenes + "/SCN_MainMenu_主菜单.unity",
                ProductionScenes + "/SCN_ManorDemo_庄园Demo.unity"
            };

            foreach (string scene in requiredScenes)
            {
                if (!File.Exists(scene)) errors.Add("缺少场景 / Missing scene: " + scene);
            }

            if (EditorBuildSettings.scenes.Length < 3)
                errors.Add("Build Settings 场景数量不足 / Build Settings has fewer than 3 scenes.");

            ValidateInputAsset(errors);
            ValidateFoundationTestScene(errors, TestScenes + "/TST_Player_玩家测试.unity", "Player_汤姆测试胶囊");
            ValidateFoundationTestScene(errors, TestScenes + "/TST_Interaction_交互测试.unity", "Door_测试门");
            ValidateSceneReferences(errors);

            if (errors.Count == 0)
            {
                Debug.Log("[Manor][Validation] 项目检查通过 / Project validation passed.");
                return;
            }

            foreach (string error in errors) Debug.LogError("[Manor][Validation] " + error);
            throw new InvalidOperationException("《庄园》项目验证失败，共 " + errors.Count + " 个问题 / Validation failed.");
        }

        [MenuItem("庄园/自动化/导出场景层级")]
        public static void ExportSceneHierarchy()
        {
            string outputFolder = "AutomationLogs";
            Directory.CreateDirectory(outputFolder);
            string path = Path.Combine(outputFolder, "scene_hierarchy.txt");
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
                {
                    Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                    writer.WriteLine("SCENE: " + buildScene.path);
                    foreach (GameObject root in scene.GetRootGameObjects()) WriteHierarchy(writer, root, 0);
                    writer.WriteLine();
                }
            }
            Debug.Log("[Manor][Automation] 场景层级已导出 / Scene hierarchy exported: " + path);
        }

        [MenuItem("庄园/自动化/构建 Windows")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            EditorBuildSettingsScene[] enabledScenes = Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled);
            string[] scenes = Array.ConvertAll(enabledScenes, scene => scene.path);
            if (scenes.Length == 0) throw new InvalidOperationException("没有可构建场景 / No scenes configured.");
            BuildPipeline.BuildPlayer(scenes, "Builds/Windows/ManorDemo.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log("[Manor][Automation] Windows 构建完成 / Windows build completed.");
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Art/Characters", "Art/Environment", "Art/Materials", "Art/Textures", "Art/VFX", "Art/UI",
                "Audio/Ambience", "Audio/SFX", "Audio/Voices", "Audio/Music",
                "Data/Clues", "Data/Story", "Data/Objectives", "Data/Save", "Data/Settings",
                "Prefabs/Actors", "Prefabs/Interactables", "Prefabs/Environment", "Prefabs/Gameplay", "Prefabs/UI",
                "Scenes/Production", "Scenes/Tests", "Scripts/Core", "Scripts/Gameplay", "Scripts/Player",
                "Scripts/AI", "Scripts/Narrative", "Scripts/UI", "Scripts/Audio", "Scripts/Debug", "Scripts/Editor",
                "Scripts/Runtime", "Settings", "Tests", "Tests/EditMode", "Tests/PlayMode", "ThirdParty"
            };

            foreach (string relative in folders)
            {
                string path = Root + "/" + relative;
                if (AssetDatabase.IsValidFolder(path)) continue;
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            }
        }

        private static void EnsureProductionScenes()
        {
            if (!File.Exists(ProductionScenes + "/SCN_Boot_启动场景.unity")) CreateBootScene();
            if (!File.Exists(ProductionScenes + "/SCN_MainMenu_主菜单.unity")) CreateMainMenuScene();
            if (!File.Exists(ProductionScenes + "/SCN_ManorDemo_庄园Demo.unity")) CreateManorDemoScene();
        }

        private static void CreateBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Boot_初始化入口");
            CreateChild(root, "SceneFlowService_场景流程服务");
            SaveScene(scene, ProductionScenes + "/SCN_Boot_启动场景.unity");
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("MainMenu_主菜单");
            CreateCamera("MainMenuCamera_主菜单摄像机", root.transform, new Vector3(0f, 1.6f, -6f));
            CreateLight("MainMenuLight_主菜单灯光", root.transform, new Vector3(0f, 3f, -2f));
            SaveScene(scene, ProductionScenes + "/SCN_MainMenu_主菜单.unity");
        }

        private static void CreateManorDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("ManorDemo_庄园Demo");
            string[] groups = { "Environment_环境", "Navigation_导航", "Gameplay_玩法", "Narrative_叙事", "Actors_角色", "Audio_音频", "Lighting_灯光", "Debug_调试" };
            foreach (string group in groups) CreateChild(root, group);
            Transform environment = root.transform.Find("Environment_环境");
            Transform gameplay = root.transform.Find("Gameplay_玩法");
            Transform actors = root.transform.Find("Actors_角色");
            Transform lighting = root.transform.Find("Lighting_灯光");

            CreateLight("KeyLight_主灯光", lighting, new Vector3(0f, 8f, -2f));
            CreateGreyboxManor(environment);

            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "/Settings/IA_Player_玩家输入.inputactions");
            CreateFoundationPlayer(actors, inputAsset, out PlayerInputReader input, out InteractionScanner scanner);
            input.transform.position = new Vector3(-10.5f, 0.05f, -4.5f);
            CreateFoundationHud(root.transform, input, scanner);

            GameObject spawn = CreateChild(gameplay.gameObject, "PlayerSpawn_玩家出生点");
            spawn.transform.position = new Vector3(-10.5f, 0.1f, -4.5f);
            SaveScene(scene, ProductionScenes + "/SCN_ManorDemo_庄园Demo.unity");
        }

        private static void CreateGreyboxManor(Transform parent)
        {
            GameObject ground = CreateChild(parent.gameObject, "GroundLevel_一层区域");
            GameObject upper = CreateChild(parent.gameObject, "UpperLevel_二层区域");
            GameObject basement = CreateChild(parent.gameObject, "BasementLevel_地下区域");
            GameObject outdoor = CreateChild(parent.gameObject, "OutdoorRoute_室外路线");

            CreateGreyboxFloor("GroundFloor_一层主地面", ground.transform, new Vector3(0f, -0.1f, 0.5f), new Vector3(28f, 0.2f, 16f));
            CreateRoom("EntranceHall_玄关", ground.transform, new Vector3(-10.5f, 0f, -4.5f), new Vector2(7f, 6f), DoorSide.North);
            CreateRoom("LivingRoom_客厅", ground.transform, new Vector3(-3f, 0f, -4.5f), new Vector2(8f, 6f), DoorSide.North);
            CreateRoom("Kitchen_厨房", ground.transform, new Vector3(5f, 0f, -4.5f), new Vector2(8f, 6f), DoorSide.North);
            CreateRoom("StorageRoom_储藏室", ground.transform, new Vector3(11.5f, 0f, -4.5f), new Vector2(5f, 6f), DoorSide.North);
            CreateOpenCorridor("MainCorridor_主走廊", ground.transform, new Vector3(0f, 0f, 0f), new Vector2(28f, 3f));
            CreateRoom("Stairwell_楼梯间", ground.transform, new Vector3(-11f, 0f, 5f), new Vector2(6f, 7f), DoorSide.South);
            CreateRoom("BackDoorPassage_后门通道", ground.transform, new Vector3(-4.5f, 0f, 5f), new Vector2(7f, 7f), DoorSide.South, DoorSide.North);
            CreateRoom("TransitionRoom_地下入口上方过渡房", ground.transform, new Vector3(6.5f, 0f, 5f), new Vector2(15f, 7f), DoorSide.South, DoorSide.North);

            CreateStaircase("MainStair_主楼梯", ground.transform, new Vector3(-11f, 0f, 2.1f), Vector3.forward, 18, 0.2f, 0.32f, 2.4f);
            CreateConnector("UpperLanding_二层楼梯平台", upper.transform, new Vector3(-11f, 3.5f, 7.8f), new Vector3(4f, 0.2f, 1.2f));
            CreateConnector("UpperSideWalkway_二层楼梯侧走道", upper.transform, new Vector3(-8.5f, 3.5f, 4.65f), new Vector3(2f, 0.2f, 7.5f));
            CreateConnector("UpperCorridor_二层走廊", upper.transform, new Vector3(0f, 3.5f, 0f), new Vector3(28f, 0.2f, 3f));
            CreateRoom("PregnantRoom_孕妇房间", upper.transform, new Vector3(-10.5f, 3.6f, -4.5f), new Vector2(7f, 6f), DoorSide.North);
            CreateRoom("ChildrenRoom_儿童房", upper.transform, new Vector3(-3.5f, 3.6f, -4.5f), new Vector2(7f, 6f), DoorSide.North);
            CreateRoom("PrayerRoom_祷告室", upper.transform, new Vector3(3.5f, 3.6f, -4.5f), new Vector2(7f, 6f), DoorSide.North);
            CreateRoom("ButcherStudy_屠夫研究室", upper.transform, new Vector3(10.5f, 3.6f, -4.5f), new Vector2(7f, 6f), DoorSide.North);
            CreateWall("UpperRail_North_二层北侧护栏", upper.transform, new Vector3(2f, 4.15f, 1.5f), new Vector3(24f, 1.1f, 0.15f));
            CreateWall("UpperRail_West_二层楼梯护栏", upper.transform, new Vector3(-13.9f, 4.15f, 1.5f), new Vector3(0.15f, 1.1f, 3f));

            CreateStaircase("BasementStair_地下入口楼梯", basement.transform, new Vector3(6.5f, 0f, 8.2f), Vector3.forward, 15, 0.2f, 0.36f, 2.2f, true);
            CreateConnector("BasementEntrance_地下入口", basement.transform, new Vector3(6.5f, -3.1f, 13.2f), new Vector3(4f, 0.2f, 2f));
            CreateTunnel("BasementPassage_地下连接通道", basement.transform, new Vector3(6.5f, -3f, 16f), new Vector2(4f, 5.6f), DoorSide.North, DoorSide.South);
            CreateRoom("RitualChamber_地下祭祀室", basement.transform, new Vector3(6.5f, -3f, 22f), new Vector2(14f, 7f), DoorSide.South);

            CreateGreyboxFloor("Courtyard_庭院地面", outdoor.transform, new Vector3(-2f, -0.1f, 15f), new Vector3(28f, 0.2f, 13f));
            CreateConnector("BackDoorThreshold_后门门槛", outdoor.transform, new Vector3(-4.5f, -0.05f, 8.8f), new Vector3(2.2f, 0.1f, 1f));
            CreateOutdoorShelter("Shed_A_废弃木屋A", outdoor.transform, new Vector3(-10f, 0f, 15f), new Vector2(6f, 5f), DoorSide.East);
            CreateOutdoorShelter("Shed_B_废弃木屋B", outdoor.transform, new Vector3(5f, 0f, 15f), new Vector2(6f, 5f), DoorSide.West);
            CreateOutdoorMarker("JunkArea_杂物区域", outdoor.transform, new Vector3(11f, 0f, 18f), new Vector3(4f, 2f, 4f));
            CreateConnector("CourtyardExit_庭院出口", outdoor.transform, new Vector3(-2f, -0.05f, 22f), new Vector3(5f, 0.1f, 2f));
            CreateConnector("BrokenBridge_断桥灰盒测试桥面", outdoor.transform, new Vector3(-2f, -0.05f, 26f), new Vector3(4f, 0.1f, 6f));
            CreateConnector("ManorGateRoad_庄园大门道路", outdoor.transform, new Vector3(-2f, -0.05f, 32f), new Vector3(6f, 0.1f, 6f));
            CreateWall("CourtyardFence_West_庭院西侧围栏", outdoor.transform, new Vector3(-16f, 1.2f, 17f), new Vector3(0.2f, 2.4f, 17f));
            CreateWall("CourtyardFence_East_庭院东侧围栏", outdoor.transform, new Vector3(12f, 1.2f, 17f), new Vector3(0.2f, 2.4f, 17f));

            CreateAreaMarker("RouteStart_探索起点", ground.transform, new Vector3(-10.5f, 0.2f, -4.5f));
            CreateAreaMarker("GroundRouteComplete_一层贯通检查点", ground.transform, new Vector3(-4.5f, 0.2f, 7.8f));
            CreateAreaMarker("UpperRouteComplete_二层贯通检查点", upper.transform, new Vector3(10.5f, 3.8f, -4.5f));
            CreateAreaMarker("BasementRouteComplete_地下贯通检查点", basement.transform, new Vector3(6.5f, -2.8f, 22f));
            CreateAreaMarker("BridgeMechanism_桥梁机关位置", outdoor.transform, new Vector3(-2f, 0.2f, 24f));
            CreateAreaMarker("Gate_庄园大门", outdoor.transform, new Vector3(-2f, 0.2f, 35f));
        }

        private static GameObject CreateGreyboxFloor(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.transform.SetParent(parent);
            floor.transform.localPosition = position;
            floor.transform.localScale = scale;
            return floor;
        }

        private static GameObject CreateRoom(string name, Transform parent, Vector3 center, Vector2 size, params DoorSide[] doorSides)
        {
            GameObject room = new GameObject(name);
            room.transform.SetParent(parent);
            room.transform.position = center;
            CreateGreyboxFloor(name + "_Floor_房间地面", room.transform, new Vector3(0f, -0.1f, 0f), new Vector3(size.x, 0.2f, size.y));
            float wallHeight = 3f;
            float wallThickness = 0.25f;
            CreateWallSide(name, room.transform, DoorSide.North, size, wallHeight, wallThickness, HasDoor(doorSides, DoorSide.North));
            CreateWallSide(name, room.transform, DoorSide.South, size, wallHeight, wallThickness, HasDoor(doorSides, DoorSide.South));
            CreateWallSide(name, room.transform, DoorSide.East, size, wallHeight, wallThickness, HasDoor(doorSides, DoorSide.East));
            CreateWallSide(name, room.transform, DoorSide.West, size, wallHeight, wallThickness, HasDoor(doorSides, DoorSide.West));
            CreateAreaMarker(name + "_Marker_区域标识", room.transform, new Vector3(0f, 0.2f, 0f));
            return room;
        }

        private static GameObject CreateTunnel(string name, Transform parent, Vector3 center, Vector2 size, params DoorSide[] doorSides)
        {
            return CreateRoom(name, parent, center, size, doorSides);
        }

        private static GameObject CreateOpenCorridor(string name, Transform parent, Vector3 center, Vector2 size)
        {
            GameObject corridor = new GameObject(name);
            corridor.transform.SetParent(parent);
            corridor.transform.position = center;
            CreateGreyboxFloor(name + "_Floor_走廊地面", corridor.transform, new Vector3(0f, -0.1f, 0f), new Vector3(size.x, 0.2f, size.y));
            CreateWall(name + "_Wall_West_西端墙", corridor.transform, new Vector3(-size.x * 0.5f, 1.5f, 0f), new Vector3(0.25f, 3f, size.y));
            CreateWall(name + "_Wall_East_东端墙", corridor.transform, new Vector3(size.x * 0.5f, 1.5f, 0f), new Vector3(0.25f, 3f, size.y));
            CreateAreaMarker(name + "_Marker_区域标识", corridor.transform, new Vector3(0f, 0.2f, 0f));
            return corridor;
        }

        private static GameObject CreateOutdoorShelter(string name, Transform parent, Vector3 center, Vector2 size, params DoorSide[] doorSides)
        {
            return CreateRoom(name, parent, center, size, doorSides);
        }

        private static void CreateWallSide(string roomName, Transform parent, DoorSide side, Vector2 size, float height, float thickness, bool hasDoor)
        {
            float length = side == DoorSide.North || side == DoorSide.South ? size.x : size.y;
            Vector3 axis = side == DoorSide.North || side == DoorSide.South ? Vector3.right : Vector3.forward;
            Vector3 wallScale = side == DoorSide.North || side == DoorSide.South
                ? new Vector3(length, height, thickness)
                : new Vector3(thickness, height, length);
            Vector3 wallCenter = side switch
            {
                DoorSide.North => new Vector3(0f, height * 0.5f, size.y * 0.5f),
                DoorSide.South => new Vector3(0f, height * 0.5f, -size.y * 0.5f),
                DoorSide.East => new Vector3(size.x * 0.5f, height * 0.5f, 0f),
                _ => new Vector3(-size.x * 0.5f, height * 0.5f, 0f)
            };

            if (!hasDoor)
            {
                CreateWall(roomName + "_Wall_" + side + "_整墙", parent, wallCenter, wallScale);
                return;
            }

            const float doorWidth = 1.8f;
            const float doorHeight = 2.2f;
            float segmentLength = Mathf.Max(0.1f, (length - doorWidth) * 0.5f);
            Vector3 segmentScale = side == DoorSide.North || side == DoorSide.South
                ? new Vector3(segmentLength, height, thickness)
                : new Vector3(thickness, height, segmentLength);
            float segmentOffset = (doorWidth + segmentLength) * 0.5f;
            CreateWall(roomName + "_Wall_" + side + "_门洞左段", parent, wallCenter - axis * segmentOffset, segmentScale);
            CreateWall(roomName + "_Wall_" + side + "_门洞右段", parent, wallCenter + axis * segmentOffset, segmentScale);
            Vector3 lintelScale = side == DoorSide.North || side == DoorSide.South
                ? new Vector3(doorWidth, height - doorHeight, thickness)
                : new Vector3(thickness, height - doorHeight, doorWidth);
            Vector3 lintelCenter = wallCenter + Vector3.up * (doorHeight * 0.5f);
            CreateWall(roomName + "_Wall_" + side + "_门楣", parent, lintelCenter, lintelScale);
            CreateAreaMarker(roomName + "_Doorway_" + side + "_门洞", parent, wallCenter + Vector3.down * (height * 0.5f));
        }

        private static bool HasDoor(DoorSide[] doorSides, DoorSide target)
        {
            return doorSides != null && Array.IndexOf(doorSides, target) >= 0;
        }

        private static GameObject CreateStaircase(string name, Transform parent, Vector3 start, Vector3 direction, int stepCount, float rise, float run, float width, bool descending = false)
        {
            GameObject staircase = new GameObject(name);
            staircase.transform.SetParent(parent);
            direction = direction.normalized;
            for (int index = 0; index < stepCount; index++)
            {
                float blockHeight = descending ? rise : (index + 1) * rise;
                float verticalCenter = descending ? -(index + 0.5f) * rise : blockHeight * 0.5f;
                CreateGreyboxFloor(name + "_Step_台阶_" + (index + 1).ToString("00"), staircase.transform,
                    start + direction * (index * run) + Vector3.up * verticalCenter,
                    new Vector3(width, blockHeight, run + 0.03f));
            }

            CreateAreaMarker(name + "_Bottom_底部", staircase.transform, start);
            CreateAreaMarker(name + "_Top_顶部", staircase.transform, start + direction * ((stepCount - 1) * run) + Vector3.up * (descending ? -stepCount * rise : stepCount * rise));
            return staircase;
        }

        private static GameObject CreateOutdoorMarker(string name, Transform parent, Vector3 position, Vector3 size)
        {
            GameObject area = new GameObject(name);
            area.transform.SetParent(parent);
            area.transform.position = position;
            CreateGreyboxFloor(name + "_Floor_区域地面", area.transform, Vector3.zero, new Vector3(size.x, 0.2f, size.z));
            CreateWall(name + "_BackWall_区域边界", area.transform, new Vector3(0f, size.y * 0.5f, size.z * 0.5f), new Vector3(size.x, size.y, 0.2f));
            CreateAreaMarker(name + "_Marker_区域标识", area.transform, new Vector3(0f, 0.2f, 0f));
            return area;
        }

        private static GameObject CreateConnector(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject connector = new GameObject(name);
            connector.transform.SetParent(parent);
            connector.transform.position = position;
            CreateGreyboxFloor(name + "_Floor_连接地面", connector.transform, Vector3.zero, scale);
            CreateAreaMarker(name + "_Marker_连接标识", connector.transform, new Vector3(0f, 0.2f, 0f));
            return connector;
        }

        private static GameObject CreateWall(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            return wall;
        }

        private static GameObject CreateAreaMarker(string name, Transform parent, Vector3 position)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent);
            marker.transform.localPosition = position;
            return marker;
        }

        private static void CreateSimpleTestScene(string sceneName, string rootName)
        {
            string path = TestScenes + "/" + sceneName + ".unity";
            if (File.Exists(path)) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject(rootName);
            CreateCamera("TestCamera_测试摄像机", root.transform, new Vector3(0f, 1.6f, -6f));
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TestFloor_测试地面";
            floor.transform.SetParent(root.transform);
            SaveScene(scene, path);
        }

        private static void CreateFoundationScene(string sceneName, bool includeInteractables)
        {
            string path = TestScenes + "/" + sceneName + ".unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject(includeInteractables ? "InteractionTestArea_交互测试区域" : "PlayerTestArea_玩家测试区域");
            root.AddComponent<FoundationTestBootstrap>();
            CreateFoundationLighting(root.transform);
            CreateFoundationEnvironment(root.transform, includeInteractables);
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "/Settings/IA_Player_玩家输入.inputactions");
            CreateFoundationPlayer(root.transform, inputAsset, out PlayerInputReader input, out InteractionScanner scanner);
            CreateFoundationHud(root.transform, input, scanner);
            if (includeInteractables) CreateFoundationInteractables(root.transform);
            SaveScene(scene, path);
        }

        private static void CreateFoundationLighting(Transform parent)
        {
            GameObject lightObject = CreateLight("TestLight_测试灯光", parent, new Vector3(0f, 5f, 0f));
            lightObject.GetComponent<Light>().intensity = 1.2f;
        }

        private static void CreateFoundationEnvironment(Transform parent, bool includeSightBlocker)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TestFloor_测试地面";
            floor.transform.SetParent(parent);
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "TestWall_测试墙体";
            backWall.transform.SetParent(parent);
            backWall.transform.position = new Vector3(0f, 1.5f, 6f);
            backWall.transform.localScale = new Vector3(8f, 3f, 0.25f);

            if (!includeSightBlocker) return;
            GameObject sideWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideWall.name = "SightBlocker_视线遮挡测试墙";
            sideWall.transform.SetParent(parent);
            sideWall.transform.position = new Vector3(3f, 1f, 2f);
            sideWall.transform.localScale = new Vector3(0.25f, 2f, 3f);
        }

        private static void CreateFoundationPlayer(Transform parent, InputActionAsset inputAsset, out PlayerInputReader input, out InteractionScanner scanner)
        {
            GameObject player = new GameObject("Player_汤姆测试胶囊");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(0f, 0.05f, -4f);
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.stepOffset = 0.3f;

            input = player.AddComponent<PlayerInputReader>();
            input.Configure(inputAsset);
            GameObject cameraObject = new GameObject("PlayerCamera_玩家摄像机");
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            FirstPersonCamera view = cameraObject.AddComponent<FirstPersonCamera>();
            view.Configure(player.transform);
            scanner = player.AddComponent<InteractionScanner>();
            scanner.Configure(camera, null, 2.5f);
            FirstPersonController controller = player.AddComponent<FirstPersonController>();
            controller.Configure(input, view, scanner);
        }

        private static void CreateFoundationInteractables(Transform parent)
        {
            GameObject doorPivot = new GameObject("DoorPivot_测试门轴");
            doorPivot.transform.SetParent(parent);
            doorPivot.transform.position = new Vector3(0f, 0f, 3f);
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door_测试门";
            door.transform.SetParent(doorPivot.transform);
            door.transform.localPosition = new Vector3(0.75f, 1f, 0f);
            door.transform.localScale = new Vector3(1.5f, 2f, 0.15f);
            DoorInteractable doorInteractable = door.AddComponent<DoorInteractable>();
            doorInteractable.ConfigureDoor("INT_DOOR_FOUNDATION_01", doorPivot.transform, "KEY_FOUNDATION_01");

            GameObject keyPedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keyPedestal.name = "KeyPedestal_钥匙测试台";
            keyPedestal.transform.SetParent(parent);
            keyPedestal.transform.position = new Vector3(-1.5f, 0.5f, 0f);
            keyPedestal.transform.localScale = new Vector3(1f, 1f, 1f);

            GameObject key = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            key.name = "Key_测试钥匙";
            key.transform.SetParent(parent);
            key.transform.position = new Vector3(-1.5f, 1.65f, 0f);
            key.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            key.transform.localScale = new Vector3(0.3f, 0.08f, 0.3f);
            KeyInteractable keyInteractable = key.AddComponent<KeyInteractable>();
            keyInteractable.ConfigureKey("INT_KEY_FOUNDATION_01", "KEY_FOUNDATION_01");

            GameObject notePedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            notePedestal.name = "NotePedestal_纸条测试台";
            notePedestal.transform.SetParent(parent);
            notePedestal.transform.position = new Vector3(1.5f, 0.5f, 0f);
            notePedestal.transform.localScale = new Vector3(1f, 1f, 1f);

            GameObject note = GameObject.CreatePrimitive(PrimitiveType.Cube);
            note.name = "Note_测试纸条";
            note.transform.SetParent(parent);
            note.transform.position = new Vector3(1.5f, 1.65f, 0f);
            note.transform.localScale = new Vector3(0.7f, 0.5f, 0.04f);
            NoteInteractable noteInteractable = note.AddComponent<NoteInteractable>();
            noteInteractable.ConfigureNote("INT_NOTE_FOUNDATION_01", "CLUE_NOTE_FOUNDATION_01", "TEST_NOTE_TITLE", "TEST_NOTE_BODY");
        }

        private static void CreateFoundationHud(Transform parent, PlayerInputReader input, InteractionScanner scanner)
        {
            GameObject hudObject = new GameObject("HUD_基础界面");
            hudObject.transform.SetParent(parent);
            RuntimeHud hud = hudObject.AddComponent<RuntimeHud>();
            hud.Configure(scanner);
            CluePanelController clues = hudObject.AddComponent<CluePanelController>();
            clues.Configure(input);
            PauseMenuController pause = hudObject.AddComponent<PauseMenuController>();
            pause.Configure(input, clues);
            hudObject.AddComponent<DeathRetryController>();
        }

        private static void ValidateInputAsset(List<string> errors)
        {
            string path = Root + "/Settings/IA_Player_玩家输入.inputactions";
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (asset == null) errors.Add("缺少输入资产 / Missing input asset: " + path);
        }

        private static void ValidateFoundationTestScene(List<string> errors, string scenePath, string requiredObjectName)
        {
            if (!File.Exists(scenePath))
            {
                errors.Add("缺少基础测试场景 / Missing foundation test scene: " + scenePath);
                return;
            }

            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject found = Array.Find(scene.GetRootGameObjects(), root => FindNamedTransform(root.transform, requiredObjectName) != null);
            if (found == null) errors.Add("测试场景缺少对象 / Test scene missing object: " + requiredObjectName);
            if (previousScene.IsValid() && !string.IsNullOrEmpty(previousScene.path)) EditorSceneManager.OpenScene(previousScene.path, OpenSceneMode.Single);
        }

        private static void ValidateSceneReferences(List<string> errors)
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { Root + "/Scenes" });
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (GameObject rootObject in scene.GetRootGameObjects()) ValidateGameObjectReferences(rootObject, scenePath, errors);
            }
        }

        private static void ValidateGameObjectReferences(GameObject gameObject, string scenePath, List<string> errors)
        {
            MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    errors.Add("Missing Script: " + scenePath + " / " + GetHierarchyPath(gameObject.transform));
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject(behaviour);
                SerializedProperty iterator = serializedObject.GetIterator();
                while (iterator.NextVisible(true))
                {
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (iterator.objectReferenceValue == null && iterator.objectReferenceEntityIdValue != 0)
                        errors.Add("Missing Reference: " + scenePath + " / " + GetHierarchyPath(gameObject.transform) + " / " + iterator.propertyPath);
                }
            }

            foreach (Transform child in gameObject.transform) ValidateGameObjectReferences(child.gameObject, scenePath, errors);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private static Transform FindNamedTransform(Transform root, string objectName)
        {
            if (root.name == objectName) return root;
            foreach (Transform child in root)
            {
                Transform result = FindNamedTransform(child, objectName);
                if (result != null) return result;
            }

            return null;
        }

        private static GameObject CreateCamera(string name, Transform parent, Vector3 position)
        {
            GameObject camera = new GameObject(name);
            camera.AddComponent<Camera>();
            camera.transform.SetParent(parent);
            camera.transform.position = position;
            return camera;
        }

        private static GameObject CreateLight(string name, Transform parent, Vector3 position)
        {
            GameObject lightObject = new GameObject(name);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.SetParent(parent);
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

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void ConfigureBuildSettings()
        {
            string[] paths =
            {
                ProductionScenes + "/SCN_Boot_启动场景.unity",
                ProductionScenes + "/SCN_MainMenu_主菜单.unity",
                ProductionScenes + "/SCN_ManorDemo_庄园Demo.unity"
            };
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in paths) scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void CaptureGreyboxGroup(Transform environment, string groupName, Vector3 cameraPosition, float orthographicSize, string outputPath)
        {
            Dictionary<GameObject, bool> activeStates = new Dictionary<GameObject, bool>();
            Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
            foreach (Transform child in environment)
            {
                activeStates[child.gameObject] = child.gameObject.activeSelf;
                child.gameObject.SetActive(child.name == groupName);
            }

            Shader captureShader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            Material floorMaterial = new Material(captureShader) { color = new Color(0.29f, 0.34f, 0.33f) };
            Material wallMaterial = new Material(captureShader) { color = new Color(0.07f, 0.09f, 0.10f) };
            Material routeMaterial = new Material(captureShader) { color = new Color(0.52f, 0.39f, 0.19f) };
            Transform visibleGroup = FindNamedTransform(environment, groupName);
            foreach (Renderer renderer in visibleGroup.GetComponentsInChildren<Renderer>(true))
            {
                originalMaterials[renderer] = renderer.sharedMaterial;
                string objectName = renderer.gameObject.name;
                renderer.sharedMaterial = objectName.Contains("Wall_") || objectName.Contains("Rail_") || objectName.Contains("Fence_")
                    ? wallMaterial
                    : objectName.Contains("连接") || objectName.Contains("道路") || objectName.Contains("桥") || objectName.Contains("Step_")
                        ? routeMaterial
                        : floorMaterial;
            }

            GameObject cameraObject = new GameObject("GreyboxCaptureCamera_灰盒截图摄像机");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f);
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            RenderTexture target = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(outputPath, image.EncodeToPNG());

            RenderTexture.active = null;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            foreach (KeyValuePair<Renderer, Material> state in originalMaterials) state.Key.sharedMaterial = state.Value;
            UnityEngine.Object.DestroyImmediate(floorMaterial);
            UnityEngine.Object.DestroyImmediate(wallMaterial);
            UnityEngine.Object.DestroyImmediate(routeMaterial);
            foreach (KeyValuePair<GameObject, bool> state in activeStates) state.Key.SetActive(state.Value);
        }

        private static void WriteHierarchy(StreamWriter writer, GameObject obj, int depth)
        {
            writer.WriteLine(new string(' ', depth * 2) + "- " + obj.name);
            foreach (Transform child in obj.transform) WriteHierarchy(writer, child.gameObject, depth + 1);
        }
    }
}
#endif
