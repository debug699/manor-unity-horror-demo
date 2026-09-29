#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Manor.Tests.EditMode
{
    public sealed class ManorPhysicalSceneAuditTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity";

        [SetUp]
        public void OpenScene() => EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        [Test]
        public void MajorEstateStructuresUseUserAuthoredModels_NotProgrammaticBoards()
        {
            Assert.That(Find("PF_UserGate_用户大门"), Is.Not.Null, "The estate gate must use 管理/参考图/建模/大门.");
            Assert.That(Find("PF_UserWoodBridge_用户木桥"), Is.Not.Null, "The ravine bridge must use 管理/参考图/建模/木桥.");
            Assert.That(Find("PF_UserWorkshopHut_加工木屋主用"), Is.Not.Null);
            Assert.That(Find("PF_UserAbandonedHut_废弃木屋主用"), Is.Not.Null);
            Assert.That(Find("PF_UserStoneWorkshop_木石工坊主用"), Is.Not.Null);
            Assert.That(Find("O02_Workshop_可进入工棚/Wall_Left"), Is.Null, "Programmatic board shells must not surround the authored houses.");
            Assert.That(Find("O03_AbandonedHut_可进入废屋/Wall_Left"), Is.Null);
            Assert.That(Find("O04_StoneWorkshop_可进入石工坊/Wall_Left"), Is.Null);
            Assert.That(Find("BridgeDeck_桥面"), Is.Null, "A cube must not substitute for the user's wooden bridge.");
        }

        [Test]
        public void DoorsUseAuthoredLeavesAndFitTheirWallApertures()
        {
            Assert.That(Find("ManorDoorLeaf_Interactive"), Is.Null, "The main entrance must not be a primitive cube leaf.");
            Assert.That(Find("DoorLeaf_可交互木门"), Is.Null, "The estate gate must not be a primitive cube leaf.");
            foreach (GameObject leaf in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(go => go.name.Contains("UserDoorLeaf_")))
            {
                Renderer[] renderers = leaf.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers, Is.Not.Empty, leaf.name + " has no authored visible mesh.");
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                Assert.That(bounds.size.y, Is.InRange(1.9f, 4.5f), leaf.name + " has an implausible physical height.");
                Assert.That(leaf.GetComponentsInChildren<Collider>(true), Is.Not.Empty, leaf.name + " needs physical collision.");
            }
        }

        [Test]
        public void DecorativePropsAreSupportedByPhysicalSurfaces()
        {
            Transform root = Find("UserDecoration_用户装饰");
            Assert.That(root, Is.Not.Null, "User-authored decoration set has not replaced the generated fragments.");
            Physics.SyncTransforms();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled))
            {
                Bounds bounds = renderer.bounds;
                Vector3 origin = new(bounds.center.x, bounds.min.y + .04f, bounds.center.z);
                RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, .35f, ~0, QueryTriggerInteraction.Ignore);
                bool supported = hits.Any(hit => !hit.transform.IsChildOf(renderer.transform) && !renderer.transform.IsChildOf(hit.transform));
                Assert.That(supported || bounds.min.y <= .08f, Is.True,
                    $"Floating decoration: {HierarchyPath(renderer.transform)} bounds.min.y={bounds.min.y:F3}");
            }
        }

        [Test]
        public void SelectedRoomPropsAreGroundedAndHavePhysicalCollision()
        {
            Transform root = Find("InteriorArt_v2_连续主楼陈设");
            Assert.That(root, Is.Not.Null);
            Transform[] groups = root.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform.name.StartsWith("ART_G", System.StringComparison.Ordinal) && transform.name.EndsWith("_精选子件", System.StringComparison.Ordinal))
                .ToArray();
            Assert.That(groups.Length, Is.GreaterThanOrEqualTo(7));
            Physics.SyncTransforms();

            foreach (Transform group in groups)
            foreach (Renderer renderer in group.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled))
            {
                Assert.That(renderer.GetComponentInParent<Collider>(), Is.Not.Null,
                    HierarchyPath(renderer.transform) + " is visible furniture without physical collision.");
                Bounds bounds = renderer.bounds;
                Vector3 origin = new(bounds.center.x, bounds.min.y + .06f, bounds.center.z);
                bool supported = Physics.RaycastAll(origin, Vector3.down, .18f, ~0, QueryTriggerInteraction.Ignore)
                    .Any(hit => !hit.transform.IsChildOf(renderer.transform) && !renderer.transform.IsChildOf(hit.transform));
                float expectedFloor = bounds.center.y < 3.2f ? 0f : 3.6f;
                Assert.That(supported || Mathf.Abs(bounds.min.y - expectedFloor) <= .065f, Is.True,
                    $"Floating room prop: {HierarchyPath(renderer.transform)} bounds.min.y={bounds.min.y:F3}");
            }
        }

        [Test]
        public void ButcherIsSlightlyTallerThanPlayerAndStartsUnderground()
        {
            Transform butcher = Find("Butcher_屠夫临时角色");
            Transform player = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(transform => transform.name.StartsWith("Player_汤姆"));
            Assert.That(butcher, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Bounds butcherBounds = CombinedBounds(butcher);
            CharacterController playerController = player.GetComponent<CharacterController>();
            Assert.That(butcherBounds.size.y, Is.GreaterThan(playerController.height + .05f));
            Assert.That(butcherBounds.size.y, Is.LessThan(playerController.height + .45f));
            Assert.That(butcher.position.y, Is.LessThan(-1f), "The current butcher start must be in B01/B02, not the courtyard.");
        }

        [Test]
        public void VisibleRoomWallsMatchPhysicalRoomWalls()
        {
            Transform layout = Find("ManagedInteriorV2_连续主楼布局");
            Assert.That(layout, Is.Not.Null);
            foreach (Collider collider in layout.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && c.name.Contains("Wall_")))
            {
                Renderer renderer = collider.GetComponent<Renderer>();
                Assert.That(renderer, Is.Not.Null, HierarchyPath(collider.transform) + " has collision without a visible wall.");
                Assert.That(renderer.enabled, Is.True, HierarchyPath(collider.transform) + " is an invisible air wall.");
            }
        }

        [Test]
        public void G06StairsHaveThinTreadsAndRisers()
        {
            Transform stairs = Find("G06_实体楼梯");
            Assert.That(stairs, Is.Not.Null);
            foreach (BoxCollider collider in stairs.GetComponentsInChildren<BoxCollider>(true))
            {
                if (collider.name.Contains("Stringer")) continue;
                if (collider.name.Contains("Step_")) Assert.That(collider.bounds.size.y, Is.LessThanOrEqualTo(.13f), collider.name);
                if (collider.name.Contains("Riser_")) Assert.That(collider.bounds.size.y, Is.LessThanOrEqualTo(.21f), collider.name);
            }
        }

        [Test]
        public void MainEntranceHasPlayerCapsuleClearanceAfterDoorOpens()
        {
            Transform pivot = Find("ManorDoorPivot_HouseEntrance");
            Assert.That(pivot, Is.Not.Null);
            pivot.localRotation = Quaternion.Euler(0f, 105f, 0f);
            Physics.SyncTransforms();

            for (float z = 11.1f; z >= 8.9f; z -= .2f)
            {
                Vector3 foot = new(0f, .02f, z);
                Collider[] blockers = PlayerCapsuleOverlaps(foot)
                    .Where(collider => !collider.transform.IsChildOf(pivot))
                    .Where(collider => !IsWalkableSupport(collider, foot.y))
                    .ToArray();
                Assert.That(blockers, Is.Empty,
                    $"Main entrance air wall at z={z:F2}: {string.Join(", ", blockers.Select(c => HierarchyPath(c.transform)))}");
            }
        }

        [Test]
        public void G06StairRouteHasPlayerCapsuleClearanceToUpperFloor()
        {
            Transform stairs = Find("G06_实体楼梯");
            Assert.That(stairs, Is.Not.Null);
            Physics.SyncTransforms();

            for (int index = 0; index < 18; index++)
            {
                Vector3 foot = new(8f, (index + 1) * .2f + .02f, -.8f + index * .32f);
                Collider[] blockers = PlayerCapsuleOverlaps(foot)
                    .Where(collider => !collider.transform.IsChildOf(stairs))
                    .Where(collider => !IsWalkableSupport(collider, foot.y))
                    .ToArray();
                Assert.That(blockers, Is.Empty,
                    $"G06 stair capsule blocked at step {index}: {string.Join(", ", blockers.Select(c => HierarchyPath(c.transform)))}");
            }
        }

        [Test]
        public void ButcherBasementPatrolPointsAreOnOneCompleteNavMeshRoute()
        {
            Transform[] points = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(transform => transform.name.StartsWith("ButcherPoint_B", System.StringComparison.Ordinal))
                .OrderBy(transform => transform.name)
                .ToArray();
            Assert.That(points.Length, Is.EqualTo(5));

            NavMeshHit[] sampled = points.Select(point =>
            {
                Assert.That(NavMesh.SamplePosition(point.position, out NavMeshHit hit, 1.2f, NavMesh.AllAreas), Is.True,
                    point.name + " is not on the baked basement NavMesh.");
                return hit;
            }).ToArray();

            for (int index = 0; index < sampled.Length; index++)
            {
                NavMeshHit start = sampled[index];
                NavMeshHit end = sampled[(index + 1) % sampled.Length];
                NavMeshPath path = new();
                Assert.That(NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path), Is.True,
                    points[index].name + " could not calculate a path to the next basement patrol point.");
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete),
                    points[index].name + " has an incomplete basement patrol segment.");
            }
        }

        private static Collider[] PlayerCapsuleOverlaps(Vector3 foot)
        {
            const float radius = .3f;
            Vector3 bottom = foot + Vector3.up * .35f;
            Vector3 top = foot + Vector3.up * 1.5f;
            return Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
        }

        private static bool IsWalkableSupport(Collider collider, float footY) =>
            collider.bounds.max.y <= footY + .3f;

        private static Bounds CombinedBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            Assert.That(renderers, Is.Not.Empty, root.name + " has no visible renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Transform Find(string nameOrPath)
        {
            string[] parts = nameOrPath.Split('/');
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform current = FindByName(root.transform, parts[0]);
                if (current == null) continue;
                for (int i = 1; i < parts.Length && current != null; i++) current = current.Find(parts[i]);
                if (current != null) return current;
            }
            return null;
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static string HierarchyPath(Transform transform)
        {
            string value = transform.name;
            while (transform.parent != null) { transform = transform.parent; value = transform.name + "/" + value; }
            return value;
        }
    }
}
#endif
