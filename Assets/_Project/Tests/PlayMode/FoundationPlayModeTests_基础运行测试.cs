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
            Assert.That(scanner.TryInteract(), Is.True);
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
            camera.transform.position = Vector3.zero;
            camera.transform.forward = Vector3.forward;

            GameObject scannerObject = new GameObject("Scanner");
            InteractionScanner scanner = scannerObject.AddComponent<InteractionScanner>();
            GameStateService state = new GameStateService();
            scanner.Configure(camera, state, 2.5f);

            GameObject noteObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            noteObject.transform.position = new Vector3(0f, 0f, 2f);
            NoteInteractable note = noteObject.AddComponent<NoteInteractable>();
            note.ConfigureNote("INT_NOTE_TEST_01", "CLUE_NOTE_TEST_01", "TEST_NOTE_TITLE", "TEST_NOTE_BODY");
            Physics.SyncTransforms();
            yield return null;

            Assert.That(scanner.Scan(), Is.True);

            noteObject.transform.position = new Vector3(0f, 0f, 4f);
            Physics.SyncTransforms();
            Assert.That(scanner.Scan(), Is.False);

            noteObject.transform.position = new Vector3(0f, 0f, 2f);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 0f, 1f);
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
    }
}
