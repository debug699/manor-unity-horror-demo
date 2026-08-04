namespace Manor.Gameplay
{
    public interface IInteractable
    {
        string InteractionId { get; }
        bool CanInteract(InteractionContext context);
        string GetPromptKey(InteractionContext context);
        void Interact(InteractionContext context);
    }
}
