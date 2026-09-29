using Manor.Core;
using Manor.Runtime;
using UnityEngine;

namespace Manor.Gameplay
{
    /// <summary>The only active basement entrance. It opens physically after the three route clues are known.</summary>
    public sealed class BasementHatchInteractable : InteractableBase
    {
        [SerializeField] private Transform _pivot;
        [SerializeField] private StableId[] _requiredClueIds;
        [SerializeField] private float _openAngle = -95f;
        [SerializeField, Min(.05f)] private float _animationDuration = .8f;

        private Quaternion _closedRotation;
        private Quaternion _targetRotation;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        public void ConfigureHatch(string interactionId, Transform pivot, string[] requiredClueIds, float openAngle = -95f)
        {
            ConfigureId(interactionId);
            _pivot = pivot == null ? transform : pivot;
            _requiredClueIds = new StableId[requiredClueIds?.Length ?? 0];
            for (int i = 0; i < _requiredClueIds.Length; i++) _requiredClueIds[i] = new StableId(requiredClueIds[i]);
            _openAngle = openAngle;
            _closedRotation = _pivot.localRotation;
            _targetRotation = GetTargetRotation();
        }

        private void Awake()
        {
            if (_pivot == null) _pivot = transform;
            _closedRotation = _pivot.localRotation;
        }

        private void Start()
        {
            IGameStateService state = GameSession.Current?.GameState;
            _isOpen = state != null && state.IsDoorOpen(InteractionId);
            _targetRotation = GetTargetRotation();
            _pivot.localRotation = _targetRotation;
        }

        public override bool CanInteract(InteractionContext context) => context.GameState != null;
        public override string GetPromptKey(InteractionContext context) => HasAllClues(context.GameState)
            ? (_isOpen ? "PROMPT_CLOSE_HATCH" : "PROMPT_OPEN_HATCH")
            : "PROMPT_HATCH_NEEDS_CLUES";

        public override void Interact(InteractionContext context)
        {
            if (!CanInteract(context)) return;
            if (!HasAllClues(context.GameState))
            {
                GameSession.Current?.NotifyFeedback("HATCH_NEEDS_CLUES");
                return;
            }

            _isOpen = !_isOpen;
            _targetRotation = GetTargetRotation();
            context.GameState.SetDoorOpen(InteractionId, _isOpen);
            if (_isOpen)
            {
                context.GameState.AdvanceStory(StoryStage.EnterBasement);
                context.GameState.SetObjective(ProjectIds.ObjectiveDescendToBasement);
            }
            GameSession.Current?.NotifyFeedback(_isOpen ? "HATCH_OPENED" : "HATCH_CLOSED");
            GameSession.Current?.RequestAutoSave(context.GameState);
        }

        private void Update()
        {
            if (_pivot == null) return;
            float speed = Mathf.Abs(_openAngle) / Mathf.Max(.05f, _animationDuration);
            _pivot.localRotation = Quaternion.RotateTowards(_pivot.localRotation, _targetRotation, speed * Time.deltaTime);
        }

        private bool HasAllClues(IGameStateService state)
        {
            if (state == null || _requiredClueIds == null || _requiredClueIds.Length == 0) return false;
            foreach (StableId clueId in _requiredClueIds)
                if (!clueId.IsValid || !state.HasReadClue(clueId.Value)) return false;
            return true;
        }

        private Quaternion GetTargetRotation() => _closedRotation * Quaternion.Euler(0f, 0f, _isOpen ? _openAngle : 0f);
    }
}
