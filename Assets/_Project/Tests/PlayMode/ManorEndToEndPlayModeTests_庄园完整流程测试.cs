using System.Collections;
using Manor.Core;
using Manor.Gameplay;
using Manor.Narrative;
using Manor.Player;
using Manor.Runtime;
using Manor.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Manor.Tests.PlayMode
{
    public sealed class ManorEndToEndPlayModeTests
    {
        [UnityTest]
        public IEnumerator FreshGameCanCompleteTheCanonicalEscapeRouteThroughRealInteractions()
        {
#if UNITY_EDITOR
            const string scenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";
            GameSession session = GameSession.Current;
            session.AutoSaveEnabled = false;
            session.GameState.Restore(new GameStateData());

            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone) yield return null;
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            IGameStateService state = session.GameState;
            FirstPersonController player = FindComponentInScene<FirstPersonController>(scene);
            Assert.That(player, Is.Not.Null, "The production route needs a real player actor.");
            DisableThreatsDuringDeterministicRoute(scene);
            InteractionContext context = new InteractionContext(
                player.gameObject, player.GetComponentInChildren<Camera>(true), state);

            ItemPickupInteractable wire = FindInteraction<ItemPickupInteractable>(scene, "INT_PICKUP_G11_WIRE");
            LockpickDoorInteractable g11Door = FindInteraction<LockpickDoorInteractable>(scene, "INT_DOOR_G11");
            KeyInteractable researchKey = FindInteraction<KeyInteractable>(scene, "INT_PICKUP_G10_KEY");
            DoorInteractable g10Door = FindInteraction<DoorInteractable>(scene, "INT_DOOR_G10");
            Assert.That(state.Snapshot.storyStage, Is.EqualTo(StoryStage.Awake));

            wire.Interact(context);
            Assert.That(state.HasItem("ITEM_G11_LOCKPICK_WIRE"), Is.True);
            Assert.That(g11Door.RequiredAction, Is.EqualTo(InteractionAction.HoldUse));
            g11Door.Interact(context);
            Assert.That(state.HasKey("KEY_G11_LOCKPICKED"), Is.True);
            Assert.That(g11Door.RequiredAction, Is.EqualTo(InteractionAction.Interact));
            g11Door.Interact(context);
            Assert.That(g11Door.IsOpen, Is.True);

            g10Door.Interact(context);
            Assert.That(g10Door.IsOpen, Is.False, "G10 must remain locked before its key is collected.");
            researchKey.Interact(context);
            Assert.That(state.HasKey("KEY_G10_RESEARCH_ROOM"), Is.True);
            g10Door.Interact(context);
            Assert.That(g10Door.IsOpen, Is.True);

            ReadClue(scene, context, "INT_CLUE_G08_DIARY", "CLUE_G08_DIARY");
            ReadClue(scene, context, "INT_CLUE_G09_BOY", "CLUE_G09_BOY_FAMILY");
            ReadClue(scene, context, "INT_CLUE_G09_GIRL", "CLUE_G09_GIRL_FAMILY");
            ReadClue(scene, context, "INT_CLUE_G10_RITUAL", "CLUE_G10_RITUAL_RECORD");
            ReadClue(scene, context, "INT_CLUE_G04_TRACES", "CLUE_G04_TRACES");
            Assert.That(state.Snapshot.storyStage, Is.EqualTo(StoryStage.DiscoverRitual));
            Assert.That(state.Snapshot.objectiveId, Is.EqualTo(ProjectIds.ObjectiveOpenKitchenHatch));

            BasementHatchInteractable hatch = FindInteraction<BasementHatchInteractable>(scene, "INT_G03_BASEMENT_HATCH");
            hatch.Interact(context);
            Assert.That(hatch.IsOpen, Is.True);
            Assert.That(state.Snapshot.storyStage, Is.EqualTo(StoryStage.EnterBasement));
            Assert.That(state.Snapshot.objectiveId, Is.EqualTo(ProjectIds.ObjectiveDescendToBasement));

            StoryRegionTrigger b01 = FindNamedComponent<StoryRegionTrigger>(scene, "B01_EntryRegion_地下入口触发");
            StoryRegionTrigger b02 = FindNamedComponent<StoryRegionTrigger>(scene, "B02_RitualRegion_祭祀室触发");
            yield return EnterTrigger(player, b01.transform);
            Assert.That(state.Snapshot.objectiveId, Is.EqualTo(ProjectIds.ObjectiveInvestigateRitualRoom));
            yield return EnterTrigger(player, b02.transform);

            ItemPickupInteractable parchment = FindInteraction<ItemPickupInteractable>(scene, "INT_PICKUP_COMPLETE_PARCHMENT");
            parchment.Interact(context);
            Assert.That(state.HasItem("ITEM_COMPLETE_PARCHMENT"), Is.True);
            Assert.That(state.Snapshot.storyStage, Is.EqualTo(StoryStage.CompleteParchment));

            ParchmentCommunicationInteractable communication =
                FindComponentInScene<ParchmentCommunicationInteractable>(scene);
            communication.Interact(context);
            Assert.That(communication.IsEditing, Is.True);
            Assert.That(communication.SubmitAnswer(2), Is.True);
            Assert.That(communication.SubmitAnswer(1), Is.True);
            Assert.That(communication.SubmitAnswer(3), Is.True);
            Assert.That(state.CommunicationComplete, Is.True);
            Assert.That(state.GatePasswordKnown, Is.True);
            Assert.That(state.ButcherChaseStarted, Is.True);
            Assert.That(state.Snapshot.storyStage, Is.EqualTo(StoryStage.ButcherChase));

            ButcherChaseTimer timer = FindComponentInScene<ButcherChaseTimer>(scene);
            timer.Advance(state, ButcherChaseTimer.DawnSeconds - .1f);
            Assert.That(state.DawnTriggered, Is.False);
            timer.Advance(state, .2f);
            Assert.That(state.DawnTriggered, Is.True);
            Assert.That(state.Snapshot.storyStage, Is.EqualTo(StoryStage.DawnSurvival));

            BridgeMechanismInteractable bridge = FindInteraction<BridgeMechanismInteractable>(scene, "INT_BRIDGE_MECHANISM");
            bridge.Interact(context);
            Assert.That(state.BridgeRestored, Is.True);
            Assert.That(state.Snapshot.storyStage, Is.EqualTo(StoryStage.BridgeRestored));

            GatePasswordInteractable keypad = FindInteraction<GatePasswordInteractable>(scene, "INT_GATE_PASSWORD_1016");
            Assert.That(keypad.SubmitPassword("1016", context), Is.True);
            Assert.That(state.GateUnlocked, Is.True);

            EscapeTrigger escape = FindComponentInScene<EscapeTrigger>(scene);
            yield return EnterTrigger(player, escape.transform);
            Assert.That(state.Snapshot.storyStage, Is.EqualTo(StoryStage.Escape));

            Time.timeScale = 1f;
            player.SetInputLocked(false);
            session.AutoSaveEnabled = true;
#else
            yield break;
#endif
        }

        private static void ReadClue(Scene scene, InteractionContext context, string interactionId, string clueId)
        {
            NoteInteractable note = FindInteraction<NoteInteractable>(scene, interactionId);
            note.Interact(context);
            Assert.That(context.GameState.HasReadClue(clueId), Is.True, $"Expected clue {clueId} to be recorded.");
            CluePanelController panel = FindComponentInScene<CluePanelController>(scene);
            Assert.That(panel.IsOpen, Is.True, "Reading a clue should present it to the player.");
            panel.Close();
        }

        private static IEnumerator EnterTrigger(FirstPersonController player, Transform trigger)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.position = trigger.position + Vector3.up * .25f;
            if (controller != null) controller.enabled = true;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        private static T FindInteraction<T>(Scene scene, string interactionId) where T : InteractableBase
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (T candidate in root.GetComponentsInChildren<T>(true))
                if (candidate.InteractionId == interactionId) return candidate;
            Assert.Fail($"Missing {typeof(T).Name} with interaction ID {interactionId}.");
            return null;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T candidate = root.GetComponentInChildren<T>(true);
                if (candidate != null) return candidate;
            }
            Assert.Fail($"Missing component {typeof(T).Name} in production scene.");
            return null;
        }

        private static T FindNamedComponent<T>(Scene scene, string objectName) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (T candidate in root.GetComponentsInChildren<T>(true))
                if (candidate.name == objectName) return candidate;
            Assert.Fail($"Missing {typeof(T).Name} named {objectName}.");
            return null;
        }

        private static void DisableThreatsDuringDeterministicRoute(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (PregnantWraithController wraith in root.GetComponentsInChildren<PregnantWraithController>(true))
                    wraith.enabled = false;
                foreach (ButcherController butcher in root.GetComponentsInChildren<ButcherController>(true))
                    butcher.enabled = false;
            }
        }
    }
}
