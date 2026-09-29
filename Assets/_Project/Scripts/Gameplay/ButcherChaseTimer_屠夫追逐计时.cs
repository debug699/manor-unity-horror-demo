using Manor.Core;
using Manor.Runtime;
using Manor.Player;
using UnityEngine;

namespace Manor.Gameplay
{
    /// <summary>Independent 480-second chase clock. AI never owns story time.</summary>
    public sealed class ButcherChaseTimer : MonoBehaviour
    {
        public const float DawnSeconds = 480f;
        [SerializeField, Min(1f)] private float _autoSaveInterval = 30f;
        private float _lastSavedAt;

        private void Update()
        {
            IGameStateService state = GameSession.Current?.GameState;
            if (state == null || !state.ButcherChaseStarted || state.DawnTriggered || Time.timeScale <= 0f) return;
            Advance(state, Time.deltaTime);
        }

        public void Advance(IGameStateService state, float deltaSeconds)
        {
            if (state == null || !state.ButcherChaseStarted || state.DawnTriggered || deltaSeconds <= 0f) return;
            float elapsed = state.ChaseElapsedSeconds + deltaSeconds;
            state.SetChaseElapsedSeconds(elapsed);
            if (elapsed - _lastSavedAt >= _autoSaveInterval)
            {
                _lastSavedAt = elapsed;
                GameSession.Current?.RequestAutoSave(state);
            }
            if (elapsed < DawnSeconds) return;
            state.SetDawnTriggered(true);
            state.AdvanceStory(StoryStage.DawnSurvival);
            state.SetObjective(ProjectIds.ObjectiveRestoreBridge);
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            Vector3 safePosition = player != null ? player.transform.position : state.Snapshot.lastSafePosition;
            state.SetCheckpoint(PlayerCheckpointRestorer.DawnId, safePosition);
            GameSession.Current?.NotifyFeedback("DAWN_TRIGGERED");
            GameSession.Current?.RequestAutoSave(state);
        }
    }
}
