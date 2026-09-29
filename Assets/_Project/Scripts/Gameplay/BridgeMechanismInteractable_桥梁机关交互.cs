using Manor.Core;
using Manor.Runtime;
using UnityEngine;

namespace Manor.Gameplay
{
    public sealed class BridgeMechanismInteractable : InteractableBase
    {
        [SerializeField] private Transform _bridgeDeck;
        [SerializeField] private Vector3 _cutOffset = new Vector3(0f, -6f, 0f);
        [SerializeField, Min(.1f)] private float _restoreDuration = 2.5f;
        private Vector3 _connectedPosition;
        private Vector3 _targetPosition;
        private IGameStateService _state;

        public void ConfigureBridge(string interactionId, Transform bridgeDeck)
        {
            ConfigureId(interactionId);
            _bridgeDeck = bridgeDeck;
            _connectedPosition = _bridgeDeck.localPosition;
        }

        private void Start()
        {
            if (_bridgeDeck == null) return;
            _connectedPosition = _bridgeDeck.localPosition;
            _state = GameSession.Current?.GameState;
            if (_state != null) _state.StateChanged += Reconcile;
            bool cut = _state != null && _state.ButcherChaseStarted && !_state.BridgeRestored;
            _targetPosition = cut ? _connectedPosition + _cutOffset : _connectedPosition;
            _bridgeDeck.localPosition = _targetPosition;
        }

        private void OnDestroy()
        {
            if (_state != null) _state.StateChanged -= Reconcile;
        }

        private void Reconcile()
        {
            if (_state == null || _bridgeDeck == null) return;
            _targetPosition = _state.ButcherChaseStarted && !_state.BridgeRestored
                ? _connectedPosition + _cutOffset
                : _connectedPosition;
        }

        public override bool CanInteract(InteractionContext context) => context.GameState != null;
        public override string GetPromptKey(InteractionContext context)
        {
            if (context.GameState == null) return string.Empty;
            if (context.GameState.BridgeRestored) return "PROMPT_BRIDGE_RESTORED";
            return context.GameState.DawnTriggered ? "PROMPT_RESTORE_BRIDGE" : "PROMPT_BRIDGE_NOT_DAWN";
        }

        public override void Interact(InteractionContext context)
        {
            if (!context.GameState.DawnTriggered)
            {
                GameSession.Current?.NotifyFeedback("BRIDGE_NOT_DAWN");
                return;
            }
            if (context.GameState.BridgeRestored) return;
            context.GameState.SetBridgeRestored(true);
            context.GameState.AdvanceStory(StoryStage.BridgeRestored);
            context.GameState.SetObjective(ProjectIds.ObjectiveEscapeGate);
            _targetPosition = _connectedPosition;
            GameSession.Current?.NotifyFeedback("BRIDGE_RESTORING");
            GameSession.Current?.RequestAutoSave(context.GameState);
        }

        private void Update()
        {
            if (_bridgeDeck == null) return;
            float speed = _cutOffset.magnitude / Mathf.Max(.1f, _restoreDuration);
            _bridgeDeck.localPosition = Vector3.MoveTowards(_bridgeDeck.localPosition, _targetPosition, speed * Time.deltaTime);
        }
    }
}
