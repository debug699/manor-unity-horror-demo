#if UNITY_EDITOR
using System;
using Manor.Core;
using Manor.Gameplay;
using Manor.Narrative;
using UnityEngine;

namespace Manor.Editor
{
    /// <summary>Builds the confirmed G01-G12 topology as one continuous layout inside the exterior shell.</summary>
    internal static class ManorContinuousInteriorBuilder
    {
        internal const string RootName = "ManagedInteriorV2_连续主楼布局";
        private enum Side { North, South, East, West }

        internal static Transform Build(Transform environment, Material floor, Material wall, Material basement)
        {
            Transform existing = Find(environment, RootName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            Transform root = new GameObject(RootName).transform;
            root.SetParent(environment);

            Transform ground = Group(root, "GroundFloor_G01-G07_一层");
            Transform upper = Group(root, "UpperFloor_G08-G12_二层");
            Transform below = Group(root, "Basement_B01-B02_地下");
            Transform doors = Group(root, "Doors_实体门");
            root.gameObject.AddComponent<StoryFlowController>();
            root.gameObject.AddComponent<ButcherChaseTimer>();

            Room("G01_玄关", ground, new Vector3(0f, 0f, 8f), new Vector2(6f, 4f), floor, wall, true, Side.North, Side.South);
            Room("G02_客厅套间", ground, new Vector3(-11f, 0f, 3f), new Vector2(16f, 10f), floor, wall, true, Side.East);
            RoomWithFloorOpening("G03_厨房套间", ground, new Vector3(-11f, 0f, -8f), new Vector2(16f, 10f),
                new Vector2(6.4f, 2.4f), new Vector2(2.8f, 0f), floor, wall, Side.East);
            Room("G04_屠宰工作间", ground, new Vector3(10f, 0f, -8f), new Vector2(14f, 10f), floor, wall, true, Side.West, Side.East);
            Corridor("G05_主走廊", ground, new Vector3(0f, -0.1f, -1.5f), new Vector3(5f, .2f, 17f), floor);
            Room("G06_楼梯间", ground, new Vector3(8f, 0f, 3f), new Vector2(8f, 10f), floor, wall, false, Side.West, Side.East);
            Corridor("G05_G06_连续门槛", ground, new Vector3(3.25f, -0.1f, 3f), new Vector3(1.5f, .2f, 2f), floor);
            Room("G07_后门通道", ground, new Vector3(19f, 0f, -1f), new Vector2(4f, 20f), floor, wall, true, Side.West, Side.North);
            Corridor("G04_G07_清理后连接", ground, new Vector3(17.5f, -0.08f, -8f), new Vector3(3f, .16f, 2f), floor);
            Corridor("G06_G07_连续通道", ground, new Vector3(14.5f, -0.08f, 3f), new Vector3(5f, .16f, 2f), floor);
            Corridor("G07_后门门槛", ground, new Vector3(19f, -0.08f, 9.65f), new Vector3(2f, .16f, 1.3f), floor);

            Room("G08_孕妇房间套间", upper, new Vector3(-11f, 3.6f, 5f), new Vector2(12f, 8f), floor, wall, true, Side.East);
            RoomWithFloorOpening("G09_儿童房双区", upper, new Vector3(8f, 3.6f, 5f), new Vector2(16f, 8f),
                new Vector2(3f, 3.5f), new Vector2(0f, -2.25f), floor, wall, Side.West);
            Room("G10_屠夫研究室", upper, new Vector3(-11f, 3.6f, -3f), new Vector2(12f, 7f), floor, wall, true, Side.East, Side.South);
            Room("G11_祷告室", upper, new Vector3(-11f, 3.6f, -10f), new Vector2(12f, 6f), floor, wall, true, Side.East, Side.North);
            Room("G12_旧客房与封闭过渡空间", upper, new Vector3(8f, 3.6f, -4f), new Vector2(12f, 9f), floor, wall, true, Side.West, Side.South);
            Corridor("UpperCorridor_二层中心走廊", upper, new Vector3(0f, 3.5f, -1.5f), new Vector3(5f, .2f, 17f), floor);
            Corridor("UpperCorridor_G08_连续门槛", upper, new Vector3(-3.75f, 3.5f, 5f), new Vector3(2.5f, .2f, 2f), floor);
            Corridor("UpperCorridor_G10_连续门槛", upper, new Vector3(-3.75f, 3.5f, -3f), new Vector3(2.5f, .2f, 2f), floor);
            Corridor("UpperCorridor_G11_连续门槛", upper, new Vector3(-3.75f, 3.5f, -10f), new Vector3(2.5f, .2f, 2f), floor);

            Stairs("G06_实体楼梯", ground, new Vector3(8f, 0f, -0.8f), Vector3.forward, 18, .2f, .32f, 2.4f, floor, false);
            Corridor("G06_二层落脚平台", upper, new Vector3(8f, 3.5f, 5.1f), new Vector3(4f, .2f, 1.2f), floor);

            // Kitchen hatch is the only active basement entrance. G06 receives no descending stairs.
            Stairs("G03_活板门至B01", below, new Vector3(-11f, 0f, -8f), Vector3.right, 16, .2f, .34f, 2f, basement, true);
            Room("B01_地下服务路线", below, new Vector3(-2f, -3.2f, -8f), new Vector2(8f, 4f), basement, basement, true, Side.West, Side.East);
            Corridor("B01_B02_地下连接", below, new Vector3(2.5f, -3.3f, -8f), new Vector3(1f, .2f, 2f), basement);
            Room("B02_地下祭祀室", below, new Vector3(7.5f, -3.2f, -8f), new Vector2(10f, 10f), basement, basement, true, Side.West);

            Door("Door_G02", doors, new Vector3(-3f, 1.1f, 3f), Quaternion.Euler(0f, 90f, 0f), "INT_DOOR_G02");
            Door("Door_G03", doors, new Vector3(-3f, 1.1f, -8f), Quaternion.Euler(0f, 90f, 0f), "INT_DOOR_G03");
            Door("Door_G04", doors, new Vector3(3f, 1.1f, -8f), Quaternion.Euler(0f, 90f, 0f), "INT_DOOR_G04");
            Door("Door_G06", doors, new Vector3(4f, 1.1f, 3f), Quaternion.Euler(0f, 90f, 0f), "INT_DOOR_G06");
            Door("Door_G08", doors, new Vector3(-5f, 4.7f, 5f), Quaternion.Euler(0f, 90f, 0f), "INT_DOOR_G08");
            Door("Door_G09", doors, new Vector3(0f, 4.7f, 5f), Quaternion.Euler(0f, 90f, 0f), "INT_DOOR_G09");
            Door("Door_G10", doors, new Vector3(-5f, 4.7f, -3f), Quaternion.Euler(0f, 90f, 0f), "INT_DOOR_G10", "KEY_G10_RESEARCH_ROOM");
            LockpickDoor("Door_G11", doors, new Vector3(-5f, 4.7f, -10f), Quaternion.Euler(0f, 90f, 0f));
            Door("Door_G12", doors, new Vector3(2f, 4.7f, -4f), Quaternion.Euler(0f, 90f, 0f), "INT_DOOR_G12");
            Pickup("Pickup_UpperCorridor_LockpickWire_门外细铁丝", upper, new Vector3(-2.8f, 3.72f, -8.8f),
                "INT_PICKUP_G11_WIRE", "ITEM_G11_LOCKPICK_WIRE", "WIRE_ADDED");
            Key("Pickup_G10_ResearchKey_研究室钥匙", upper, new Vector3(-12.5f, 4.05f, -10f),
                "INT_PICKUP_G10_KEY", "KEY_G10_RESEARCH_ROOM");
            Note("Clue_G08_BedDiary_床下日记", upper, new Vector3(-12f, 3.72f, 5.8f),
                "INT_CLUE_G08_DIARY", "CLUE_G08_DIARY", "CLUE_G08_DIARY_TITLE", "CLUE_G08_DIARY_BODY");
            Note("Clue_G04_Traces_拖痕石灰木屑", ground, new Vector3(9f, .08f, -8f),
                "INT_CLUE_G04_TRACES", "CLUE_G04_TRACES", "CLUE_G04_TRACES_TITLE", "CLUE_G04_TRACES_BODY");
            Note("Clue_G10_RitualRecord_残缺阵图", upper, new Vector3(-10f, 3.9f, -3f),
                "INT_CLUE_G10_RITUAL", "CLUE_G10_RITUAL_RECORD", "CLUE_G10_RITUAL_TITLE", "CLUE_G10_RITUAL_BODY");
            Note("Clue_G09_BoyFamily_男童家庭遗物", upper, new Vector3(5.5f, 3.72f, 5.8f),
                "INT_CLUE_G09_BOY", "CLUE_G09_BOY_FAMILY", "CLUE_G09_BOY_TITLE", "CLUE_G09_BOY_BODY");
            Note("Clue_G09_GirlFamily_女童家庭遗物", upper, new Vector3(10.5f, 3.72f, 4.2f),
                "INT_CLUE_G09_GIRL", "CLUE_G09_GIRL_FAMILY", "CLUE_G09_GIRL_TITLE", "CLUE_G09_GIRL_BODY");
            Hatch("G03_KitchenHatch_厨房活板门", ground, new Vector3(-11.4f, .02f, -8f));
            Region("B01_EntryRegion_地下入口触发", below, new Vector3(-5.8f, -2.1f, -8f), new Vector3(1.5f, 2.2f, 2f),
                StoryStage.EnterBasement, ProjectIds.ObjectiveInvestigateRitualRoom);
            Region("B02_RitualRegion_祭祀室触发", below, new Vector3(7.5f, -1.8f, -8f), new Vector3(7f, 2.5f, 7f),
                StoryStage.EnterBasement, ProjectIds.ObjectiveInvestigateRitualRoom);
            Pickup("Pickup_B02_CompleteParchment_完整羊皮纸", below, new Vector3(8.5f, -3.02f, -8f),
                "INT_PICKUP_COMPLETE_PARCHMENT", "ITEM_COMPLETE_PARCHMENT", "PARCHMENT_ADDED");
            Communication("B02_ParchmentCommunication_羊皮纸沟通", below, new Vector3(7.5f, -2.3f, -10f));
            return root;
        }

        private static void Room(string name, Transform parent, Vector3 center, Vector2 size, Material floor, Material wall, bool ceiling, params Side[] openings)
        {
            Transform room = Group(parent, name); room.position = center;
            Box(name + "_Floor", room, new Vector3(0f, -.1f, 0f), new Vector3(size.x, .2f, size.y), floor, true);
            if (ceiling) Box(name + "_Ceiling", room, new Vector3(0f, 3.08f, 0f), new Vector3(size.x, .16f, size.y), wall, true);
            BuildWalls(name, room, size, wall, openings);
        }

        private static void RoomWithFloorOpening(string name, Transform parent, Vector3 center, Vector2 size,
            Vector2 openingSize, Vector2 openingOffset, Material floor, Material wall, params Side[] openings)
        {
            Transform room = Group(parent, name); room.position = center;
            float leftWidth = openingOffset.x - openingSize.x * .5f + size.x * .5f;
            float rightWidth = size.x * .5f - openingOffset.x - openingSize.x * .5f;
            float backDepth = openingOffset.y - openingSize.y * .5f + size.y * .5f;
            float frontDepth = size.y * .5f - openingOffset.y - openingSize.y * .5f;

            if (leftWidth > 0f)
                Box(name + "_Floor_Left", room, new Vector3(-size.x * .5f + leftWidth * .5f, -.1f, 0f), new Vector3(leftWidth, .2f, size.y), floor, true);
            if (rightWidth > 0f)
                Box(name + "_Floor_Right", room, new Vector3(size.x * .5f - rightWidth * .5f, -.1f, 0f), new Vector3(rightWidth, .2f, size.y), floor, true);
            if (backDepth > 0f)
                Box(name + "_Floor_Back", room, new Vector3(openingOffset.x, -.1f, -size.y * .5f + backDepth * .5f), new Vector3(openingSize.x, .2f, backDepth), floor, true);
            if (frontDepth > 0f)
                Box(name + "_Floor_Front", room, new Vector3(openingOffset.x, -.1f, size.y * .5f - frontDepth * .5f), new Vector3(openingSize.x, .2f, frontDepth), floor, true);
            Box(name + "_Ceiling", room, new Vector3(0f, 3.08f, 0f), new Vector3(size.x, .16f, size.y), wall, true);

            BuildWalls(name, room, size, wall, openings);
        }

        private static void BuildWalls(string name, Transform room, Vector2 size, Material wall, Side[] openings)
        {
            WallSide(name, room, Side.North, size, wall, Has(openings, Side.North));
            WallSide(name, room, Side.South, size, wall, Has(openings, Side.South));
            WallSide(name, room, Side.East, size, wall, Has(openings, Side.East));
            WallSide(name, room, Side.West, size, wall, Has(openings, Side.West));
        }

        private static void WallSide(string roomName, Transform parent, Side side, Vector2 size, Material material, bool opening)
        {
            const float height = 3f, thickness = .22f, doorWidth = 1.8f, doorHeight = 2.2f;
            bool horizontal = side == Side.North || side == Side.South;
            float length = horizontal ? size.x : size.y;
            Vector3 center = side switch { Side.North => new(0f, height / 2f, size.y / 2f), Side.South => new(0f, height / 2f, -size.y / 2f), Side.East => new(size.x / 2f, height / 2f, 0f), _ => new(-size.x / 2f, height / 2f, 0f) };
            Vector3 full = horizontal ? new(length, height, thickness) : new(thickness, height, length);
            if (!opening) { Box(roomName + "_Wall_" + side, parent, center, full, material, true); return; }
            float segment = Mathf.Max(.1f, (length - doorWidth) / 2f), offset = (doorWidth + segment) / 2f;
            Vector3 axis = horizontal ? Vector3.right : Vector3.forward;
            Vector3 segmentSize = horizontal ? new(segment, height, thickness) : new(thickness, height, segment);
            Box(roomName + "_Wall_" + side + "_L", parent, center - axis * offset, segmentSize, material, true);
            Box(roomName + "_Wall_" + side + "_R", parent, center + axis * offset, segmentSize, material, true);
            Vector3 lintel = horizontal ? new(doorWidth, height - doorHeight, thickness) : new(thickness, height - doorHeight, doorWidth);
            Box(roomName + "_Wall_" + side + "_Lintel", parent, center + Vector3.up * (doorHeight / 2f), lintel, material, true);
        }

        private static void Corridor(string name, Transform parent, Vector3 center, Vector3 size, Material material) => Box(name, parent, center, size, material, true);

        private static void Stairs(string name, Transform parent, Vector3 start, Vector3 direction, int count, float rise, float run, float width, Material material, bool descending)
        {
            Transform stairs = Group(parent, name);
            float verticalSign = descending ? -1f : 1f;
            const float treadThickness = .12f;
            bool travelsAlongX = Mathf.Abs(direction.x) > Mathf.Abs(direction.z);
            for (int index = 0; index < count; index++)
            {
                float surfaceY = verticalSign * (index + 1) * rise;
                Vector3 treadSize = travelsAlongX
                    ? new Vector3(run + .03f, treadThickness, width)
                    : new Vector3(width, treadThickness, run + .03f);
                Box(name + "_Step_" + index.ToString("00"), stairs,
                    start + direction * (index * run) + Vector3.up * (surfaceY - treadThickness * .5f),
                    treadSize, material, true);

                float previousSurfaceY = verticalSign * index * rise;
                Vector3 riserSize = travelsAlongX
                    ? new Vector3(.08f, rise, width)
                    : new Vector3(width, rise, .08f);
                Box(name + "_Riser_" + index.ToString("00"), stairs,
                    start + direction * (index * run - run * .5f) + Vector3.up * ((surfaceY + previousSurfaceY) * .5f),
                    riserSize, material, true);
            }

            // Two slim structural stringers make the staircase read as supported 3D construction
            // instead of floating treads, without recreating the floor-to-ceiling slab that used
            // to block the high-end first-person view.
            Vector3 slope = direction * ((count - 1) * run) + Vector3.up * (verticalSign * (count - 1) * rise);
            Vector3 midpoint = start + slope * .5f + Vector3.up * (verticalSign * rise - .16f);
            Vector3 lateral = new Vector3(-direction.z, 0f, direction.x).normalized;
            foreach (float side in new[] { -.38f, .38f })
            {
                GameObject stringer = Box(name + "_Stringer_" + (side < 0f ? "L" : "R"), stairs,
                    midpoint + lateral * (width * side), new Vector3(.14f, .18f, slope.magnitude + run), material, true);
                stringer.transform.rotation = Quaternion.FromToRotation(Vector3.forward, slope.normalized);
            }
        }

        private static void Door(string name, Transform parent, Vector3 position, Quaternion rotation, string id, string requiredKeyId = null)
        {
            Transform pivot = Group(parent, name + "_Pivot"); pivot.SetPositionAndRotation(position, rotation);
            GameObject leaf = ManorOutdoorV1Builder.CreateInteriorAuthoredDoorLeaf(pivot, name, 1.8f, 2.2f);
            DoorInteractable door = leaf.AddComponent<DoorInteractable>(); door.ConfigureDoor(id, pivot, requiredKeyId, 92f);
        }

        private static void LockpickDoor(string name, Transform parent, Vector3 position, Quaternion rotation)
        {
            Transform pivot = Group(parent, name + "_Pivot"); pivot.SetPositionAndRotation(position, rotation);
            GameObject leaf = ManorOutdoorV1Builder.CreateInteriorAuthoredDoorLeaf(pivot, name, 1.8f, 2.2f);
            LockpickDoorInteractable door = leaf.AddComponent<LockpickDoorInteractable>();
            door.ConfigureLockpickDoor("INT_DOOR_G11", pivot, "ITEM_G11_LOCKPICK_WIRE", "KEY_G11_LOCKPICKED", 92f);
        }

        private static void Pickup(string name, Transform parent, Vector3 position, string interactionId, string itemId, string feedbackKey)
        {
            GameObject pickup = Box(name, parent, position, new Vector3(.28f, .04f, .08f), null, true);
            ItemPickupInteractable interactable = pickup.AddComponent<ItemPickupInteractable>();
            interactable.ConfigureItem(interactionId, itemId, "PROMPT_PICKUP_ITEM", feedbackKey);
        }

        private static void Key(string name, Transform parent, Vector3 position, string interactionId, string keyId)
        {
            GameObject pickup = Box(name, parent, position, new Vector3(.18f, .05f, .06f), null, true);
            KeyInteractable interactable = pickup.AddComponent<KeyInteractable>();
            interactable.ConfigureKey(interactionId, keyId);
        }

        private static void Note(string name, Transform parent, Vector3 position, string interactionId, string clueId, string titleKey, string bodyKey)
        {
            GameObject note = Box(name, parent, position, new Vector3(.35f, .025f, .24f), null, true);
            NoteInteractable interactable = note.AddComponent<NoteInteractable>();
            interactable.ConfigureNote(interactionId, clueId, titleKey, bodyKey);
        }

        private static void Hatch(string name, Transform parent, Vector3 hingePosition)
        {
            Transform pivot = Group(parent, name + "_Pivot");
            pivot.position = hingePosition;
            GameObject leaf = Box(name + "_Leaf", pivot, new Vector3(3.2f, 0f, 0f), new Vector3(6.4f, .12f, 2.4f), null, true);
            BasementHatchInteractable hatch = leaf.AddComponent<BasementHatchInteractable>();
            hatch.ConfigureHatch("INT_G03_BASEMENT_HATCH", pivot,
                new[] { "CLUE_G08_DIARY", "CLUE_G04_TRACES", "CLUE_G10_RITUAL_RECORD" });
        }

        private static void Region(string name, Transform parent, Vector3 position, Vector3 size, StoryStage stage, string objectiveId)
        {
            GameObject region = Box(name, parent, position, size, null, true);
            Renderer renderer = region.GetComponent<Renderer>();
            renderer.enabled = false;
            region.GetComponent<Collider>().isTrigger = true;
            StoryRegionTrigger trigger = region.AddComponent<StoryRegionTrigger>();
            trigger.Configure(stage, objectiveId);
        }

        private static void Communication(string name, Transform parent, Vector3 position)
        {
            GameObject altar = Box(name, parent, position, new Vector3(1.2f, 1.6f, .7f), null, true);
            altar.AddComponent<ParchmentCommunicationInteractable>();
        }

        private static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool collider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; box.transform.SetParent(parent); box.transform.localPosition = localPosition; box.transform.localScale = scale;
            if (material != null) box.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        private static bool Has(Side[] values, Side value) => values != null && Array.IndexOf(values, value) >= 0;
        private static Transform Group(Transform parent, string name) { Transform child = new GameObject(name).transform; child.SetParent(parent); return child; }
        private static Transform Find(Transform root, string name) { if (root.name == name) return root; foreach (Transform child in root) { Transform found = Find(child, name); if (found != null) return found; } return null; }
    }
}
#endif
