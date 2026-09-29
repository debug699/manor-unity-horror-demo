using Manor.Player;
using Manor.Runtime;
using Manor.UI;
using UnityEngine;
using UnityEngine.AI;

namespace Manor.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ButcherController : MonoBehaviour
    {
        public enum ButcherState { Waiting, Warning, Patrol, Investigate, Chase, Search, Petrified }

        [SerializeField] private Transform[] _patrolPoints;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private float _warningSeconds = 4f;
        [SerializeField] private float _patrolSpeed = 2.1f;
        [SerializeField] private float _chaseSpeed = 4.8f;
        [SerializeField] private float _viewDistance = 18f;
        [SerializeField, Range(10f, 180f)] private float _viewAngle = 105f;
        [SerializeField] private float _searchSeconds = 8f;

        private CharacterController _controller;
        private NavMeshAgent _agent;
        private FirstPersonController _player;
        private ButcherState _state;
        private int _patrolIndex;
        private float _stateTimer;
        private Vector3 _lastKnownPosition;
        private int _searchStep;

        public ButcherState State => _state;
        public void Configure(Transform[] patrolPoints, Renderer[] renderers) { _patrolPoints = patrolPoints; _renderers = renderers; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable() => NoiseBus.Emitted += OnNoise;
        private void OnDisable() => NoiseBus.Emitted -= OnNoise;

        private void Update()
        {
            var story = GameSession.Current?.GameState;
            if (story == null) return;
            if (story.DawnTriggered) { Petrify(); return; }
            if (!story.ButcherChaseStarted) { _state = ButcherState.Waiting; return; }
            _player ??= Object.FindFirstObjectByType<FirstPersonController>();
            if (_player == null) return;
            if (_state == ButcherState.Waiting)
            {
                _state = ButcherState.Warning; _stateTimer = _warningSeconds;
                GameSession.Current?.NotifyFeedback("BUTCHER_WARNING");
                return;
            }
            if (_state == ButcherState.Warning)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f) _state = ButcherState.Patrol;
                return;
            }
            bool seesPlayer = CanSeePlayer();
            if (seesPlayer) { _lastKnownPosition = _player.transform.position; _state = ButcherState.Chase; }
            if (_state == ButcherState.Chase)
            {
                MoveTowards(_player.transform.position, _chaseSpeed);
                if (!seesPlayer) { _state = ButcherState.Search; _stateTimer = _searchSeconds; _searchStep = 0; }
                return;
            }
            if (_state == ButcherState.Investigate)
            {
                MoveTowards(_lastKnownPosition, _patrolSpeed * 1.15f);
                _stateTimer -= Time.deltaTime;
                if (Vector3.Distance(transform.position, _lastKnownPosition) < .8f || _stateTimer <= 0f) { _state = ButcherState.Search; _stateTimer = _searchSeconds; _searchStep = 0; }
                return;
            }
            if (_state == ButcherState.Search)
            {
                Vector3 offset = _searchStep == 0 ? transform.right * 2.2f : _searchStep == 1 ? -transform.right * 2.2f : -transform.forward * 1.8f;
                MoveTowards(_lastKnownPosition + offset, _patrolSpeed);
                if (Vector3.Distance(transform.position, _lastKnownPosition + offset) < .65f) _searchStep = Mathf.Min(2, _searchStep + 1);
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f) _state = ButcherState.Patrol;
                return;
            }
            Patrol();
        }

        private void OnNoise(NoiseBus.Noise noise)
        {
            if (_state == ButcherState.Waiting || _state == ButcherState.Warning || _state == ButcherState.Petrified || _state == ButcherState.Chase) return;
            if (Vector3.Distance(transform.position, noise.Position) > noise.Radius) return;
            _lastKnownPosition = noise.Position; _state = ButcherState.Investigate; _stateTimer = 10f;
        }

        private void Patrol()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0) return;
            Transform target = _patrolPoints[_patrolIndex % _patrolPoints.Length];
            MoveTowards(target.position, _patrolSpeed);
            if (Vector3.Distance(transform.position, target.position) < .6f) _patrolIndex = (_patrolIndex + 1) % _patrolPoints.Length;
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
            direction.Normalize(); transform.forward = Vector3.Slerp(transform.forward, direction, 7f * Time.deltaTime);
            _controller.Move((direction * speed + Vector3.down * 2f) * Time.deltaTime);
        }

        private bool CanSeePlayer()
        {
            PlayerStealthState stealth = _player.GetComponent<PlayerStealthState>();
            if (stealth != null && stealth.IsHidden) return false;
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 target = _player.transform.position + Vector3.up * 1.2f;
            Vector3 delta = target - origin;
            if (delta.magnitude > _viewDistance || Vector3.Angle(transform.forward, delta) > _viewAngle * .5f) return false;
            if (!Physics.Raycast(origin, delta.normalized, out RaycastHit hit, delta.magnitude, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform.IsChildOf(_player.transform) || _player.transform.IsChildOf(hit.transform);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            BreakableBoard board = hit.collider.GetComponentInParent<BreakableBoard>();
            if (board != null) { board.BeginBreak(); return; }
            if (_state != ButcherState.Chase || hit.collider.GetComponentInParent<FirstPersonController>() == null) return;
            Object.FindFirstObjectByType<DeathRetryController>()?.ShowDeath("被屠夫抓住");
        }

        private void Petrify()
        {
            if (_state == ButcherState.Petrified) return;
            _state = ButcherState.Petrified;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
            _controller.enabled = false;
            if (_renderers != null)
            {
                foreach (Renderer renderer in _renderers)
                {
                    if (renderer == null) continue;
                    foreach (Material material in renderer.materials)
                    {
                        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(.28f, .3f, .29f));
                        else if (material.HasProperty("_Color")) material.color = new Color(.28f, .3f, .29f);
                    }
                }
            }
            GameSession.Current?.NotifyFeedback("BUTCHER_PETRIFIED");
        }
    }
}
