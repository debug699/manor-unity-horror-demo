namespace Manor.Gameplay
{
    public enum InteractionAction
    {
        Interact,
        Pickup,
        HoldUse
    }

    public interface IInteractable
    {
        string InteractionId { get; }
        InteractionAction RequiredAction { get; }
        bool CanInteract(InteractionContext context);
        string GetPromptKey(InteractionContext context);
        void Interact(InteractionContext context);
    }
}
