using Manor.Player;
using Manor.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manor.Gameplay
{
    public sealed class HideSpotInteractable : InteractableBase
    {
        [SerializeField] private Transform _insidePoint;
        [SerializeField] private Transform _exitPoint;
        private FirstPersonController _occupant;

        public bool Occupied => _occupant != null;

        public void Configure(string interactionId, Transform insidePoint, Transform exitPoint)
        {
            ConfigureId(interactionId); _insidePoint = insidePoint; _exitPoint = exitPoint;
        }

        public override bool CanInteract(InteractionContext context) => context.Actor != null && _insidePoint != null && _exitPoint != null && (_occupant == null || _occupant.gameObject == context.Actor);
        public override string GetPromptKey(InteractionContext context) => _occupant == null ? "PROMPT_HIDE" : "PROMPT_EXIT_HIDE";
        public override void Interact(InteractionContext context)
        {
            if (!CanInteract(context)) return;
            FirstPersonController player = context.Actor.GetComponentInParent<FirstPersonController>();
            if (player == null) return;
            CharacterController controller = player.GetComponent<CharacterController>();
            bool entering = _occupant == null;
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(entering ? _insidePoint.position : _exitPoint.position, entering ? _insidePoint.rotation : _exitPoint.rotation);
            if (controller != null) controller.enabled = !entering;
            player.SetInputLocked(entering);
            PlayerStealthState stealth = player.GetComponent<PlayerStealthState>();
            if (stealth == null) stealth = player.gameObject.AddComponent<PlayerStealthState>();
            stealth.SetHidden(entering);
            _occupant = entering ? player : null;
            GameSession.Current?.NotifyFeedback(entering ? "HIDE_ENTERED" : "HIDE_EXITED");
        }

        private void Update()
        {
            if (_occupant == null || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
            CharacterController controller = _occupant.GetComponent<CharacterController>();
            _occupant.transform.SetPositionAndRotation(_exitPoint.position, _exitPoint.rotation);
            if (controller != null) controller.enabled = true;
            _occupant.SetInputLocked(false);
            _occupant.GetComponent<PlayerStealthState>()?.SetHidden(false);
            _occupant = null;
            GameSession.Current?.NotifyFeedback("HIDE_EXITED");
        }
    }
}
