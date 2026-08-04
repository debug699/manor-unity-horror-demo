using Manor.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.UI
{
    public sealed class DeathRetryController : MonoBehaviour
    {
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

        private void OnGUI()
        {
            if (!IsDead) return;
            GUI.Box(new Rect(Screen.width * 0.25f, Screen.height * 0.25f, Screen.width * 0.5f, Screen.height * 0.5f), string.Empty);
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            GUI.Label(new Rect(Screen.width * 0.3f, Screen.height * 0.34f, Screen.width * 0.4f, 160f), "汤姆未能逃脱\n" + DeathReason + "\n\n基础重试接口已就绪", style);
            if (GUI.Button(new Rect(Screen.width * 0.42f, Screen.height * 0.62f, Screen.width * 0.16f, 48f), "重新开始")) RetryLatestCheckpoint();
        }
    }
}
