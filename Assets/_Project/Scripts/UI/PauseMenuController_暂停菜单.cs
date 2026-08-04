using Manor.Player;
using Manor.Narrative;
using UnityEngine;

namespace Manor.UI
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private CluePanelController _cluePanel;

        public bool IsPaused { get; private set; }

        private void OnEnable()
        {
            if (_input != null) _input.PausePressed += TogglePause;
        }

        private void OnDisable()
        {
            if (_input != null) _input.PausePressed -= TogglePause;
            if (IsPaused) Resume();
        }

        public void Configure(PlayerInputReader input, CluePanelController cluePanel = null)
        {
            if (enabled && _input != null) _input.PausePressed -= TogglePause;
            _input = input;
            _cluePanel = cluePanel;
            if (enabled && _input != null) _input.PausePressed += TogglePause;
        }

        public void TogglePause()
        {
            if (_cluePanel != null && _cluePanel.IsOpen)
            {
                _cluePanel.Close();
                return;
            }

            if (IsPaused) Resume(); else Pause();
        }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnGUI()
        {
            if (!IsPaused) return;
            GUI.Box(new Rect(Screen.width * 0.3f, Screen.height * 0.3f, Screen.width * 0.4f, Screen.height * 0.4f), string.Empty);
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(Screen.width * 0.3f, Screen.height * 0.38f, Screen.width * 0.4f, 100f), ManorTextCatalog.Resolve("PAUSE_TEXT"), style);
        }
    }
}
