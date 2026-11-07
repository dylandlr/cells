using UnityEngine;
using Cells.Substances;

namespace Cells.Physiology
{
    /// <summary>
    /// Versioned scripted baseline connecting physical sensing to actions.
    /// It cannot read global chemical totals, hidden organism state or map labels.
    /// The emission decision depends on the agent's OWN local nutrient receptor
    /// and energy, not on truth about other patches.
    /// </summary>
    [RequireComponent(typeof(CellMetabolism), typeof(CellChemicalSignaling))]
    public sealed class LocalSignalPolicyV1 : MonoBehaviour
    {
        public enum Role { Silent, SignalWhenFed, FollowSignal }
        public Role role;
        public CellMetabolism metabolism;
        public CellChemicalSignaling signaling;
        public CellMotility motility;
        [Min(0f)] public float nutrientThreshold = .12f;
        [Min(0f)] public float minimumEnergy = 1f;
        [Min(0.05f)] public float decisionInterval = 1f;
        [Min(0f)] public float signalPulseMass = .03f;
        [Min(0f)] public float signalChemotaxisGain = 2f;
        public string nutrientId = "nutrient-A";
        public int Decisions { get; private set; }
        public int SuccessfulSignals { get; private set; }
        public float LastNutrientConcentration { get; private set; }
        public bool FollowEnabled { get; private set; }
        float elapsed;

        void Awake()
        {
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!signaling) signaling = GetComponent<CellChemicalSignaling>();
            if (!motility) motility = GetComponent<CellMotility>();
        }

        void OnDisable()
        {
            if (motility) motility.signalAttractantGain = 0f;
        }

        void FixedUpdate()
        {
            elapsed += Time.fixedDeltaTime;
            if (elapsed < Mathf.Max(.05f, decisionInterval)) return;
            float dt = elapsed;
            elapsed = 0f;
            Decide(dt);
        }

        /// <summary>Explicit decision tick for deterministic edit-mode tests.</summary>
        public void Decide(float elapsedSeconds)
        {
            if (!(elapsedSeconds > 0f) || !MultiChemicalField2D.Finite(elapsedSeconds)) return;
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!signaling) signaling = GetComponent<CellChemicalSignaling>();
            if (!motility) motility = GetComponent<CellMotility>();
            if (!metabolism || !metabolism.alive || !signaling) return;

            Decisions++;
            // Food quality inferred solely from the locally sampled nutrient field.
            LastNutrientConcentration = signaling.field
                ? signaling.field.Sample(nutrientId, transform.position) : 0f;
            float signalLevel = signaling.ReadLocal();
            FollowEnabled = role == Role.FollowSignal &&
                signalLevel >= signaling.detectionThreshold;

            if (motility)
            {
                motility.signaling = signaling;
                motility.signalAttractantGain = FollowEnabled ? signalChemotaxisGain : 0f;
            }

            if (role == Role.SignalWhenFed &&
                LastNutrientConcentration >= nutrientThreshold &&
                metabolism.energy >= minimumEnergy)
            {
                if (signaling.Emit(signalPulseMass) > 0f) SuccessfulSignals++;
            }
        }
    }
}
