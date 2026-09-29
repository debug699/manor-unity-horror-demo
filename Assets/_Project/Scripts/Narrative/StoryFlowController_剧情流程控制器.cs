using Manor.Core;
using Manor.Runtime;
using UnityEngine;

namespace Manor.Narrative
{
    /// <summary>Central, idempotent progression rules for the first playable route.</summary>
    public sealed class StoryFlowController : MonoBehaviour
    {
        private const string WireItem = "ITEM_G11_LOCKPICK_WIRE";
        private const string PrayerDoorUnlock = "KEY_G11_LOCKPICKED";
        private const string ResearchKey = "KEY_G10_RESEARCH_ROOM";
        private const string DiaryClue = "CLUE_G08_DIARY";
        private const string TracesClue = "CLUE_G04_TRACES";
        private const string RitualClue = "CLUE_G10_RITUAL_RECORD";
        private const string CompleteParchment = "ITEM_COMPLETE_PARCHMENT";

        private IGameStateService _state;

        private void Start()
        {
            _state = GameSession.Current?.GameState;
            if (_state == null) return;
            _state.StateChanged += Reconcile;
            Reconcile();
        }

        private void OnDestroy()
        {
            if (_state != null) _state.StateChanged -= Reconcile;
        }

        private void Reconcile()
        {
            if (_state == null) return;
            GameStateData snapshot = _state.Snapshot;
            if (_state.HasItem(WireItem) && snapshot.storyStage < StoryStage.FindFirstKey)
            {
                _state.AdvanceStory(StoryStage.FindFirstKey);
                _state.SetObjective(ProjectIds.ObjectiveUnlockPrayerRoom);
            }
            if (_state.HasKey(PrayerDoorUnlock) && _state.Snapshot.storyStage < StoryStage.FirstHaunting)
            {
                _state.AdvanceStory(StoryStage.FirstHaunting);
                _state.SetObjective(ProjectIds.ObjectiveFindResearchKey);
            }
            if (_state.HasKey(ResearchKey) && _state.Snapshot.storyStage < StoryStage.CollectVictimClues)
            {
                _state.AdvanceStory(StoryStage.CollectVictimClues);
                _state.SetObjective(ProjectIds.ObjectiveInvestigateResearchRoom);
            }
            if (_state.HasReadClue(DiaryClue) && _state.HasReadClue(TracesClue) && _state.HasReadClue(RitualClue))
            {
                if (_state.Snapshot.storyStage < StoryStage.DiscoverRitual)
                {
                    _state.AdvanceStory(StoryStage.DiscoverRitual);
                    _state.SetObjective(ProjectIds.ObjectiveOpenKitchenHatch);
                }
            }
            if (_state.HasItem(CompleteParchment) && _state.Snapshot.storyStage < StoryStage.CompleteParchment)
            {
                _state.AdvanceStory(StoryStage.CompleteParchment);
                _state.SetObjective(ProjectIds.ObjectiveCommunicateWithWraith);
            }
        }
    }
}
