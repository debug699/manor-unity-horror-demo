using Manor.Core;
using Manor.Gameplay;
using Manor.Player;
using Manor.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manor.Narrative
{
    /// <summary>Three-step finite communication proving Tom understands the victims. Wrong answers are retryable.</summary>
    public sealed class ParchmentCommunicationInteractable : InteractableBase
    {
        [SerializeField] private StableId _parchmentItemId = new StableId("ITEM_COMPLETE_PARCHMENT");
        private static readonly string[] RequiredClues =
        {
            "CLUE_G08_DIARY", "CLUE_G09_BOY_FAMILY", "CLUE_G09_GIRL_FAMILY", "CLUE_G10_RITUAL_RECORD"
        };
        private static readonly int[] CorrectAnswers = { 2, 1, 3 };
        private bool _editing;
        private int _step;
        private InteractionContext _context;
        private FirstPersonController _lockedPlayer;

        /// <summary>True while the three-question communication UI owns the player input.</summary>
        public bool IsEditing => _editing;

        public override bool CanInteract(InteractionContext context) => context.GameState != null && !_editing;
        public override string GetPromptKey(InteractionContext context)
        {
            if (context.GameState == null) return string.Empty;
            if (context.GameState.CommunicationComplete) return "PROMPT_COMMUNICATION_COMPLETE";
            if (!context.GameState.HasItem(_parchmentItemId.Value)) return "PROMPT_NEED_PARCHMENT";
            return HasRequiredUnderstanding(context.GameState) ? "PROMPT_USE_PARCHMENT" : "PROMPT_NEED_VICTIM_CLUES";
        }

        public override void Interact(InteractionContext context)
        {
            if (!CanInteract(context) || context.GameState.CommunicationComplete) return;
            if (!context.GameState.HasItem(_parchmentItemId.Value))
            {
                GameSession.Current?.NotifyFeedback("NEED_PARCHMENT");
                return;
            }
            if (!HasRequiredUnderstanding(context.GameState))
            {
                GameSession.Current?.NotifyFeedback("NEED_VICTIM_CLUES");
                return;
            }
            _context = context;
            _step = 0;
            _editing = true;
            _lockedPlayer = context.Actor.GetComponentInParent<FirstPersonController>();
            _lockedPlayer?.SetInputLocked(true);
        }

        private void Update()
        {
            if (!_editing || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) SubmitAnswer(1);
            if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) SubmitAnswer(2);
            if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) SubmitAnswer(3);
        }

        /// <summary>
        /// Submits one answer through the same state machine used by the keyboard UI.
        /// Returns true when the answer advances the current step (including completion).
        /// Wrong answers reset the retryable conversation and return false.
        /// </summary>
        public bool SubmitAnswer(int answer)
        {
            if (!_editing || answer < 1 || answer > 3) return false;
            if (answer != CorrectAnswers[_step])
            {
                GameSession.Current?.NotifyFeedback("COMMUNICATION_RETRY");
                _step = 0;
                return false;
            }
            _step++;
            if (_step < CorrectAnswers.Length) return true;
            IGameStateService state = _context.GameState;
            state.SetCommunicationComplete(true);
            state.SetGatePasswordKnown(true);
            state.AdvanceStory(StoryStage.RageDissipates);
            state.AdvanceStory(StoryStage.RitualCollapse);
            state.SetButcherChaseStarted(true);
            state.AdvanceStory(StoryStage.ButcherChase);
            state.SetObjective(ProjectIds.ObjectiveSurviveUntilDawn);
            state.SetCheckpoint(PlayerCheckpointRestorer.ChaseStartId, _context.Actor.transform.position);
            GameSession.Current?.NotifyFeedback("COMMUNICATION_SUCCEEDED");
            GameSession.Current?.RequestAutoSave(state);
            Close();
            return true;
        }

        private void Close()
        {
            _editing = false;
            _lockedPlayer?.SetInputLocked(false);
            _lockedPlayer = null;
        }

        private void OnDisable() => Close();

        private static bool HasRequiredUnderstanding(IGameStateService state)
        {
            foreach (string clueId in RequiredClues) if (!state.HasReadClue(clueId)) return false;
            return true;
        }

        private void OnGUI()
        {
            if (!_editing) return;
            Rect panel = new Rect((Screen.width - 720f) * .5f, (Screen.height - 350f) * .5f, 720f, 350f);
            GUI.Box(panel, string.Empty);
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            string[] questions =
            {
                "羊皮纸浮现文字：你是谁？\n1. 屠夫的新帮手\n2. 调查失踪案的汤姆，不是祭品同伙\n3. 法圣的信徒",
                "两个孩子是谁？\n1. 来自两个不同家庭，各有自己的名字和遗物\n2. 孕妇的双胞胎\n3. 屠夫收养的一对兄妹",
                "你如何回应她的痛苦？\n1. 命令她让路\n2. 承诺替屠夫完成仪式\n3. 承认她和腹中孩子的伤害，并帮助受害者脱离法阵"
            };
            GUI.Label(new Rect(panel.x + 35f, panel.y + 30f, 650f, 260f), questions[Mathf.Clamp(_step, 0, 2)], style);
            GUI.Label(new Rect(panel.x + 35f, panel.y + 300f, 650f, 30f), "按 1 / 2 / 3 选择，Esc 暂停沟通", style);
        }
    }
}
