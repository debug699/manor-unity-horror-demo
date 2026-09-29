using Manor.Core;
using Manor.Player;
using UnityEngine;

namespace Manor.Runtime
{
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class PlayerCheckpointRestorer : MonoBehaviour
    {
        public const string GateStartId = "CHECKPOINT_GATE_START";
        public const string ChaseStartId = "CHECKPOINT_CHASE_START";
        public const string DawnId = "CHECKPOINT_DAWN";
        // Temporary layout-inspection start: the clear landing in front of the manor's main door.
        // This replaces the old bridge position, which was inside detailed railing geometry.
        public static readonly Vector3 GateStart = new Vector3(0f, .05f, 16.2f);

        private void Start()
        {
            if (GetComponent<StaticWorldMeshColliders>() == null) gameObject.AddComponent<StaticWorldMeshColliders>();
            IGameStateService state = GameSession.Current?.GameState;
            if (state == null) return;
            GameStateData snapshot = state.Snapshot;
            // During layout work always use the known-clear entrance position. Old auto-saves
            // otherwise keep restoring the former bridge start and hide scene changes.
            Vector3 target = GateStart;
            CharacterController controller = GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = target;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            controller.enabled = wasEnabled;
        }

    }
}

