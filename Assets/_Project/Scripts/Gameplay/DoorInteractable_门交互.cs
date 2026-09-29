using Manor.Core;
using Manor.Runtime;
using System.Collections.Generic;
using UnityEngine;

namespace Manor.Gameplay
{
    public sealed class DoorInteractable : InteractableBase
    {
        [SerializeField] private Transform _doorPivot;
        [SerializeField] private StableId _requiredKeyId;
        [SerializeField] private float _openAngle = 90f;
        [SerializeField, Min(0.05f)] private float _animationDuration = 0.45f;
        [SerializeField] private bool _startsOpen;

        private Quaternion _closedRotation;
        private bool _isOpen;
        private Quaternion _targetRotation;
        private Collider[] _doorColliders;
        private readonly HashSet<Collider> _closedFrameContacts = new HashSet<Collider>();

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (_doorPivot == null) _doorPivot = transform;
            EnsureDoorUsesItsOwnMeshCollision();
            _closedRotation = _doorPivot.localRotation;
            _isOpen = _startsOpen;
            _targetRotation = GetTargetRotation();
            _doorPivot.localRotation = _targetRotation;
            _doorColliders = _doorPivot.GetComponentsInChildren<Collider>(true);
            Physics.SyncTransforms();
            CacheClosedFrameContacts();
        }

        private void Start()
        {
            IGameStateService state = GameSession.Current?.GameState;
            if (state != null && StableId.IsValidValue(InteractionId)) _isOpen = state.IsDoorOpen(InteractionId);
            _targetRotation = GetTargetRotation();
            _doorPivot.localRotation = _targetRotation;
        }

        public void ConfigureDoor(string interactionId, Transform pivot, string requiredKeyId = null, float openAngle = 90f)
        {
            ConfigureId(interactionId);
            _doorPivot = pivot == null ? transform : pivot;
            _requiredKeyId = new StableId(requiredKeyId);
            _openAngle = openAngle;
            _closedRotation = _doorPivot.localRotation;
            _targetRotation = GetTargetRotation();
        }

        public override bool CanInteract(InteractionContext context) => context.GameState != null;

        public override string GetPromptKey(InteractionContext context)
        {
            if (!CanUnlock(context)) return "PROMPT_LOCKED_DOOR";
            return _isOpen ? "PROMPT_CLOSE_DOOR" : "PROMPT_OPEN_DOOR";
        }

        public override void Interact(InteractionContext context)
        {
            if (!CanUnlock(context))
            {
                GameSession.Current?.NotifyFeedback("PROMPT_LOCKED_DOOR");
                return;
            }

            _isOpen = !_isOpen;
            _targetRotation = GetTargetRotation();
            context.GameState.SetDoorOpen(InteractionId, _isOpen);
            GameSession.Current?.NotifyFeedback(_isOpen ? "DOOR_OPENED" : "DOOR_CLOSED");
            NoiseBus.Emit(_doorPivot != null ? _doorPivot.position : transform.position, _isOpen ? 10f : 7f, gameObject);
            GameSession.Current?.RequestAutoSave(context.GameState);
        }

        private void Update()
        {
            if (_doorPivot == null) return;
            float degreesPerSecond = Mathf.Abs(_openAngle) / Mathf.Max(0.05f, _animationDuration);
            Quaternion before = _doorPivot.localRotation;
            Quaternion next = Quaternion.RotateTowards(before, _targetRotation, degreesPerSecond * Time.deltaTime);
            _doorPivot.localRotation = next;
            Physics.SyncTransforms();
            if (HasBlockingOverlap()) _doorPivot.localRotation = before;
        }

        private bool CanUnlock(InteractionContext context)
        {
            return !_requiredKeyId.IsValid || context.GameState.HasKey(_requiredKeyId.Value);
        }

        private Quaternion GetTargetRotation()
        {
            return _closedRotation * Quaternion.Euler(0f, _isOpen ? _openAngle : 0f, 0f);
        }

        private bool HasBlockingOverlap()
        {
            if (_doorColliders == null) return false;
            foreach (Collider doorCollider in _doorColliders)
            {
                if (doorCollider == null || !doorCollider.enabled || doorCollider.isTrigger) continue;
                Bounds bounds = doorCollider.bounds;
                Collider[] overlaps = Physics.OverlapBox(bounds.center, bounds.extents * .96f, doorCollider.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
                foreach (Collider overlap in overlaps)
                {
                    if (overlap == doorCollider || overlap.transform.IsChildOf(_doorPivot)) continue;
                    if (_closedFrameContacts.Contains(overlap)) continue;
                    if (overlap.GetComponentInParent<Manor.Player.FirstPersonController>() != null) return true;
                    if (!overlap.isTrigger && overlap.gameObject.isStatic) return true;
                }
            }
            return false;
        }

        private void CacheClosedFrameContacts()
        {
            if (_doorColliders == null) return;
            foreach (Collider doorCollider in _doorColliders)
            {
                if (doorCollider == null || !doorCollider.enabled || doorCollider.isTrigger) continue;
                Bounds bounds = doorCollider.bounds;
                foreach (Collider overlap in Physics.OverlapBox(bounds.center, bounds.extents * .96f, doorCollider.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (overlap == doorCollider || overlap.transform.IsChildOf(_doorPivot) || overlap.isTrigger) continue;
                    if (overlap.gameObject.isStatic) _closedFrameContacts.Add(overlap);
                }
            }
        }

        private void EnsureDoorUsesItsOwnMeshCollision()
        {
            // A rotating leaf needs a collider attached to the leaf itself. MeshCollider rotates
            // exactly with the imported door; old box proxies are removed to prevent invisible
            // rectangular blockage beside detailed Rhino door geometry.
            foreach (MeshFilter filter in _doorPivot.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0) continue;
                // Disable the former proxy instead of Destroy() during Awake. Destroy is deferred
                // until the frame ends, while _doorColliders is cached immediately below; keeping
                // a disabled component avoids stale MissingReferenceException entries.
                foreach (BoxCollider box in filter.GetComponents<BoxCollider>()) box.enabled = false;
                MeshCollider meshCollider = filter.GetComponent<MeshCollider>();
                if (meshCollider == null) meshCollider = filter.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = filter.sharedMesh;
                meshCollider.convex = false;
                meshCollider.isTrigger = false;
            }
        }
    }
}
