using System;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>Heritable three-node regulatory circuit: nutrient sensor,
    /// internal energy sensor and motor/uptake output. Values are dimensionless.
    /// This is pseudo-genetics, not a molecular nucleus model.</summary>
    [Serializable]
    public sealed class RegulatoryGenome
    {
        [Range(-4f,4f)] public float nutrientToMotor = 1f;
        [Range(-4f,4f)] public float energyToMotor = -1f;
        [Range(-4f,4f)] public float nutrientToUptake = 1f;
        [Range(-4f,4f)] public float energyToUptake = -1f;
        [Range(-4f,4f)] public float motorBias;
        [Range(-4f,4f)] public float uptakeBias;
        [Range(.05f,20f)] public float responseSeconds = 2f;
        public RegulatoryGenome Copy() => (RegulatoryGenome)MemberwiseClone();
    }

    /// <summary>A slowly responding regulatory network, usable without a
    /// membrane-bound nucleus; nucleus-like compartments can later alter capacity.</summary>
    [Serializable]
    public sealed class RegulatoryNetwork
    {
        public float motorOutput { get; private set; }
        public float uptakeOutput { get; private set; }

        static float Logistic(float x) => 1f/(1f+Mathf.Exp(-Mathf.Clamp(x,-20f,20f)));

        public void Tick(RegulatoryGenome genome, float nutrientObservation,
            float energyFraction, float dt)
        {
            if (genome == null || !float.IsFinite(dt) || dt <= 0f) return;
            float nutrient = Mathf.Clamp01(nutrientObservation);
            float energy = Mathf.Clamp01(energyFraction);
            float requestedMotor = Logistic(genome.motorBias + genome.nutrientToMotor*nutrient
                + genome.energyToMotor*energy);
            float requestedUptake = Logistic(genome.uptakeBias + genome.nutrientToUptake*nutrient
                + genome.energyToUptake*energy);
            float fraction = 1f-Mathf.Exp(-dt/Mathf.Max(.05f,genome.responseSeconds));
            motorOutput = Mathf.Lerp(motorOutput,requestedMotor,fraction);
            uptakeOutput = Mathf.Lerp(uptakeOutput,requestedUptake,fraction);
        }

        public void Reset() { motorOutput=0f; uptakeOutput=0f; }
    }
}
