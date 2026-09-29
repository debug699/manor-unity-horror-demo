#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Manor.Core;
using Manor.Gameplay;
using Manor.Runtime;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Manor.Tests.PlayMode
{
    public sealed class DoorWindowAcceptancePlayModeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";

        [UnityTest]
        public IEnumerator EveryOrdinaryDoorOpensToAPassableAngleWithoutStaticSweepCollision()
        {
            GameSession.Current.AutoSaveEnabled = false;
            GameStateData data = new GameStateData();
            data.collectedKeys.Add("KEY_G10_RESEARCH_ROOM");
            data.collectedKeys.Add("KEY_MANOR_GATE_PASSWORD");
            GameSession.Current.GameState.Restore(data);
            AsyncOperation operation = EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            while (!operation.isDone) yield return null;
            yield return null;

            DoorInteractable[] doors = Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(doors.Length, Is.GreaterThanOrEqualTo(10));
            foreach (DoorInteractable door in doors)
            {
                Transform pivot = door.transform.parent;
                Quaternion closed = pivot.localRotation;
                door.Interact(new InteractionContext(null, null, GameSession.Current.GameState));
                yield return new WaitForSeconds(.65f);
                float openedAngle = Quaternion.Angle(closed, pivot.localRotation);
                Assert.That(door.IsOpen, Is.True, door.name + " did not enter open state");
                Assert.That(openedAngle, Is.GreaterThan(55f), door.name + " was blocked by static geometry at only " + openedAngle.ToString("F1") + " degrees");
            }
            GameSession.Current.AutoSaveEnabled = true;
        }

        [UnityTest]
        public IEnumerator G07WindowHasTwoDistinctLandingSidesAndARealSill()
        {
            AsyncOperation operation = EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            while (!operation.isDone) yield return null;
            yield return null;
            Transform outside = Find("G07_VaultLanding_外侧落点");
            Transform inside = Find("G07_VaultLanding_内侧落点");
            Transform sill = Find("G07_WindowSill_实体窗台");
            Assert.That(outside, Is.Not.Null);
            Assert.That(inside, Is.Not.Null);
            Assert.That(Vector3.Distance(outside.position, inside.position), Is.GreaterThan(3f));
            Assert.That(sill.GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(sill.GetComponent<BoxCollider>().isTrigger, Is.False);
            Assert.That(Find("G07_VaultTrigger_翻窗交互").GetComponent<VaultWindowInteractable>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator AuthoredManorExteriorHidesInsideAndRestoresOutside()
        {
            AsyncOperation operation = EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            while (!operation.isDone) yield return null;
            yield return null;

            ManorExteriorVisibilityController visibility = Object.FindFirstObjectByType<ManorExteriorVisibilityController>();
            Assert.That(visibility, Is.Not.Null);
            Renderer shell = Find("PF_NewUserManor_新版主楼").GetComponentsInChildren<Renderer>(true)
                .OrderByDescending(renderer => renderer.bounds.size.x * renderer.bounds.size.y * renderer.bounds.size.z)
                .First();
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);

            camera.transform.position = new Vector3(0f, 1.65f, 5f);
            yield return null;
            Assert.That(shell.enabled, Is.False, "The merged authored shell must not clip through G rooms while the player is inside.");

            camera.transform.position = new Vector3(0f, 1.65f, 24f);
            yield return null;
            Assert.That(shell.enabled, Is.True, "The authored new manor must be restored when viewed from the courtyard.");
        }

        private static Transform Find(string name)
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .Select(root => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name))
                .FirstOrDefault(found => found != null);
        }
    }
}
#endif
