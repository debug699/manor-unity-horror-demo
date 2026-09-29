using System;
using System.IO;
using Manor.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;
using Manor.Narrative;
using UnityEngine;

namespace Manor.Tests.EditMode
{
    public sealed class FoundationSystemsTests
    {
        [TestCase("INT_DOOR_ENTRY_01")]
        [TestCase("KEY_STORAGE_01")]
        [TestCase("CLUE_NOTE_TEST_01")]
        public void StableIdsAcceptProjectFormat(string value)
        {
            Assert.That(new StableId(value).IsValid, Is.True);
        }

        [TestCase("door-entry")]
        [TestCase("有中文")]
        [TestCase("")]
        public void StableIdsRejectUnstableFormat(string value)
        {
            Assert.That(StableId.IsValidValue(value), Is.False);
        }

        [Test]
        public void GameStateCollectionsAreIdempotent()
        {
            GameStateService service = new GameStateService();

            Assert.That(service.AddKey("KEY_STORAGE_01"), Is.True);
            Assert.That(service.AddKey("KEY_STORAGE_01"), Is.False);
            Assert.That(service.MarkClueRead("CLUE_NOTE_TEST_01"), Is.True);
            Assert.That(service.MarkClueRead("CLUE_NOTE_TEST_01"), Is.False);
            Assert.That(service.Snapshot.collectedKeys, Has.Count.EqualTo(1));
            Assert.That(service.Snapshot.readClues, Has.Count.EqualTo(1));
            Assert.That(service.AddItem("ITEM_FLASHLIGHT_DEAD"), Is.True);
            Assert.That(service.AddItem("ITEM_FLASHLIGHT_DEAD"), Is.False);
            Assert.That(service.HasItem("ITEM_FLASHLIGHT_DEAD"), Is.True);
        }

        [Test]
        public void DoorStateCanOpenCloseAndRemainIdempotent()
        {
            GameStateService service = new GameStateService();

            Assert.That(service.SetDoorOpen("INT_DOOR_TEST_01", true), Is.True);
            Assert.That(service.SetDoorOpen("INT_DOOR_TEST_01", true), Is.False);
            Assert.That(service.IsDoorOpen("INT_DOOR_TEST_01"), Is.True);
            Assert.That(service.SetDoorOpen("INT_DOOR_TEST_01", false), Is.True);
            Assert.That(service.IsDoorOpen("INT_DOOR_TEST_01"), Is.False);
        }

        [Test]
        public void StoryStageOnlyAdvancesForward()
        {
            GameStateService service = new GameStateService();

            Assert.That(service.AdvanceStory(StoryStage.FindFirstKey), Is.True);
            Assert.That(service.AdvanceStory(StoryStage.Awake), Is.False);
            Assert.That(service.Snapshot.storyStage, Is.EqualTo(StoryStage.FindFirstKey));
        }

        [Test]
        public void CheckpointUpdatesIdAndSafePositionAtomically()
        {
            GameStateService service = new GameStateService();
            Vector3 position = new Vector3(7f, -3f, -8f);

            Assert.That(service.SetCheckpoint("CHECKPOINT_CHASE_START", position), Is.True);
            Assert.That(service.SetCheckpoint("CHECKPOINT_CHASE_START", position), Is.False);
            Assert.That(service.Snapshot.checkpointId, Is.EqualTo("CHECKPOINT_CHASE_START"));
            Assert.That(service.Snapshot.lastSafePosition, Is.EqualTo(position));
        }

        [TestCase("SCN_ManorDemo_庄园Demo", ProjectIds.SceneManorDemo)]
        [TestCase("SCN_MainMenu_主菜单", ProjectIds.SceneMainMenu)]
        [TestCase("SCN_Boot_启动场景", ProjectIds.SceneBoot)]
        public void SceneNamesConvertToStableIds(string sceneName, string expectedId)
        {
            Assert.That(SceneIdUtility.FromSceneName(sceneName), Is.EqualTo(expectedId));
        }

        [Test]
        public void AutoSaveRoundTripsFoundationState()
        {
            string directory = Path.Combine(Path.GetTempPath(), "ManorFoundationTests", Guid.NewGuid().ToString("N"));
            try
            {
                GameStateService state = new GameStateService();
                state.SetScene("SCN_MANOR_DEMO");
                state.SetObjective("OBJ_TEST_INTERACTIONS");
                state.AddKey("KEY_STORAGE_01");
                state.AddItem("ITEM_MATCHBOX");
                state.MarkClueRead("CLUE_NOTE_TEST_01");
                state.SetDoorOpen("INT_DOOR_TEST_01", true);
                state.SetGatePasswordKnown(true);
                state.SetCommunicationComplete(true);
                state.SetButcherChaseStarted(true);
                state.SetChaseElapsedSeconds(123.5f);
                state.SetDawnTriggered(true);
                state.SetBridgeRestored(true);
                state.SetGateUnlocked(true);
                state.SetCheckpoint("CHECKPOINT_DAWN", new Vector3(1f, 2f, 3f));
                JsonAutoSaveService save = new JsonAutoSaveService(directory);

                save.Save(state.Snapshot);

                Assert.That(save.TryLoad(out GameStateData loaded), Is.True);
                Assert.That(loaded.sceneId, Is.EqualTo("SCN_MANOR_DEMO"));
                Assert.That(loaded.objectiveId, Is.EqualTo("OBJ_TEST_INTERACTIONS"));
                Assert.That(loaded.collectedKeys, Contains.Item("KEY_STORAGE_01"));
                Assert.That(loaded.collectedItems, Contains.Item("ITEM_MATCHBOX"));
                Assert.That(loaded.readClues, Contains.Item("CLUE_NOTE_TEST_01"));
                Assert.That(loaded.openDoors, Contains.Item("INT_DOOR_TEST_01"));
                Assert.That(loaded.gatePasswordKnown, Is.True);
                Assert.That(loaded.communicationComplete, Is.True);
                Assert.That(loaded.butcherChaseStarted, Is.True);
                Assert.That(loaded.chaseElapsedSeconds, Is.EqualTo(123.5f).Within(.01f));
                Assert.That(loaded.dawnTriggered, Is.True);
                Assert.That(loaded.bridgeRestored, Is.True);
                Assert.That(loaded.gateUnlocked, Is.True);
                Assert.That(loaded.checkpointId, Is.EqualTo("CHECKPOINT_DAWN"));
                Assert.That(loaded.lastSafePosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void InputAssetContainsRequiredBindings()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/IA_Player_玩家输入.inputactions");

            Assert.That(asset, Is.Not.Null);
            InputActionMap gameplay = asset.FindActionMap("Gameplay", true);
            Assert.That(gameplay.FindAction("Move", true).bindings, Has.Some.Property("effectivePath").Contains("Keyboard"));
            Assert.That(gameplay.FindAction("Look", true).bindings, Has.Some.Property("effectivePath").Contains("Mouse"));
            Assert.That(gameplay.FindAction("Run", true).bindings, Has.Some.Property("effectivePath").Contains("leftShift"));
            Assert.That(gameplay.FindAction("Crouch", true).bindings, Has.Some.Property("effectivePath").Contains("leftCtrl"));
            Assert.That(gameplay.FindAction("Interact", true).bindings, Has.Some.Property("effectivePath").Contains("/e"));
            Assert.That(gameplay.FindAction("Pickup", true).bindings, Has.Some.Property("effectivePath").Contains("/f"));
            Assert.That(gameplay.FindAction("UseItem", true).bindings, Has.Some.Property("effectivePath").Contains("/k"));
            Assert.That(gameplay.FindAction("Clues", true).bindings, Has.Some.Property("effectivePath").Contains("tab"));
            Assert.That(gameplay.FindAction("Pause", true).bindings, Has.Some.Property("effectivePath").Contains("escape"));
        }

        [TestCase("PROMPT_PICKUP_KEY", "按 F 拾取钥匙")]
        [TestCase("PROMPT_LOCKED_DOOR", "需要钥匙")]
        [TestCase("PROMPT_READ_NOTE", "按 E 阅读纸条")]
        [TestCase("KEY_ADDED", "已获得钥匙")]
        public void InteractionTextsResolveFromCentralCatalog(string key, string expected)
        {
            Assert.That(ManorTextCatalog.Resolve(key), Is.EqualTo(expected));
        }
    }
}
