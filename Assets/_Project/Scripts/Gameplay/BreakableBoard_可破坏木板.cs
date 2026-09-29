using UnityEngine;

namespace Manor.Gameplay
{
    public sealed class BreakableBoard : MonoBehaviour
    {
        [SerializeField, Min(.2f)] private float _breakDelay = 1.25f;
        private float _remaining = -1f;
        public bool IsBreaking => _remaining >= 0f;
        public bool IsBroken { get; private set; }

        public void BeginBreak()
        {
            if (!IsBroken && !IsBreaking) _remaining = _breakDelay;
        }

        private void Update()
        {
            if (!IsBreaking) return;
            _remaining -= Time.deltaTime;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 28f) * 4f);
            if (_remaining > 0f) return;
            IsBroken = true; _remaining = -1f;
            foreach (Collider collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            NoiseBus.Emit(transform.position, 18f, gameObject);
        }
    }
}
