using Manor.Core;
using UnityEngine;

namespace Manor.Narrative
{
    [CreateAssetMenu(menuName = "庄园/数据/线索定义", fileName = "SO_Clue_线索")]
    public sealed class ClueDefinition : ScriptableObject
    {
        [SerializeField] private StableId _clueId;
        [SerializeField] private string _titleKey;
        [SerializeField, TextArea] private string _bodyKey;
        [SerializeField] private bool _isMainStoryClue;

        public StableId ClueId => _clueId;
        public string TitleKey => _titleKey;
        public string BodyKey => _bodyKey;
        public bool IsMainStoryClue => _isMainStoryClue;
    }
}
