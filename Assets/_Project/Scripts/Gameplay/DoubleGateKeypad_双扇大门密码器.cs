using UnityEngine;
using UnityEngine.InputSystem;

namespace Manor.Gameplay
{
    /// <summary>Standalone final-exit keypad: press E nearby, enter 1016, and open both gate leaves inward.</summary>
    public sealed class DoubleGateKeypad : MonoBehaviour
    {
        [SerializeField] private Transform _leftHinge;
        [SerializeField] private Transform _rightHinge;
        [SerializeField] private string _password = "1016";
        [SerializeField] private float _openAngle = 92f;
        [SerializeField] private float _openSpeed = 120f;
        [SerializeField] private float _useDistance = 2.4f;

        private Quaternion _leftClosed;
        private Quaternion _rightClosed;
        private bool _editing;
        private bool _opened;
        private string _entered = string.Empty;

        public void Configure(Transform leftHinge, Transform rightHinge, string password, float openAngle)
        {
            _leftHinge = leftHinge;
            _rightHinge = rightHinge;
            _password = password;
            _openAngle = openAngle;
            CacheClosedRotations();
        }

        private void Awake() => CacheClosedRotations();

        private void CacheClosedRotations()
        {
            if (_leftHinge != null) _leftClosed = _leftHinge.localRotation;
            if (_rightHinge != null) _rightClosed = _rightHinge.localRotation;
        }

        private void Update()
        {
            AnimateGate();
            if (_opened || Keyboard.current == null) return;
            if (!_editing)
            {
                if (IsPlayerNear() && Keyboard.current.eKey.wasPressedThisFrame) _editing = true;
                return;
            }

            for (int digit = 0; digit <= 9; digit++)
            {
                Key key = digit == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + digit - 1);
                Key numpad = digit == 0 ? Key.Numpad0 : (Key)((int)Key.Numpad1 + digit - 1);
                if ((Keyboard.current[key]?.wasPressedThisFrame ?? false) || (Keyboard.current[numpad]?.wasPressedThisFrame ?? false))
                    if (_entered.Length < _password.Length) _entered += (char)('0' + digit);
            }
            if (Keyboard.current.backspaceKey.wasPressedThisFrame && _entered.Length > 0) _entered = _entered.Substring(0, _entered.Length - 1);
            if (Keyboard.current.escapeKey.wasPressedThisFrame) { _editing = false; _entered = string.Empty; }
            if ((Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) && _entered == _password)
            {
                _editing = false;
                _opened = true;
            }
        }

        private bool IsPlayerNear()
        {
            Camera camera = Camera.main;
            return camera != null && Vector3.Distance(camera.transform.position, transform.position) <= _useDistance;
        }

        private void AnimateGate()
        {
            if (!_opened) return;
            if (_leftHinge != null)
                _leftHinge.localRotation = Quaternion.RotateTowards(_leftHinge.localRotation, _leftClosed * Quaternion.Euler(0f, -_openAngle, 0f), _openSpeed * Time.deltaTime);
            if (_rightHinge != null)
                _rightHinge.localRotation = Quaternion.RotateTowards(_rightHinge.localRotation, _rightClosed * Quaternion.Euler(0f, _openAngle, 0f), _openSpeed * Time.deltaTime);
        }

        private void OnGUI()
        {
            if (_opened) return;
            if (!_editing)
            {
                if (!IsPlayerNear()) return;
                GUI.Label(new Rect(Screen.width * .5f - 110f, Screen.height * .5f + 55f, 220f, 28f), "按 E 输入大门密码");
                return;
            }
            Rect panel = new Rect(Screen.width * .5f - 210f, Screen.height * .5f - 85f, 420f, 170f);
            GUI.Box(panel, "庄园大门密码器");
            GUI.Label(new Rect(panel.x + 28f, panel.y + 55f, panel.width - 56f, 35f), _entered.PadRight(_password.Length, '—'));
            GUI.Label(new Rect(panel.x + 28f, panel.y + 105f, panel.width - 56f, 30f), "数字键输入　Enter 确认　Esc 取消");
        }
    }
}
