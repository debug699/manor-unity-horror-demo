using Manor.Core;
using Manor.Player;
using Manor.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Narrative
{
    [RequireComponent(typeof(Collider))]
    public sealed class EscapeTrigger : MonoBehaviour
    {
        private const string BootSceneName = "SCN_Boot_启动场景";
        private bool _escaped;

        private void Start() => _escaped = GameSession.Current?.GameState?.Snapshot.storyStage == StoryStage.Escape;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (_escaped || other.GetComponentInParent<FirstPersonController>() == null) return;
            IGameStateService state = GameSession.Current?.GameState;
            if (state == null || !state.GateUnlocked || !state.BridgeRestored || !state.DawnTriggered) return;
            _escaped = true;
            state.AdvanceStory(StoryStage.Escape);
            GameSession.Current?.NotifyFeedback("ESCAPE_COMPLETE");
            GameSession.Current?.RequestAutoSave(state);
            other.GetComponentInParent<FirstPersonController>()?.SetInputLocked(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void RestartNewGame()
        {
            Time.timeScale = 1f;
            GameSession.Current?.StartNewGame();
            SceneManager.LoadScene(BootSceneName);
        }

        public void ReturnToBootMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(BootSceneName);
        }

        private void OnGUI()
        {
            if (!_escaped) return;
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            GUI.Box(new Rect((Screen.width - 680f) * .5f, (Screen.height - 240f) * .5f, 680f, 240f), string.Empty);
            GUI.Label(new Rect((Screen.width - 620f) * .5f, (Screen.height - 170f) * .5f, 620f, 170f),
                "汤姆逃出了庄园。\n受害者的真相仍需被带回外界。\n\n第一版 Demo 完成", style);
            if (GUI.Button(new Rect(Screen.width * .34f, Screen.height * .67f, Screen.width * .15f, 48f), "重新开始")) RestartNewGame();
            if (GUI.Button(new Rect(Screen.width * .51f, Screen.height * .67f, Screen.width * .15f, 48f), "返回主菜单")) ReturnToBootMenu();
        }
    }
}
