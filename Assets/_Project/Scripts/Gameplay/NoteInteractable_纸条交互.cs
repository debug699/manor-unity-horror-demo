using Manor.Core;
using Manor.Runtime;
using UnityEngine;

namespace Manor.Gameplay
{
    public sealed class NoteInteractable : InteractableBase
    {
        [SerializeField] private StableId _clueId;
        [SerializeField] private string _titleKey = "TEST_NOTE_TITLE";
        [SerializeField] private string _bodyKey = "TEST_NOTE_BODY";

        public void ConfigureNote(string interactionId, string clueId, string titleKey, string bodyKey)
        {
            ConfigureId(interactionId);
            _clueId = new StableId(clueId);
            _titleKey = titleKey;
            _bodyKey = bodyKey;
        }

        public override bool CanInteract(InteractionContext context) => context.GameState != null && _clueId.IsValid;
        public override string GetPromptKey(InteractionContext context) => "PROMPT_READ_NOTE";

        public override void Interact(InteractionContext context)
        {
            if (!CanInteract(context)) return;
            bool wasAdded = context.GameState.MarkClueRead(_clueId.Value);
            GameSession.Current?.NotifyClue(_titleKey, _bodyKey);
            GameSession.Current?.NotifyFeedback(wasAdded ? "CLUE_ADDED" : "NOTE_ALREADY_READ");
            if (wasAdded) GameSession.Current?.RequestAutoSave(context.GameState);
        }
    }
}
