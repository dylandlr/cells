using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>Resource-paid allocation across neighboring material regions of ONE cell.
    /// Inspired by ERO local tissue growth; this is not a multicellular tissue simulator.
    /// Deposits record allocation history, not a second biomass pool.</summary>
    public sealed class CellDevelopment : MonoBehaviour
    {
        public const int Regions = MembraneIntegrity.SegmentCount;
        readonly float[] deposits = new float[Regions];
        readonly float[] weights = new float[Regions];
        public float AllocatedMass { get; private set; }
        public float DepositAt(int i) => deposits[(i % Regions + Regions) % Regions];

        // Called only after assimilation has paid and transferred nutrient to biomass.
        public void Allocate(float mass)
        {
            if (!float.IsFinite(mass) || mass <= 0f) return;
            var phenotype = GetComponent<OrganismPhenotype>();
            var genome = phenotype ? phenotype.genome : null;
            float bias = genome == null || !float.IsFinite(genome.developmentBias)
                ? 0f : Mathf.Clamp01(genome.developmentBias);
            float axis = genome == null ? 0f : genome.developmentAxis;
            if (!float.IsFinite(axis)) axis = 0f;
            float total = 0f;
            // Read the previous state for all proposals before accepting any deposit.
            for (int i = 0; i < Regions; i++)
            {
                float angle = i * 2f * Mathf.PI / Regions - axis;
                float local = DepositAt(i - 1) + DepositAt(i + 1);
                float reinforcement = AllocatedMass > 0f ? local / AllocatedMass : 0f;
                weights[i] = (1f + bias * .85f * Mathf.Cos(2f * angle))
                    * (1f + bias * reinforcement);
                total += weights[i];
            }
            for (int i = 0; i < Regions; i++) deposits[i] += mass * weights[i] / total;
            AllocatedMass += mass;
        }

        public float RadiusFactor(int i, float biomass)
        {
            if (AllocatedMass <= 0f || !float.IsFinite(biomass) || biomass <= 0f) return 1f;
            float mean = AllocatedMass / Regions;
            float developedFraction = Mathf.Clamp01(AllocatedMass / biomass);
            return Mathf.Sqrt(Mathf.Max(.2f, 1f + developedFraction * (DepositAt(i) / mean - 1f)));
        }
    }
}
