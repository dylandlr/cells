using UnityEngine;
using Cells.Physiology;
using Cells.Substances;

namespace Cells.Demo
{
    /// <summary>
    /// Observational chemotaxis trial recorder. Samples actual post-boundary
    /// displacement and separate authoritative motor ATP meter.
    /// Nutrient exposure is NOT nutrient uptake; record it separately.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class ChemotaxisTrialRecorder : MonoBehaviour
    {
        public CellMotility motility;
        public CellMetabolism metabolism;
        public CellFlagellarActuator motor;
        public CellChemicalExchange exchange;
        public MultiChemicalField2D field;
        public string controllerLabel = "unconfigured";
        public string nutrientId = "nutrient-A";
        public float samplePeriod = .2f;

        public float ElapsedSeconds { get; private set; }
        public float PathLength { get; private set; }
        public float CumulativeNutrientExposure { get; private set; }
        public float InitialEnergy { get; private set; }
        public float LastEnergy { get; private set; }
        public float MotorEnergySpent => motor ? motor.CumulativeMotorEnergySpent - initialMotorEnergy : 0f;
        public float NutrientAbsorbed => exchange ? exchange.CumulativeNutrientAbsorbed - initialAbsorbed : 0f;
        public float NutrientDigested => exchange ? exchange.CumulativeNutrientDigested - initialDigested : 0f;
        public int TumbleCount => motility ? motility.TumbleCount : 0;
        public bool Alive => metabolism && metabolism.alive;
        float initialMotorEnergy;
        float initialAbsorbed;
        float initialDigested;
        float nextReport;
        bool started;

        void Awake()
        {
            if (!motility) motility = GetComponent<CellMotility>();
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!motor) motor = GetComponent<CellFlagellarActuator>();
            if (!exchange) exchange = GetComponent<CellChemicalExchange>();
            if (!field && motility) field = motility.field;
        }

        public void BeginTrial()
        {
            if (!motility) motility = GetComponent<CellMotility>();
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!motor) motor = GetComponent<CellFlagellarActuator>();
            if (!field && motility) field = motility.field;
            ElapsedSeconds = PathLength = CumulativeNutrientExposure = 0f;
            InitialEnergy = metabolism ? metabolism.energy : 0f;
            LastEnergy = InitialEnergy;
            initialMotorEnergy = motor ? motor.CumulativeMotorEnergySpent : 0f;
            initialAbsorbed = exchange ? exchange.CumulativeNutrientAbsorbed : 0f;
            initialDigested = exchange ? exchange.CumulativeNutrientDigested : 0f;
            nextReport = Mathf.Max(.01f,samplePeriod);
            started = true;
        }

        void Start() { if (!started) BeginTrial(); }
        void FixedUpdate() => RecordStep(Time.fixedDeltaTime);

        public void RecordStep(float dt)
        {
            if (!started) BeginTrial();
            if (!motility || dt <= 0f || !float.IsFinite(dt)) return;
            ElapsedSeconds += dt;
            PathLength += motility.LastDisplacementVelocity.magnitude * dt;
            if (field)
                CumulativeNutrientExposure += Mathf.Max(0f,field.Sample(nutrientId,transform.position))*dt;
            LastEnergy = metabolism ? metabolism.energy : 0f;
            if (ElapsedSeconds >= nextReport)
            {
                // Single-line structured output can be filtered by CELL_TRIAL.
                Debug.Log("CELL_TRIAL " + JsonUtility.ToJson(new TrialSnapshot {
                    controller = controllerLabel,
                    elapsedSeconds = ElapsedSeconds,
                    distance = PathLength,
                    nutrientExposure = CumulativeNutrientExposure,
                    totalEnergyChange = LastEnergy-InitialEnergy,
                    motorEnergySpent = MotorEnergySpent,
                    nutrientAbsorbed = NutrientAbsorbed,
                    nutrientDigested = NutrientDigested,
                    tumbles = TumbleCount,
                    alive = Alive
                }));
                nextReport += Mathf.Max(.01f,samplePeriod);
            }
        }

        [System.Serializable]
        public struct TrialSnapshot
        {
            public string controller;
            public float elapsedSeconds;
            public float distance;
            public float nutrientExposure;
            public float totalEnergyChange;
            public float motorEnergySpent;
            public float nutrientAbsorbed;
            public float nutrientDigested;
            public int tumbles;
            public bool alive;
        }
    }
}
