using UnityEngine;
using Cells.Physiology;

namespace Cells.Rendering
{
    /// <summary>Color/shape cue backed by the membrane integrity scalar. Spatial rupture remains future work.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MembraneStateRenderer : MonoBehaviour
    {
        public MembraneIntegrity membrane;
        public Color healthy = new Color(.51f,.91f,.79f);
        public Color injured = new Color(1f,.46f,.3f);
        public float damagedPulseHz = 2f;
        SpriteRenderer sr;
        Vector3 initialScale;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (!membrane) membrane = GetComponent<MembraneIntegrity>();
            initialScale = transform.localScale;
        }

        void LateUpdate()
        {
            if (!membrane) return;
            float injury = 1f - membrane.integrity;
            sr.color = Color.Lerp(healthy,injured,injury);
            float tremor = injury * .05f * Mathf.Sin(Time.time*damagedPulseHz*Mathf.PI*2f);
            transform.localScale = initialScale * (1f+tremor);
            if (membrane.ruptured) sr.color = injured * .6f;
        }
    }
}
