using Manor.Player;
using Manor.UI;
using UnityEngine;

namespace Manor.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class FallDeathTrigger : MonoBehaviour
    {
        private void Awake() => GetComponent<Collider>().isTrigger = true;
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<FirstPersonController>() == null) return;
            Object.FindFirstObjectByType<DeathRetryController>()?.ShowDeath("坠入庄园裂谷");
        }
    }
}
