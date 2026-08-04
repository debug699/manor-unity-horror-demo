using System;
using Manor.Core;
using UnityEngine;

namespace Manor.Gameplay
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] private StableId _interactionId;

        public string InteractionId => _interactionId.Value;

        public abstract bool CanInteract(InteractionContext context);
        public abstract string GetPromptKey(InteractionContext context);
        public abstract void Interact(InteractionContext context);

        public void ConfigureId(string interactionId)
        {
            if (!StableId.IsValidValue(interactionId)) throw new ArgumentException("Invalid stable interaction ID.", nameof(interactionId));
            _interactionId = new StableId(interactionId);
        }

        protected virtual void OnValidate()
        {
            if (!string.IsNullOrEmpty(_interactionId.Value) && !_interactionId.IsValid)
                Debug.LogError("[Interaction] 稳定 ID 格式无效 / Invalid stable ID: " + _interactionId.Value, this);
        }
    }
}
