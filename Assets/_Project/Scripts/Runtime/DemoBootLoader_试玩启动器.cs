using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Runtime
{
    /// <summary>Temporary first-demo entry point: routes a build into the existing outdoor playable scene.</summary>
    public sealed class DemoBootLoader : MonoBehaviour
    {
        [SerializeField] private string _demoSceneName = "SCN_ManorDemo_庄园Demo";
        private bool _showMenu = true;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            if (!_showMenu) return;
            Rect panel = new Rect((Screen.width - 520f) * .5f, (Screen.height - 380f) * .5f, 520f, 380f);
            GUI.Box(panel, string.Empty);
            GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 42, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(panel.x + 30f, panel.y + 35f, 460f, 70f), "庄园", title);
            if (GUI.Button(new Rect(panel.x + 110f, panel.y + 135f, 300f, 52f), "新游戏")) StartNewGame();
            if (GUI.Button(new Rect(panel.x + 110f, panel.y + 205f, 300f, 52f), "继续游戏")) ContinueGame();
            if (GUI.Button(new Rect(panel.x + 110f, panel.y + 275f, 300f, 52f), "退出")) Application.Quit();
        }

        public void StartNewGame()
        {
            GameSession.Current?.StartNewGame();
            LoadDemo();
        }

        public void ContinueGame()
        {
            GameSession.Current?.TryLoadAutoSave();
            LoadDemo();
        }

        private void LoadDemo()
        {
            _showMenu = false;
            SceneManager.LoadScene(_demoSceneName, LoadSceneMode.Single);
        }
    }
}
