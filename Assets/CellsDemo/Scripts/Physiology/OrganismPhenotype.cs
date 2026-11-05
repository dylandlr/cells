using UnityEngine;
using Cells.Substances;

namespace Cells.Physiology
{
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
            var exchange = GetComponent<CellChemicalExchange>();
            if (exchange) exchange.feedingRate = genome.feedingRate;
            var movement = GetComponent<CellMotility>();
            if (movement) movement.attractantGain = genome.sensorGain;
            var motor = GetComponent<CellFlagellarActuator>();
            if (motor) motor.motorActivation = Mathf.Clamp01(genome.motorInvestment);
        }
    }
}
