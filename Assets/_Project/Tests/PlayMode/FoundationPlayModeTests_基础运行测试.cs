using System.Collections;
using Manor.Core;
using Manor.Gameplay;
using Manor.Player;
using Manor.Runtime;
using Manor.UI;
using Manor.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;

namespace Manor.Tests.PlayMode
{
    public sealed class FoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator InteractionTestSceneLoadsWithWorkingKeyTarget()
        {
#if UNITY_EDITOR
            const string scenePath = "Assets/_Project/Scenes/Tests/TST_Interaction_交互测试.unity";
            AsyncOperation operation = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            while (!operation.isDone) yield return null;
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Transform player = FindInScene(scene, "Player_汤姆测试胶囊");
            Transform keyTransform = FindInScene(scene, "Key_测试钥匙");
            Transform doorTransform = FindInScene(scene, "Door_测试门");
            Transform noteTransform = FindInScene(scene, "Note_测试纸条");
            InteractionScanner scanner = player.GetComponent<InteractionScanner>();
            Camera camera = player.GetComponentInChildren<Camera>(true);
            RuntimeHud hud = FindInScene(scene, "HUD_基础界面").GetComponent<RuntimeHud>();
            KeyInteractable key = keyTransform.GetComponent<KeyInteractable>();
            DoorInteractable door = doorTransform.GetComponent<DoorInteractable>();
            NoteInteractable note = noteTransform.GetComponent<NoteInteractable>();

            Assert.That(player, Is.Not.Null);
            Assert.That(key, Is.Not.Null);
            Assert.That(door, Is.Not.Null);
            Assert.That(note, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(key.InteractionId, Is.EqualTo("INT_KEY_FOUNDATION_01"));
            Assert.That(door.InteractionId, Is.EqualTo("INT_DOOR_FOUNDATION_01"));
            Assert.That(note.InteractionId, Is.EqualTo("INT_NOTE_FOUNDATION_01"));

            Manor.Runtime.GameSession session = Manor.Runtime.GameSession.Current;
            session.AutoSaveEnabled = false;
            session.GameState.Restore(new GameStateData());
            keyTransform.gameObject.SetActive(true);

            player.position = doorTransform.position - Vector3.forward * 1.5f - Vector3.up;
            player.rotation = Quaternion.identity;
            camera.transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();
            yield return null;

            Assert.That(scanner.TryInteract(), Is.True);
            Assert.That(door.IsOpen, Is.False);
            Assert.That(hud.CurrentFeedbackText, Is.EqualTo(ManorTextCatalog.Resolve("PROMPT_LOCKED_DOOR")));

            player.position = keyTransform.position - Vector3.forward * 1.5f - Vector3.up * 1.65f;
            player.rotation = Quaternion.identity;
            camera.transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();
            yield return null;

            Assert.That(scanner.Scan(), Is.True);
            Assert.That(scanner.Current, Is.SameAs(key));
            Assert.That(scanner.Current.GetPromptKey(new InteractionContext(player.gameObject, camera, session.GameState)), Is.EqualTo("PROMPT_PICKUP_KEY"));
            Assert.That(hud.CurrentPromptText, Is.EqualTo(ManorTextCatalog.Resolve("PROMPT_PICKUP_KEY")));
            Assert.That(scanner.TryInteract(InteractionAction.Pickup), Is.True);
            Assert.That(keyTransform.gameObject.activeSelf, Is.False);
            Assert.That(session.GameState.HasKey("KEY_FOUNDATION_01"), Is.True);
            Assert.That(hud.CurrentFeedbackText, Is.EqualTo(ManorTextCatalog.Resolve("KEY_ADDED")));

            player.position = doorTransform.position - Vector3.forward * 1.5f - Vector3.up;
            player.rotation = Quaternion.identity;
            camera.transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();
            yield return null;

            Assert.That(scanner.TryInteract(), Is.True);
            yield return new WaitForSeconds(0.1f);
            Assert.That(door.IsOpen, Is.True);
            Assert.That(session.GameState.IsDoorOpen("INT_DOOR_FOUNDATION_01"), Is.True);
            Assert.That(hud.CurrentFeedbackText, Is.EqualTo(ManorTextCatalog.Resolve("DOOR_OPENED")));

            player.position = noteTransform.position - Vector3.forward * 1.5f - Vector3.up * 1.65f;
            player.rotation = Quaternion.identity;
            camera.transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();
            yield return null;

            Assert.That(scanner.TryInteract(), Is.True);
            Assert.That(session.GameState.HasReadClue("CLUE_NOTE_FOUNDATION_01"), Is.True);
            Assert.That(FindInScene(scene, "HUD_基础界面").GetComponent<CluePanelController>().IsOpen, Is.True);
            session.AutoSaveEnabled = true;
#else
            yield break;
#endif
        }

        private static Transform FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform result = FindInChildren(root.transform, objectName);
                if (result != null) return result;
            }

            return null;
        }

        private static Transform FindInChildren(Transform root, string objectName)
        {
            if (root.name == objectName) return root;
            foreach (Transform child in root)
            {
                Transform result = FindInChildren(child, objectName);
                if (result != null) return result;
            }

            return null;
        }

        [UnityTest]
        public IEnumerator InteractionScannerRequiresDistanceAndLineOfSight()
        {
            GameObject cameraObject = new GameObject("TestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            Vector3 isolatedOrigin = new Vector3(1000f, 1000f, 1000f);
            camera.transform.position = isolatedOrigin;
            camera.transform.forward = Vector3.forward;

            GameObject scannerObject = new GameObject("Scanner");
            InteractionScanner scanner = scannerObject.AddComponent<InteractionScanner>();
            GameStateService state = new GameStateService();
            scanner.Configure(camera, state, 2.5f);

            GameObject noteObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            noteObject.transform.position = isolatedOrigin + Vector3.forward * 2f;
            NoteInteractable note = noteObject.AddComponent<NoteInteractable>();
            note.ConfigureNote("INT_NOTE_TEST_01", "CLUE_NOTE_TEST_01", "TEST_NOTE_TITLE", "TEST_NOTE_BODY");
            Physics.SyncTransforms();
            yield return null;

            Assert.That(scanner.Scan(), Is.True);

            noteObject.transform.position = isolatedOrigin + Vector3.forward * 4f;
            Physics.SyncTransforms();
            Assert.That(scanner.Scan(), Is.False);

            noteObject.transform.position = isolatedOrigin + Vector3.forward * 2f;
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = isolatedOrigin + Vector3.forward;
            wall.transform.localScale = new Vector3(2f, 2f, 0.2f);
            Physics.SyncTransforms();
            Assert.That(scanner.Scan(), Is.False);

            Object.Destroy(cameraObject);
            Object.Destroy(scannerObject);
            Object.Destroy(noteObject);
            Object.Destroy(wall);
        }

        [UnityTest]
        public IEnumerator KeyAndNoteInteractionsAreIdempotent()
        {
            GameStateService state = new GameStateService();
            InteractionContext context = new InteractionContext(null, null, state);

            GameObject keyObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            KeyInteractable key = keyObject.AddComponent<KeyInteractable>();
            key.ConfigureKey("INT_KEY_PLAYMODE_01", "KEY_PLAYMODE_01");
            key.Interact(context);
            key.Interact(context);

            GameObject noteObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            NoteInteractable note = noteObject.AddComponent<NoteInteractable>();
            note.ConfigureNote("INT_NOTE_PLAYMODE_01", "CLUE_NOTE_PLAYMODE_01", "TEST_NOTE_TITLE", "TEST_NOTE_BODY");
            note.Interact(context);
            note.Interact(context);
            yield return null;

            Assert.That(state.Snapshot.collectedKeys, Has.Count.EqualTo(1));
            Assert.That(state.Snapshot.readClues, Has.Count.EqualTo(1));

            Object.Destroy(keyObject);
            Object.Destroy(noteObject);
        }

        [UnityTest]
        public IEnumerator DoorRequiresKeyThenAnimatesAndRecordsState()
        {
            GameStateService state = new GameStateService();
            InteractionContext context = new InteractionContext(null, null, state);
            GameObject pivot = new GameObject("DoorPivot");
            GameObject doorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorObject.transform.SetParent(pivot.transform);
            DoorInteractable door = doorObject.AddComponent<DoorInteractable>();
            door.ConfigureDoor("INT_DOOR_PLAYMODE_01", pivot.transform, "KEY_PLAYMODE_02");
            yield return null;

            door.Interact(context);
            Assert.That(door.IsOpen, Is.False);
            Assert.That(state.IsDoorOpen("INT_DOOR_PLAYMODE_01"), Is.False);

            state.AddKey("KEY_PLAYMODE_02");
            door.Interact(context);
            yield return new WaitForSeconds(0.1f);

            Assert.That(door.IsOpen, Is.True);
            Assert.That(state.IsDoorOpen("INT_DOOR_PLAYMODE_01"), Is.True);
            Assert.That(Quaternion.Angle(Quaternion.identity, pivot.transform.localRotation), Is.GreaterThan(0.1f));

            Object.Destroy(pivot);
        }

        [UnityTest]
        public IEnumerator DoorStopsInsteadOfSweepingThroughPlayer()
        {
            GameStateService state = new GameStateService();
            InteractionContext context = new InteractionContext(null, null, state);
            GameObject pivot = new GameObject("SafeDoorPivot");
            GameObject doorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorObject.transform.SetParent(pivot.transform); doorObject.transform.localPosition = Vector3.right * .9f;
            doorObject.transform.localScale = new Vector3(1.8f, 2.2f, .12f);
            DoorInteractable door = doorObject.AddComponent<DoorInteractable>();
            door.ConfigureDoor("INT_DOOR_SAFE_TEST", pivot.transform, null, 90f);
            GameObject player = new GameObject("BlockingPlayer");
            player.transform.position = new Vector3(.9f, 0f, -.7f);
            player.AddComponent<CharacterController>(); player.AddComponent<FirstPersonController>();
            yield return null;
            door.Interact(context);
            yield return new WaitForSeconds(.35f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, pivot.transform.eulerAngles.y)), Is.LessThan(90f));
            Object.Destroy(pivot); Object.Destroy(player);
        }

        [UnityTest]
        public IEnumerator PlayerCanCrouchAndReturnToStandingHeight()
        {
            GameObject player = new GameObject("Player");
            CharacterController characterController = player.AddComponent<CharacterController>();
            FirstPersonController controller = player.AddComponent<FirstPersonController>();
            yield return null;

            controller.UpdateCrouch(true);
            Assert.That(controller.IsCrouching, Is.True);
            Assert.That(characterController.height, Is.LessThan(1.8f));

            controller.UpdateCrouch(false);
            Assert.That(controller.IsCrouching, Is.False);
            Assert.That(characterController.height, Is.EqualTo(1.8f).Within(0.01f));

            Object.Destroy(player);
        }

        [UnityTest]
        public IEnumerator PauseMenuStopsAndRestoresGameTime()
        {
            GameObject root = new GameObject("PauseRoot");
            PauseMenuController pause = root.AddComponent<PauseMenuController>();
            pause.Configure(null);
            yield return null;

            pause.Pause();
            Assert.That(Time.timeScale, Is.Zero);

            pause.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ContinuousDemoRespondsToChaseByCuttingBridgeAndKeepsWireOutsideG11()
        {
#if UNITY_EDITOR
            const string scenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";
            GameSession.Current.AutoSaveEnabled = false;
            GameSession.Current.GameState.Restore(new GameStateData());
            AsyncOperation operation = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            while (!operation.isDone) yield return null;
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Transform bridge = FindInScene(scene, "PF_UserWoodBridge_用户木桥");
            Transform wire = FindInScene(scene, "Pickup_UpperCorridor_LockpickWire_门外细铁丝");
            Transform g11Door = FindInScene(scene, "Door_G11_Pivot");
            Transform step = FindInScene(scene, "G03_活板门至B01_Step_00");
            float connectedY = bridge.localPosition.y;

            Assert.That(wire.position.x, Is.GreaterThan(g11Door.position.x));
            Assert.That(step.localScale.z, Is.GreaterThanOrEqualTo(.7f));
            GameSession.Current.GameState.SetButcherChaseStarted(true);
            yield return new WaitForSeconds(.25f);
            Assert.That(bridge.localPosition.y, Is.LessThan(connectedY - .1f), "Bridge must begin cutting in the same play session.");
            GameSession.Current.AutoSaveEnabled = true;
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GatePasswordRequiresKnowledgeAndRestoredBridgeThenAccepts1016Only()
        {
            GameStateService state = new GameStateService();
            InteractionContext context = new InteractionContext(null, null, state);
            GameObject keypadObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GatePasswordInteractable keypad = keypadObject.AddComponent<GatePasswordInteractable>();
            keypad.ConfigurePassword("INT_GATE_PASSWORD_TEST", "1016", null);
            yield return null;

            Assert.That(keypad.SubmitPassword("1016", context), Is.False);
            state.SetGatePasswordKnown(true);
            state.SetBridgeRestored(true);
            Assert.That(keypad.SubmitPassword("9999", context), Is.False);
            Assert.That(state.GateUnlocked, Is.False);
            Assert.That(keypad.SubmitPassword("1016", context), Is.True);
            Assert.That(state.GateUnlocked, Is.True);
            Object.Destroy(keypadObject);
        }

        [UnityTest]
        public IEnumerator PickupInteractionRejectsEAndAcceptsFAction()
        {
            GameStateService state = new GameStateService();
            GameObject cameraObject = new GameObject("PickupCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = Vector3.zero; camera.transform.forward = Vector3.forward;
            GameObject scannerObject = new GameObject("PickupScanner");
            InteractionScanner scanner = scannerObject.AddComponent<InteractionScanner>();
            scanner.Configure(camera, state, 2.5f);
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickupObject.transform.position = Vector3.forward * 1.5f;
            ItemPickupInteractable pickup = pickupObject.AddComponent<ItemPickupInteractable>();
            pickup.ConfigureItem("INT_PICKUP_ACTION_TEST", "ITEM_PICKUP_ACTION_TEST");
            Physics.SyncTransforms(); yield return null;

            Assert.That(scanner.TryInteract(InteractionAction.Interact), Is.False);
            Assert.That(state.HasItem("ITEM_PICKUP_ACTION_TEST"), Is.False);
            Assert.That(scanner.TryInteract(InteractionAction.Pickup), Is.True);
            Assert.That(state.HasItem("ITEM_PICKUP_ACTION_TEST"), Is.True);
            Object.Destroy(cameraObject); Object.Destroy(scannerObject); Object.Destroy(pickupObject);
        }

        [UnityTest]
        public IEnumerator BreakableBoardDisablesItsCollisionAfterDelay()
        {
            GameObject boardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BreakableBoard board = boardObject.AddComponent<BreakableBoard>();
            board.BeginBreak();
            Assert.That(board.IsBreaking, Is.True);
            yield return new WaitForSeconds(1.4f);
            Assert.That(board.IsBroken, Is.True);
            Assert.That(boardObject.GetComponent<Collider>().enabled, Is.False);
            Object.Destroy(boardObject);
        }

        [UnityTest]
        public IEnumerator HideSpotMarksPlayerHiddenAndLocksMovement()
        {
            GameObject playerObject = new GameObject("HideTestPlayer");
            playerObject.AddComponent<CharacterController>();
            FirstPersonController player = playerObject.AddComponent<FirstPersonController>();
            GameObject root = new GameObject("HideRoot");
            Transform inside = new GameObject("Inside").transform; inside.SetParent(root.transform); inside.position = new Vector3(0f,0f,1f);
            Transform exit = new GameObject("Exit").transform; exit.SetParent(root.transform); exit.position = new Vector3(0f,0f,-1f);
            HideSpotInteractable hide = root.AddComponent<HideSpotInteractable>(); hide.Configure("INT_HIDE_TEST", inside, exit);
            yield return null;
            hide.Interact(new InteractionContext(playerObject, null, new GameStateService()));
            Assert.That(player.InputLocked, Is.True);
            Assert.That(playerObject.GetComponent<PlayerStealthState>().IsHidden, Is.True);
            Assert.That(playerObject.GetComponent<CharacterController>().enabled, Is.False);
            Object.Destroy(playerObject); Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ChaseTimerCanVerifyTheFull480SecondBoundaryWithoutWaiting()
        {
            GameStateService state = new GameStateService();
            state.SetButcherChaseStarted(true);
            GameObject timerObject = new GameObject("Timer");
            ButcherChaseTimer timer = timerObject.AddComponent<ButcherChaseTimer>();
            timer.Advance(state, 479.9f);
            Assert.That(state.DawnTriggered, Is.False);
            timer.Advance(state, .2f);
            Assert.That(state.DawnTriggered, Is.True);
            Assert.That(state.Snapshot.checkpointId, Is.EqualTo(PlayerCheckpointRestorer.DawnId));
            Object.Destroy(timerObject);
            yield return null;
        }
    }
}
