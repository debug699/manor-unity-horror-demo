using UnityEngine;

namespace Manor.Runtime
{
    /// <summary>
    /// Kept only for compatibility with existing player setup. Collision is authored once into
    /// the scene by ManorRealModelCollisionRepair; runtime must never manufacture proxy boxes.
    /// </summary>
    public sealed class StaticWorldMeshColliders : MonoBehaviour
    {
        private void Awake()
        {
            foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (transform.gameObject.scene.IsValid() && transform.name.StartsWith("UserProp_"))
                    Destroy(transform.gameObject);
            }
        }
    }
}
