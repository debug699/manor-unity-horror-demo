using Manor.Core;
using UnityEngine;

namespace Manor.Gameplay
{
    public readonly struct InteractionContext
    {
        public InteractionContext(GameObject actor, Camera viewCamera, IGameStateService gameState)
        {
            Actor = actor;
            ViewCamera = viewCamera;
            GameState = gameState;
        }

        public GameObject Actor { get; }
        public Camera ViewCamera { get; }
        public IGameStateService GameState { get; }
    }
}
