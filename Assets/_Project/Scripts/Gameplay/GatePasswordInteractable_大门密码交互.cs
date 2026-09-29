using Manor.Core;
using Manor.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using Manor.Player;

namespace Manor.Gameplay
{
    /// <summary>Small in-world keypad for the estate gate. Correct input unlocks the real hinged leaf.</summary>
    public sealed class GatePasswordInteractable : InteractableBase
    {
        [SerializeField] private string _correctPassword = "1016";
        [SerializeField] private string _unlockKeyId = "KEY_MANOR_GATE_PASSWORD";
        [SerializeField] private DoorInteractable _gateDoor;
        [SerializeField, Min(1)] private int _maxDigits = 4;

        private bool _editing;
        private string _entered = string.Empty;
        private InteractionContext _context;
        private FirstPersonController _lockedPlayer;

        public string CorrectPassword => _correctPassword;
        public bool IsEditing => _editing;

        public void ConfigurePassword(string interactionId, string password, DoorInteractable gateDoor)
        {
            ConfigureId(interactionId);
            _correctPassword = password;
            _maxDigits = Mathf.Max(1, password?.Length ?? 4);
            _gateDoor = gateDoor;
        }

        public override bool CanInteract(InteractionContext context) => context.GameState != null && !_editing;
        public override string GetPromptKey(InteractionContext context)
        {
            if (context.GameState == null) return string.Empty;
            if (!context.GameState.GatePasswordKnown) return "PROMPT_GATE_PASSWORD_UNKNOWN";
            if (!context.GameState.BridgeRestored) return "PROMPT_GATE_BRIDGE_BLOCKED";
            return "PROMPT_GATE_KEYPAD";
        }

        public override void Interact(InteractionContext context)
        {
            if (!CanInteract(context)) return;
            _context = context;
            if (!context.GameState.GatePasswordKnown)
            {
                GameSession.Current?.NotifyFeedback("GATE_PASSWORD_UNKNOWN");
                return;
            }
            if (!context.GameState.BridgeRestored)
            {
                GameSession.Current?.NotifyFeedback("GATE_BRIDGE_BLOCKED");
                return;
            }
            if (context.GameState.HasKey(_unlockKeyId))
            {
                OpenGate();
                return;
            }
            _entered = string.Empty;
            _editing = true;
            _lockedPlayer = context.Actor.GetComponentInParent<FirstPersonController>();
            _lockedPlayer?.SetInputLocked(true);
        }

        private void Update()
        {
            if (!_editing || Keyboard.current == null) return;
            for (int digit = 0; digit <= 9; digit++)
            {
                Key key = digit == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + digit - 1);
                Key numpad = digit == 0 ? Key.Numpad0 : (Key)((int)Key.Numpad1 + digit - 1);
                if ((Keyboard.current[key]?.wasPressedThisFrame ?? false) || (Keyboard.current[numpad]?.wasPressedThisFrame ?? false))
                    AppendDigit((char)('0' + digit));
            }
            if (Keyboard.current.backspaceKey.wasPressedThisFrame && _entered.Length > 0)
                _entered = _entered.Substring(0, _entered.Length - 1);
            if (Keyboard.current.escapeKey.wasPressedThisFrame) Cancel();
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) Submit();
        }

        private void AppendDigit(char digit)
        {
            if (_entered.Length < _maxDigits) _entered += digit;
        }

        private void Submit()
        {
            SubmitPassword(_entered, _context);
        }

        public bool SubmitPassword(string enteredPassword, InteractionContext context)
        {
            if (context.GameState == null || !context.GameState.GatePasswordKnown || !context.GameState.BridgeRestored) return false;
            _context = context;
            if (string.Equals(enteredPassword, _correctPassword, System.StringComparison.Ordinal))
            {
                _context.GameState.AddKey(_unlockKeyId);
                _context.GameState.SetGateUnlocked(true);
                _editing = false;
                ReleasePlayer();
                GameSession.Current?.NotifyFeedback("GATE_PASSWORD_CORRECT");
                GameSession.Current?.RequestAutoSave(_context.GameState);
                OpenGate();
                return true;
            }
            _entered = string.Empty;
            GameSession.Current?.NotifyFeedback("GATE_PASSWORD_WRONG");
            return false;
        }

        private void OpenGate()
        {
            if (_gateDoor != null && !_gateDoor.IsOpen) _gateDoor.Interact(_context);
        }

        private void Cancel()
        {
            _editing = false;
            _entered = string.Empty;
            ReleasePlayer();
        }

        private void OnDisable() => Cancel();
        private void ReleasePlayer()
        {
            _lockedPlayer?.SetInputLocked(false);
            _lockedPlayer = null;
        }

        private void OnGUI()
        {
            if (!_editing) return;
            const float width = 460f;
            const float height = 190f;
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, string.Empty);
            GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
            GUIStyle digits = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(panel.x + 20f, panel.y + 18f, width - 40f, 38f), "庄园大门密码", title);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 62f, width - 40f, 50f), _entered.PadRight(_maxDigits, '—'), digits);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 126f, width - 40f, 35f), "数字键输入　Enter 确认　Backspace 删除　Esc 取消", title);
        }
    }
}
