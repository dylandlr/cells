using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Local temporal sensing: compare current receptor concentration with a
    /// leaky previous observation, then bias run/tumble behavior. No access to
    /// field gradients or global locations is required.
    /// </summary>
    public sealed class TemporalChemotaxisSensor
    {
        public float adaptationSeconds = 1.5f;
        public float sensitivity = 2f;
        public float baselineTurnRateDegrees = 24f;
        public float unfavorableTurnRateDegrees = 125f;
        float filtered;
        bool initialized;

        public float Response { get; private set; }
        public float TurnRateDegrees { get; private set; }

        public void Reset()
        {
            initialized = false;
            filtered = Response = TurnRateDegrees = 0f;
        }

        public void Tick(float concentration, float dt)
        {
            if (!float.IsFinite(concentration) || !float.IsFinite(dt) || dt <= 0f)
                return;
            if (!initialized)
            {
                filtered = concentration;
                initialized = true;
            }
            float difference = concentration - filtered;
            Response = Mathf.Clamp(difference * Mathf.Max(0f,sensitivity), -1f, 1f);
            float blend = 1f - Mathf.Exp(-dt / Mathf.Max(.01f,adaptationSeconds));
            filtered += difference * blend;
            TurnRateDegrees = Mathf.Max(0f,baselineTurnRateDegrees) +
                Mathf.Max(0f,-Response) * Mathf.Max(0f,unfavorableTurnRateDegrees);
        }
    }
}
