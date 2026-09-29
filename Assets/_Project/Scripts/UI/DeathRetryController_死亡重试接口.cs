using Manor.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.UI
{
    public sealed class DeathRetryController : MonoBehaviour
    {
        private const string BootSceneName = "SCN_Boot_启动场景";
        public bool IsDead { get; private set; }
        public string DeathReason { get; private set; }

        public void ShowDeath(string reason)
        {
            IsDead = true;
            DeathReason = reason ?? string.Empty;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void RetryLatestCheckpoint()
        {
            Time.timeScale = 1f;
            GameSession.Current?.TryLoadAutoSave();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
            if (!IsDead) return;
            GUI.Box(new Rect(Screen.width * 0.25f, Screen.height * 0.25f, Screen.width * 0.5f, Screen.height * 0.5f), string.Empty);
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            GUI.Label(new Rect(Screen.width * 0.3f, Screen.height * 0.32f, Screen.width * 0.4f, 150f), "汤姆未能逃脱\n" + DeathReason, style);
            float buttonY = Screen.height * 0.57f;
            float buttonWidth = Screen.width * 0.14f;
            if (GUI.Button(new Rect(Screen.width * 0.28f, buttonY, buttonWidth, 48f), "重试检查点")) RetryLatestCheckpoint();
            if (GUI.Button(new Rect(Screen.width * 0.43f, buttonY, buttonWidth, 48f), "重新开始")) RestartNewGame();
            if (GUI.Button(new Rect(Screen.width * 0.58f, buttonY, buttonWidth, 48f), "返回主菜单")) ReturnToBootMenu();
        }
    }
}
