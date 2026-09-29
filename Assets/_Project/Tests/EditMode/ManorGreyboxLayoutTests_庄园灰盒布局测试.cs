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
        public void ConfirmedGroundAndUpperRoomsUseDistinctFloors()
        {
            Transform layout = Find("ManagedInteriorV2_连续主楼布局");
            Transform ground = FindChild(layout, "GroundFloor_G01-G07_一层");
            Transform upper = FindChild(layout, "UpperFloor_G08-G12_二层");
            AssertNames(ground, "G01_玄关", "G02_客厅套间", "G03_厨房套间", "G04_屠宰工作间", "G05_主走廊", "G06_楼梯间", "G07_后门通道");
            AssertNames(upper, "G08_孕妇房间套间", "G09_儿童房双区", "G10_屠夫研究室", "G11_祷告室", "G12_旧客房与封闭过渡空间");
            Assert.That(FindChild(upper, "G12_旧客房与封闭过渡空间").position.y, Is.GreaterThan(3f));
            Assert.That(FindChild(ground, "G01_玄关").position.y, Is.LessThan(.5f));
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

            Assert.That(CountChildrenContaining(Find("G06_实体楼梯"), "_Step_"), Is.EqualTo(18));
            Assert.That(CountChildrenContaining(Find("G03_活板门至B01"), "_Step_"), Is.EqualTo(16));
            Assert.That(Find("G06_地下楼梯"), Is.Null);
        }

        [Test]
        public void BasementAndOutdoorRouteAreConnectedAndPresent()
        {
            Assert.That(Find("G03_活板门至B01"), Is.Not.Null);
            Assert.That(Find("B01_地下服务路线"), Is.Not.Null);
            Assert.That(Find("B02_地下祭祀室"), Is.Not.Null);
            Assert.That(Find("PF_UserWorkshopHut_加工木屋主用"), Is.Not.Null);
            Assert.That(Find("PF_UserAbandonedHut_废弃木屋主用"), Is.Not.Null);
            Assert.That(Find("PF_UserStoneWorkshop_木石工坊主用"), Is.Not.Null);
            Assert.That(Find("PF_UserWoodBridge_用户木桥"), Is.Not.Null);
            Assert.That(Find("PF_UserGate_用户大门"), Is.Not.Null);
        }

        [Test]
        public void GreyboxContainsExactlyTwelveGroundRoomsPlusBasementChamber()
        {
            string[] roomNames = { "G01_玄关", "G02_客厅套间", "G03_厨房套间", "G04_屠宰工作间", "G05_主走廊", "G06_楼梯间", "G07_后门通道", "G08_孕妇房间套间", "G09_儿童房双区", "G10_屠夫研究室", "G11_祷告室", "G12_旧客房与封闭过渡空间" };

            foreach (string roomName in roomNames) Assert.That(Find(roomName), Is.Not.Null, roomName);
            Assert.That(Find("B02_地下祭祀室"), Is.Not.Null);
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
