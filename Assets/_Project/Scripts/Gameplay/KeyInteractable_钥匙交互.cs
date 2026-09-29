using Manor.Core;
using Manor.Runtime;
using UnityEngine;

namespace Manor.Gameplay
{
    public sealed class KeyInteractable : InteractableBase
    {
        [SerializeField] private StableId _keyId;
        [SerializeField] private bool _hideAfterPickup = true;
        public override InteractionAction RequiredAction => InteractionAction.Pickup;

        public void ConfigureKey(string interactionId, string keyId)
        {
            ConfigureId(interactionId);
            _keyId = new StableId(keyId);
        }

        public override bool CanInteract(InteractionContext context)
        {
            return context.GameState != null && _keyId.IsValid && !context.GameState.HasKey(_keyId.Value);
        }

        public override string GetPromptKey(InteractionContext context) => "PROMPT_PICKUP_KEY";

        public override void Interact(InteractionContext context)
        {
            if (!CanInteract(context)) return;
            if (!context.GameState.AddKey(_keyId.Value)) return;
            GameSession.Current?.NotifyFeedback("KEY_ADDED");
            GameSession.Current?.RequestAutoSave(context.GameState);
            if (_hideAfterPickup) gameObject.SetActive(false);
        }

        private void Start()
        {
            IGameStateService state = GameSession.Current?.GameState;
            if (_hideAfterPickup && state != null && _keyId.IsValid && state.HasKey(_keyId.Value)) gameObject.SetActive(false);
        }
    }
}
