using System;
using Manor.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Runtime
{
    public sealed class GameSession : MonoBehaviour
    {
        private static GameSession _current;
        private IAutoSaveService _autoSave;

        public static GameSession Current => _current;
        public IGameStateService GameState { get; private set; }
        public bool AutoSaveEnabled { get; set; } = true;
        public event Action<string, string> ClueRequested;
        public event Action<string> FeedbackRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureCreated()
        {
            if (_current != null) return;
            GameObject root = new GameObject("GameSession_游戏会话");
            _current = root.AddComponent<GameSession>();
            DontDestroyOnLoad(root);
        }

        private void Awake()
        {
            if (_current != null && _current != this)
            {
                Destroy(gameObject);
                return;
            }

            _current = this;
            DontDestroyOnLoad(gameObject);
            _autoSave = new JsonAutoSaveService();
            GameState = new GameStateService();
            TryLoadAutoSave();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_current != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _current = null;
        }

        public void RequestAutoSave()
        {
            if (!AutoSaveEnabled || GameState == null) return;
            _autoSave.Save(GameState.Snapshot);
            Debug.Log("[Save] 自动存档完成 / Autosave completed: " + _autoSave.SavePath);
        }

        public void RequestAutoSave(IGameStateService source)
        {
            if (!ReferenceEquals(source, GameState)) return;
            RequestAutoSave();
        }

        public bool TryLoadAutoSave()
        {
            if (!_autoSave.TryLoad(out GameStateData data)) return false;
            GameState.Restore(data);
            return true;
        }

        public void NotifyClue(string title, string body) => ClueRequested?.Invoke(title, body);
        public void NotifyFeedback(string textKey) => FeedbackRequested?.Invoke(textKey);

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string sceneId = SceneIdUtility.FromSceneName(scene.name);
            if (StableId.IsValidValue(sceneId)) GameState.SetScene(sceneId);
        }
    }
}
