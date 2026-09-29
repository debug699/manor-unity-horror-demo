using System.Collections;
using Manor.Player;
using Manor.Runtime;
using UnityEngine;

namespace Manor.Gameplay
{
    /// <summary>Moves the player only between authored window-side landing points; no free teleporting.</summary>
    public sealed class VaultWindowInteractable : InteractableBase
    {
        [SerializeField] private Transform _outsideLandingPoint;
        [SerializeField] private Transform _insideLandingPoint;
        private Transform _windowPivot;
        [SerializeField] private float _openAngle = 75f;
        [SerializeField, Min(.25f)] private float _vaultDuration = .65f;
        private bool _isVaulting;

        public void ConfigureWindow(string interactionId, Transform outsideLandingPoint, Transform insideLandingPoint, Transform windowPivot = null)
        {
            ConfigureId(interactionId);
            _outsideLandingPoint = outsideLandingPoint;
            _insideLandingPoint = insideLandingPoint;
            _windowPivot = windowPivot;
        }

        // Compatibility for the retired indoor preview builder. Production scenes always provide
        // two authored sides; the preview only needs to remain compilable for asset inspection.
        public void ConfigureWindow(string interactionId, Transform landingPoint, Transform windowPivot = null)
        {
            ConfigureWindow(interactionId, landingPoint, landingPoint, windowPivot);
        }

        public override bool CanInteract(InteractionContext context) => !_isVaulting && context.Actor != null && _outsideLandingPoint != null && _insideLandingPoint != null;
        public override string GetPromptKey(InteractionContext context) => "PROMPT_VAULT_WINDOW";
        public override void Interact(InteractionContext context)
        {
            if (!CanInteract(context)) return;
            if (_windowPivot == null && transform.parent != null) _windowPivot = transform.parent.Find("G07_WindowPivot_窗扇铰链");
            StartCoroutine(VaultRoutine(context.Actor));
        }

        private IEnumerator VaultRoutine(GameObject actor)
        {
            _isVaulting = true;
            FirstPersonController player = actor.GetComponentInParent<FirstPersonController>();
            player?.SetInputLocked(true);
            Transform target = transform.InverseTransformPoint(actor.transform.position).z < 0f ? _outsideLandingPoint : _insideLandingPoint;
            CharacterController controller = actor.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            Vector3 startPosition = actor.transform.position;
            Quaternion startRotation = actor.transform.rotation;
            Quaternion closedRotation = _windowPivot != null ? _windowPivot.localRotation : Quaternion.identity;
            Quaternion openedRotation = Quaternion.Euler(0f, _openAngle, 0f);
            float elapsed = 0f;
            while (elapsed < _vaultDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _vaultDuration));
                actor.transform.SetPositionAndRotation(Vector3.Lerp(startPosition, target.position, t), Quaternion.Slerp(startRotation, target.rotation, t));
                if (_windowPivot != null) _windowPivot.localRotation = Quaternion.Slerp(closedRotation, openedRotation, Mathf.Clamp01(t * 2f));
                yield return null;
            }
            actor.transform.SetPositionAndRotation(target.position, target.rotation);
            if (controller != null) controller.enabled = true;
            player?.SetInputLocked(false);
            _isVaulting = false;
            GameSession.Current?.NotifyFeedback("WINDOW_VAULTED");
        }
    }
}
