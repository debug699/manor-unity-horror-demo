#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Manor.Gameplay;
using Manor.Narrative;
using Manor.Player;
using Manor.Runtime;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace Manor.Editor
{
    public static class ManorOutdoorV1Builder
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";
        private const string Models = Root + "/Art/Environment/Models";
        private const string Textures = Root + "/Art/Textures/Environment";
        private const string Materials = Root + "/Art/Materials";
        private const string Prefabs = Root + "/Prefabs/Environment";
        private const string UserLibrary = Root + "/Art/Environment/SourceModels/UserLibrary";
        private const string NewManorUnityModel = Root + "/Art/Environment/DerivedModels/新版主楼/新版主楼_UnityCompatible.fbx";
        private const string O03DoorwayModel = Root + "/Art/Environment/DerivedModels/废弃木屋主用/ai查看编辑_UnityDoorway.fbx";
        private const string O02O04DoorModel = Root + "/Art/Environment/DerivedModels/门/木门1_UnityCompatible.fbx";
        private const string O03DoorModel = Root + "/Art/Environment/DerivedModels/门/木门2_UnityCompatible.fbx";
        private const string ManagedRoot = "OutdoorArt_v1_首版室外美术";
        private const string InteriorArtRoot = "InteriorArt_v2_连续主楼陈设";

        internal static GameObject CreateInteriorAuthoredDoorLeaf(Transform pivot, string name, float apertureWidth, float apertureHeight, string sourceOverride = null)
        {
            string roomId = name.StartsWith("Door_G", StringComparison.Ordinal) ? name.Substring("Door_".Length) : null;
            string roomFile = roomId switch
            {
                "G02" => "已拆开，删除椅子G02.fbx",
                "G09" => "已拆开.fbx",
                "G01" or "G03" or "G04" or "G05" or "G06" or "G07" or "G08" or "G10" or "G11" or "G12" => "已拆开" + roomId + ".fbx",
                _ => null
            };
            string explicitSourcePath = !string.IsNullOrEmpty(sourceOverride)
                ? (sourceOverride.StartsWith("Assets/", StringComparison.Ordinal) ? sourceOverride : UserLibrary + "/" + sourceOverride)
                : null;
            string sourcePath = explicitSourcePath ?? (roomId != null && roomFile != null ? UserLibrary + "/" + roomId + "/" + roomFile : null);
            GameObject source = !string.IsNullOrEmpty(sourcePath)
                ? AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath)
                : roomId != null && roomFile != null
                ? AssetDatabase.LoadAssetAtPath<GameObject>(UserLibrary + "/" + roomId + "/" + roomFile)
                : null;
            // Only fall back to the shared door set when that specific room package has no usable
            // door leaf. The room-authored door/window/cabinet set always has first priority.
            if (source == null)
            {
                sourcePath = UserLibrary + "/各种门/已拆开.fbx";
                source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            }
            if (source == null)
                return Cube(name + "_UserDoorLeaf_Fallback", pivot, new Vector3(apertureWidth * .5f, apertureHeight * .5f, 0f), new Vector3(apertureWidth, apertureHeight, .12f), null, true);
            GameObject probe = PrefabUtility.InstantiatePrefab(source) as GameObject;
            float targetAspect = apertureWidth / Mathf.Max(.001f, apertureHeight);
            Renderer selected = probe.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.bounds.size.y > .05f)
                .Where(renderer =>
                {
                    Bounds candidate = renderer.bounds;
                    float width = Mathf.Max(candidate.size.x, candidate.size.z);
                    float thickness = Mathf.Min(candidate.size.x, candidate.size.z);
                    return candidate.size.y > width * .9f && thickness < width * .45f;
                })
                // A room package can also contain tall cabinets or near-square window pieces.
                // Prefer the authored piece whose silhouette best matches this actual doorway.
                .OrderBy(renderer =>
                {
                    Bounds candidate = renderer.bounds;
                    float candidateAspect = Mathf.Max(candidate.size.x, candidate.size.z) / Mathf.Max(.001f, candidate.size.y);
                    return Mathf.Abs(Mathf.Log(Mathf.Max(.001f, candidateAspect) / targetAspect));
                })
                .ThenByDescending(renderer => renderer.bounds.size.y * Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z))
                .FirstOrDefault();
            MeshFilter sourceFilter = selected != null ? selected.GetComponent<MeshFilter>() : null;
            Mesh[] importedMeshes = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<Mesh>().ToArray();
            Mesh mesh = sourceFilter != null ? sourceFilter.sharedMesh : importedMeshes
                .OfType<Mesh>()
                .Where(candidate => candidate.bounds.size.y > .05f)
                .Where(candidate =>
                {
                    float width = Mathf.Max(candidate.bounds.size.x, candidate.bounds.size.z);
                    float thickness = Mathf.Min(candidate.bounds.size.x, candidate.bounds.size.z);
                    return candidate.bounds.size.y > width * .9f && thickness < width * .45f;
                })
                .OrderBy(candidate =>
                {
                    float candidateAspect = Mathf.Max(candidate.bounds.size.x, candidate.bounds.size.z) / Mathf.Max(.001f, candidate.bounds.size.y);
                    return Mathf.Abs(Mathf.Log(Mathf.Max(.001f, candidateAspect) / targetAspect));
                })
                .ThenByDescending(candidate => candidate.bounds.size.y * Mathf.Max(candidate.bounds.size.x, candidate.bounds.size.z))
                .FirstOrDefault();
            // Explicit user-door files are already curated door sets. Some FBX importers expose
            // their renderers with zero/rotated preview bounds; in that case use the dominant
            // authored mesh instead of silently replacing it with a generated cube.
            if (mesh == null && !string.IsNullOrEmpty(explicitSourcePath))
                mesh = importedMeshes.OrderByDescending(candidate => candidate.vertexCount).FirstOrDefault();
            if (mesh == null)
            {
                UnityEngine.Object.DestroyImmediate(probe);
                return Cube(name + "_UserDoorLeaf_Fallback", pivot, new Vector3(apertureWidth * .5f, apertureHeight * .5f, 0f), new Vector3(apertureWidth, apertureHeight, .12f), null, true);
            }
            Material[] materials = selected != null ? selected.sharedMaterials : Array.Empty<Material>();
            GameObject leaf = new GameObject("UserDoorLeaf_" + name + "_用户门扇");
            leaf.transform.SetParent(pivot, false);
            leaf.AddComponent<MeshFilter>().sharedMesh = mesh;
            leaf.AddComponent<MeshRenderer>().sharedMaterials = materials;
            Bounds meshBounds = mesh.bounds;
            bool widthAlongX = meshBounds.size.x >= meshBounds.size.z;
            float originalWidth = widthAlongX ? meshBounds.size.x : meshBounds.size.z;
            float widthScale = apertureWidth / Mathf.Max(.001f, originalWidth);
            float heightScale = apertureHeight / Mathf.Max(.001f, meshBounds.size.y);
            float thicknessScale = Mathf.Min(widthScale, heightScale);
            leaf.transform.localScale = widthAlongX
                ? new Vector3(widthScale, heightScale, thicknessScale)
                : new Vector3(thicknessScale, heightScale, widthScale);
            leaf.transform.localRotation = widthAlongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
            Vector3 hingeLocal = widthAlongX
                ? new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.center.z)
                : new Vector3(meshBounds.center.x, meshBounds.min.y, meshBounds.min.z);
            leaf.transform.localPosition = -(leaf.transform.localRotation * Vector3.Scale(hingeLocal, leaf.transform.localScale));
            BoxCollider box = leaf.AddComponent<BoxCollider>(); box.center = meshBounds.center; box.size = meshBounds.size;
            UnityEngine.Object.DestroyImmediate(probe);
            return leaf;
        }

        [MenuItem("庄园/自动化/构建首版室外庄园")]
        public static void BuildOutdoorV1()
        {
            EnsureFolders();
            ConfigureRenderPipeline();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject manorRoot = scene.GetRootGameObjects()[0];
            Transform environment = Find(manorRoot.transform, "Environment_环境");
            Transform lighting = Find(manorRoot.transform, "Lighting_灯光");
            Transform existing = Find(environment, ManagedRoot);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                Transform legacyTransition = Find(sceneRoot.transform, "ENT_进入主楼室内");
                if (legacyTransition != null) UnityEngine.Object.DestroyImmediate(legacyTransition.gameObject);
            }

            // The original greybox is the reliable continuous collision shell for both floors and
            // the basement. Keep it active; only hide the obsolete small outdoor route which is
            // replaced by the enlarged estate below.
            SetLegacyAreaEnabled(environment, "GroundLevel_一层区域", false);
            SetLegacyAreaEnabled(environment, "UpperLevel_二层区域", false);
            SetLegacyAreaEnabled(environment, "BasementLevel_地下区域", false);
            SetLegacyAreaEnabled(environment, "OutdoorRoute_室外路线", false);

            GameObject artRoot = Child(environment, ManagedRoot);
            Material ground = Material("MAT_Ground_WetMud", new Color(0.105f, 0.12f, 0.10f), 0.18f, null);
            Material path = Material("MAT_Path_MudStone", new Color(0.16f, 0.15f, 0.12f), 0.25f, null);
            Material wood = Material("MAT_Wood_WetDark", new Color(0.16f, 0.105f, 0.07f), 0.28f, "T_WoodDoor_BaseColor.jpg");
            Material house = Material("MAT_ManorHouse", Color.white, 0.24f, "T_ManorHouse_BaseColor.jpg");
            ConfigureManorFacadeCutout(house);
            Material hutA = Material("MAT_WorkshopHut", Color.white, 0.32f, "T_WorkshopHut_BaseColor.jpg");
            Material hutB = Material("MAT_AbandonedHut", Color.white, 0.35f, "T_AbandonedHut_BaseColor.jpg");
            Material propsA = Material("MAT_CourtyardProps_A", Color.white, 0.3f, "T_CourtyardProps_A_BaseColor.jpg");
            Material propsB = Material("MAT_CourtyardProps_B", Color.white, 0.3f, "T_CourtyardProps_B_BaseColor.jpg");
            Material tree = Material("MAT_DeadTree", Color.white, 0.42f, "T_DeadTree_BaseColor.jpg");
            Material stone = Material("MAT_Cliff_Damp", new Color(0.10f, 0.115f, 0.105f), 0.2f, null);
            Material interiorFloor = Material("MAT_InteriorFloor_Worn", new Color(0.12f, 0.085f, 0.055f), 0.18f, null);
            Material interiorWall = Material("MAT_InteriorWall_Mold", new Color(0.105f, 0.115f, 0.095f), 0.12f, null);
            ConfigureInteriorCameraVolume(interiorWall);
            Material basement = Material("MAT_Basement_DampStone", new Color(0.075f, 0.085f, 0.08f), 0.16f, null);

            Transform continuousLayout = ManorContinuousInteriorBuilder.Build(environment, interiorFloor, interiorWall, basement);
            ApplyContinuousShellVisualRules(continuousLayout);

            CreateTerrain(artRoot.transform, ground, path, stone);
            PlaceNewUserManor(artRoot.transform, house);
            CreateEnterableManorCollisionAndDoor(artRoot.transform, wood, ground);
            CreateG07VaultWindow(artRoot.transform, wood);
            PlaceUserAuthoredEstateAssets(artRoot.transform, wood);
            PlaceContinuousInteriorArt(environment, continuousLayout);
            CreateThreatActors(environment, continuousLayout);
            CreateChaseRouteInteractions(artRoot.transform, wood);

            Vector3[] treePositions =
            {
                new(-39f, 0f, -20f), new(-38f, 0f, 8f), new(-39f, 0f, 34f), new(39f, 0f, -14f),
                new(39f, 0f, 12f), new(38f, 0f, 35f), new(-22f, 0f, 48f), new(22f, 0f, 50f)
            };
            for (int i = 0; i < treePositions.Length; i++)
            {
                float scale = 0.85f + (i % 3) * 0.1f;
                GameObject deadTree = PlaceModel("PF_DeadTree_" + (i + 1).ToString("00"), "SM_DeadTree.fbx", artRoot.transform, treePositions[i], Vector3.one * scale, Quaternion.Euler(0f, i * 41f, 0f), tree, Vector3.zero);
                CapsuleCollider trunk = deadTree.AddComponent<CapsuleCollider>();
                trunk.radius = .45f; trunk.height = 4.2f; trunk.center = Vector3.up * 2.1f;
            }

            CreateUserEstateGate(artRoot.transform, wood);
            BakeNavigation(environment);
            ConfigureAtmosphere(lighting, artRoot.transform);
            ConfigurePlayer(manorRoot.transform);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Manor][OutdoorV1] 首版室外庄园已增量构建 / Outdoor manor v1 built incrementally.");
        }

        [MenuItem("庄园/自动化/构建并截图首版室外庄园")]
        public static void BuildAndCaptureOutdoorV1()
        {
            BuildOutdoorV1();
            Scene scene = SceneManager.GetActiveScene();
            Transform root = Find(scene.GetRootGameObjects()[0].transform, ManagedRoot);
            string output = Path.GetFullPath("AutomationLogs/OutdoorV1");
            Directory.CreateDirectory(output);
            Capture(root, new Vector3(0f, 1.7f, 34f), Quaternion.Euler(0f, 180f, 0f), 67f, Path.Combine(output, "OutdoorV1_GateView.png"));
            Capture(root, new Vector3(-1f, 2.2f, 24f), Quaternion.Euler(4f, 180f, 0f), 67f, Path.Combine(output, "OutdoorV1_CourtyardView.png"));
            Capture(root, new Vector3(0f, 48f, 12f), Quaternion.Euler(90f, 0f, 0f), 70f, Path.Combine(output, "OutdoorV1_TopView.png"));
            CaptureHeightSlice(root.root, new Vector3(0f, 16f, -1.5f), 60f, Path.Combine(output, "ContinuousManor_GroundTop.png"), -0.6f, 3.15f);
            CaptureHeightSlice(root.root, new Vector3(0f, 13f, -1.5f), 60f, Path.Combine(output, "ContinuousManor_UpperTop.png"), 3.15f, 6.9f);
            CaptureHeightSlice(root.root, new Vector3(1f, 7f, -8f), 55f, Path.Combine(output, "ContinuousManor_BasementTop.png"), -4.2f, -2.3f);
        }

        [MenuItem("庄园/自动化/逐房第一人称视觉验收截图")]
        public static void CaptureRoomFirstPersonQA()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string output = Path.GetFullPath("AutomationLogs/RoomQA");
            Directory.CreateDirectory(output);

            // Standing eye height (about 1.65 m) and a restrained 67 degree vertical FOV mirror
            // normal first-person exploration. A temporary low-intensity fill follows the camera
            // so geometry defects remain readable without changing the authored scene lighting.
            RoomShot[] shots =
            {
                new("G01_玄关", new Vector3(0f, 1.65f, 9.15f), new Vector3(0f, 1.25f, 6.9f)),
                new("G02_客厅套间", new Vector3(-4.7f, 1.65f, 3.0f), new Vector3(-11.0f, 1.15f, 3.0f)),
                new("G03_厨房套间", new Vector3(-4.7f, 1.65f, -7.0f), new Vector3(-11.0f, 1.05f, -8.2f)),
                new("G04_屠宰工作间", new Vector3(4.6f, 1.65f, -8.0f), new Vector3(10.0f, 1.05f, -8.0f)),
                new("G05_主走廊", new Vector3(0f, 1.65f, 6.2f), new Vector3(0f, 1.35f, -5.8f)),
                new("G06_楼梯间", new Vector3(7.6f, 1.65f, 7.0f), new Vector3(8.0f, 2.0f, 0.4f)),
                new("G07_后门通道", new Vector3(19f, 1.65f, 7.4f), new Vector3(19f, 1.35f, -7.5f)),
                new("G08_孕妇房间套间", new Vector3(-5.8f, 5.25f, 5.0f), new Vector3(-11f, 4.75f, 5.0f)),
                new("G09_儿童房双区", new Vector3(1.7f, 5.25f, 5.0f), new Vector3(8f, 4.75f, 5.0f)),
                new("G10_屠夫研究室", new Vector3(-5.8f, 5.25f, -3.0f), new Vector3(-11f, 4.75f, -3.0f)),
                new("G11_祷告室", new Vector3(-5.8f, 5.25f, -10.0f), new Vector3(-11f, 4.75f, -10.0f)),
                new("G12_旧客房与过渡空间", new Vector3(2.8f, 5.25f, -4.0f), new Vector3(8f, 4.75f, -4.0f)),
                new("B01_地下服务路线", new Vector3(-5.0f, -1.55f, -8.0f), new Vector3(-1.5f, -2.0f, -8.0f)),
                new("B02_地下祭祀室", new Vector3(3.4f, -1.55f, -8.0f), new Vector3(7.5f, -2.0f, -8.0f))
            };

            foreach (RoomShot shot in shots)
            {
                Quaternion rotation = Quaternion.LookRotation((shot.Target - shot.Position).normalized, Vector3.up);
                CaptureWithInspectionFill(shot.Position, rotation, Path.Combine(output, shot.FileStem + ".png"));
            }

            File.WriteAllLines(Path.Combine(output, "RoomQA_manifest.txt"), shots.Select(shot =>
                $"{shot.FileStem}.png | camera={shot.Position:F2} | target={shot.Target:F2} | 1600x900"));
            Debug.Log($"[Manor][RoomQA] Captured {shots.Length} first-person room views to {output}");
        }

        [MenuItem("庄园/自动化/外屋门洞第一人称验收截图")]
        public static void CaptureOutbuildingQA()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            LogOutbuildingGeometry("PF_UserWorkshopHut_加工木屋主用", "UserDoor_O02_Pivot");
            LogOutbuildingGeometry("PF_UserAbandonedHut_废弃木屋主用", "UserDoor_O03_Pivot");
            LogOutbuildingGeometry("PF_UserStoneWorkshop_木石工坊主用", "UserDoor_O04_Pivot");
            string output = Path.GetFullPath("AutomationLogs/OutbuildingQA");
            Directory.CreateDirectory(output);
            RoomShot[] shots =
            {
                new("O02_加工木屋正门", new Vector3(-25.16f, 1.65f, 12.6f), new Vector3(-25.16f, 1.55f, 17.49f)),
                new("O03_废弃木屋正门", new Vector3(25f, 1.65f, 28.1f), new Vector3(25f, 1.2f, 23.15f)),
                new("O04_木石工坊正门", new Vector3(34.35f, 1.65f, 9.35f), new Vector3(32.37f, .95f, 3.91f)),
                new("O02_O03_庭院关系", new Vector3(0f, 3.2f, 37f), new Vector3(0f, 1.2f, 20f))
            };
            foreach (RoomShot shot in shots)
            {
                Quaternion rotation = Quaternion.LookRotation((shot.Target - shot.Position).normalized, Vector3.up);
                CaptureWithInspectionFill(shot.Position, rotation, Path.Combine(output, shot.FileStem + ".png"));
            }
            Debug.Log($"[Manor][OutbuildingQA] Captured {shots.Length} views to {output}");
        }

        private static void LogOutbuildingGeometry(string buildingName, string pivotName)
        {
            GameObject building = GameObject.Find(buildingName);
            GameObject pivot = GameObject.Find(pivotName);
            if (building == null || pivot == null)
            {
                Debug.LogError($"[Manor][OutbuildingQA] Missing building={buildingName} or pivot={pivotName}");
                return;
            }
            Bounds buildingBounds = RendererBounds(building);
            Bounds doorBounds = RendererBounds(pivot);
            string children = string.Join(", ", pivot.GetComponentsInChildren<Transform>(true).Select(child => child.name));
            Debug.Log($"[Manor][OutbuildingQA] {buildingName} bounds center={buildingBounds.center:F3} size={buildingBounds.size:F3} min={buildingBounds.min:F3} max={buildingBounds.max:F3}; " +
                $"{pivotName} hinge={pivot.transform.position:F3} yaw={pivot.transform.eulerAngles.y:F2} door center={doorBounds.center:F3} size={doorBounds.size:F3} min={doorBounds.min:F3} max={doorBounds.max:F3}; children=[{children}]");
        }

        private readonly struct RoomShot
        {
            public RoomShot(string fileStem, Vector3 position, Vector3 target)
            {
                FileStem = fileStem;
                Position = position;
                Target = target;
            }

            public string FileStem { get; }
            public Vector3 Position { get; }
            public Vector3 Target { get; }
        }

        [MenuItem("庄园/自动化/审计主楼前立面渲染器")]
        public static void AuditFrontFacadeRenderers()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform authoredManor = Find(scene.GetRootGameObjects()[0].transform, "PF_NewUserManor_新版主楼");
            if (authoredManor != null)
            {
                Bounds manorBounds = RendererBounds(authoredManor.gameObject);
                Debug.Log($"[FacadeAudit][NewManor] position={authoredManor.position:F2} scale={authoredManor.localScale:F2} boundsCenter={manorBounds.center:F2} boundsSize={manorBounds.size:F2}");
            }
            foreach (Renderer renderer in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true)))
            {
                Bounds bounds = renderer.bounds;
                if (!renderer.enabled || bounds.max.z < 8.5f || bounds.min.z > 11.5f || bounds.max.x < -24f || bounds.min.x > 24f) continue;
                Debug.Log($"[FacadeAudit] {HierarchyPath(renderer.transform)} | center={bounds.center:F2} size={bounds.size:F2} material={renderer.sharedMaterial?.name}");
            }
        }

        [MenuItem("庄园/自动化/审计用户房间门窗柜候选")]
        public static void AuditUserRoomFunctionalPieces()
        {
            string output = Path.GetFullPath("AutomationLogs/UserRoomFunctionalPieces.tsv");
            List<string> rows = new() { "room\tpath\tname\tsize_x\tsize_y\tsize_z\tcenter_x\tcenter_y\tcenter_z" };
            foreach (string roomId in Enumerable.Range(1, 12).Select(index => "G" + index.ToString("00")))
            {
                string[] guids = AssetDatabase.FindAssets("t:Model", new[] { UserLibrary + "/" + roomId });
                if (guids.Length == 0) continue;
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                GameObject probe = PrefabUtility.InstantiatePrefab(source) as GameObject;
                foreach (Renderer renderer in probe.GetComponentsInChildren<Renderer>(true))
                {
                    Bounds bounds = renderer.bounds;
                    rows.Add($"{roomId}\t{assetPath}\t{renderer.name}\t{bounds.size.x:F4}\t{bounds.size.y:F4}\t{bounds.size.z:F4}\t{bounds.center.x:F4}\t{bounds.center.y:F4}\t{bounds.center.z:F4}");
                }
                UnityEngine.Object.DestroyImmediate(probe);
            }
            File.WriteAllLines(output, rows);
            Debug.Log("[Manor][UserAssetAudit] " + output);
        }

        [MenuItem("庄园/自动化/审计新版主楼拆分子件")]
        public static void AuditNewManorPieces()
        {
            const string assetPath = NewManorUnityModel;
            string output = Path.GetFullPath("AutomationLogs/NewManorPieces.tsv");
            List<string> rows = new() { "name\tsize_x\tsize_y\tsize_z\tcenter_x\tcenter_y\tcenter_z\tvertices" };
            foreach (Mesh mesh in AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Mesh>())
            {
                Bounds bounds = mesh.bounds;
                rows.Add($"{mesh.name}\t{bounds.size.x:F5}\t{bounds.size.y:F5}\t{bounds.size.z:F5}\t{bounds.center.x:F5}\t{bounds.center.y:F5}\t{bounds.center.z:F5}\t{mesh.vertexCount}");
            }
            File.WriteAllLines(output, rows);
            Debug.Log("[Manor][NewManorAudit] " + output);
        }

        private static string HierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
            return path;
        }

        private static void ConfigureRenderPipeline()
        {
            const string rendererPath = Root + "/Settings/URP_ManorRenderer.asset";
            const string pipelinePath = Root + "/Settings/URP_Manor.asset";
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                SerializedObject serialized = new SerializedObject(pipeline);
                serialized.FindProperty("m_RendererDataList").arraySize = 1;
                serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        private static void CreateTerrain(Transform parent, Material ground, Material path, Material stone)
        {
            Cube("TerrainSouth_主楼侧地皮", parent, new Vector3(0f, -0.35f, -4.25f), new Vector3(90f, 0.6f, 79.5f), ground, true);
            Cube("TerrainNorth_大门侧地皮", parent, new Vector3(0f, -0.35f, 51.25f), new Vector3(90f, 0.6f, 17.5f), ground, true);
            Cube("MainPathSouth_主楼侧道路", parent, new Vector3(0f, -0.02f, 25.5f), new Vector3(5.5f, 0.08f, 20f), path, true);
            Cube("MainPathNorth_大门侧道路", parent, new Vector3(0f, -0.02f, 46f), new Vector3(5.5f, 0.08f, 7f), path, true);
            Cube("CourtyardCrossPath_庭院横路", parent, new Vector3(0f, -0.01f, 19f), new Vector3(60f, 0.07f, 4f), path, true);
            // The cliff visuals sit below the walkable lips. Their former top surface was level with
            // the terrain and accidentally formed two giant alternate bridges around the real deck.
            Cube("CliffWest_裂谷西侧", parent, new Vector3(-23f, -4f, 39f), new Vector3(42f, 4f, 7f), stone, true);
            Cube("CliffEast_裂谷东侧", parent, new Vector3(23f, -4f, 39f), new Vector3(42f, 4f, 7f), stone, true);
            GameObject bridge = PlaceUserBridge(parent, new Vector3(0f, 0.05f, 39f), 4.2f, 7f, path);
            GameObject mechanism = Cube("BridgeMechanism_桥梁机关", parent, new Vector3(-3.2f, .75f, 35.8f), new Vector3(.65f, 1.5f, .65f), stone, true);
            BridgeMechanismInteractable bridgeControl = mechanism.AddComponent<BridgeMechanismInteractable>();
            bridgeControl.ConfigureBridge("INT_BRIDGE_MECHANISM", bridge.transform);
            GameObject fall = Child(parent, "RavineFallTrigger_裂谷坠落区");
            fall.transform.position = new Vector3(0f, -3.5f, 39f);
            BoxCollider fallCollider = fall.AddComponent<BoxCollider>(); fallCollider.size = new Vector3(88f, 8f, 7f); fallCollider.isTrigger = true;
            fall.AddComponent<FallDeathTrigger>();

            CreateEstateBoundary(parent, stone);
        }

        private static GameObject PlaceUserBridge(Transform parent, Vector3 groundPoint, float targetWidth, float targetLength, Material fallbackMaterial)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(UserLibrary + "/木桥/不用拆.fbx");
            if (source == null) return Cube("PF_UserWoodBridge_用户木桥", parent, groundPoint, new Vector3(targetWidth, .25f, targetLength), fallbackMaterial, true);
            GameObject bridge = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            bridge.name = "PF_UserWoodBridge_用户木桥";
            foreach (Collider collider in bridge.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            Bounds bounds = RendererBounds(bridge);
            float longSide = Mathf.Max(bounds.size.x, bounds.size.z);
            float scale = longSide > .001f ? targetLength / longSide : 1f;
            bridge.transform.localScale = Vector3.one * scale;
            bounds = RendererBounds(bridge);
            if (bounds.size.x > bounds.size.z) bridge.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            bounds = RendererBounds(bridge);
            bridge.transform.position += groundPoint - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            AddStaticMeshColliders(bridge);
            return bridge;
        }

        private static void CreateEstateBoundary(Transform parent, Material material)
        {
            GameObject boundary = Child(parent, "EstateBoundary_庄园外围闭环");
            Cube("BoundaryWest_西侧腐朽围墙", boundary.transform, new Vector3(-44.5f, 1.1f, 10f), new Vector3(.55f, 2.2f, 96f), material, true);
            Cube("BoundaryEast_东侧腐朽围墙", boundary.transform, new Vector3(44.5f, 1.1f, 10f), new Vector3(.55f, 2.2f, 96f), material, true);
            Cube("BoundarySouth_主楼后侧围墙", boundary.transform, new Vector3(0f, 1.1f, -37.5f), new Vector3(89f, 2.2f, .55f), material, true);
            Cube("BoundaryNorthLeft_大门左围墙", boundary.transform, new Vector3(-24f, 1.1f, 57.5f), new Vector3(41f, 2.2f, .55f), material, true);
            Cube("BoundaryNorthRight_大门右围墙", boundary.transform, new Vector3(24f, 1.1f, 57.5f), new Vector3(41f, 2.2f, .55f), material, true);
        }

        private static void CreateEnterableOutbuilding(Transform parent, string name, string interactionId, Vector3 position, float yaw, Vector2 footprint, Material wall, Material wood)
        {
            GameObject shell = Child(parent, name);
            shell.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            float width = footprint.x;
            float depth = footprint.y;
            const float height = 3.2f;
            const float thickness = .22f;
            const float doorWidth = 1.45f;
            const float doorHeight = 2.25f;
            Cube("Floor_可进入地面", shell.transform, new Vector3(0f, .04f, 0f), new Vector3(width, .08f, depth), wall, true);
            Cube("Wall_Left", shell.transform, new Vector3(-width * .5f, height * .5f, 0f), new Vector3(thickness, height, depth), wall, true);
            Cube("Wall_Right", shell.transform, new Vector3(width * .5f, height * .5f, 0f), new Vector3(thickness, height, depth), wall, true);
            Cube("Wall_Back", shell.transform, new Vector3(0f, height * .5f, -depth * .5f), new Vector3(width, height, thickness), wall, true);
            float sideWidth = (width - doorWidth) * .5f;
            Cube("Wall_FrontLeft", shell.transform, new Vector3(-(doorWidth + sideWidth) * .5f, height * .5f, depth * .5f), new Vector3(sideWidth, height, thickness), wall, true);
            Cube("Wall_FrontRight", shell.transform, new Vector3((doorWidth + sideWidth) * .5f, height * .5f, depth * .5f), new Vector3(sideWidth, height, thickness), wall, true);
            Cube("Wall_AboveDoor", shell.transform, new Vector3(0f, doorHeight + (height - doorHeight) * .5f, depth * .5f), new Vector3(doorWidth, height - doorHeight, thickness), wall, true);

            GameObject pivot = Child(shell.transform, "DoorPivot_外屋门铰链");
            pivot.transform.localPosition = new Vector3(-doorWidth * .5f, 0f, depth * .5f + .13f);
            GameObject leaf = Cube("DoorLeaf_外屋真实门扇", pivot.transform, new Vector3(doorWidth * .5f, doorHeight * .5f, 0f), new Vector3(doorWidth, doorHeight, .14f), wood, true);
            DoorInteractable door = leaf.AddComponent<DoorInteractable>();
            door.ConfigureDoor(interactionId, pivot.transform, null, -100f);
        }

        private static void PlaceUserAuthoredEstateAssets(Transform parent, Material doorMaterial)
        {
            Transform root = Child(parent, "UserEstateAssets_用户房屋与装饰").transform;
            PlaceUserBuilding("PF_UserWorkshopHut_加工木屋主用", "小屋/不用拆.fbx", root,
                new Vector3(-27f, 0f, 20f), 90f, 7.2f);
            PlaceUserBuilding("PF_UserAbandonedHut_废弃木屋主用", O03DoorwayModel, root,
                new Vector3(25f, 0f, 20f), -90f, 7.0f);
            PlaceUserBuilding("PF_UserStoneWorkshop_木石工坊主用", "木石工坊主用/不用拆.fbx", root,
                new Vector3(31f, 0f, 2f), -70f, 7.2f);

            CreateAuthoredDoorAtBuilding(root, "UserDoor_O02", "INT_DOOR_OUTBUILDING_O02",
                new Vector3(-24.43f, .52f, 17.05f), 180f, O02O04DoorModel, 1.40f, 2.28f, -100f, doorMaterial);
            CreateAuthoredDoorAtBuilding(root, "UserDoor_O03", "INT_DOOR_OUTBUILDING_O03",
                new Vector3(24.098f, 0f, 23.28f), 0f, O03DoorModel, 1.804f, 2.2f, -100f, doorMaterial);
            CreateAuthoredDoorAtBuilding(root, "UserDoor_O04", "INT_DOOR_OUTBUILDING_O04",
                new Vector3(31.897f, 0f, 4.082f), 20f, O02O04DoorModel, 1.00f, 1.90f, -100f, doorMaterial);

            Transform decoration = Child(root, "UserDecoration_用户装饰").transform;
            // Imported decoration sets are not scattered automatically. Their unassigned white
            // fragments were the small objects visible across the estate and are deliberately
            // removed instead of being treated as decoration.
        }

        private static void PlaceNewUserManor(Transform parent, Material fallbackMaterial)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(NewManorUnityModel);
            if (source == null) { Debug.LogError("[Manor][UserAsset] 新版主楼缺失"); return; }
            GameObject manor = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            manor.name = "PF_NewUserManor_新版主楼";
            foreach (Collider collider in manor.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            Bounds bounds = RendererBounds(manor);
            // This authored FBX is normalized with height as its longest axis. Uniformly scaling
            // its horizontal maximum to the 43 m room layout made it roughly 55 m tall and read
            // as a giant roof slab from first person. Fit each world axis to the actual two-floor
            // continuous layout instead, keeping the user's facade upright and centered on G01.
            float sourceWidth = Mathf.Max(.001f, bounds.size.x);
            float sourceHeight = Mathf.Max(.001f, bounds.size.y);
            float sourceDepth = Mathf.Max(.001f, bounds.size.z);
            manor.transform.localScale = new Vector3(43.2f / sourceWidth, 8.2f / sourceHeight, 25f / sourceDepth);
            manor.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            bounds = RendererBounds(manor);
            manor.transform.position += new Vector3(5.2f, -.05f, -2.5f) - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            // The user-imported manor itself owns its physical surface: wall, floor, facade and
            // stair triangles get MeshColliders. No hidden greybox is used as its replacement.
            AddStaticMeshColliders(manor);
            foreach (Renderer renderer in manor.GetComponentsInChildren<Renderer>(true))
                if (renderer.sharedMaterial == null) renderer.sharedMaterial = fallbackMaterial;
            ManorExteriorVisibilityController visibility = manor.AddComponent<ManorExteriorVisibilityController>();
            visibility.Configure(manor.GetComponentsInChildren<Renderer>(true), new Vector3(0f, 3.2f, -2.5f), new Vector3(42f, 8f, 24f));
        }

        private static void PlaceUserBuilding(string name, string relativeAssetPath, Transform parent, Vector3 groundPoint, float yaw, float maxFootprint)
        {
            string assetPath = relativeAssetPath.StartsWith("Assets/", StringComparison.Ordinal)
                ? relativeAssetPath
                : UserLibrary + "/" + relativeAssetPath;
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (source == null) { Debug.LogError("[Manor][UserAsset] Missing " + relativeAssetPath); return; }
            GameObject instance = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            instance.name = name;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            Bounds bounds = RendererBounds(instance);
            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > .001f) instance.transform.localScale = Vector3.one * (maxFootprint / footprint);
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            bounds = RendererBounds(instance);
            instance.transform.position += groundPoint - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            AddStaticMeshColliders(instance);
        }

        private static void CreateAuthoredDoorAtBuilding(Transform parent, string name, string interactionId, Vector3 hingePoint,
            float yaw, string relativeAssetPath, float apertureWidth, float apertureHeight, float openAngle, Material fallbackMaterial)
        {
            GameObject pivotObject = Child(parent, name + "_Pivot");
            pivotObject.transform.SetPositionAndRotation(hingePoint, Quaternion.Euler(0f, yaw, 0f));
            GameObject visual = CreateInteriorAuthoredDoorLeaf(pivotObject.transform, name, apertureWidth, apertureHeight, relativeAssetPath);
            if (visual == null)
                visual = Cube(name + "_FallbackLeaf", pivotObject.transform, new Vector3(apertureWidth * .5f, apertureHeight * .5f, 0f), new Vector3(apertureWidth, apertureHeight, .12f), fallbackMaterial, true);
            DoorInteractable interactable = visual.AddComponent<DoorInteractable>();
            interactable.ConfigureDoor(interactionId, pivotObject.transform, null, openAngle);
            // Door leaves are kinematic interaction geometry. Keeping them out of static flags
            // prevents the overlap guard and NavMesh bake from treating the closed leaf itself as
            // immovable architecture.
            foreach (Transform child in pivotObject.GetComponentsInChildren<Transform>(true)) child.gameObject.isStatic = false;
        }

        private static GameObject InstantiateAuthoredDoorVisual(string relativeAssetPath, Transform pivot, string name, float targetHeight)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(UserLibrary + "/" + relativeAssetPath);
            if (source == null) return null;
            GameObject visual = PrefabUtility.InstantiatePrefab(source, pivot) as GameObject;
            visual.name = name;
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            Bounds bounds = RendererBounds(visual);
            if (bounds.size.y > .001f) visual.transform.localScale = Vector3.one * (targetHeight / bounds.size.y);
            bounds = RendererBounds(visual);
            visual.transform.position += pivot.position - new Vector3(bounds.min.x, bounds.min.y, bounds.center.z);
            AddStaticMeshColliders(visual, false);
            return visual;
        }

        private static void PlaceUserDecorationSet(Transform parent, string relativeAssetPath, Vector3 center, int count, float radius)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(UserLibrary + "/" + relativeAssetPath);
            if (source == null) return;
            GameObject probe = PrefabUtility.InstantiatePrefab(source) as GameObject;
            MeshFilter[] filters = probe.GetComponentsInChildren<MeshFilter>(true)
                .Where(filter => filter.sharedMesh != null)
                .OrderByDescending(filter => filter.sharedMesh.bounds.size.x * filter.sharedMesh.bounds.size.y * filter.sharedMesh.bounds.size.z)
                .Take(count).ToArray();
            for (int index = 0; index < filters.Length; index++)
            {
                Mesh mesh = filters[index].sharedMesh;
                GameObject prop = new GameObject("UserProp_" + Path.GetFileNameWithoutExtension(relativeAssetPath) + "_" + index.ToString("00"));
                prop.transform.SetParent(parent);
                prop.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = prop.AddComponent<MeshRenderer>();
                MeshRenderer sourceRenderer = filters[index].GetComponent<MeshRenderer>();
                renderer.sharedMaterials = sourceRenderer != null ? sourceRenderer.sharedMaterials : Array.Empty<Material>();
                float angle = index * Mathf.PI * 2f / Mathf.Max(1, filters.Length);
                prop.transform.position = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                prop.transform.rotation = Quaternion.Euler(0f, index * 43f, 0f);
                Bounds bounds = renderer.bounds;
                float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                if (largest > 3f) prop.transform.localScale *= 3f / largest;
                bounds = renderer.bounds;
                prop.transform.position += Vector3.up * (0.02f - bounds.min.y);
                MeshCollider collider = prop.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.convex = false;
                prop.isStatic = true;
            }
            UnityEngine.Object.DestroyImmediate(probe);
        }

        private static void AddStaticMeshColliders(GameObject root, bool markStatic = true)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; collider.convex = false;
                if (markStatic) filter.gameObject.isStatic = true;
            }
        }

        private static void CreateGateAndDoor(Transform parent, Material wood)
        {
            GameObject gate = Child(parent, "O08_ManorGate_庄园大门");
            gate.transform.position = new Vector3(0f, 0f, 49f);
            Cube("GatePost_L_左门柱", gate.transform, new Vector3(-2.2f, 1.5f, 0f), new Vector3(0.7f, 3f, 0.8f), wood, true);
            Cube("GatePost_R_右门柱", gate.transform, new Vector3(2.2f, 1.5f, 0f), new Vector3(0.7f, 3f, 0.8f), wood, true);
            GameObject pivot = Child(gate.transform, "DoorPivot_木门铰链");
            pivot.transform.localPosition = new Vector3(-1.85f, 0f, 0f);
            GameObject leaf = Cube("DoorLeaf_可交互木门", pivot.transform, new Vector3(1.85f, 1.25f, 0f), new Vector3(3.7f, 2.5f, 0.18f), wood, true);
            DoorInteractable interactable = leaf.AddComponent<DoorInteractable>();
            interactable.ConfigureDoor("INT_DOOR_MANOR_GATE_01", pivot.transform, "KEY_MANOR_GATE_PASSWORD", -95f);
            GameObject keypad = Cube("GateKeypad_密码输入器", gate.transform, new Vector3(2.75f, 1.35f, -0.15f), new Vector3(0.34f, 0.5f, 0.18f), wood, true);
            GatePasswordInteractable password = keypad.AddComponent<GatePasswordInteractable>();
            password.ConfigurePassword("INT_GATE_PASSWORD_1016", "1016", interactable);
            GameObject escape = Child(gate.transform, "EscapeTrigger_唯一逃生触发");
            escape.transform.localPosition = new Vector3(0f, 1.5f, 2.1f);
            BoxCollider escapeCollider = escape.AddComponent<BoxCollider>();
            escapeCollider.size = new Vector3(4f, 3f, 1.2f);
            escapeCollider.isTrigger = true;
            escape.AddComponent<EscapeTrigger>();
        }

        private static void CreateUserEstateGate(Transform parent, Material fallbackMaterial)
        {
            GameObject gate = Child(parent, "PF_UserGate_用户大门");
            gate.transform.position = new Vector3(0f, 0f, 49f);
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(UserLibrary + "/大门/已拆开.fbx");
            GameObject visual = source != null ? PrefabUtility.InstantiatePrefab(source, gate.transform) as GameObject : null;
            if (visual != null)
            {
                visual.name = "GateFrameAndLeaves_用户大门模型";
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                Bounds bounds = RendererBounds(visual);
                float width = Mathf.Max(bounds.size.x, bounds.size.z);
                if (width > .001f) visual.transform.localScale = Vector3.one * (5.2f / width);
                bounds = RendererBounds(visual);
                visual.transform.position += gate.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
            GameObject pivot = Child(gate.transform, "UserGateDoorPivot_用户大门铰链");
            pivot.transform.localPosition = new Vector3(-1.85f, 0f, 0f);
            GameObject leaf = CreateInteriorAuthoredDoorLeaf(pivot.transform, "庄园大门", 3.7f, 2.7f);
            if (leaf == null) leaf = Cube("UserGateFallbackLeaf", pivot.transform, new Vector3(1.85f, 1.25f, 0f), new Vector3(3.7f, 2.5f, .18f), fallbackMaterial, true);
            DoorInteractable interactable = leaf.AddComponent<DoorInteractable>();
            interactable.ConfigureDoor("INT_DOOR_MANOR_GATE_01", pivot.transform, "KEY_MANOR_GATE_PASSWORD", -95f);
            GameObject keypad = Cube("GateKeypad_密码输入器", gate.transform, new Vector3(2.75f, 1.35f, -.15f), new Vector3(.34f, .5f, .18f), fallbackMaterial, true);
            keypad.AddComponent<GatePasswordInteractable>().ConfigurePassword("INT_GATE_PASSWORD_1016", "1016", interactable);
            GameObject escape = Child(gate.transform, "EscapeTrigger_唯一逃生触发"); escape.transform.localPosition = new Vector3(0f, 1.5f, 2.1f);
            BoxCollider trigger = escape.AddComponent<BoxCollider>(); trigger.size = new Vector3(4f, 3f, 1.2f); trigger.isTrigger = true; escape.AddComponent<EscapeTrigger>();
        }

        private static void CreateThreatActors(Transform environment, Transform layout)
        {
            Transform existing = Find(environment, "ThreatActors_有限敌人");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            GameObject root = Child(environment, "ThreatActors_有限敌人");

            Transform[] wraithPatrol =
            {
                Point(root.transform, "WraithPoint_G08", new Vector3(-8f, 3.65f, 5f)),
                Point(root.transform, "WraithPoint_UpperCorridor", new Vector3(-1f, 3.65f, 2f)),
                Point(root.transform, "WraithPoint_G12", new Vector3(5f, 3.65f, -4f))
            };
            GameObject wraith = new GameObject("PregnantWraith_孕妇怨灵临时形体");
            wraith.transform.SetParent(root.transform); wraith.transform.position = wraithPatrol[0].position;
            CharacterController wraithController = wraith.AddComponent<CharacterController>();
            wraithController.height = 1.85f; wraithController.radius = .32f; wraithController.center = Vector3.up * .925f;
            NavMeshAgent wraithAgent = wraith.AddComponent<NavMeshAgent>(); wraithAgent.radius = .32f; wraithAgent.height = 1.85f; wraithAgent.angularSpeed = 540f;
            GameObject wraithVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            wraithVisual.name = "WraithVisual_非性感化占位"; wraithVisual.transform.SetParent(wraith.transform);
            wraithVisual.transform.localPosition = Vector3.up * .9f; wraithVisual.transform.localScale = new Vector3(.55f, .9f, .55f);
            UnityEngine.Object.DestroyImmediate(wraithVisual.GetComponent<Collider>());
            // Keep the authored scene truthful before Play Mode lifecycle methods run: the
            // wraith only becomes visible after the G08 diary has actually been read.
            foreach (Renderer renderer in wraithVisual.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            PregnantWraithController wraithAI = wraith.AddComponent<PregnantWraithController>();
            wraithAI.Configure(wraithPatrol, wraithVisual.GetComponentsInChildren<Renderer>(true));

            Transform[] butcherPatrol =
            {
                Point(root.transform, "ButcherPoint_B02_SouthEast", new Vector3(10.2f, -3.05f, -10.8f)),
                Point(root.transform, "ButcherPoint_B02_NorthEast", new Vector3(10f, -3.05f, -5.5f)),
                Point(root.transform, "ButcherPoint_B02_WestDoor", new Vector3(3.4f, -3.05f, -8f)),
                Point(root.transform, "ButcherPoint_B01_East", new Vector3(1.2f, -3.05f, -8f)),
                Point(root.transform, "ButcherPoint_B01_West", new Vector3(-4.5f, -3.05f, -8f))
            };
            GameObject butcher = new GameObject("Butcher_屠夫临时角色");
            butcher.transform.SetParent(root.transform); butcher.transform.position = butcherPatrol[0].position;
            CharacterController butcherController = butcher.AddComponent<CharacterController>();
            butcherController.height = 2f; butcherController.radius = .38f; butcherController.center = Vector3.up;
            NavMeshAgent butcherAgent = butcher.AddComponent<NavMeshAgent>(); butcherAgent.radius = .38f; butcherAgent.height = 2f; butcherAgent.angularSpeed = 480f;
            Renderer[] visuals = Array.Empty<Renderer>();
            GameObject rig = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Characters/StagedRigs/Butcher_ProvisionalRig.fbx");
            if (rig != null)
            {
                GameObject visual = PrefabUtility.InstantiatePrefab(rig, butcher.transform) as GameObject;
                visual.name = "ButcherVisual_临时模型";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = Vector3.one * 2.05f;
                visuals = visual.GetComponentsInChildren<Renderer>(true);
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            }
            else
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.transform.SetParent(butcher.transform); fallback.transform.localPosition = Vector3.up * .95f;
                UnityEngine.Object.DestroyImmediate(fallback.GetComponent<Collider>());
                visuals = fallback.GetComponentsInChildren<Renderer>(true);
            }
            ButcherController butcherAI = butcher.AddComponent<ButcherController>();
            butcherAI.Configure(butcherPatrol, visuals);
        }

        private static void CreateG07VaultWindow(Transform parent, Material wood)
        {
            GameObject root = Child(parent, "G07_可翻越实体窗");
            root.transform.position = new Vector3(19f, 0f, 10.35f);
            GameObject pivot = Child(root.transform, "G07_WindowPivot_窗扇铰链");
            pivot.transform.localPosition = new Vector3(-.75f, 1.25f, 0f);
            GameObject leaf = Cube("G07_WindowLeaf_真实窗扇", pivot.transform, new Vector3(.75f, 0f, 0f), new Vector3(1.5f, 1.5f, .08f), wood, false);
            GameObject trigger = Child(root.transform, "G07_VaultTrigger_翻窗交互");
            trigger.transform.localPosition = new Vector3(0f, 1.1f, -.3f);
            BoxCollider collider = trigger.AddComponent<BoxCollider>(); collider.size = new Vector3(1.5f, 1.8f, .8f); collider.isTrigger = true;
            Cube("G07_WindowSill_实体窗台", root.transform, new Vector3(0f, .36f, 0f), new Vector3(2f, .72f, .55f), wood, true);
            Transform outsideLanding = Child(root.transform, "G07_VaultLanding_外侧落点").transform;
            outsideLanding.localPosition = new Vector3(0f, 0f, 1.8f); outsideLanding.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Transform insideLanding = Child(root.transform, "G07_VaultLanding_内侧落点").transform;
            insideLanding.localPosition = new Vector3(0f, 0f, -1.8f); insideLanding.localRotation = Quaternion.identity;
            VaultWindowInteractable vault = trigger.AddComponent<VaultWindowInteractable>();
            vault.ConfigureWindow("INT_G07_VAULT_WINDOW", outsideLanding, insideLanding, pivot.transform);
        }

        private static Transform Point(Transform parent, string name, Vector3 position)
        {
            Transform point = Child(parent, name).transform;
            point.position = position;
            return point;
        }

        private static void BakeNavigation(Transform environment)
        {
            NavMeshSurface surface = environment.GetComponent<NavMeshSurface>();
            if (surface == null) surface = environment.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }

        private static void CreateEnterableManorCollisionAndDoor(Transform parent, Material wood, Material floor)
        {
            const float frontZ = 10.3f;
            const float doorCenterX = 0f;
            const float doorWidth = 3.2f;

            // Room and corridor modules own all walkable surfaces. A former whole-building floor
            // silently sealed the G03 hatch and masked route gaps, so only the entrance threshold remains.
            Cube("ManorEntranceThreshold_主入口门槛", parent, new Vector3(0f, -0.06f, 9.65f), new Vector3(3.2f, 0.12f, 1.3f), floor, true);

            GameObject pivot = Child(parent, "ManorDoorPivot_HouseEntrance");
            pivot.transform.position = new Vector3(doorCenterX - doorWidth * 0.5f, 0f, frontZ + 0.18f);
            GameObject leaf = CreateInteriorAuthoredDoorLeaf(pivot.transform, "主楼入口", doorWidth, 3.5f, NewManorUnityModel);
            if (leaf == null) leaf = Cube("UserDoorLeaf_主楼入口后备门扇", pivot.transform,
                new Vector3(doorWidth * 0.5f, 2.05f, 0f), new Vector3(doorWidth, 4.1f, .18f), wood, true);
            DoorInteractable interactable = leaf.AddComponent<DoorInteractable>();
            interactable.ConfigureDoor("INT_DOOR_MANOR_HOUSE_01", pivot.transform, null, 100f);

            // There is intentionally no scene transition here. Opening the hinged leaf exposes the
            // same-scene interior, so the player physically walks from the estate into G01 and up G06.
        }

        private static void SetLegacyAreaEnabled(Transform environment, string areaName, bool enabled)
        {
            Transform area = Find(environment, areaName);
            if (area == null) return;
            foreach (Renderer renderer in area.GetComponentsInChildren<Renderer>(true)) renderer.enabled = enabled;
            foreach (Collider collider in area.GetComponentsInChildren<Collider>(true)) collider.enabled = enabled;
        }

        private static void ApplyGreyboxMaterials(Transform environment, Material floor, Material wall, Material basement)
        {
            foreach (string areaName in new[] { "GroundLevel_一层区域", "UpperLevel_二层区域" })
            {
                Transform area = Find(environment, areaName);
                if (area == null) continue;
                foreach (Renderer renderer in area.GetComponentsInChildren<Renderer>(true))
                {
                    string objectName = renderer.gameObject.name;
                    // The exterior FBX owns the visible facade. Keep these boundary colliders for
                    // traversal, but do not let greybox north walls poke through the manor shell.
                    if (renderer.bounds.center.z > 7.45f)
                    {
                        renderer.enabled = false;
                        continue;
                    }
                    bool horizontal = objectName.Contains("Floor_") || objectName.Contains("Step_") || objectName.Contains("Threshold_") || objectName.Contains("Landing_") || objectName.Contains("Walkway_");
                    renderer.sharedMaterial = horizontal ? floor : wall;
                }
            }
            Transform basementArea = Find(environment, "BasementLevel_地下区域");
            if (basementArea == null) return;
            foreach (Renderer renderer in basementArea.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = basement;
        }

        private static void ApplyContinuousShellVisualRules(Transform layout)
        {
            // A visible room boundary must accompany every physical wall. Camera-volume materials
            // handle exterior isolation; disabling these renderers created views into the courtyard
            // followed by invisible colliders.
            foreach (Renderer renderer in layout.GetComponentsInChildren<Renderer>(true))
                if (renderer.GetComponent<Collider>() != null) renderer.enabled = true;
        }

        private static void CreateChaseRouteInteractions(Transform parent, Material wood)
        {
            GameObject root = Child(parent, "ChaseInteractions_追逐路线交互");
            GameObject wardrobe = Cube("HideWardrobe_O02_可用隐藏柜", root.transform, new Vector3(-27f, 1.15f, 18f), new Vector3(1.5f, 2.3f, .9f), wood, true);
            Transform inside = Child(wardrobe.transform, "HideInside_柜内点").transform;
            inside.localPosition = new Vector3(0f, -1.15f, 0f);
            Transform exit = Child(root.transform, "HideExit_柜外点").transform;
            exit.position = new Vector3(-27f, .05f, 19.4f); exit.rotation = Quaternion.Euler(0f, 180f, 0f);
            GameObject trigger = Child(root.transform, "HideWardrobeTrigger_隐藏柜交互");
            trigger.transform.position = new Vector3(-27f, 1f, 18.65f);
            BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>(); triggerCollider.size = new Vector3(1.4f, 2f, .7f); triggerCollider.isTrigger = true;
            HideSpotInteractable hide = trigger.AddComponent<HideSpotInteractable>(); hide.Configure("INT_HIDE_O02_WARDROBE", inside, exit);

            GameObject board = Cube("BreakableBoard_Courtyard_屠夫木板", root.transform, new Vector3(12f, 1.05f, 19f), new Vector3(3.2f, 2.1f, .16f), wood, true);
            board.AddComponent<BreakableBoard>();
        }

        private static void PlaceContinuousInteriorArt(Transform environment, Transform layout)
        {
            Transform existing = Find(environment, InteriorArtRoot);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            Transform root = Child(environment, InteriorArtRoot).transform;

            // Room models are visual dressing only. The authored greybox owns all collision, which
            // prevents AI-generated mesh fragments from creating air walls. Bounds grounding keeps
            // every imported set resting on its room floor instead of floating.
            PlaceSelectedRoomArt(root, "G01", "已拆开G01.fbx", new[]
            {
                Piece("tripo_node_635ac15b_10", new Vector3(-2.25f,0f,8.25f), 1.3f, 90f),
                Piece("tripo_node_635ac15b_9", new Vector3(2.25f,0f,8f), 1.35f, -90f),
                Piece("tripo_node_635ac15b_6", new Vector3(-2.72f,0f,8.8f), 1.45f, 90f)
            });
            PlaceG02ControlledDressing(root, layout);
            PlaceG03ModularDressing(root, layout);
            PlaceG04ModularDressing(root, layout);
            PlaceSelectedRoomArt(root, "G05", "已拆开G05.fbx", new[]
            {
                Piece("tripo_node_00d95598_8", new Vector3(2.05f,0f,-1f), .9f, -90f)
            });
            // G06 relies on the authored continuous stair, landing and shell. The two generated
            // FBX fragments previously selected here read as a floor-to-ceiling slab from the
            // first-person approach and blocked the route visually, so they are intentionally
            // omitted instead of risking another non-physical shard in the stairwell.
            PlaceSelectedRoomArt(root, "G07", "已拆开G07.fbx", new[]
            {
                Piece("tripo_node_0c5dbe35", new Vector3(17.55f,0f,5.8f), .55f, 20f),
                Piece("tripo_node_0c5dbe35_2", new Vector3(20.45f,0f,3f), .65f, -15f),
                Piece("tripo_node_0c5dbe35_11", new Vector3(17.55f,0f,-5.9f), .8f, 0f),
                Piece("tripo_node_0c5dbe35_12", new Vector3(20.45f,0f,5.3f), .75f, 0f)
            });
            PlaceSelectedRoomArt(root, "G08", "已拆开G08.fbx", new[]
            {
                Piece("tripo_node_a833bcc3_16", new Vector3(-14.2f,3.6f,4.2f), 2.8f, 0f),
                Piece("tripo_node_a833bcc3_13", new Vector3(-8f,3.6f,7.8f), 1.45f, 0f),
                Piece("tripo_node_a833bcc3_4", new Vector3(-8f,3.6f,6.8f), .95f, 180f),
                Piece("tripo_node_a833bcc3_14", new Vector3(-16.45f,3.6f,7.6f), 2f, 90f),
                Piece("tripo_node_a833bcc3_5", new Vector3(-16.35f,3.6f,1.8f), 1.05f, 90f)
            });
            PlaceG09DistinctChildZones(root, layout);
            PlaceSelectedRoomArt(root, "G10", "已拆开G10.fbx", new[]
            {
                Piece("tripo_node_d404fdb9_7", new Vector3(-12.6f,3.6f,-2.4f), 2.2f, 0f),
                Piece("tripo_node_d404fdb9_6", new Vector3(-16.35f,3.6f,-2f), 1.6f, 90f),
                Piece("tripo_node_d404fdb9_11", new Vector3(-15.7f,3.6f,-5.5f), .65f, 0f)
            });
            PlaceSelectedRoomArt(root, "G11", "已拆开G11.fbx", new[]
            {
                Piece("tripo_node_99d1758f", new Vector3(-16f,3.6f,-10f), 1.45f, 90f)
            });
            PlaceSelectedRoomArt(root, "G12", "已拆开G12.fbx", new[]
            {
                Piece("tripo_node_eee6ebae_22", new Vector3(11.3f,3.6f,-1.5f), 2.2f, 0f),
                Piece("tripo_node_eee6ebae_21", new Vector3(4.2f,3.6f,-1.2f), 1.35f, 0f),
                Piece("tripo_node_eee6ebae_20", new Vector3(4.2f,3.6f,-2.15f), .9f, 180f),
                Piece("tripo_node_eee6ebae_9", new Vector3(11.8f,3.6f,-5.3f), .9f, 15f),
                Piece("tripo_node_eee6ebae_4", new Vector3(13.35f,3.6f,-4f), 1f, -90f)
            });

            PlaceBasementArt(root, layout, "SM_KitchenHatch.fbx", "G03_厨房套间", 1.5f, new Vector3(1.8f, 0.03f, 0.8f));
            PlaceBasementArt(root, layout, "SM_RitualCircle.fbx", "B02_地下祭祀室", 3.25f, Vector3.up * 0.01f);
            PlaceBasementArt(root, layout, "SM_SaintStatue.fbx", "B02_地下祭祀室", 1.5f, new Vector3(0f, 0.03f, 2.1f));
            PlaceB02RitualLayout(root, layout);
        }

        private static void PlaceG03ModularDressing(Transform parent, Transform layout)
        {
            Transform room = Find(layout, "G03_厨房套间"); if (room == null) return;
            Transform group = Child(parent, "ART_G03_模块化陈设").transform;
            Material wood = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/MAT_Wood_WetDark.mat");
            Material stone = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/MAT_Basement_DampStone.mat");
            Cube("KitchenCounter_North_北墙操作台", group, room.position + new Vector3(-3.8f, .45f, 3.9f), new Vector3(5.5f, .9f, .7f), wood, true);
            Cube("KitchenSink_West_水槽台", group, room.position + new Vector3(-7.1f, .45f, 1.6f), new Vector3(.7f, .9f, 3.2f), stone, true);
            Cube("KitchenWorktable_中央工作桌", group, room.position + new Vector3(-3.5f, .42f, -.5f), new Vector3(2.5f, .84f, 1.1f), wood, true);
            Cube("KitchenShelf_South_储藏架", group, room.position + new Vector3(-5.5f, 1f, -4.25f), new Vector3(2.4f, 2f, .5f), wood, true);
            foreach (Vector3 offset in new[] { new Vector3(-6.6f,.35f,-2.8f), new Vector3(-5.8f,.3f,-2.6f), new Vector3(-4.9f,.4f,-3f) })
                Cube("KitchenBarrel_落地桶", group, room.position + offset, new Vector3(.6f, offset.y * 2f, .6f), wood, true);
        }

        private static void PlaceG02ControlledDressing(Transform parent, Transform layout)
        {
            Transform room = Find(layout, "G02_客厅套间"); if (room == null) return;
            Transform group = Child(parent, "ART_G02_可控客厅陈设").transform;
            Material wood = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/MAT_Wood_WetDark.mat");
            Material floor = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/MAT_InteriorFloor_Worn.mat");
            Cube("Sofa_旧长沙发", group, new Vector3(-13.5f,.48f,1f), new Vector3(2.4f,.96f,.9f), wood, true);
            Cube("CoffeeTable_客厅茶几", group, new Vector3(-13.5f,.28f,3f), new Vector3(1.45f,.56f,.85f), wood, true);
            Cube("Fireplace_西墙壁炉", group, new Vector3(-18.6f,1f,2.8f), new Vector3(.5f,2f,1.6f), floor, true);
            Cube("Sideboard_北墙餐具柜", group, new Vector3(-17.8f,.9f,6.65f), new Vector3(1.2f,1.8f,.5f), wood, true);
            Cube("LowCabinet_东侧低柜", group, new Vector3(-6.2f,.5f,6.65f), new Vector3(1.3f,1f,.5f), wood, true);
        }

        private static void PlaceG04ModularDressing(Transform parent, Transform layout)
        {
            Transform room = Find(layout, "G04_屠宰工作间"); if (room == null) return;
            Transform group = Child(parent, "ART_G04_低碎片陈设").transform;
            Material wood = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/MAT_Wood_WetDark.mat");
            Material stone = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/MAT_Basement_DampStone.mat");
            Cube("ButcherCentralTable_中央工作台", group, room.position + new Vector3(0f,.48f,0f), new Vector3(2.6f,.96f,1.1f), wood, true);
            Cube("ButcherRearBench_后墙工作柜", group, room.position + new Vector3(0f,.48f,4.15f), new Vector3(4.8f,.96f,.55f), wood, true);
            Cube("ButcherToolRack_工具挂架", group, room.position + new Vector3(-5.9f,1.25f,1.4f), new Vector3(.3f,2.2f,2.6f), wood, true);
            Cube("ButcherDrain_排水沟", group, room.position + new Vector3(4.8f,.01f,0f), new Vector3(.35f,.02f,7f), stone, false);
            foreach (Vector3 offset in new[] { new Vector3(4.6f,.35f,3.3f), new Vector3(5.3f,.3f,2.7f), new Vector3(-4.8f,.4f,-3.3f) })
                Cube("ButcherContainer_落地容器", group, room.position + offset, new Vector3(.65f, offset.y * 2f, .65f), stone, true);
        }

        private static void PlaceG09DistinctChildZones(Transform parent, Transform layout)
        {
            Transform room = Find(layout, "G09_儿童房双区"); if (room == null) return;
            Transform group = Child(parent, "ART_G09_不同家庭双区").transform;
            Material wood = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/MAT_Wood_WetDark.mat");
            Material floor = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/MAT_InteriorFloor_Worn.mat");
            Cube("BoyBed_男童独立床", group, room.position + new Vector3(2.8f,.35f,2.3f), new Vector3(2.0f,.7f,3.3f), wood, true);
            Cube("BoyChest_男童家庭木箱", group, room.position + new Vector3(5.7f,.35f,2.8f), new Vector3(1.2f,.7f,.8f), wood, true);
            Cube("BoyToyCar_磨损木车", group, room.position + new Vector3(3.6f,.14f,.5f), new Vector3(.55f,.28f,.3f), wood, false);
            Cube("GirlBed_女童独立床", group, room.position + new Vector3(2.4f,.32f,-2.3f), new Vector3(1.8f,.64f,3.0f), floor, true);
            Cube("GirlChest_女童家庭木箱", group, room.position + new Vector3(5.6f,.3f,-2.7f), new Vector3(1.0f,.6f,.75f), floor, true);
            Cube("GirlPinwheel_破纸风车", group, room.position + new Vector3(3.5f,.65f,-.6f), new Vector3(.12f,1.3f,.12f), floor, false);
        }

        private static void PlaceB02RitualLayout(Transform parent, Transform layout)
        {
            Transform room = Find(layout, "B02_地下祭祀室"); if (room == null) return;
            Transform group = Child(parent, "ART_B02_四祭位与外围遗骸").transform;
            Material stone = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/MAT_Basement_DampStone.mat");
            foreach (Vector3 offset in new[] { new Vector3(0f,.18f,2.2f), new Vector3(2.2f,.18f,0f), new Vector3(0f,.18f,-2.2f), new Vector3(-2.2f,.18f,0f) })
                Cube("RitualPosition_四祭位", group, room.position + offset, new Vector3(.8f,.36f,.8f), stone, true);
            Cube("RitualAltar_后方祭坛", group, room.position + new Vector3(0f,.55f,4f), new Vector3(2.4f,1.1f,.8f), stone, true);
            foreach (Vector3 offset in new[] { new Vector3(4f,.15f,3.5f), new Vector3(4.2f,.15f,-3.2f), new Vector3(-3.7f,.15f,-3.4f), new Vector3(-3.8f,.15f,3.4f) })
                Cube("Remains_外围遗骸占位", group, room.position + offset, new Vector3(.9f,.3f,.45f), stone, false);
            GameObject lightObject = Child(group, "B02_验收用低强度冷光"); lightObject.transform.position = room.position + Vector3.up * 2.3f;
            Light light = lightObject.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(.48f,.58f,.62f); light.intensity = 4f; light.range = 13f;
        }

        private static void PlaceRoomArt(Transform parent, Transform layout, string roomId, string anchorName, float maxFootprint, string fileName = null)
        {
            Transform anchor = Find(layout, anchorName);
            if (anchor == null) return;
            fileName ??= "已拆开" + roomId + ".fbx";
            string path = Root + "/Art/Environment/SourceModels/Rooms/" + fileName;
            PlaceGroundedVisual(path, "ART_" + roomId + "_房间陈设", parent, anchor.position + Vector3.up * 0.02f, maxFootprint);
        }

        private readonly struct RoomPiece
        {
            internal readonly string Name;
            internal readonly Vector3 Position;
            internal readonly float MaxSize;
            internal readonly float Yaw;
            internal RoomPiece(string name, Vector3 position, float maxSize, float yaw) { Name = name; Position = position; MaxSize = maxSize; Yaw = yaw; }
        }

        private static RoomPiece Piece(string name, Vector3 position, float maxSize, float yaw) => new(name, position, maxSize, yaw);

        private static void PlaceSelectedRoomArt(Transform parent, string roomId, string fileName, IEnumerable<RoomPiece> pieces)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Environment/SourceModels/Rooms/" + fileName);
            if (source == null) { Debug.LogWarning("[Manor][RoomArt] Missing " + fileName); return; }
            Transform group = Child(parent, "ART_" + roomId + "_精选子件").transform;
            foreach (RoomPiece spec in pieces)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(source, group) as GameObject;
                if (instance == null) continue;
                Transform selected = Find(instance.transform, spec.Name);
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true)) renderer.enabled = selected != null && (renderer.transform == selected || renderer.transform.IsChildOf(selected));
                foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                if (selected == null) { Debug.LogWarning($"[Manor][RoomArt] {roomId} missing child {spec.Name}"); UnityEngine.Object.DestroyImmediate(instance); continue; }
                instance.name = "ART_" + roomId + "_" + spec.Name;
                Bounds bounds = EnabledRendererBounds(instance);
                float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                if (largest > .001f) instance.transform.localScale = Vector3.one * (spec.MaxSize / largest);
                instance.transform.rotation = Quaternion.Euler(0f, spec.Yaw, 0f);
                bounds = EnabledRendererBounds(instance);
                instance.transform.position += spec.Position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>(true))
                {
                    Renderer renderer = filter.GetComponent<Renderer>();
                    if (filter.sharedMesh == null || renderer == null || !renderer.enabled) continue;
                    MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                    collider.convex = false;
                    filter.gameObject.isStatic = true;
                }
            }
        }

        private static Bounds EnabledRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void PlaceBasementArt(Transform parent, Transform layout, string fileName, string anchorName, float maxFootprint, Vector3 offset)
        {
            Transform anchor = Find(layout, anchorName);
            if (anchor == null) return;
            string path = Root + "/Art/Environment/SourceModels/Basement/" + fileName;
            PlaceGroundedVisual(path, "ART_" + Path.GetFileNameWithoutExtension(fileName), parent, anchor.position + offset, maxFootprint);
        }

        private static void PlaceGroundedVisual(string assetPath, string name, Transform parent, Vector3 groundPoint, float maxFootprint)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (source == null) return;
            GameObject instance = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            if (instance == null) return;
            instance.name = name;
            instance.transform.position = groundPoint;
            Bounds bounds = RendererBounds(instance);
            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > 0.001f) instance.transform.localScale = Vector3.one * (maxFootprint / footprint);
            bounds = RendererBounds(instance);
            instance.transform.position += Vector3.up * (groundPoint.y - bounds.min.y);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            FilterRoomFragments(instance, name);
        }

        private static void FilterRoomFragments(GameObject instance, string roomArtName)
        {
            // Imported room FBXs contain hundreds of generated shards. Keep only the largest
            // authored-looking pieces; the continuous shell and modular props own collision.
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers) renderer.enabled = false;
            int keepCount = roomArtName.Contains("G06") ? 6 : 18;
            foreach (Renderer renderer in renderers
                .Where(r => r != null && r.bounds.size.x > 0.06f && r.bounds.size.y > 0.04f && r.bounds.size.z > 0.06f)
                .OrderByDescending(r => r.bounds.size.x * r.bounds.size.y * r.bounds.size.z)
                .Take(keepCount))
                renderer.enabled = true;
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void InvisibleColliderCube(string name, Transform parent, Vector3 position, Vector3 size)
        {
            GameObject cube = Child(parent, name);
            cube.transform.localPosition = position;
            BoxCollider collider = cube.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static void ConfigureAtmosphere(Transform lighting, Transform artRoot)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.fogColor = new Color(0.105f, 0.13f, 0.14f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.20f, 0.235f, 0.245f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.13f, 0.125f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.04f, 0.035f);
            foreach (Light light in lighting.GetComponentsInChildren<Light>()) light.gameObject.SetActive(false);
            GameObject moon = Child(artRoot, "MoonLight_冷月光");
            Light source = moon.AddComponent<Light>();
            source.type = LightType.Directional;
            source.color = new Color(0.55f, 0.66f, 0.72f);
            source.intensity = 1.15f;
            source.shadows = LightShadows.Soft;
            moon.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
            Vector3[] lamps = { new(-5f, 2.2f, 11f), new(5f, 2.2f, 11f), new(0f, 2.2f, 34.5f) };
            foreach (Vector3 position in lamps)
            {
                GameObject lamp = Child(artRoot, "Lamp_油灯微光");
                lamp.transform.position = position;
                Light point = lamp.AddComponent<Light>();
                point.type = LightType.Point;
                point.color = new Color(1f, 0.55f, 0.25f);
                point.intensity = 5f;
                point.range = 8f;
                point.shadows = LightShadows.Soft;
            }
        }

        private static void ConfigurePlayer(Transform manorRoot)
        {
            FirstPersonController player = manorRoot.GetComponentInChildren<FirstPersonController>(true);
            if (player == null) return;
            // Default to the manor entrance while the outdoor layout is being inspected.
            player.transform.position = new Vector3(0f, .05f, 16.2f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            if (player.GetComponent<PlayerCheckpointRestorer>() == null) player.gameObject.AddComponent<PlayerCheckpointRestorer>();
            Camera camera = player.GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.075f, 0.08f);
            }
            Transform spawn = Find(manorRoot, "PlayerSpawn_玩家出生点");
            if (spawn != null)
            {
                spawn.position = player.transform.position;
                spawn.rotation = player.transform.rotation;
            }
        }

        private static GameObject PlaceModel(string prefabName, string modelFile, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material, Vector3 colliderSize)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/" + modelFile);
            if (source == null) throw new FileNotFoundException("Missing imported model", modelFile);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = prefabName;
            instance.transform.SetParent(parent);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
            if (colliderSize != Vector3.zero)
            {
                BoxCollider collider = instance.AddComponent<BoxCollider>();
                collider.size = colliderSize;
                collider.center = new Vector3(0f, colliderSize.y * 0.5f, 0f);
            }
            string prefabPath = Prefabs + "/" + prefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
            return instance;
        }

        private static Material Material(string name, Color tint, float smoothness, string textureName)
        {
            string path = Materials + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = shader;
            material.color = tint;
            material.SetFloat("_Smoothness", smoothness);
            if (!string.IsNullOrEmpty(textureName))
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/" + textureName);
                material.mainTexture = texture;
                material.color = Color.white;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureManorFacadeCutout(Material material)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Materials + "/SH_ManorFacadeDoorCutout.shader");
            if (shader == null) return;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/T_ManorHouse_BaseColor.jpg");
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureInteriorCameraVolume(Material material)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Materials + "/SH_InteriorCameraVolume.shader");
            if (shader == null) return;
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.105f, 0.115f, 0.095f));
            EditorUtility.SetDirty(material);
        }

        private static void CreatePropPrefabs(string modelFile, string prefix, Material material)
        {
            string propsFolder = Prefabs + "/Props";
            if (!AssetDatabase.IsValidFolder(propsFolder)) AssetDatabase.CreateFolder(Prefabs, "Props");
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/" + modelFile);
            GameObject temporary = (GameObject)PrefabUtility.InstantiatePrefab(source);
            temporary.hideFlags = HideFlags.HideAndDontSave;
            int index = 0;
            foreach (MeshFilter meshFilter in temporary.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null) continue;
                index++;
                GameObject prop = new GameObject(prefix + "_" + index.ToString("00"));
                MeshFilter filter = prop.AddComponent<MeshFilter>();
                filter.sharedMesh = meshFilter.sharedMesh;
                MeshRenderer renderer = prop.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                BoxCollider collider = prop.AddComponent<BoxCollider>();
                collider.center = meshFilter.sharedMesh.bounds.center;
                collider.size = meshFilter.sharedMesh.bounds.size;
                PrefabUtility.SaveAsPrefabAsset(prop, propsFolder + "/" + prop.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(prop);
            }
            UnityEngine.Object.DestroyImmediate(temporary);
        }

        private static void PlaceGroundedIndependentProps(Transform parent)
        {
            GameObject propsRoot = Child(parent, "O04_IndependentGroundedProps");
            string propsFolder = Prefabs + "/Props";
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { propsFolder });
            List<GameObject> prefabs = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name)
                .Take(24)
                .ToList();
            Vector3[] positions =
            {
                new(-27f, 0f, 20f), new(-27.8f, 0f, 20.8f), new(-25.5f, 0f, 19.4f), new(25f, 0f, 20f),
                new(25.8f, 0f, 20.7f), new(23.5f, 0f, 19.5f), new(-16f, 0f, 19f), new(-11f, 0f, 21f),
                new(-8f, 0f, 27f), new(11f, 0f, 28f), new(17f, 0f, 29f), new(23f, 0f, 27f),
                new(-29f, 0f, 12f), new(-25f, 0f, 14f), new(27f, 0f, 12f), new(31f, 0f, 15f),
                new(-32f, 0f, 31f), new(-28f, 0f, 34f), new(28f, 0f, 33f), new(33f, 0f, 35f),
                new(-5f, 0f, 34f), new(6f, 0f, 34f), new(-18f, 0f, 39f), new(18f, 0f, 40f)
            };
            for (int i = 0; i < prefabs.Count && i < positions.Length; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
                instance.name = "PropGrounded_" + (i + 1).ToString("00") + "_" + prefabs[i].name;
                instance.transform.SetParent(propsRoot.transform);
                instance.transform.SetPositionAndRotation(positions[i], Quaternion.Euler(0f, (i * 37f) % 360f, 0f));
                Physics.SyncTransforms();
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                float minimumY = renderers.Min(renderer => renderer.bounds.min.y);
                instance.transform.position += Vector3.down * minimumY;
            }
        }

        private static GameObject Cube(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material wood, bool addCollider)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = scale;
            if (wood != null) cube.GetComponent<Renderer>().sharedMaterial = wood;
            if (!addCollider) UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            return cube;
        }

        private static void Capture(Transform root, Vector3 position, Quaternion rotation, float fieldOfView, string path, float nearClip = 0.3f, float farClip = 1000f)
        {
            GameObject cameraObject = new GameObject("CaptureCamera_验收相机");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = nearClip;
            camera.farClipPlane = farClip;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.07f, 0.072f);
            RenderTexture target = new RenderTexture(1600, 900, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureWithInspectionFill(Vector3 position, Quaternion rotation, string path)
        {
            GameObject fillObject = new GameObject("RoomQA_InspectionFill_验收补光");
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.66f, 0.73f, 0.70f);
            fill.intensity = 1.15f;
            fill.range = 8f;
            fill.shadows = LightShadows.None;
            fillObject.transform.position = position + Vector3.up * .15f;
            try
            {
                Capture(null, position, rotation, 67f, path, .12f, 45f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fillObject);
            }
        }

        private static void CaptureHeightSlice(Transform sceneRoot, Vector3 position, float fieldOfView, string path, float minY, float maxY)
        {
            Renderer[] renderers = sceneRoot.GetComponentsInChildren<Renderer>(true);
            bool[] states = renderers.Select(renderer => renderer.enabled).ToArray();
            try
            {
                for (int index = 0; index < renderers.Length; index++)
                    renderers[index].enabled = states[index] && renderers[index].bounds.center.y >= minY && renderers[index].bounds.center.y <= maxY;
                Capture(sceneRoot, position, Quaternion.Euler(90f, 0f, 0f), fieldOfView, path);
            }
            finally
            {
                for (int index = 0; index < renderers.Length; index++) renderers[index].enabled = states[index];
            }
        }

        private static void EnsureFolders()
        {
            foreach (string path in new[] { Textures, Materials, Prefabs })
            {
                string current = "Assets";
                foreach (string part in path.Substring(7).Split('/'))
                {
                    string next = current + "/" + part;
                    if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                    current = next;
                }
            }
        }

        private static GameObject Child(Transform parent, string name) => Child(parent.gameObject, name);
        private static GameObject Child(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = Find(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
