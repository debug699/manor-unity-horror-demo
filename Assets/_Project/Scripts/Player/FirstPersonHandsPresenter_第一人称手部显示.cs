using Manor.Gameplay;
using UnityEngine;

namespace Manor.Player
{
    /// <summary>Displays the staged Tom arm meshes below the first-person camera and adds simple interaction motion.</summary>
    public sealed class FirstPersonHandsPresenter : MonoBehaviour
    {
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightHand;
        [SerializeField] private InteractionScanner _scanner;
        [SerializeField, Min(0.01f)] private float _interactionDuration = .22f;
        [SerializeField] private float _reachDistance = .12f;

        private Vector3 _leftBase;
        private Vector3 _rightBase;
        private float _interactionTimer;

        public void Configure(Transform leftHand, Transform rightHand, InteractionScanner scanner)
        {
            _leftHand = leftHand; _rightHand = rightHand; _scanner = scanner;
            CacheBases();
        }

        private void OnEnable() { if (_scanner != null) _scanner.Interacted += OnInteracted; }
        private void OnDisable() { if (_scanner != null) _scanner.Interacted -= OnInteracted; }
        private void Start() { CacheBases(); if (_scanner != null) _scanner.Interacted += OnInteracted; }
        private void OnDestroy() { if (_scanner != null) _scanner.Interacted -= OnInteracted; }

        private void Update()
        {
            if (_leftHand == null || _rightHand == null) return;
            _interactionTimer = Mathf.Max(0f, _interactionTimer - Time.deltaTime);
            float reach = Mathf.Sin((1f - _interactionTimer / _interactionDuration) * Mathf.PI) * _reachDistance;
            _leftHand.localPosition = _leftBase + Vector3.forward * reach * .45f;
            _rightHand.localPosition = _rightBase + Vector3.forward * reach;
        }

        private void OnInteracted(IInteractable interactable) => _interactionTimer = _interactionDuration;
        private void CacheBases() { if (_leftHand != null) _leftBase = _leftHand.localPosition; if (_rightHand != null) _rightBase = _rightHand.localPosition; }
    }
}
