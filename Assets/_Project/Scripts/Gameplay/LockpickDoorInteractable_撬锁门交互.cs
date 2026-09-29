using Manor.Core;
using Manor.Runtime;
using UnityEngine;

namespace Manor.Gameplay
{
    /// <summary>A door that is first unlocked by holding K with a required improvised tool, then uses E normally.</summary>
    public sealed class LockpickDoorInteractable : InteractableBase
    {
        [SerializeField] private Transform _doorPivot;
        [SerializeField] private StableId _requiredItemId;
        [SerializeField] private StableId _unlockMarkerId;
        [SerializeField] private float _openAngle = 92f;
        [SerializeField, Min(.05f)] private float _animationDuration = .45f;

        private Quaternion _closedRotation;
        private Quaternion _targetRotation;
        private bool _isOpen;
        private Collider[] _doorColliders;

        public override InteractionAction RequiredAction => IsUnlocked(GameSession.Current?.GameState)
            ? InteractionAction.Interact
            : InteractionAction.HoldUse;

        public bool IsOpen => _isOpen;

        public void ConfigureLockpickDoor(string interactionId, Transform pivot, string requiredItemId, string unlockMarkerId, float openAngle = 92f)
        {
            ConfigureId(interactionId);
            _doorPivot = pivot == null ? transform : pivot;
            _requiredItemId = new StableId(requiredItemId);
            _unlockMarkerId = new StableId(unlockMarkerId);
            _openAngle = openAngle;
            _closedRotation = _doorPivot.localRotation;
            _doorColliders = _doorPivot.GetComponentsInChildren<Collider>(true);
            _targetRotation = GetTargetRotation();
        }

        private void Awake()
        {
            if (_doorPivot == null) _doorPivot = transform;
            _closedRotation = _doorPivot.localRotation;
        }

        private void Start()
        {
            IGameStateService state = GameSession.Current?.GameState;
            _isOpen = state != null && state.IsDoorOpen(InteractionId);
            _targetRotation = GetTargetRotation();
            _doorPivot.localRotation = _targetRotation;
        }

        public override bool CanInteract(InteractionContext context) => context.GameState != null;

        public override string GetPromptKey(InteractionContext context)
        {
            if (IsUnlocked(context.GameState)) return _isOpen ? "PROMPT_CLOSE_DOOR" : "PROMPT_OPEN_DOOR";
            return context.GameState != null && context.GameState.HasItem(_requiredItemId.Value)
                ? "PROMPT_HOLD_K_LOCKPICK"
                : "PROMPT_NEED_LOCKPICK_WIRE";
        }

        public override void Interact(InteractionContext context)
        {
            if (!CanInteract(context)) return;
            if (!IsUnlocked(context.GameState))
            {
                if (!context.GameState.HasItem(_requiredItemId.Value))
                {
                    GameSession.Current?.NotifyFeedback("NEED_LOCKPICK_WIRE");
                    return;
                }
                context.GameState.AddKey(_unlockMarkerId.Value);
                GameSession.Current?.NotifyFeedback("LOCKPICK_SUCCEEDED");
                GameSession.Current?.RequestAutoSave(context.GameState);
                return;
            }

            _isOpen = !_isOpen;
            _targetRotation = GetTargetRotation();
            context.GameState.SetDoorOpen(InteractionId, _isOpen);
            GameSession.Current?.NotifyFeedback(_isOpen ? "DOOR_OPENED" : "DOOR_CLOSED");
            GameSession.Current?.RequestAutoSave(context.GameState);
        }

        private void Update()
        {
            if (_doorPivot == null) return;
            float speed = Mathf.Abs(_openAngle) / Mathf.Max(.05f, _animationDuration);
            Quaternion before = _doorPivot.localRotation;
            _doorPivot.localRotation = Quaternion.RotateTowards(before, _targetRotation, speed * Time.deltaTime);
            Physics.SyncTransforms();
            if (HasBlockingOverlap()) _doorPivot.localRotation = before;
        }

        private bool IsUnlocked(IGameStateService state) => state != null && _unlockMarkerId.IsValid && state.HasKey(_unlockMarkerId.Value);
        private Quaternion GetTargetRotation() => _closedRotation * Quaternion.Euler(0f, _isOpen ? _openAngle : 0f, 0f);

        private bool HasBlockingOverlap()
        {
            if (_doorColliders == null) return false;
            foreach (Collider doorCollider in _doorColliders)
            {
                if (!(doorCollider is BoxCollider box) || !box.enabled || box.isTrigger) continue;
                Vector3 center = box.transform.TransformPoint(box.center);
                Vector3 half = Vector3.Scale(box.size * .5f, box.transform.lossyScale);
                foreach (Collider overlap in Physics.OverlapBox(center, half * .96f, box.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (overlap == doorCollider || overlap.transform.IsChildOf(_doorPivot)) continue;
                    if (overlap.GetComponentInParent<Manor.Player.FirstPersonController>() != null) return true;
                    if (!overlap.isTrigger && overlap.gameObject.isStatic) return true;
                }
            }
            return false;
        }
    }
}
