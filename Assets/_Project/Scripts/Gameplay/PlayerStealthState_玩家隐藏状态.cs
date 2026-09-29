using UnityEngine;

namespace Manor.Gameplay
{
    public sealed class PlayerStealthState : MonoBehaviour
    {
        public bool IsHidden { get; private set; }
        public void SetHidden(bool hidden) => IsHidden = hidden;
    }
}
