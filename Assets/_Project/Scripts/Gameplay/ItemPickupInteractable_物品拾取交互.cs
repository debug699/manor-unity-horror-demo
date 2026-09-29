using Manor.Core;
using Manor.Runtime;
using UnityEngine;

namespace Manor.Gameplay
{
    /// <summary>Reusable stateful pickup for phone, torch, medicine, matches and other prop meshes.</summary>
    public sealed class ItemPickupInteractable : InteractableBase
    {
        [SerializeField] private StableId _itemId;
        [SerializeField] private string _promptKey = "PROMPT_PICKUP_ITEM";
        [SerializeField] private string _feedbackKey = "ITEM_ADDED";
        [SerializeField] private bool _hideAfterPickup = true;
        public override InteractionAction RequiredAction => InteractionAction.Pickup;

        public void ConfigureItem(string interactionId, string itemId, string promptKey = "PROMPT_PICKUP_ITEM", string feedbackKey = "ITEM_ADDED")
        {
            ConfigureId(interactionId);
            _itemId = new StableId(itemId);
            _promptKey = promptKey;
            _feedbackKey = feedbackKey;
        }

        public override bool CanInteract(InteractionContext context) => context.GameState != null && _itemId.IsValid && !context.GameState.HasItem(_itemId.Value);
        public override string GetPromptKey(InteractionContext context) => _promptKey;

        public override void Interact(InteractionContext context)
        {
            if (!CanInteract(context) || !context.GameState.AddItem(_itemId.Value)) return;
            GameSession.Current?.NotifyFeedback(_feedbackKey);
            GameSession.Current?.RequestAutoSave(context.GameState);
            if (_hideAfterPickup) gameObject.SetActive(false);
        }

        private void Start()
        {
            IGameStateService state = GameSession.Current?.GameState;
            if (_hideAfterPickup && state != null && _itemId.IsValid && state.HasItem(_itemId.Value)) gameObject.SetActive(false);
        }
    }
}
