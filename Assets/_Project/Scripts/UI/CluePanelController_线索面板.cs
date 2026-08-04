using Manor.Player;
using Manor.Narrative;
using Manor.Runtime;
using UnityEngine;

namespace Manor.UI
{
    public sealed class CluePanelController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        private bool _isOpen;
        private string _title = string.Empty;
        private string _body = string.Empty;

        private void OnEnable()
        {
            if (_input != null) _input.CluesPressed += Toggle;
            if (GameSession.Current != null) GameSession.Current.ClueRequested += ShowLatestClue;
        }

        private void OnDisable()
        {
            if (_input != null) _input.CluesPressed -= Toggle;
            if (GameSession.Current != null) GameSession.Current.ClueRequested -= ShowLatestClue;
        }

        public void Configure(PlayerInputReader input)
        {
            if (enabled && _input != null) _input.CluesPressed -= Toggle;
            _input = input;
            if (enabled && _input != null) _input.CluesPressed += Toggle;
            _title = ManorTextCatalog.Resolve("CLUE_PANEL_TITLE");
            _body = ManorTextCatalog.Resolve("CLUE_EMPTY");
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            Time.timeScale = _isOpen ? 0f : 1f;
            Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _isOpen;
        }

        public bool IsOpen => _isOpen;

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnGUI()
        {
            if (!_isOpen) return;
            GUI.Box(new Rect(Screen.width * 0.15f, Screen.height * 0.12f, Screen.width * 0.7f, Screen.height * 0.76f), string.Empty);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, alignment = TextAnchor.UpperCenter };
            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
            GUI.Label(new Rect(Screen.width * 0.2f, Screen.height * 0.18f, Screen.width * 0.6f, 50f), _title, titleStyle);
            GUI.Label(new Rect(Screen.width * 0.22f, Screen.height * 0.28f, Screen.width * 0.56f, Screen.height * 0.4f), _body, bodyStyle);
            GUI.Label(new Rect(Screen.width * 0.35f, Screen.height * 0.78f, Screen.width * 0.3f, 40f), ManorTextCatalog.Resolve("CLUE_CLOSE_HINT"), titleStyle);
        }

        private void ShowLatestClue(string title, string body)
        {
            _title = ManorTextCatalog.Resolve(title);
            _body = ManorTextCatalog.Resolve(body);
            if (!_isOpen) Toggle();
        }
    }
}
