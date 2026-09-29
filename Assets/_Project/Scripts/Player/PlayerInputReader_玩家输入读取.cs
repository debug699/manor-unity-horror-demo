using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manor.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;

        private InputActionMap _gameplayMap;
        private InputAction _move;
        private InputAction _look;
        private InputAction _run;
        private InputAction _crouch;
        private InputAction _interact;
        private InputAction _pickup;
        private InputAction _useItem;
        private InputAction _clues;
        private InputAction _pause;

        public Vector2 Move => _move?.ReadValue<Vector2>() ?? Vector2.zero;
        public Vector2 Look => _look?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool RunHeld => _run?.IsPressed() ?? false;
        public bool CrouchHeld => _crouch?.IsPressed() ?? false;
        public event Action InteractPressed;
        public event Action PickupPressed;
        public event Action UseItemStarted;
        public event Action UseItemCompleted;
        public event Action UseItemCanceled;
        public event Action CluesPressed;
        public event Action PausePressed;

        public void Configure(InputActionAsset inputActions)
        {
            if (enabled) Unbind();
            _inputActions = inputActions;
            if (enabled) Bind();
        }

        private void OnEnable() => Bind();
        private void OnDisable() => Unbind();

        private void Bind()
        {
            if (_inputActions == null || _gameplayMap != null) return;
            _gameplayMap = _inputActions.FindActionMap("Gameplay", true);
            _move = _gameplayMap.FindAction("Move", true);
            _look = _gameplayMap.FindAction("Look", true);
            _run = _gameplayMap.FindAction("Run", true);
            _crouch = _gameplayMap.FindAction("Crouch", true);
            _interact = _gameplayMap.FindAction("Interact", true);
            _pickup = _gameplayMap.FindAction("Pickup", true);
            _useItem = _gameplayMap.FindAction("UseItem", true);
            _clues = _gameplayMap.FindAction("Clues", true);
            _pause = _gameplayMap.FindAction("Pause", true);
            _interact.performed += OnInteract;
            _pickup.performed += OnPickup;
            _useItem.started += OnUseItemStarted;
            _useItem.performed += OnUseItemCompleted;
            _useItem.canceled += OnUseItemCanceled;
            _clues.performed += OnClues;
            _pause.performed += OnPause;
            _gameplayMap.Enable();
        }

        private void Unbind()
        {
            if (_gameplayMap == null) return;
            _interact.performed -= OnInteract;
            _pickup.performed -= OnPickup;
            _useItem.started -= OnUseItemStarted;
            _useItem.performed -= OnUseItemCompleted;
            _useItem.canceled -= OnUseItemCanceled;
            _clues.performed -= OnClues;
            _pause.performed -= OnPause;
            _gameplayMap.Disable();
            _gameplayMap = null;
        }

        private void OnInteract(InputAction.CallbackContext context) => InteractPressed?.Invoke();
        private void OnPickup(InputAction.CallbackContext context) => PickupPressed?.Invoke();
        private void OnUseItemStarted(InputAction.CallbackContext context) => UseItemStarted?.Invoke();
        private void OnUseItemCompleted(InputAction.CallbackContext context) => UseItemCompleted?.Invoke();
        private void OnUseItemCanceled(InputAction.CallbackContext context) => UseItemCanceled?.Invoke();
        private void OnClues(InputAction.CallbackContext context) => CluesPressed?.Invoke();
        private void OnPause(InputAction.CallbackContext context) => PausePressed?.Invoke();
    }
}
