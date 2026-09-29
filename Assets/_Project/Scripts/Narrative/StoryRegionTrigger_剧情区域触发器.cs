using Manor.Core;
using Manor.Player;
using Manor.Runtime;
using UnityEngine;

namespace Manor.Narrative
{
    [RequireComponent(typeof(Collider))]
    public sealed class StoryRegionTrigger : MonoBehaviour
    {
        [SerializeField] private StoryStage _stage;
        [SerializeField] private string _objectiveId;

        public void Configure(StoryStage stage, string objectiveId)
        {
            _stage = stage;
            _objectiveId = objectiveId;
            Collider region = GetComponent<Collider>();
            region.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<FirstPersonController>() == null) return;
            IGameStateService state = GameSession.Current?.GameState;
            if (state == null) return;
            state.AdvanceStory(_stage);
            if (StableId.IsValidValue(_objectiveId)) state.SetObjective(_objectiveId);
            GameSession.Current?.RequestAutoSave(state);
        }
    }
}
