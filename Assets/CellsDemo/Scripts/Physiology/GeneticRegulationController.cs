using UnityEngine;
using Cells.Substances;

namespace Cells.Physiology
{
    /// <summary>Genome-derived regulator that only modulates transport and motor
    /// activation. It cannot change position, inject resources, or bypass physics.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class GeneticRegulationController : MonoBehaviour
    {
        public RegulatoryGenome inherited = new RegulatoryGenome();
        public RegulatoryNetwork state = new RegulatoryNetwork();
        public CellMetabolism metabolism;
        public CellChemicalExchange exchange;
        public CellFlagellarActuator motor;
        public MultiChemicalField2D field;
        [Min(.001f)] public float referenceEnergy = 10f;
        [Min(0f)] public float referenceFeedingRate = .2f;
        [Range(0f,1f)] public float inheritedMotorCapacity = 1f;
        public string sensedNutrient = "nutrient-A";

        void Awake()
        {
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!exchange) exchange = GetComponent<CellChemicalExchange>();
            if (!motor) motor = GetComponent<CellFlagellarActuator>();
            if (!field && exchange) field = exchange.field;
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);
        public void Tick(float dt)
        {
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!exchange) exchange = GetComponent<CellChemicalExchange>();
            if (!motor) motor = GetComponent<CellFlagellarActuator>();
            if (!field && exchange) field = exchange.field;
            if (!metabolism || !metabolism.alive)
            {
                if (motor) motor.motorActivation = 0f;
                return;
            }
            float nutrient = field ? Mathf.Max(0f,field.Sample(sensedNutrient,transform.position)) : 0f;
            state.Tick(inherited,nutrient,metabolism.energy/Mathf.Max(.001f,referenceEnergy),dt);
            if (motor) motor.motorActivation = inheritedMotorCapacity*state.motorOutput;
            if (exchange) exchange.feedingRate = referenceFeedingRate*state.uptakeOutput;
        }
    }
}
