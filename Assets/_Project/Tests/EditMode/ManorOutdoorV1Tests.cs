#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace Manor.Tests.EditMode
{
    public sealed class ManorOutdoorV1Tests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";
        private Scene _scene;

        [SetUp]
        public void OpenScene() => _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        [Test]
        public void OutdoorV1ContainsRequiredLayoutAndArt()
        {
            Assert.That(Find("OutdoorArt_v1_首版室外美术"), Is.Not.Null);
            Assert.That(Find("PF_NewUserManor_新版主楼"), Is.Not.Null);
            Assert.That(Find("PF_UserWorkshopHut_加工木屋主用"), Is.Not.Null);
            Assert.That(Find("PF_UserAbandonedHut_废弃木屋主用"), Is.Not.Null);
            Assert.That(Find("PF_UserWoodBridge_用户木桥"), Is.Not.Null);
            Assert.That(Find("TerrainBase_地皮"), Is.Null, "A continuous terrain slab would let the player bypass the cut bridge.");
            Assert.That(Find("TerrainSouth_主楼侧地皮"), Is.Not.Null);
            Assert.That(Find("TerrainNorth_大门侧地皮"), Is.Not.Null);
            Assert.That(Find("RavineFallTrigger_裂谷坠落区").GetComponent<Manor.Gameplay.FallDeathTrigger>(), Is.Not.Null);
            Assert.That(Find("EstateBoundary_庄园外围闭环"), Is.Not.Null);
            Assert.That(Find("PF_UserGate_用户大门"), Is.Not.Null);
        }

        [Test]
        public void ManorGateHasHingedInteractableDoor()
        {
            Transform pivot = Find("UserGateDoorPivot_用户大门铰链");
            Transform leaf = Find("UserDoorLeaf_庄园大门_用户门扇");
            Assert.That(pivot, Is.Not.Null);
            Assert.That(leaf, Is.Not.Null);
            Assert.That(leaf.parent, Is.EqualTo(pivot));
            Assert.That(leaf.GetComponent<Manor.Gameplay.DoorInteractable>(), Is.Not.Null);
            Assert.That(leaf.GetComponentsInChildren<Collider>(true), Is.Not.Empty);
        }

        [Test]
        public void ManorGateUsesTheRequested1016PasswordOnAnIndependentKeypad()
        {
            Transform keypad = Find("GateKeypad_密码输入器");
            Assert.That(keypad, Is.Not.Null);
            Manor.Gameplay.GatePasswordInteractable password = keypad.GetComponent<Manor.Gameplay.GatePasswordInteractable>();
            Assert.That(password, Is.Not.Null);
            Assert.That(password.CorrectPassword, Is.EqualTo("1016"));
            Assert.That(Find("UserDoorLeaf_庄园大门_用户门扇").GetComponent<Manor.Gameplay.DoorInteractable>(), Is.Not.Null);
        }

        [Test]
        public void PlayerStartsInsideGateFacingManor()
        {
            Transform player = Find("Player_汤姆测试胶囊");
            Assert.That(player.position.z, Is.GreaterThan(30f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(player.eulerAngles.y, 180f)), Is.LessThan(1f));
        }

        [Test]
        public void SplitCourtyardPropsAreAvailableAsIndependentPrefabs()
        {
            string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Environment/Props" });
            Assert.That(prefabs.Length, Is.GreaterThanOrEqualTo(30));
        }

        [Test]
        public void ConfirmedContinuousLayoutIsEnabledAndEveryLegacyGreyboxAreaIsDisabled()
        {
            Transform environment = FindByPrefix("Environment_");
            Assert.That(environment, Is.Not.Null);
            Transform confirmed = Find("ManagedInteriorV2_连续主楼布局");
            Transform ground = Find("GroundLevel_一层区域");
            Transform upper = Find("UpperLevel_二层区域");
            Transform basement = Find("BasementLevel_地下区域");
            Transform obsoleteOutdoor = Find("OutdoorRoute_室外路线");
            Assert.That(confirmed, Is.Not.Null);
            Assert.That(confirmed.GetComponentsInChildren<Collider>(true).All(collider => collider.enabled), Is.True);
            Assert.That(ground.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled), Is.True);
            Assert.That(upper.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled), Is.True);
            Assert.That(basement.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled), Is.True);
            Assert.That(obsoleteOutdoor.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled), Is.True);
        }

        [Test]
        public void MainHouseFacesGateAtDoubleScaleAndHasEnterableDoor()
        {
            Transform house = Find("PF_NewUserManor_新版主楼");
            Transform pivot = Find("ManorDoorPivot_HouseEntrance");
            Transform leaf = Find("UserDoorLeaf_主楼入口_用户门扇");

            Assert.That(house, Is.Not.Null);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(house.eulerAngles.y, 180f)), Is.LessThan(1f),
                "The authored front porch must face the estate gate after its source-axis correction.");
            Assert.That(RendererBounds(house).size.x, Is.GreaterThan(35f));
            Assert.That(house.GetComponent<BoxCollider>(), Is.Null,
                "A whole-building box collider blocks the entrance and interior.");
            Assert.That(pivot, Is.Not.Null);
            Assert.That(leaf, Is.Not.Null);
            Assert.That(leaf.parent, Is.EqualTo(pivot));
            Assert.That(leaf.GetComponent<Manor.Gameplay.DoorInteractable>(), Is.Not.Null);
        }

        [Test]
        public void OpenManorUsesAContinuousSameSceneInteriorWithoutTeleportTransition()
        {
            Transform entrance = Find("ENT_进入主楼室内");
            Assert.That(entrance, Is.Null, "The main entrance must not load another scene or teleport the player.");
            Assert.That(Find("InteriorArt_v2_连续主楼陈设"), Is.Not.Null);
            Assert.That(Find("G06_实体楼梯").GetComponentsInChildren<Collider>(true).All(c => c.enabled), Is.True);
        }

        [Test]
        public void ConfirmedLayoutKeepsG12UpstairsAndKitchenAsTheOnlyBasementEntrance()
        {
            Transform g03 = Find("G03_厨房套间");
            Transform g06 = Find("G06_楼梯间");
            Transform g12 = Find("G12_旧客房与封闭过渡空间");
            Assert.That(g03, Is.Not.Null);
            Assert.That(g06, Is.Not.Null);
            Assert.That(g12, Is.Not.Null);
            Assert.That(g03.position.y, Is.LessThan(.5f));
            Assert.That(g12.position.y, Is.GreaterThan(3f), "G12 is a confirmed second-floor space.");
            Assert.That(Find("G03_活板门至B01"), Is.Not.Null);
            Assert.That(Find("B01_地下服务路线"), Is.Not.Null);
            Assert.That(Find("B02_地下祭祀室"), Is.Not.Null);
            Assert.That(Find("G06_地下楼梯"), Is.Null, "G06 only retains a sealed narrative hint, not an active entrance.");
        }

        [Test]
        public void ConfirmedLayoutHasContinuousThresholdsAndAnOpenKitchenStairwell()
        {
            string[] thresholds =
            {
                "G05_G06_连续门槛",
                "G06_G07_连续通道",
                "G07_后门门槛",
                "UpperCorridor_G08_连续门槛",
                "UpperCorridor_G10_连续门槛",
                "UpperCorridor_G11_连续门槛",
                "B01_B02_地下连接"
            };
            foreach (string threshold in thresholds)
            {
                Transform found = Find(threshold);
                Assert.That(found, Is.Not.Null, threshold);
                Assert.That(found.GetComponent<Collider>(), Is.Not.Null, threshold + " must support the player capsule.");
            }

            Assert.That(Find("G03_厨房套间_Floor"), Is.Null,
                "The kitchen cannot keep a single uncut floor over the basement stairs.");
            Assert.That(Find("G03_厨房套间_Floor_Left"), Is.Not.Null);
            Assert.That(Find("G03_厨房套间_Floor_Right"), Is.Not.Null);
            Transform basementStep = Find("G03_活板门至B01_Step_00");
            Assert.That(basementStep.localScale.z, Is.GreaterThanOrEqualTo(1.99f),
                "X-axis basement stairs must be wide across Z for the player capsule.");
            Assert.That(basementStep.localScale.x, Is.LessThan(.5f),
                "The run dimension belongs on X when descending along X.");
            Assert.That(Find("ManorInteriorFloor_EmptyWalkableShell"), Is.Null,
                "A whole-building floor would seal the hatch and hide missing route geometry.");
            Assert.That(Find("ManorCollision_VisibleWallsOnly"), Is.Null,
                "A separate 15 m invisible collision shell must not override the authored manor and continuous room walls.");

            Transform b01 = Find("B01_地下服务路线");
            Assert.That(b01.position.z, Is.EqualTo(-8f).Within(.01f),
                "The descending stairs must meet B01's west doorway instead of a closed north wall.");
        }

        [Test]
        public void G11LockpickAndG10ResearchKeyFormTheRequiredOpeningChain()
        {
            Transform wire = Find("Pickup_UpperCorridor_LockpickWire_门外细铁丝");
            Transform prayerDoor = Find("UserDoorLeaf_Door_G11_用户门扇");
            Transform researchKey = Find("Pickup_G10_ResearchKey_研究室钥匙");
            Transform researchDoor = Find("UserDoorLeaf_Door_G10_用户门扇");
            Assert.That(wire.GetComponent<Manor.Gameplay.ItemPickupInteractable>(), Is.Not.Null);
            Assert.That(wire.position.x, Is.GreaterThan(-5f), "The wire must remain in the corridor outside the locked G11 door.");
            Assert.That(prayerDoor.GetComponent<Manor.Gameplay.LockpickDoorInteractable>(), Is.Not.Null);
            Assert.That(researchKey.GetComponent<Manor.Gameplay.KeyInteractable>(), Is.Not.Null);
            Assert.That(researchDoor.GetComponent<Manor.Gameplay.DoorInteractable>(), Is.Not.Null);
            Assert.That(Find("ManagedInteriorV2_连续主楼布局").GetComponent<Manor.Narrative.StoryFlowController>(), Is.Not.Null);
        }

        [Test]
        public void KitchenHatchRequiresThreeFormalCluesAndLeadsIntoBasementRegions()
        {
            Assert.That(Find("Clue_G08_BedDiary_床下日记").GetComponent<Manor.Gameplay.NoteInteractable>(), Is.Not.Null);
            Assert.That(Find("Clue_G04_Traces_拖痕石灰木屑").GetComponent<Manor.Gameplay.NoteInteractable>(), Is.Not.Null);
            Assert.That(Find("Clue_G10_RitualRecord_残缺阵图").GetComponent<Manor.Gameplay.NoteInteractable>(), Is.Not.Null);
            Assert.That(Find("Clue_G09_BoyFamily_男童家庭遗物").GetComponent<Manor.Gameplay.NoteInteractable>(), Is.Not.Null);
            Assert.That(Find("Clue_G09_GirlFamily_女童家庭遗物").GetComponent<Manor.Gameplay.NoteInteractable>(), Is.Not.Null);
            Assert.That(Find("G03_KitchenHatch_厨房活板门_Leaf").GetComponent<Manor.Gameplay.BasementHatchInteractable>(), Is.Not.Null);
            Assert.That(Find("B01_EntryRegion_地下入口触发").GetComponent<Manor.Narrative.StoryRegionTrigger>(), Is.Not.Null);
            Assert.That(Find("B02_RitualRegion_祭祀室触发").GetComponent<Manor.Narrative.StoryRegionTrigger>(), Is.Not.Null);
        }

        [Test]
        public void ParchmentChaseBridgePasswordAndEscapeFormOneEndingChain()
        {
            Assert.That(Find("Pickup_B02_CompleteParchment_完整羊皮纸").GetComponent<Manor.Gameplay.ItemPickupInteractable>(), Is.Not.Null);
            Assert.That(Find("B02_ParchmentCommunication_羊皮纸沟通").GetComponent<Manor.Narrative.ParchmentCommunicationInteractable>(), Is.Not.Null);
            Assert.That(Find("ManagedInteriorV2_连续主楼布局").GetComponent<Manor.Gameplay.ButcherChaseTimer>(), Is.Not.Null);
            Assert.That(Find("BridgeMechanism_桥梁机关").GetComponent<Manor.Gameplay.BridgeMechanismInteractable>(), Is.Not.Null);
            Assert.That(Find("EscapeTrigger_唯一逃生触发").GetComponent<Manor.Narrative.EscapeTrigger>(), Is.Not.Null);
            Assert.That(Find("Player_汤姆测试胶囊").GetComponent<Manor.Runtime.PlayerCheckpointRestorer>(), Is.Not.Null);
        }

        [Test]
        public void ContinuousSceneContainsFiniteWraithAndButcherControllers()
        {
            Assert.That(Find("PregnantWraith_孕妇怨灵临时形体").GetComponent<Manor.Gameplay.PregnantWraithController>(), Is.Not.Null);
            Assert.That(Find("Butcher_屠夫临时角色").GetComponent<Manor.Gameplay.ButcherController>(), Is.Not.Null);
            Assert.That(Find("PregnantWraith_孕妇怨灵临时形体").GetComponent<UnityEngine.AI.NavMeshAgent>(), Is.Not.Null);
            Assert.That(Find("WraithVisual_非性感化占位").GetComponentsInChildren<Renderer>(true).All(renderer => !renderer.enabled), Is.True,
                "The G08 wraith must remain visually hidden in the authored scene until its diary trigger is read.");
            Assert.That(Find("Butcher_屠夫临时角色").GetComponent<UnityEngine.AI.NavMeshAgent>(), Is.Not.Null);
            Assert.That(FindByPrefix("Environment_").GetComponent<Unity.AI.Navigation.NavMeshSurface>(), Is.Not.Null);
            Assert.That(Find("WraithPoint_G08"), Is.Not.Null);
            Assert.That(Find("ButcherPoint_B02_SouthEast"), Is.Not.Null);
        }

        [Test]
        public void ChaseActorsSupportSoundInvestigationAndMovingSearch()
        {
            Assert.That(System.Enum.GetNames(typeof(Manor.Gameplay.ButcherController.ButcherState)), Does.Contain("Investigate"));
            Assert.That(System.Enum.GetNames(typeof(Manor.Gameplay.PregnantWraithController.WraithState)), Does.Contain("Investigate"));
            Assert.That(typeof(Manor.Gameplay.NoiseBus).GetEvent("Emitted"), Is.Not.Null);
        }

        [Test]
        public void ChaseRouteHasOneWorkingHideSpotAndBreakableBoard()
        {
            Transform hide = Find("HideWardrobeTrigger_隐藏柜交互");
            Transform board = Find("BreakableBoard_Courtyard_屠夫木板");
            Assert.That(hide.GetComponent<Manor.Gameplay.HideSpotInteractable>(), Is.Not.Null);
            Assert.That(hide.GetComponent<BoxCollider>().isTrigger, Is.True);
            Assert.That(board.GetComponent<Manor.Gameplay.BreakableBoard>(), Is.Not.Null);
            Assert.That(board.GetComponent<BoxCollider>(), Is.Not.Null);
        }

        [Test]
        public void ContinuousSceneHasAVisibleHingedG07VaultWindow()
        {
            Assert.That(Find("G07_WindowLeaf_真实窗扇"), Is.Not.Null);
            Transform trigger = Find("G07_VaultTrigger_翻窗交互");
            Assert.That(trigger.GetComponent<Manor.Gameplay.VaultWindowInteractable>(), Is.Not.Null);
            Assert.That(trigger.GetComponent<BoxCollider>().isTrigger, Is.True);
            Assert.That(Find("G07_VaultLanding_外侧落点"), Is.Not.Null);
            Assert.That(Find("G07_VaultLanding_内侧落点"), Is.Not.Null);
            Assert.That(Find("G07_WindowSill_实体窗台").GetComponent<BoxCollider>(), Is.Not.Null);
        }

        [Test]
        public void OutbuildingsUseUserAuthoredModelsWithoutDuplicateBoardShells()
        {
            foreach (string visualName in new[] { "PF_UserWorkshopHut_加工木屋主用", "PF_UserAbandonedHut_废弃木屋主用", "PF_UserStoneWorkshop_木石工坊主用" })
                Assert.That(Find(visualName).GetComponentsInChildren<MeshCollider>(true), Is.Not.Empty, visualName);
            Assert.That(Find("O02_Workshop_可进入工棚"), Is.Null);
            Assert.That(Find("O03_AbandonedHut_可进入废屋"), Is.Null);
            Assert.That(Find("O04_StoneWorkshop_可进入石工坊"), Is.Null);
        }

        [Test]
        public void OutbuildingDoorsAreGroundedAlignedToTheirAuthoredWallsAndUseTheRequestedModels()
        {
            AssertDoor("PF_UserWorkshopHut_加工木屋主用", "UserDoor_O02_Pivot", "UserDoorLeaf_UserDoor_O02_用户门扇", "d7258bac", 0.52f, 0.08f);
            AssertDoor("PF_UserAbandonedHut_废弃木屋主用", "UserDoor_O03_Pivot", "UserDoorLeaf_UserDoor_O03_用户门扇", "d06d413c", 0f, 0.08f);
            AssertDoor("PF_UserStoneWorkshop_木石工坊主用", "UserDoor_O04_Pivot", "UserDoorLeaf_UserDoor_O04_用户门扇", "d7258bac", 0f, 0.08f);
        }

        [Test]
        public void RavineAndEstateBoundaryPreventSideBypass()
        {
            BoxCollider fall = Find("RavineFallTrigger_裂谷坠落区").GetComponent<BoxCollider>();
            Assert.That(fall.size.x, Is.GreaterThanOrEqualTo(88f));
            Assert.That(Find("CliffWest_裂谷西侧").position.y, Is.LessThanOrEqualTo(-4f));
            Assert.That(Find("BoundaryWest_西侧腐朽围墙").GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(Find("BoundaryEast_东侧腐朽围墙").GetComponent<BoxCollider>(), Is.Not.Null);
        }

        [Test]
        public void TreesHaveTrunkOnlyCollision()
        {
            for (int index = 1; index <= 8; index++)
            {
                Transform tree = Find("PF_DeadTree_" + index.ToString("00"));
                Assert.That(tree.GetComponent<CapsuleCollider>(), Is.Not.Null);
                Assert.That(tree.GetComponent<MeshCollider>(), Is.Null);
            }
        }

        [Test]
        public void ImportedRoomDressingIsGroundedAndCannotCreateAirWalls()
        {
            Transform art = Find("InteriorArt_v2_连续主楼陈设");
            Assert.That(art, Is.Not.Null);
            Assert.That(art.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(100));
            Collider[] colliders = art.GetComponentsInChildren<Collider>(true);
            Assert.That(colliders.All(collider => collider is BoxCollider), Is.True,
                "Only authored simple furniture colliders are allowed; imported fragment MeshColliders create air walls.");
            Assert.That(colliders.All(collider => !collider.transform.name.StartsWith("ART_G0") && !collider.transform.name.StartsWith("ART_G1")), Is.True,
                "Whole imported room FBXs must remain visual-only.");
            Assert.That(Find("ART_G01_精选子件"), Is.Not.Null);
            Assert.That(Find("ART_G09_房间陈设"), Is.Null);
            Assert.That(Find("ART_G09_不同家庭双区"), Is.Not.Null);
            Assert.That(Find("ART_G11_精选子件"), Is.Not.Null);
            Assert.That(Find("ART_SM_RitualCircle"), Is.Not.Null);
        }

        [Test]
        public void CourtyardUsesGroundedUserAuthoredPropsInsteadOfDisplayBoards()
        {
            Assert.That(Find("PF_CourtyardProps_O04_A"), Is.Null);
            Assert.That(Find("PF_CourtyardProps_O04_B"), Is.Null);

            Transform propsRoot = Find("UserDecoration_用户装饰");
            Assert.That(propsRoot, Is.Not.Null);
            Renderer[] renderers = propsRoot.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(20));
            Assert.That(renderers.All(renderer => Mathf.Abs(renderer.bounds.min.y) <= 0.12f), Is.True,
                "Every placed prop must rest on the ground instead of floating.");
        }

        private Transform Find(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                Transform found = Find(root.transform, name);
                if (found != null) return found;
            }
            return null;
        }

        private Transform FindByPrefix(string prefix)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                Transform found = FindByPrefix(root.transform, prefix);
                if (found != null) return found;
            }
            return null;
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

        private static Transform FindByPrefix(Transform root, string prefix)
        {
            if (root.name.StartsWith(prefix)) return root;
            foreach (Transform child in root)
            {
                Transform found = FindByPrefix(child, prefix);
                if (found != null) return found;
            }
            return null;
        }

        private static Bounds RendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private void AssertDoor(string buildingName, string pivotName, string leafName, string meshNameFragment, float expectedBottom, float bottomTolerance)
        {
            Transform building = Find(buildingName);
            Transform pivot = Find(pivotName);
            Transform leaf = Find(leafName);
            Assert.That(building, Is.Not.Null, buildingName);
            Assert.That(pivot, Is.Not.Null, pivotName);
            Assert.That(leaf, Is.Not.Null, leafName);
            Bounds buildingBounds = RendererBounds(building);
            Bounds doorBounds = RendererBounds(leaf);
            float boundaryDistance = Mathf.Min(
                Mathf.Min(Mathf.Abs(doorBounds.center.x - buildingBounds.min.x), Mathf.Abs(doorBounds.center.x - buildingBounds.max.x)),
                Mathf.Min(Mathf.Abs(doorBounds.center.z - buildingBounds.min.z), Mathf.Abs(doorBounds.center.z - buildingBounds.max.z)));
            Assert.That(boundaryDistance, Is.LessThan(1.1f), pivotName + " must sit on the authored building envelope instead of floating beside it.");
            Assert.That(doorBounds.min.y, Is.EqualTo(expectedBottom).Within(bottomTolerance), leafName + " is not grounded on its authored threshold.");
            MeshFilter filter = leaf.GetComponent<MeshFilter>();
            Assert.That(filter, Is.Not.Null, leafName);
            Assert.That(filter.sharedMesh.name, Does.Contain(meshNameFragment), leafName + " did not use the requested user door FBX.");
        }
    }
}
#endif
