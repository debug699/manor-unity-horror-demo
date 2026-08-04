using Manor.Core;
using Manor.Runtime;
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

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (_doorPivot == null) _doorPivot = transform;
            _closedRotation = _doorPivot.localRotation;
            _isOpen = _startsOpen;
            _targetRotation = GetTargetRotation();
            _doorPivot.localRotation = _targetRotation;
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
            GameSession.Current?.RequestAutoSave(context.GameState);
        }

        private void Update()
        {
            if (_doorPivot == null) return;
            float degreesPerSecond = Mathf.Abs(_openAngle) / Mathf.Max(0.05f, _animationDuration);
            _doorPivot.localRotation = Quaternion.RotateTowards(_doorPivot.localRotation, _targetRotation, degreesPerSecond * Time.deltaTime);
        }

        private bool CanUnlock(InteractionContext context)
        {
            return !_requiredKeyId.IsValid || context.GameState.HasKey(_requiredKeyId.Value);
        }

        private Quaternion GetTargetRotation()
        {
            return _closedRotation * Quaternion.Euler(0f, _isOpen ? _openAngle : 0f, 0f);
        }
    }
}
