using UnityEngine;

namespace Manor.Player
{
    public sealed class FirstPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform _playerBody;
        [SerializeField, Min(0.01f)] private float _sensitivity = 0.12f;
        [SerializeField] private float _minimumPitch = -85f;
        [SerializeField] private float _maximumPitch = 85f;

        private float _pitch;

        public void Configure(Transform playerBody, float sensitivity = 0.12f)
        {
            _playerBody = playerBody;
            _sensitivity = Mathf.Max(0.01f, sensitivity);
        }

        public void ApplyLook(Vector2 lookDelta)
        {
            if (_playerBody == null) return;
            _pitch = Mathf.Clamp(_pitch - lookDelta.y * _sensitivity, _minimumPitch, _maximumPitch);
            transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            _playerBody.Rotate(Vector3.up, lookDelta.x * _sensitivity, Space.Self);
        }
    }
}
