using UnityEngine;
using Cells.Physiology;

namespace Cells.Substances
{
    /// <summary>
    /// Physical signaling interface. Emission consumes metabolic energy and adds actual
    /// dissolved signal mass to the chemical grid. Reception is a local concentration
    /// measurement, NOT a global message or a semantic statement about food quality.
    /// All coefficients are provisional simulation units, not calibrated biology.
    /// </summary>
    [RequireComponent(typeof(CellMetabolism))]
    public sealed class CellChemicalSignaling : MonoBehaviour
    {
        public MultiChemicalField2D field;
        public CellMetabolism metabolism;
        public string signalId = "signal-S";
        [Min(0f)] public float energyPerUnitMass = 2f;
        [Min(0f)] public float maximumEmissionPerPulse = .025f;
        [Min(0f)] public float detectionThreshold = .008f;
        [Min(.01f)] public float receptorOffset = .2f;
        public bool demoAutoPulse;
        [Min(.05f)] public float demoPulsePeriod = 2f;

        public float CumulativeEmittedMass { get; private set; }
        public float CumulativeEnergySpent { get; private set; }
        public float LastLocalConcentration { get; private set; }
        public bool LastDetected { get; private set; }
        float elapsed;

        void Awake()
        {
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
        }

        /// <summary>
        /// Try to synthesize and release signal. The returned mass is the actual
        /// quantity deposited, with no energy debit for failed/out-of-domain emission.
        /// </summary>
        public float Emit(float requestedMass)
        {
            if (!field || !metabolism || !metabolism.alive
                || !MultiChemicalField2D.Finite(requestedMass) || requestedMass <= 0f
                || !MultiChemicalField2D.Finite(energyPerUnitMass) || energyPerUnitMass <= 0f
                || !MultiChemicalField2D.Finite(maximumEmissionPerPulse)) return 0f;

            float affordable = Mathf.Min(requestedMass, maximumEmissionPerPulse,
                metabolism.energy / energyPerUnitMass);
            if (affordable <= 0f) return 0f;
            Vector2 point = transform.position;
            // Only deposit before paying. Rejected deposits cannot drain energy.
            float deposited = field.Deposit(signalId, point, affordable);
            if (deposited <= 0f) return 0f;
            float spent = metabolism.Spend(deposited * energyPerUnitMass);
            CumulativeEmittedMass += deposited;
            CumulativeEnergySpent += spent;
            return deposited;
        }

        /// <summary>Local sensed concentration without privileged chemical metadata.</summary>
        public float ReadLocal()
        {
            LastLocalConcentration = field
                ? Mathf.Max(0f, field.Sample(signalId, transform.position))
                : 0f;
            LastDetected = LastLocalConcentration >= detectionThreshold;
            return LastLocalConcentration;
        }

        /// <summary>Centered finite-difference physical gradient; world-space units.</summary>
        public Vector2 ReadGradient()
        {
            if (!field) return Vector2.zero;
            float d = Mathf.Max(.01f, receptorOffset);
            Vector2 p = transform.position;
            return new Vector2(
                (field.Sample(signalId, p + Vector2.right * d)
                    - field.Sample(signalId, p - Vector2.right * d)) / (2f * d),
                (field.Sample(signalId, p + Vector2.up * d)
                    - field.Sample(signalId, p - Vector2.up * d)) / (2f * d));
        }

        void FixedUpdate()
        {
            ReadLocal();
            if (!demoAutoPulse || !metabolism || !metabolism.alive) return;
            elapsed += Time.fixedDeltaTime;
            if (elapsed >= Mathf.Max(.05f, demoPulsePeriod))
            {
                elapsed = 0f;
                Emit(maximumEmissionPerPulse);
            }
        }
    }
}
