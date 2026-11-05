using System;
using UnityEngine;

namespace Cells.Substances
{
    /// <summary>Only organism-accessible chemical observations. No names, icons, classes or harm labels.</summary>
    [Serializable]
    public struct ChemicalObservation
    {
        public float receptorA;
        public float receptorB;
        public float receptorC;
        public float gradientX;
        public float gradientY;
        public float internalStress;
    }

    /// <summary>
    /// A phenotype-dependent receptor translation. The world supplies local chemical amplitudes;
    /// the cell gets only receptor activations and a local signal direction.
    /// </summary>
    public static class AgentChemicalSensor
    {
        public static ChemicalObservation Sample(
            Vector3 concentrations, Vector2 localGradient, Vector3 receptorAffinity,
            float sensorGain, float stress, float noise = 0f)
        {
            var sensed = Vector3.Scale(concentrations, receptorAffinity)
                         * Mathf.Clamp01(sensorGain);
            // Keep noise injected by the simulator; never use rendering classifications.
            float n = Mathf.Max(0f, noise);
            return new ChemicalObservation
            {
                receptorA = Mathf.Max(0f, sensed.x + n),
                receptorB = Mathf.Max(0f, sensed.y + n),
                receptorC = Mathf.Max(0f, sensed.z + n),
                gradientX = Mathf.Clamp(localGradient.x, -1f, 1f),
                gradientY = Mathf.Clamp(localGradient.y, -1f, 1f),
                internalStress = Mathf.Clamp01(stress)
            };
        }
    }
}
