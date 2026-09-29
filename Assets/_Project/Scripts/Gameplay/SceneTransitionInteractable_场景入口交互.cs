using Manor.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manor.Gameplay
{
    /// <summary>An explicit doorway transition; uses a trigger collider so it never creates a blocking wall.</summary>
    public sealed class SceneTransitionInteractable : InteractableBase
    {
        [SerializeField] private string _targetSceneName;
        [SerializeField] private string _promptKey = "PROMPT_ENTER_INTERIOR";

        public void ConfigureTransition(string interactionId, string targetSceneName, string promptKey)
        {
            ConfigureId(interactionId);
            _targetSceneName = targetSceneName;
            _promptKey = promptKey;
        }

        public override bool CanInteract(InteractionContext context) => context.GameState != null && !string.IsNullOrWhiteSpace(_targetSceneName);
        public override string GetPromptKey(InteractionContext context) => _promptKey;
        public override void Interact(InteractionContext context)
        {
            if (!CanInteract(context)) return;
            SceneManager.LoadScene(_targetSceneName, LoadSceneMode.Single);
        }
    }
}
