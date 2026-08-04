using Manor.Core;
using UnityEngine;

namespace Manor.Runtime
{
    public sealed class FoundationTestBootstrap : MonoBehaviour
    {
        private void Start()
        {
            GameSession.Current?.GameState.SetObjective(ProjectIds.ObjectiveTestInteractions);
        }
    }
}
