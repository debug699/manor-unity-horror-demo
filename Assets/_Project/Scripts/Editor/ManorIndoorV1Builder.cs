#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Manor.Gameplay;
using Manor.Player;
using UnityEngine.InputSystem;

namespace Manor.Editor
{
    /// <summary>Creates a separate, non-destructive indoor traversal scene.</summary>
    public static class ManorIndoorV1Builder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorInteriorV1_室内首版.unity";

        [MenuItem("庄园/室内V1/创建室内白盒场景")]
        public static void BuildIndoorV1()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("ManorInteriorV1_室内首版");
            Transform env = NewGroup(root, "Managed_InteriorV1/Environment");
            Transform gameplay = NewGroup(root, "Managed_InteriorV1/Gameplay");
            Transform markers = NewGroup(root, "Managed_InteriorV1/Markers");
            Transform art = NewGroup(root, "Managed_InteriorV1/ArtVisuals_NoCollision");

            Material floorMat = MakeMaterial(new Color(0.16f, 0.12f, 0.10f));
            Material wallMat = MakeMaterial(new Color(0.08f, 0.07f, 0.07f));
            Material markerMat = MakeMaterial(new Color(0.35f, 0.12f, 0.12f));

            // Main floor: two wings connected by a central corridor. Open corridors avoid invisible blockers.
            CreateBox("MainCorridor_G05", env, new Vector3(0f, -0.1f, 0f), new Vector3(26f, 0.2f, 3f), floorMat, true);
            CreateBox("UpperCorridor_G06", env, new Vector3(0f, 3.2f, 4.5f), new Vector3(26f, 0.2f, 3f), floorMat, true);

            string[] ground = { "G01_玄关", "G02_客厅套间", "G03_厨房套间", "G04_屠宰工作间", "G07_后门通道", "G12_旧客房过渡区" };
            string[] upper = { "G08_孕妇房", "G09_儿童房双区_占位", "G10_研究室", "G11_祷告室", "G06_楼梯间", "G05_主走廊" };
            for (int i = 0; i < ground.Length; i++)
            {
                float x = -10.5f + i * 4.2f;
                CreateRoom(ground[i], env, new Vector3(x, 0f, -4.5f), floorMat, wallMat, true, DoorSide.North);
                CreateBox(ground[i] + "_CorridorThreshold", env, new Vector3(x, -0.1f, -1.95f), new Vector3(1.1f, .2f, 1.2f), floorMat, true);
                AddRoomArt(art, ground[i], new Vector3(x, .02f, -4.5f), 3.4f);
            }
            for (int i = 0; i < upper.Length; i++)
            {
                float x = -10.5f + i * 4.2f;
                CreateRoom(upper[i], env, new Vector3(x, 3.3f, 4.5f), floorMat, wallMat, true, DoorSide.South);
                CreateBox(upper[i] + "_CorridorThreshold", env, new Vector3(x, 3.2f, 1.95f), new Vector3(1.1f, .2f, 1.2f), floorMat, true);
                AddRoomArt(art, upper[i], new Vector3(x, 3.32f, 4.5f), 3.4f);
            }

            CreateStairs("G06_楼梯连接", env, new Vector3(-10.5f, 0f, 1.8f), floorMat);
            CreateBasementStairs("G03_厨房活板门至B01", env, new Vector3(-2.1f, -.25f, -3.5f), floorMat);
            CreateRoom("B01_地下服务通道_白盒", env, new Vector3(-2.1f, -3.0f, 2.5f), floorMat, wallMat, true, DoorSide.North);
            CreateRoom("B02_地下祭祀室_白盒", env, new Vector3(-2.1f, -3.0f, 9.5f), floorMat, wallMat, true, DoorSide.South, 8f, 6f);
            CreateBox("B01_B02_地下连接", env, new Vector3(-2.1f, -3.1f, 6f), new Vector3(2.2f, 0.2f, 3f), floorMat, true);
            AddBasementArt(art, new Vector3(-2.1f, -2.88f, 9.5f));

            // A real interaction pivot, deliberately independent from the high-poly house shell.
            CreateDoor("G01_主入口可开门", gameplay, new Vector3(-10.5f, 1f, -1.55f), "INT_DOOR_INDOOR_G01");
            CreateDoor("G03_厨房活板门占位", gameplay, new Vector3(-2.1f, 0.1f, -4.5f), "INT_HATCH_INDOOR_G03");
            CreateTransition("ENT_返回庭院", gameplay, new Vector3(-10.5f, 1.5f, -1.9f), "INT_RETURN_COURTYARD", "SCN_ManorDemo_庄园Demo", "PROMPT_RETURN_COURTYARD");
            CreateVaultWindow(gameplay, new Vector3(6.3f, 1.1f, -2.45f), new Vector3(6.3f, .15f, -.8f));
            CreatePickup("ITEM_PHONE_BROKEN", gameplay, new Vector3(4.2f, 3.75f, 4.1f), PrimitiveType.Cube, new Vector3(.22f, .04f, .38f));
            CreatePickup("ITEM_FLASHLIGHT_DEAD", gameplay, new Vector3(.0f, .35f, -4.0f), PrimitiveType.Cylinder, new Vector3(.08f, .22f, .08f));
            CreatePickup("ITEM_MEDICINE_BOTTLE", gameplay, new Vector3(-4.2f, .35f, -4.0f), PrimitiveType.Cylinder, new Vector3(.12f, .18f, .12f));
            CreatePickup("ITEM_MATCHBOX", gameplay, new Vector3(-8.4f, .35f, -4.0f), PrimitiveType.Cube, new Vector3(.22f, .07f, .14f));

            CreateMarker("PlayerSpawn_G11_正式开场", markers, new Vector3(4.2f, 3.35f, 4.5f), markerMat);
            CreateMarker("B02_RitualAnchor", markers, new Vector3(-2.1f, -2.85f, 9.5f), markerMat);
            CreatePlayer(gameplay, new Vector3(4.2f, 3.35f, 4.5f));

            RenderSettings.ambientLight = new Color(0.025f, 0.02f, 0.03f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.04f, 0.035f, 0.05f);
            RenderSettings.fogDensity = 0.018f;
            DirectoryEnsure(ScenePath);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Manor][IndoorV1] Created separate indoor whitebox: " + ScenePath);
        }

        private static Transform NewGroup(GameObject root, string path)
        {
            Transform parent = root.transform;
            foreach (string part in path.Split('/'))
            {
                GameObject child = new GameObject(part);
                child.transform.SetParent(parent);
                parent = child.transform;
            }
            return parent;
        }

        private enum DoorSide { North, South }

        private static void CreateRoom(string name, Transform parent, Vector3 center, Material floor, Material wall, bool withDoor, DoorSide doorSide, float width = 3.8f, float depth = 4.2f)
        {
            Transform room = new GameObject(name).transform;
            room.SetParent(parent);
            room.position = center;
            CreateBox(name + "_Floor", room, Vector3.zero, new Vector3(width, 0.2f, depth), floor, true);
            float h = 3f, t = 0.18f;
            CreateBox(name + "_Wall_W", room, new Vector3(-width * .5f, h * .5f, 0f), new Vector3(t, h, depth), wall, true);
            CreateBox(name + "_Wall_E", room, new Vector3(width * .5f, h * .5f, 0f), new Vector3(t, h, depth), wall, true);
            CreateDoorWall(name, room, wall, width, depth, h, t, withDoor && doorSide == DoorSide.North, DoorSide.North);
            CreateDoorWall(name, room, wall, width, depth, h, t, withDoor && doorSide == DoorSide.South, DoorSide.South);
        }

        private static void CreateDoorWall(string name, Transform room, Material wall, float width, float depth, float height, float thickness, bool opening, DoorSide side)
        {
            float z = side == DoorSide.North ? depth * .5f : -depth * .5f;
            string suffix = side == DoorSide.North ? "N" : "S";
            if (!opening) { CreateBox(name + "_Wall_" + suffix, room, new Vector3(0f, height * .5f, z), new Vector3(width, height, thickness), wall, true); return; }
            CreateBox(name + "_Wall_" + suffix + "_Left", room, new Vector3(-width * .32f, height * .5f, z), new Vector3(width * .36f, height, thickness), wall, true);
            CreateBox(name + "_Wall_" + suffix + "_Right", room, new Vector3(width * .32f, height * .5f, z), new Vector3(width * .36f, height, thickness), wall, true);
        }

        private static void CreateStairs(string name, Transform parent, Vector3 start, Material mat)
        {
            for (int i = 0; i < 10; i++) CreateBox(name + "_Step_" + i, parent, start + new Vector3(0f, i * .32f, i * .42f), new Vector3(2.4f, .3f, .5f), mat, true);
        }

        private static void CreateBasementStairs(string name, Transform parent, Vector3 start, Material mat)
        {
            for (int i = 0; i < 10; i++) CreateBox(name + "_Step_" + i, parent, start + new Vector3(0f, -i * .3f, i * .4f), new Vector3(1.8f, .3f, .48f), mat, true);
        }

        private static void CreateDoor(string name, Transform parent, Vector3 pos, string id)
        {
            GameObject pivot = new GameObject(name + "_Pivot"); pivot.transform.SetParent(parent); pivot.transform.position = pos;
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube); leaf.name = name + "_Leaf"; leaf.transform.SetParent(pivot.transform); leaf.transform.localPosition = new Vector3(.45f, 0f, 0f); leaf.transform.localScale = new Vector3(.9f, 2f, .12f);
            DoorInteractable door = leaf.AddComponent<DoorInteractable>(); door.ConfigureDoor(id, pivot.transform, string.Empty);
        }

        private static void CreatePickup(string itemId, Transform parent, Vector3 pos, PrimitiveType primitive, Vector3 scale)
        {
            GameObject item = GameObject.CreatePrimitive(primitive); item.name = itemId + "_可拾取样例"; item.transform.SetParent(parent); item.transform.position = pos; item.transform.localScale = scale;
            ItemPickupInteractable pickup = item.AddComponent<ItemPickupInteractable>(); pickup.ConfigureItem("INT_" + itemId, itemId);
        }

        private static void CreateTransition(string name, Transform parent, Vector3 pos, string id, string targetScene, string promptKey)
        {
            GameObject entry = new GameObject(name); entry.transform.SetParent(parent); entry.transform.position = pos;
            BoxCollider trigger = entry.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(1.5f, 3f, .5f);
            SceneTransitionInteractable transition = entry.AddComponent<SceneTransitionInteractable>(); transition.ConfigureTransition(id, targetScene, promptKey);
        }

        private static void CreateVaultWindow(Transform parent, Vector3 windowPosition, Vector3 landingPosition)
        {
            GameObject landing = new GameObject("G07_翻窗落点"); landing.transform.SetParent(parent); landing.transform.position = landingPosition; landing.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            GameObject trigger = new GameObject("G07_可翻越窗户"); trigger.transform.SetParent(parent); trigger.transform.position = windowPosition;
            BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>(); triggerCollider.isTrigger = true; triggerCollider.size = new Vector3(1.2f, 1.4f, .2f);
            VaultWindowInteractable vault = trigger.AddComponent<VaultWindowInteractable>(); vault.ConfigureWindow("INT_VAULT_WINDOW_G07", landing.transform);
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Environment/SourceModels/Interactables/SM_VaultWindow.fbx");
            if (source == null) return;
            GameObject visual = PrefabUtility.InstantiatePrefab(source, trigger.transform) as GameObject;
            visual.name = "ART_G07_翻窗窗户"; visual.transform.localPosition = Vector3.zero;
            Bounds bounds = RenderBounds(visual); float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > .001f) visual.transform.localScale = Vector3.one * (1.2f / footprint);
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        }

        private static void AddRoomArt(Transform parent, string roomName, Vector3 floorCenter, float maxFootprint)
        {
            string assetName = roomName.StartsWith("G02") ? "已拆开，删除椅子G02" : "已拆开" + roomName.Substring(0, 3);
            string assetPath = "Assets/_Project/Art/Environment/SourceModels/Rooms/" + assetName + ".fbx";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (source == null) return; // G09 deliberately remains an explicit placeholder.
            GameObject instance = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            if (instance == null) return;
            instance.name = "ART_" + roomName + "_视觉副本";
            instance.transform.position = floorCenter;
            Bounds sourceBounds = RenderBounds(instance);
            float footprint = Mathf.Max(sourceBounds.size.x, sourceBounds.size.z);
            if (footprint > .001f) instance.transform.localScale = Vector3.one * (maxFootprint / footprint);
            Bounds finalBounds = RenderBounds(instance);
            instance.transform.position += Vector3.up * (floorCenter.y - finalBounds.min.y);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        }

        private static Bounds RenderBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void AddBasementArt(Transform parent, Vector3 chamberCenter)
        {
            AddSingleArt(parent, "SM_RitualCircle", chamberCenter, 3.2f);
            AddSingleArt(parent, "SM_SaintStatue", chamberCenter + new Vector3(0f, 0f, 2.25f), 1.1f);
            AddSingleArt(parent, "SM_SoulNail_Prototype", chamberCenter + new Vector3(-1.9f, 0f, -1.4f), .35f);
            AddSingleArt(parent, "SM_SoulNail_Prototype", chamberCenter + new Vector3(1.9f, 0f, -1.4f), .35f);
            AddSingleArt(parent, "SM_KitchenHatch", new Vector3(-2.1f, .02f, -4.5f), 1.5f);
        }

        private static void AddSingleArt(Transform parent, string assetName, Vector3 position, float maxFootprint)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Environment/SourceModels/Basement/" + assetName + ".fbx");
            if (source == null) return;
            GameObject instance = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            instance.name = "ART_" + assetName;
            instance.transform.position = position;
            Bounds bounds = RenderBounds(instance); float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > .001f) instance.transform.localScale = Vector3.one * (maxFootprint / footprint);
            bounds = RenderBounds(instance); instance.transform.position += Vector3.up * (position.y - bounds.min.y);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        }

        private static void CreateMarker(string name, Transform parent, Vector3 pos, Material mat)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); marker.name = name; marker.transform.SetParent(parent); marker.transform.position = pos; marker.transform.localScale = new Vector3(.25f, .05f, .25f); marker.GetComponent<Renderer>().sharedMaterial = mat; Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, bool collider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; box.transform.SetParent(parent); box.transform.localPosition = localPos; box.transform.localScale = size; box.GetComponent<Renderer>().sharedMaterial = mat; if (!collider) Object.DestroyImmediate(box.GetComponent<Collider>()); return box;
        }

        private static Material MakeMaterial(Color color) { Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"); return new Material(shader) { color = color }; }
        private static void CreatePlayer(Transform parent, Vector3 position)
        {
            GameObject player = new GameObject("Player_Tom_第一人称"); player.transform.SetParent(parent); player.transform.position = position;
            CharacterController cc = player.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = .35f; cc.stepOffset = .3f;
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/IA_Player_玩家输入.inputactions");
            PlayerInputReader input = player.AddComponent<PlayerInputReader>(); input.Configure(inputAsset);
            GameObject cameraObject = new GameObject("PlayerCamera_第一人称摄像机"); cameraObject.transform.SetParent(player.transform); cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera"; FirstPersonCamera view = cameraObject.AddComponent<FirstPersonCamera>(); view.Configure(player.transform);
            InteractionScanner scanner = player.AddComponent<InteractionScanner>(); scanner.Configure(camera, null, 2.5f);
            FirstPersonController controller = player.AddComponent<FirstPersonController>(); controller.Configure(input, view, scanner);
            CreateFirstPersonHands(cameraObject.transform, scanner);
        }

        private static void CreateFirstPersonHands(Transform cameraTransform, InteractionScanner scanner)
        {
            GameObject holder = new GameObject("TomHands_第一人称手部"); holder.transform.SetParent(cameraTransform); holder.transform.localPosition = Vector3.zero; holder.transform.localRotation = Quaternion.identity;
            Transform left = InstantiateHand("Tom_FirstPerson_LeftArm", holder.transform, new Vector3(-.32f, -.48f, .52f));
            Transform right = InstantiateHand("Tom_FirstPerson_RightArm", holder.transform, new Vector3(.32f, -.48f, .52f));
            FirstPersonHandsPresenter presenter = holder.AddComponent<FirstPersonHandsPresenter>(); presenter.Configure(left, right, scanner);
        }

        private static Transform InstantiateHand(string assetName, Transform parent, Vector3 localPosition)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Characters/FirstPerson/" + assetName + ".fbx");
            if (source == null) return null;
            GameObject hand = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            hand.name = assetName + "_镜头显示"; hand.transform.localPosition = localPosition; hand.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); hand.transform.localScale = Vector3.one * 1.7f;
            foreach (Collider collider in hand.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            return hand.transform;
        }
        private static void DirectoryEnsure(string assetPath) { string full = System.IO.Path.GetFullPath(assetPath); string dir = System.IO.Path.GetDirectoryName(full); if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir); }
    }
}
#endif
