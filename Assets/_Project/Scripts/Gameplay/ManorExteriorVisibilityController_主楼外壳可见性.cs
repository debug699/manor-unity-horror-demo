using UnityEngine;

namespace Manor.Gameplay
{
    public sealed class ManorExteriorVisibilityController : MonoBehaviour
    {
        [SerializeField] private Renderer[] exteriorRenderers;
        [SerializeField] private Vector3 interiorCenter = new(0f, 3.2f, -2.5f);
        [SerializeField] private Vector3 interiorSize = new(42f, 8f, 24f);

        private bool? exteriorVisible;

        public void Configure(Renderer[] renderers, Vector3 center, Vector3 size)
        {
            exteriorRenderers = renderers;
            interiorCenter = center;
            interiorSize = size;
            Apply(true);
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            Bounds interior = new(interiorCenter, interiorSize);
            Apply(!interior.Contains(camera.transform.position));
        }

        private void Apply(bool visible)
        {
            if (exteriorVisible == visible) return;
            exteriorVisible = visible;
            if (exteriorRenderers == null) return;
            foreach (Renderer renderer in exteriorRenderers)
                if (renderer != null) renderer.enabled = visible;
        }
    }
}
