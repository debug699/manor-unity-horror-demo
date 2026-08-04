using Manor.Gameplay;
using Manor.Narrative;
using Manor.Runtime;
using UnityEngine;

namespace Manor.UI
{
    public sealed class RuntimeHud : MonoBehaviour
    {
        [SerializeField] private float _toastDuration = 2.5f;
        [SerializeField] private string _controlsTextKey = "TEST_CONTROLS";

        private float _toastRemaining;
        [SerializeField] private InteractionScanner _scanner;
        private string _objective = string.Empty;
        private string _prompt = string.Empty;
        private string _toast = string.Empty;

        public void Configure(InteractionScanner scanner)
        {
            BindScanner(scanner);
            RefreshObjective();
        }

        public string CurrentPromptText => _prompt;
        public string CurrentFeedbackText => _toast;

        private void OnEnable()
        {
            InteractionScanner serializedScanner = _scanner;
            BindScanner(serializedScanner);
            if (GameSession.Current?.GameState != null) GameSession.Current.GameState.StateChanged += RefreshObjective;
            if (GameSession.Current != null) GameSession.Current.ClueRequested += ShowClue;
            if (GameSession.Current != null) GameSession.Current.FeedbackRequested += ShowFeedback;
        }

        private void OnDisable()
        {
            if (GameSession.Current?.GameState != null) GameSession.Current.GameState.StateChanged -= RefreshObjective;
            if (GameSession.Current != null) GameSession.Current.ClueRequested -= ShowClue;
            if (GameSession.Current != null) GameSession.Current.FeedbackRequested -= ShowFeedback;
            BindScanner(null);
        }

        private void Update()
        {
            if (_toastRemaining <= 0f) return;
            _toastRemaining -= Time.unscaledDeltaTime;
            if (_toastRemaining <= 0f) _toast = string.Empty;
        }

        private void OnGUI()
        {
            GUIStyle objectiveStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperLeft };
            GUIStyle promptStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
            GUIStyle toastStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperCenter };
            GUI.Label(new Rect(28f, 24f, Screen.width - 56f, 50f), _objective, objectiveStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.65f, 600f, 50f), _prompt, promptStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 350f, 80f, 700f, 50f), _toast, toastStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 500f, Screen.height - 55f, 1000f, 40f), ManorTextCatalog.Resolve(_controlsTextKey), toastStyle);
        }

        private void BindScanner(InteractionScanner scanner)
        {
            if (_scanner != null) _scanner.PromptChanged -= OnPromptChanged;
            _scanner = scanner;
            if (_scanner != null) _scanner.PromptChanged += OnPromptChanged;
        }

        private void RefreshObjective()
        {
            string objectiveId = GameSession.Current?.GameState?.Snapshot.objectiveId;
            _objective = ManorTextCatalog.Resolve("OBJECTIVE_PREFIX") + ManorTextCatalog.Resolve(objectiveId);
        }

        private void OnPromptChanged(string promptKey)
        {
            _prompt = ManorTextCatalog.Resolve(promptKey);
        }

        private void ShowClue(string title, string body)
        {
            _toast = ManorTextCatalog.Resolve("CLUE_ADDED") + "：" + ManorTextCatalog.Resolve(title);
            _toastRemaining = _toastDuration;
        }

        private void ShowFeedback(string textKey)
        {
            _toast = ManorTextCatalog.Resolve(textKey);
            _toastRemaining = _toastDuration;
        }
    }
}
