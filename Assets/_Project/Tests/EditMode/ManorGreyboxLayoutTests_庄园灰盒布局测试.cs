#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Tests.EditMode
{
    public sealed class ManorGreyboxLayoutTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";
        private Scene _scene;

        [SetUp]
        public void OpenScene()
        {
            _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void GroundAndUpperRoomsUseDistinctFloors()
        {
            Transform environment = Find("Environment_环境");
            Transform ground = FindChild(environment, "GroundLevel_一层区域");
            Transform upper = FindChild(environment, "UpperLevel_二层区域");

            AssertNames(ground, "EntranceHall_玄关", "LivingRoom_客厅", "Kitchen_厨房", "StorageRoom_储藏室",
                "MainCorridor_主走廊", "Stairwell_楼梯间", "BackDoorPassage_后门通道", "TransitionRoom_地下入口上方过渡房");
            AssertNames(upper, "PregnantRoom_孕妇房间", "ChildrenRoom_儿童房", "PrayerRoom_祷告室", "ButcherStudy_屠夫研究室");

            Assert.That(FindChild(upper, "PregnantRoom_孕妇房间").position.y, Is.GreaterThan(3f));
            Assert.That(FindChild(ground, "EntranceHall_玄关").position.y, Is.LessThan(0.5f));
        }

        [Test]
        public void RequiredRoutesHaveDoorwaysAndWalkableStairs()
        {
            Assert.That(Find("EntranceHall_玄关_Doorway_North_门洞"), Is.Not.Null);
            Assert.That(Find("LivingRoom_客厅_Doorway_North_门洞"), Is.Not.Null);
            Assert.That(Find("Kitchen_厨房_Doorway_North_门洞"), Is.Not.Null);
            Assert.That(Find("StorageRoom_储藏室_Doorway_North_门洞"), Is.Not.Null);
            Assert.That(Find("BackDoorPassage_后门通道_Doorway_North_门洞"), Is.Not.Null);
            Assert.That(Find("TransitionRoom_地下入口上方过渡房_Doorway_North_门洞"), Is.Not.Null);

            Transform mainStair = Find("MainStair_主楼梯");
            Transform basementStair = Find("BasementStair_地下入口楼梯");
            Assert.That(CountChildrenContaining(mainStair, "_Step_台阶_"), Is.EqualTo(18));
            Assert.That(CountChildrenContaining(basementStair, "_Step_台阶_"), Is.EqualTo(15));
            Assert.That(Find("MainStair_主楼梯_Top_顶部").position.y, Is.GreaterThan(3f));
            Assert.That(Find("BasementStair_地下入口楼梯_Top_顶部").position.y, Is.LessThan(-2.5f));
            Assert.That(Find("MainStair_主楼梯_Top_顶部").position.z, Is.GreaterThan(7f));
            Assert.That(Find("MainCorridor_主走廊_Wall_North_整墙"), Is.Null);
            Assert.That(Find("MainCorridor_主走廊_Wall_South_整墙"), Is.Null);
        }

        [Test]
        public void BasementAndOutdoorRouteAreConnectedAndPresent()
        {
            Assert.That(Find("BasementEntrance_地下入口"), Is.Not.Null);
            Assert.That(Find("BasementPassage_地下连接通道"), Is.Not.Null);
            Assert.That(Find("RitualChamber_地下祭祀室"), Is.Not.Null);
            Assert.That(Find("BackDoorThreshold_后门门槛"), Is.Not.Null);
            Assert.That(Find("Shed_A_废弃木屋A"), Is.Not.Null);
            Assert.That(Find("Shed_B_废弃木屋B"), Is.Not.Null);
            Assert.That(Find("BrokenBridge_断桥灰盒测试桥面"), Is.Not.Null);
            Assert.That(Find("ManorGateRoad_庄园大门道路"), Is.Not.Null);
            Assert.That(Find("Gate_庄园大门"), Is.Not.Null);
        }

        [Test]
        public void GreyboxContainsExactlyTwelveGroundRoomsPlusBasementChamber()
        {
            string[] roomNames =
            {
                "EntranceHall_玄关", "LivingRoom_客厅", "MainCorridor_主走廊", "Kitchen_厨房", "StorageRoom_储藏室",
                "Stairwell_楼梯间", "PregnantRoom_孕妇房间", "ChildrenRoom_儿童房", "PrayerRoom_祷告室",
                "ButcherStudy_屠夫研究室", "BackDoorPassage_后门通道", "TransitionRoom_地下入口上方过渡房"
            };

            foreach (string roomName in roomNames) Assert.That(Find(roomName), Is.Not.Null, roomName);
            Assert.That(Find("RitualChamber_地下祭祀室"), Is.Not.Null);
        }

        private Transform Find(string objectName)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                Transform found = FindChild(root.transform, objectName);
                if (found != null) return found;
            }

            return null;
        }

        private static Transform FindChild(Transform root, string objectName)
        {
            if (root.name == objectName) return root;
            foreach (Transform child in root)
            {
                Transform found = FindChild(child, objectName);
                if (found != null) return found;
            }

            return null;
        }

        private static void AssertNames(Transform parent, params string[] names)
        {
            foreach (string name in names) Assert.That(FindChild(parent, name), Is.Not.Null, name);
        }

        private static int CountChildrenContaining(Transform parent, string value)
        {
            int count = 0;
            Queue<Transform> pending = new Queue<Transform>();
            pending.Enqueue(parent);
            while (pending.Count > 0)
            {
                Transform current = pending.Dequeue();
                foreach (Transform child in current)
                {
                    if (child.name.Contains(value)) count++;
                    pending.Enqueue(child);
                }
            }

            return count;
        }
    }
}
#endif
