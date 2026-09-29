using Manor.Core;
using Manor.Player;
using Manor.Runtime;
using Manor.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

namespace Manor.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PregnantWraithController : MonoBehaviour
    {
        public enum WraithState { Hidden, Patrol, Investigate, Chase, Search, Grapple, Dissipated }

        [SerializeField] private Transform[] _patrolPoints;
        [SerializeField] private float _patrolSpeed = 1.3f;
        [SerializeField] private float _chaseSpeed = 3.3f;
        [SerializeField] private float _viewDistance = 11f;
        [SerializeField, Range(10f, 180f)] private float _viewAngle = 95f;
        [SerializeField] private float _searchSeconds = 7f;
        [SerializeField] private float _grappleFailSeconds = 15f;
        [SerializeField] private float _escapeHoldSeconds = 5f;

        private CharacterController _controller;
        private NavMeshAgent _agent;
        private FirstPersonController _player;
        private WraithState _state;
        private int _patrolIndex;
        private float _searchRemaining;
        private float _grappleElapsed;
        private float _escapeHeld;
        private Vector3 _lastKnownPosition;
        private int _searchStep;
        private Renderer[] _visuals;

        public WraithState State => _state;

        public void Configure(Transform[] patrolPoints, Renderer[] visuals = null) { _patrolPoints = patrolPoints; _visuals = visuals; SetVisuals(false); }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable() => NoiseBus.Emitted += OnNoise;
        private void OnDisable() => NoiseBus.Emitted -= OnNoise;

        private void Update()
        {
            IGameStateService story = GameSession.Current?.GameState;
            if (story == null) return;
            if (story.CommunicationComplete)
            {
                _state = WraithState.Dissipated;
                gameObject.SetActive(false);
                return;
            }
            if (!story.HasReadClue("CLUE_G08_DIARY")) { _state = WraithState.Hidden; SetVisuals(false); return; }
            SetVisuals(true);
            _player ??= Object.FindFirstObjectByType<FirstPersonController>();
            if (_player == null) return;
            if (_state == WraithState.Hidden) _state = WraithState.Patrol;

            if (_state == WraithState.Grapple) { UpdateGrapple(); return; }
            if (CanSeePlayer()) { _lastKnownPosition = _player.transform.position; _state = WraithState.Chase; }
            if (_state == WraithState.Chase)
            {
                MoveTowards(_player.transform.position, _chaseSpeed);
                if (!CanSeePlayer()) { _state = WraithState.Search; _searchRemaining = _searchSeconds; _searchStep = 0; }
                return;
            }
            if (_state == WraithState.Investigate)
            {
                MoveTowards(_lastKnownPosition, _patrolSpeed * 1.15f);
                _searchRemaining -= Time.deltaTime;
                if (Vector3.Distance(transform.position, _lastKnownPosition) < .7f || _searchRemaining <= 0f) { _state = WraithState.Search; _searchRemaining = _searchSeconds; _searchStep = 0; }
                return;
            }
            if (_state == WraithState.Search)
            {
                Vector3 offset = _searchStep == 0 ? transform.right * 1.7f : _searchStep == 1 ? -transform.right * 1.7f : -transform.forward * 1.4f;
                MoveTowards(_lastKnownPosition + offset, _patrolSpeed);
                if (Vector3.Distance(transform.position, _lastKnownPosition + offset) < .55f) _searchStep = Mathf.Min(2, _searchStep + 1);
                _searchRemaining -= Time.deltaTime;
                if (_searchRemaining <= 0f) _state = WraithState.Patrol;
                return;
            }
            Patrol();
        }

        private void OnNoise(NoiseBus.Noise noise)
        {
            if (_state == WraithState.Hidden || _state == WraithState.Grapple || _state == WraithState.Dissipated || _state == WraithState.Chase) return;
            if (Vector3.Distance(transform.position, noise.Position) > noise.Radius) return;
            _lastKnownPosition = noise.Position; _state = WraithState.Investigate; _searchRemaining = 8f;
        }

        private void SetVisuals(bool visible)
        {
            if (_visuals == null) return;
            foreach (Renderer renderer in _visuals) if (renderer != null) renderer.enabled = visible;
        }

        private void Patrol()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0) return;
            Transform target = _patrolPoints[_patrolIndex % _patrolPoints.Length];
            MoveTowards(target.position, _patrolSpeed);
            if (Vector3.Distance(transform.position, target.position) < .45f) _patrolIndex = (_patrolIndex + 1) % _patrolPoints.Length;
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            Vector3 direction = target - transform.position; direction.y = 0f;
            if (direction.sqrMagnitude < .01f) return;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.speed = speed;
                _agent.SetDestination(target);
                return;
            }
            direction.Normalize();
            transform.forward = Vector3.Slerp(transform.forward, direction, 8f * Time.deltaTime);
            _controller.Move((direction * speed + Vector3.down * 2f) * Time.deltaTime);
        }

        private bool CanSeePlayer()
        {
            PlayerStealthState stealth = _player.GetComponent<PlayerStealthState>();
            if (stealth != null && stealth.IsHidden) return false;
            Vector3 origin = transform.position + Vector3.up * 1.4f;
            Vector3 target = _player.transform.position + Vector3.up * 1.2f;
            Vector3 delta = target - origin;
            if (delta.magnitude > _viewDistance || Vector3.Angle(transform.forward, delta) > _viewAngle * .5f) return false;
            if (!Physics.Raycast(origin, delta.normalized, out RaycastHit hit, delta.magnitude, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform.IsChildOf(_player.transform) || _player.transform.IsChildOf(hit.transform);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_state != WraithState.Chase || hit.collider.GetComponentInParent<FirstPersonController>() == null) return;
            _state = WraithState.Grapple; _grappleElapsed = 0f; _escapeHeld = 0f;
            _player.SetInputLocked(true);
            GameSession.Current?.NotifyFeedback("WRAITH_GRAPPLE");
        }

        private void UpdateGrapple()
        {
            _grappleElapsed += Time.deltaTime;
            bool held = Keyboard.current != null && Keyboard.current.eKey.isPressed;
            _escapeHeld = held ? _escapeHeld + Time.deltaTime : 0f;
            if (_escapeHeld >= _escapeHoldSeconds)
            {
                _player.SetInputLocked(false); _state = WraithState.Search; _searchRemaining = _searchSeconds;
                transform.position -= transform.forward * 1.2f;
                GameSession.Current?.NotifyFeedback("WRAITH_ESCAPED");
                return;
            }
            if (_grappleElapsed < _grappleFailSeconds) return;
            Object.FindFirstObjectByType<DeathRetryController>()?.ShowDeath("被怨灵控制太久");
        }
    }
}
