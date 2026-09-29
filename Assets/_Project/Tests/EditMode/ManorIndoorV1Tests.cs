#if UNITY_INCLUDE_TESTS
using System.Linq;
using Manor.Gameplay;
using Manor.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Tests.EditMode
{
    public sealed class ManorIndoorV1Tests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorInteriorV1_室内首版.unity";
        private Scene _scene;

        [SetUp] public void OpenScene() => _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        [Test]
        public void HasAllLayoutAreasAndAnExplicitG09Placeholder()
        {
            string[] required = { "G01_玄关", "G02_客厅套间", "G03_厨房套间", "G04_屠宰工作间", "G05_主走廊", "G06_楼梯间", "G07_后门通道", "G08_孕妇房", "G09_儿童房双区_占位", "G10_研究室", "G11_祷告室", "G12_旧客房过渡区", "B01_地下服务通道_白盒", "B02_地下祭祀室_白盒" };
            foreach (string name in required) Assert.That(Find(name), Is.Not.Null, name);
        }

        [Test]
        public void DoorwaysUseSegmentedWallsAndIndependentInteractiveLeaves()
        {
            Assert.That(Find("G01_玄关_Wall_N"), Is.Null);
            Assert.That(Find("G01_玄关_Wall_N_Left"), Is.Not.Null);
            Assert.That(Find("G01_主入口可开门_Leaf").GetComponent<DoorInteractable>(), Is.Not.Null);
            Assert.That(Find("G03_厨房活板门占位_Leaf").GetComponent<DoorInteractable>(), Is.Not.Null);
        }

        [Test]
        public void CoreStoryPropsAreConcretePickups()
        {
            string[] items = { "ITEM_PHONE_BROKEN_可拾取样例", "ITEM_FLASHLIGHT_DEAD_可拾取样例", "ITEM_MEDICINE_BOTTLE_可拾取样例", "ITEM_MATCHBOX_可拾取样例" };
            foreach (string item in items) Assert.That(Find(item).GetComponent<ItemPickupInteractable>(), Is.Not.Null, item);
        }

        [Test]
        public void InteriorDoesNotUseAnyWholeBuildingBoxCollider()
        {
            Assert.That(_scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BoxCollider>(true)).Any(c => c.transform.name.Contains("MainHouse") || c.transform.name.Contains("WholeBuilding")), Is.False);
        }

        [Test]
        public void ImportedRoomArtIsPresentButCannotCreateAirWalls()
        {
            Transform artRoot = Find("ArtVisuals_NoCollision");
            Assert.That(artRoot, Is.Not.Null);
            Assert.That(artRoot.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(100));
            Assert.That(artRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
        }

        [Test]
        public void TomFirstPersonArmMeshesAreStagedAsSeparateUnityAssets()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Characters/FirstPerson/Tom_FirstPerson_LeftArm.fbx"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Characters/FirstPerson/Tom_FirstPerson_RightArm.fbx"), Is.Not.Null);
        }

        [Test]
        public void PlayerCameraHasTomHandMeshesAndInteractionPresenter()
        {
            Transform holder = Find("TomHands_第一人称手部");
            Assert.That(holder, Is.Not.Null);
            Assert.That(holder.GetComponent<FirstPersonHandsPresenter>(), Is.Not.Null);
            Assert.That(Find("Tom_FirstPerson_LeftArm_镜头显示"), Is.Not.Null);
            Assert.That(Find("Tom_FirstPerson_RightArm_镜头显示"), Is.Not.Null);
            Assert.That(holder.GetComponentsInChildren<Collider>(true), Is.Empty);
        }

        [Test]
        public void BasementUsesKitchenHatchAndRitualAssetsWithoutArtColliders()
        {
            Transform circle = Find("ART_SM_RitualCircle");
            Transform statue = Find("ART_SM_SaintStatue");
            Transform hatch = Find("ART_SM_KitchenHatch");
            Assert.That(circle, Is.Not.Null);
            Assert.That(statue, Is.Not.Null);
            Assert.That(hatch, Is.Not.Null);
            Assert.That(circle.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(statue.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(hatch.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(Find("G03_厨房活板门至B01_Step_9"), Is.Not.Null);
        }

        [Test]
        public void G07HasARestrictedVaultWindowWithNoBlockingCollider()
        {
            Transform window = Find("G07_可翻越窗户");
            Assert.That(window, Is.Not.Null);
            Assert.That(window.GetComponent<Manor.Gameplay.VaultWindowInteractable>(), Is.Not.Null);
            Assert.That(window.GetComponent<BoxCollider>().isTrigger, Is.True);
            Assert.That(Find("G07_翻窗落点"), Is.Not.Null);
            Transform art = Find("ART_G07_翻窗窗户");
            Assert.That(art, Is.Not.Null);
            Assert.That(art.GetComponentsInChildren<Collider>(true), Is.Empty);
        }

        [Test]
        public void ProvisionalCharacterRigsAreImportedForFutureAnimationWork()
        {
            GameObject butcher = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Characters/StagedRigs/Butcher_ProvisionalRig.fbx");
            GameObject child = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Characters/StagedRigs/GhostChild_ProvisionalRig.fbx");
            Assert.That(butcher, Is.Not.Null);
            Assert.That(child, Is.Not.Null);
            Assert.That(butcher.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.GreaterThan(0));
            Assert.That(child.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.GreaterThan(0));
        }

        [Test]
        public void ProvisionalCharactersContainIdleAndWalkClips()
        {
            Object[] clips = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Characters/StagedRigs/Animations/Butcher_ProvisionalRig_Animations.fbx");
            Assert.That(clips.OfType<AnimationClip>().Any(clip => clip.name == "Idle"), Is.True);
            Assert.That(clips.OfType<AnimationClip>().Any(clip => clip.name == "Walk"), Is.True);
        }

        private Transform Find(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                Transform result = Find(root.transform, name);
                if (result != null) return result;
            }
            return null;
        }
        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root) { Transform result = Find(child, name); if (result != null) return result; }
            return null;
        }
    }
}
#endif
