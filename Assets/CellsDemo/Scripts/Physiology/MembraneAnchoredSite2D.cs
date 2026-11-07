using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// A chemical or particle exchange site attached to one material membrane sector.
    /// Reads authoritative soft-body nodes; does not actuate the membrane or
    /// independently animate a pore. For non-soft cells uses the sector contour.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public sealed class MembraneAnchoredSite2D : MonoBehaviour
    {
        public SoftCellBody2D body;
        public MembraneIntegrity membrane;
        [Range(0, MembraneIntegrity.SegmentCount - 1)] public int sector;
        [Min(.01f)] public float legacyRadius = .9f;

        public Vector2 LocalPosition
        {
            get
            {
                if (body && body.Initialized) return body.SurfacePoint(sector);
                return membrane ? membrane.SurfacePoint(sector, legacyRadius)
                    : MembraneIntegrity.DirectionOf(sector) * legacyRadius;
            }
        }

        void Awake()
        {
            if (!body) body = GetComponentInParent<SoftCellBody2D>();
            if (!membrane) membrane = GetComponentInParent<MembraneIntegrity>();
            UpdatePosition();
        }

        void FixedUpdate() => UpdatePosition();
        void LateUpdate() => UpdatePosition();

        public void UpdatePosition()
        {
            var parent = transform.parent;
            if (!parent) return;
            Vector2 local = LocalPosition;
            // The site is parented directly to its organism by the demo builder.
            transform.localPosition = new Vector3(local.x, local.y, transform.localPosition.z);
        }
    }
}
