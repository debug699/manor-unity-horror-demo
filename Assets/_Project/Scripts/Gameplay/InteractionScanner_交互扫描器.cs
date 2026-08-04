using System;
using Manor.Core;
using Manor.Runtime;
using UnityEngine;

namespace Manor.Gameplay
{
    public sealed class InteractionScanner : MonoBehaviour
    {
        [SerializeField] private Camera _viewCamera;
        [SerializeField, Min(0.1f)] private float _interactionDistance = 2.5f;
        [SerializeField] private LayerMask _interactionMask = ~0;
        [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField, Range(0f, 0.2f)] private float _aimAssistRadius = 0.08f;

        private IInteractable _current;
        private string _currentPromptKey = string.Empty;
        private IGameStateService _gameState;

        public event Action<string> PromptChanged;
        public IInteractable Current => _current;
        public float InteractionDistance => _interactionDistance;

        public void Configure(Camera viewCamera, IGameStateService gameState, float distance = 2.5f)
        {
            _viewCamera = viewCamera;
            _gameState = gameState;
            _interactionDistance = Mathf.Max(0.1f, distance);
        }

        public bool Scan()
        {
            if (_viewCamera == null) return SetCurrent(null);
            Ray ray = new Ray(_viewCamera.transform.position, _viewCamera.transform.forward);
            InteractionContext context = CreateContext();
            RaycastHit[] hits = Physics.SphereCastAll(ray, _aimAssistRadius, _interactionDistance, _interactionMask, _triggerInteraction);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                IInteractable interactable = FindInteractable(hit.collider);
                if (interactable != null) return interactable.CanInteract(context) ? SetCurrent(interactable) : SetCurrent(null);
                if (!hit.collider.isTrigger) break;
            }

            return SetCurrent(null);
        }

        public bool TryInteract()
        {
            Scan();
            if (_current == null) return false;
            InteractionContext context = CreateContext();
            if (!_current.CanInteract(context)) return false;
            _current.Interact(context);
            Scan();
            return true;
        }

        private void Update() => Scan();

        private InteractionContext CreateContext()
        {
            return new InteractionContext(gameObject, _viewCamera, _gameState ?? GameSession.Current?.GameState);
        }

        private bool SetCurrent(IInteractable interactable)
        {
            string nextPromptKey = interactable?.GetPromptKey(CreateContext()) ?? string.Empty;
            if (ReferenceEquals(_current, interactable) && string.Equals(_currentPromptKey, nextPromptKey, StringComparison.Ordinal))
                return _current != null;
            _current = interactable;
            _currentPromptKey = nextPromptKey;
            PromptChanged?.Invoke(_currentPromptKey);
            return _current != null;
        }

        private static IInteractable FindInteractable(Collider targetCollider)
        {
            MonoBehaviour[] components = targetCollider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour component in components)
            {
                if (component is IInteractable interactable) return interactable;
            }

            return null;
        }
    }
}
