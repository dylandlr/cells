using System;
using UnityEngine;
using Cells.Physiology;
using Cells.Substances;

namespace Cells.Experiments
{
    /// <summary>
    /// Fixed-seed spatial chemical intervention with four cells and real diffusion,
    /// uptake, receptor sensing and metabolic costs. A simplified controller moves
    /// the cells explicitly; this does NOT use the continuous CellMotility/FluidGrid2D.
    /// This is a bridge-validation assay, NOT a biology/learning benchmark.
    /// </summary>
    public static class PhysicalSignalAssayV1
    {
        public const int Agents = 4;
        public const int Steps = 80;
        public const float Dt = .1f;

        public struct Result
        {
            public int Seed;
            public bool Signaling;
            public bool ResponseEnabled;
            public float[] NetEnergy;
            public float[] ConsumedMass;
            public int[] Receptions;
            public float TotalHarvestMass;
            public float SignalMassEmitted;
            public float SignalEnergySpent;
            public float SignalMassRemaining;
            public float SignalMassDecayed;
            public float NutrientMassInitial;
            public float NutrientMassRemaining;
            public int[] FirstDetectionStep;
            public int[] FirstResponseDecisionStep;
            public int[] ResponseEnabledSteps;
            public int[] GradientUsedSteps;
            public float[] PeakReceiverConcentration;
            public float[] FinalSourceDistance;
        }

        public static Result Run(int seed, bool enableSignaling)
        {
            return Run(seed, enableSignaling, true);
        }

        /// <summary>
        /// Separate signal production from the receiver's behavioral response.
        /// Receivers still measure physical concentration when response is disabled.
        /// </summary>
        public static Result Run(int seed, bool enableSignaling, bool enableResponse)
        {
            return RunCalibrated(seed, enableSignaling, enableResponse, .001f, .18f, .13f);
        }

        /// <summary>
        /// Parameterized sensitivity probe. Concentration thresholds and transport
        /// rates are simulation units, not biologically calibrated measurements.
        /// </summary>
        public static Result RunCalibrated(int seed, bool enableSignaling, bool enableResponse,
            float detectionThreshold, float signalDiffusivity, float signalDecayRate)
        {
            if (!MultiChemicalField2D.Finite(detectionThreshold) || detectionThreshold <= 0f ||
                !MultiChemicalField2D.Finite(signalDiffusivity) || signalDiffusivity < 0f ||
                !MultiChemicalField2D.Finite(signalDecayRate) || signalDecayRate < 0f)
                throw new ArgumentOutOfRangeException("signal calibration parameters");

            var rng = new System.Random(seed);
            var root = new GameObject("physical-signal-assay");
            SubstanceDefinition food = null, signal = null;
            try
            {
                var field = root.AddComponent<MultiChemicalField2D>();
                field.width = 24; field.height = 24; field.cellSize = .25f;
                food = ScriptableObject.CreateInstance<SubstanceDefinition>();
                food.stableId = "nutrient-A";
                food.diffusionCoefficient = .04f;
                signal = ScriptableObject.CreateInstance<SubstanceDefinition>();
                signal.stableId = "signal-S";
                signal.classification = SubstanceClass.Signal;
                signal.diffusionCoefficient = signalDiffusivity;
                signal.decayRate = signalDecayRate;
                field.species = new[] { food, signal };
                field.Initialize();

                // Exogenous patch placement is paired by seed between both arms.
                var patch = new Vector2(rng.Next(2) == 0 ? -1f : 1f,
                                        rng.Next(2) == 0 ? -.5f : .5f);
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    field.Deposit("nutrient-A", patch + new Vector2(dx, dy) * .25f, .5f);

                float nutrientInitial = field.Total("nutrient-A");
                var agents = new GameObject[Agents];
                var metabolism = new CellMetabolism[Agents];
                var receptor = new CellChemicalSignaling[Agents];
                var policy = new LocalSignalPolicyV1[Agents];
                var startingEnergy = new float[Agents];
                var consumed = new float[Agents];
                var receptions = new int[Agents];
                var firstDetection = new int[Agents];
                var firstDecision = new int[Agents];
                var responseSteps = new int[Agents];
                var steeringSteps = new int[Agents];
                var peakConcentration = new float[Agents];
                for (int i = 0; i < Agents; i++) { firstDetection[i] = -1; firstDecision[i] = -1; }
                for (int i = 0; i < Agents; i++)
                {
                    agents[i] = new GameObject("assay-cell-" + i);
                    agents[i].transform.SetParent(root.transform, false);
                    agents[i].transform.position = i == 0
                        ? new Vector3(patch.x, patch.y)
                        : new Vector3(-.75f + (i - 1) * .75f, -1.25f);
                    metabolism[i] = agents[i].AddComponent<CellMetabolism>();
                    metabolism[i].energy = 5f;
                    startingEnergy[i] = metabolism[i].energy;
                    receptor[i] = agents[i].AddComponent<CellChemicalSignaling>();
                    receptor[i].field = field;
                    receptor[i].metabolism = metabolism[i];
                    receptor[i].receptorOffset = .35f;
                    receptor[i].detectionThreshold = detectionThreshold;
                    receptor[i].maximumEmissionPerPulse = .09f;
                    policy[i] = agents[i].AddComponent<LocalSignalPolicyV1>();
                    policy[i].metabolism = metabolism[i];
                    policy[i].signaling = receptor[i];
                    policy[i].role = i == 0
                        ? (enableSignaling ? LocalSignalPolicyV1.Role.SignalWhenFed :
                            LocalSignalPolicyV1.Role.Silent)
                        : (enableResponse ? LocalSignalPolicyV1.Role.FollowSignal : LocalSignalPolicyV1.Role.Silent);
                    policy[i].nutrientThreshold = .02f;
                    policy[i].signalPulseMass = .09f;
                    policy[i].minimumEnergy = .2f;
                    // Explicit stepping prevents any dependence on Unity frame scheduling.
                    policy[i].enabled = false;
                    receptor[i].enabled = false;
                    metabolism[i].enabled = false;
                }

                float emitted = 0f, spent = 0f, harvested = 0f;
                for (int t = 0; t < Steps; t++)
                {
                    // Policy observes prior field concentrations before the new step.
                    for (int i = 0; i < Agents; i++)
                    {
                        if (!metabolism[i].alive) continue;
                        if (t % 10 == 0)
                        {
                            policy[i].Decide(1f);
                            if (i == 0)
                            {
                                emitted = receptor[i].CumulativeEmittedMass;
                                spent = receptor[i].CumulativeEnergySpent;
                            }
                        }
                    }

                    // First-order receptor-based discrete movement; no oracle access.
                    for (int i = 1; i < Agents; i++)
                    {
                        if (!metabolism[i].alive) continue;
                        var p = (Vector2)agents[i].transform.position;
                        float d = .25f;
                        Vector2 foodGrad = new Vector2(
                            field.Sample("nutrient-A", p + Vector2.right * d) -
                            field.Sample("nutrient-A", p - Vector2.right * d),
                            field.Sample("nutrient-A", p + Vector2.up * d) -
                            field.Sample("nutrient-A", p - Vector2.up * d));
                        float localSignal = receptor[i].ReadLocal();
                        bool heard = localSignal >= receptor[i].detectionThreshold;
                        peakConcentration[i] = Mathf.Max(peakConcentration[i], localSignal);
                        if (heard)
                        {
                            receptions[i]++;
                            if (firstDetection[i] < 0) firstDetection[i] = t;
                        }
                        // The policy decides on a fixed cadence. Receptor detection
                        // alone cannot automatically activate signal-guided movement.
                        // Keep physical exposure metrics independent of decisions.
                        bool policyFollowing = policy[i].FollowEnabled && enableResponse;
                        if (policyFollowing)
                        {
                            responseSteps[i]++;
                            if (firstDecision[i] < 0) firstDecision[i] = t;
                        }
                        Vector2 signalSteering = policyFollowing
                            ? receptor[i].ReadGradient() * .5f : Vector2.zero;
                        if (signalSteering.sqrMagnitude > 1e-9f) steeringSteps[i]++;
                        Vector2 gradient = foodGrad + signalSteering;
                        if (gradient.sqrMagnitude > 1e-9f)
                        {
                            Vector2 shift = gradient.normalized * .025f;
                            float cost = shift.magnitude * .05f;
                            if (metabolism[i].energy > cost)
                            {
                                metabolism[i].Spend(cost);
                                agents[i].transform.position = p + shift;
                            }
                        }
                    }

                    for (int i = 0; i < Agents; i++)
                    {
                        if (!metabolism[i].alive) continue;
                        float mass = field.Withdraw("nutrient-A", agents[i].transform.position, .003f);
                        metabolism[i].Feed(mass * 8f);
                        consumed[i] += mass;
                        harvested += mass;
                        metabolism[i].Tick(Dt);
                    }
                    field.Step(Dt);
                }

                var finalDistances = new float[Agents];
                for (int i = 1; i < Agents; i++)
                    finalDistances[i] = Vector2.Distance(agents[i].transform.position,
                        agents[0].transform.position);
                var returns = new float[Agents];
                for (int i = 0; i < Agents; i++)
                    returns[i] = metabolism[i].energy - startingEnergy[i];
                return new Result
                {
                    Seed = seed, Signaling = enableSignaling,
                    ResponseEnabled = enableResponse,
                    NetEnergy = returns, ConsumedMass = consumed, Receptions = receptions,
                    TotalHarvestMass = harvested, SignalMassEmitted = emitted,
                    SignalEnergySpent = spent,
                    SignalMassRemaining = field.Total("signal-S"),
                    SignalMassDecayed = field.DecayedMass,
                    NutrientMassInitial = nutrientInitial,
                    NutrientMassRemaining = field.Total("nutrient-A"),
                    FirstDetectionStep = firstDetection,
                    FirstResponseDecisionStep = firstDecision,
                    ResponseEnabledSteps = responseSteps,
                    GradientUsedSteps = steeringSteps,
                    PeakReceiverConcentration = peakConcentration,
                    FinalSourceDistance = finalDistances
                };
            }
            finally
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(root);
                    if (food) UnityEngine.Object.Destroy(food);
                    if (signal) UnityEngine.Object.Destroy(signal);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    if (food) UnityEngine.Object.DestroyImmediate(food);
                    if (signal) UnityEngine.Object.DestroyImmediate(signal);
                }
            }
        }
    }
}
