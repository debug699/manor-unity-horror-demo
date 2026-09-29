using Manor.Gameplay;
using UnityEngine;

namespace Manor.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private FirstPersonCamera _view;
        [SerializeField] private InteractionScanner _interactionScanner;
        [SerializeField, Min(0f)] private float _walkSpeed = 3.2f;
        [SerializeField, Min(0f)] private float _runSpeed = 5.5f;
        [SerializeField, Min(0f)] private float _crouchSpeed = 1.8f;
        [SerializeField] private float _standingHeight = 1.8f;
        [SerializeField] private float _crouchingHeight = 1.1f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private LayerMask _ceilingMask = ~0;

        private CharacterController _controller;
        private float _verticalVelocity;
        private bool _isCrouching;
        private float _nextRunNoiseAt;

        public bool IsCrouching => _isCrouching;
        public float CurrentSpeed { get; private set; }
        public bool InputLocked { get; private set; }

        public void SetInputLocked(bool locked)
        {
            InputLocked = locked;
            if (locked) CurrentSpeed = 0f;
        }

        public void Configure(PlayerInputReader input, FirstPersonCamera view, InteractionScanner scanner)
        {
            if (enabled && _input != null) UnbindInteractionInput();
            _input = input;
            _view = view;
            _interactionScanner = scanner;
            if (enabled && _input != null) BindInteractionInput();
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_input == null) _input = GetComponent<PlayerInputReader>();
        }

        private void OnEnable()
        {
            if (_input != null) BindInteractionInput();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (_input != null) UnbindInteractionInput();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || InputLocked) return;
            UpdateCrouch(_input.CrouchHeld);
            _view?.ApplyLook(_input.Look);
            Move(_input.Move, _input.RunHeld);
        }

        public void Move(Vector2 input, bool runHeld)
        {
            Vector3 planar = transform.right * input.x + transform.forward * input.y;
            planar = Vector3.ClampMagnitude(planar, 1f);
            CurrentSpeed = _isCrouching ? _crouchSpeed : runHeld ? _runSpeed : _walkSpeed;

            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += _gravity * Time.deltaTime;
            Vector3 velocity = planar * CurrentSpeed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
            if (runHeld && !_isCrouching && planar.sqrMagnitude > .1f && Time.time >= _nextRunNoiseAt)
            {
                _nextRunNoiseAt = Time.time + .45f;
                NoiseBus.Emit(transform.position, 13f, gameObject);
            }
        }


        public void UpdateCrouch(bool crouchHeld)
        {
            if (crouchHeld)
            {
                SetCrouching(true);
                return;
            }

            if (_isCrouching && !HasStandingClearance()) return;
            SetCrouching(false);
        }

        private void SetCrouching(bool crouching)
        {
            _isCrouching = crouching;
            float targetHeight = crouching ? _crouchingHeight : _standingHeight;
            _controller.height = targetHeight;
            _controller.center = new Vector3(0f, targetHeight * 0.5f, 0f);
            if (_view != null)
            {
                Vector3 cameraPosition = _view.transform.localPosition;
                cameraPosition.y = targetHeight - 0.15f;
                _view.transform.localPosition = cameraPosition;
            }
        }

        private bool HasStandingClearance()
        {
            float radius = Mathf.Max(0.05f, _controller.radius - 0.02f);
            Vector3 bottom = transform.position + Vector3.up * Mathf.Max(radius, _crouchingHeight - radius);
            Vector3 top = transform.position + Vector3.up * (_standingHeight - radius);
            Collider[] overlaps = Physics.OverlapCapsule(bottom, top, radius, _ceilingMask, QueryTriggerInteraction.Ignore);
            foreach (Collider overlap in overlaps)
            {
                if (overlap == _controller || overlap.transform.IsChildOf(transform)) continue;
                return false;
            }

            return true;
        }

        private void OnInteract() => _interactionScanner?.TryInteract();
        private void OnPickup() => _interactionScanner?.TryInteract(InteractionAction.Pickup);
        private void OnUseItemCompleted() => _interactionScanner?.TryInteract(InteractionAction.HoldUse);

        private void BindInteractionInput()
        {
            _input.InteractPressed += OnInteract;
            _input.PickupPressed += OnPickup;
            _input.UseItemCompleted += OnUseItemCompleted;
        }

        private void UnbindInteractionInput()
        {
            _input.InteractPressed -= OnInteract;
            _input.PickupPressed -= OnPickup;
            _input.UseItemCompleted -= OnUseItemCompleted;
        }
    }
}
