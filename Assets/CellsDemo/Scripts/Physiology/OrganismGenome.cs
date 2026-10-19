using System;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>First quantitative hereditary trait set. No claim of nucleotide-level genetics.</summary>
    [Serializable]
    public sealed class OrganismGenome
    {
        [Range(.005f, .5f)] public float assimilationRate = .03f;
        [Range(.01f, 2f)] public float feedingRate = .2f;
        [Range(.1f, 5f)] public float growthEnergyPerMass = .5f;
        [Range(.1f, 4f)] public float maximumBiomass = 2f;
        [Range(.05f, 3f)] public float sensorGain = 1f;
        [Range(.05f, 3f)] public float motorInvestment = 1f;

        public OrganismGenome Copy() => (OrganismGenome)MemberwiseClone();

        // A zero mutation probability provides an exact-copy control.
        public OrganismGenome Inherit(System.Random random, double mutationProbability,
            double relativeMutationSigma)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (mutationProbability < 0 || mutationProbability > 1 || !double.IsFinite(mutationProbability))
                throw new ArgumentOutOfRangeException(nameof(mutationProbability));
            if (relativeMutationSigma < 0 || !double.IsFinite(relativeMutationSigma))
                throw new ArgumentOutOfRangeException(nameof(relativeMutationSigma));
            var child = Copy();
            child.assimilationRate = Mutate(random, child.assimilationRate, mutationProbability, relativeMutationSigma, .005f, .5f);
            child.feedingRate = Mutate(random, child.feedingRate, mutationProbability, relativeMutationSigma, .01f, 2f);
            child.growthEnergyPerMass = Mutate(random, child.growthEnergyPerMass, mutationProbability, relativeMutationSigma, .1f, 5f);
            child.maximumBiomass = Mutate(random, child.maximumBiomass, mutationProbability, relativeMutationSigma, .1f, 4f);
            child.sensorGain = Mutate(random, child.sensorGain, mutationProbability, relativeMutationSigma, .05f, 3f);
            child.motorInvestment = Mutate(random, child.motorInvestment, mutationProbability, relativeMutationSigma, .05f, 3f);
            return child;
        }

        static float Mutate(System.Random random, float value, double probability,
            double sigma, float lower, float upper)
        {
            if (random.NextDouble() >= probability || sigma == 0) return value;
            // Box-Muller; bounded log-normal multiplicative mutation.
            double u1 = Math.Max(1e-12, random.NextDouble());
            double u2 = random.NextDouble();
            double gaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            double shifted = value * Math.Exp(Math.Clamp(gaussian * sigma, -12.0, 12.0));
            return Mathf.Clamp((float)shifted, lower, upper);
        }
    }

    /// <summary>Applies inherited parameters to living physiology, not to the world state.</summary>
    public sealed class OrganismPhenotype : MonoBehaviour
    {
        public OrganismGenome genome = new OrganismGenome();

        public void Express()
        {
            if (genome == null) return;
            var growth = GetComponent<CellBiomassGrowth>();
            if (growth)
            {
                growth.assimilationRate = genome.assimilationRate;
                growth.growthEnergyPerMass = genome.growthEnergyPerMass;
                growth.maximumBiomass = genome.maximumBiomass;
            }
            var movement = GetComponent<CellMotility>();
            if (movement) movement.attractantGain = genome.sensorGain;
            var motor = GetComponent<CellFlagellarActuator>();
            if (motor) motor.motorActivation = Mathf.Clamp01(genome.motorInvestment);
        }
    }
}
