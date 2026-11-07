#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class LocalSignalPolicyV1Tests
    {
        GameObject world, organism;
        SubstanceDefinition nutrient, signal;
        MultiChemicalField2D field;
        CellMetabolism energy;
        CellChemicalSignaling signaling;
        LocalSignalPolicyV1 policy;

        [SetUp]
        public void Setup()
        {
            world = new GameObject("environment");
            nutrient = ScriptableObject.CreateInstance<SubstanceDefinition>();
            nutrient.stableId = "nutrient-A";
            signal = ScriptableObject.CreateInstance<SubstanceDefinition>();
            signal.stableId = "signal-S";
            field = world.AddComponent<MultiChemicalField2D>();
            field.width = 16; field.height = 16; field.cellSize = .5f;
            field.species = new[] { nutrient, signal };
            field.Initialize();
            organism = new GameObject("agent");
            energy = organism.AddComponent<CellMetabolism>();
            energy.energy = 10f;
            signaling = organism.AddComponent<CellChemicalSignaling>();
            signaling.field = field;
            signaling.metabolism = energy;
            signaling.maximumEmissionPerPulse = .2f;
            policy = organism.AddComponent<LocalSignalPolicyV1>();
            policy.metabolism = energy;
            policy.signaling = signaling;
            policy.role = LocalSignalPolicyV1.Role.SignalWhenFed;
            policy.nutrientThreshold = 1f;
            policy.signalPulseMass = .1f;
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(organism);
            Object.DestroyImmediate(world);
            Object.DestroyImmediate(nutrient);
            Object.DestroyImmediate(signal);
        }

        [Test]
        public void DoesNotSignalWithoutLocallyDetectedNutrients()
        {
            policy.Decide(1f);
            Assert.AreEqual(0, policy.SuccessfulSignals);
            Assert.AreEqual(0f, field.Total("signal-S"), 1e-6f);
        }

        [Test]
        public void EmitsOnlyAfterPhysicalLocalNutrientDetection()
        {
            field.Deposit("nutrient-A", Vector2.zero, 1f);
            policy.Decide(1f);
            Assert.AreEqual(1, policy.SuccessfulSignals);
            Assert.That(field.Total("signal-S"), Is.EqualTo(.1f).Within(1e-5f));
            Assert.That(energy.energy, Is.EqualTo(9.8f).Within(1e-5f));
        }

        [Test]
        public void SilentRoleNeverEmitsEvenWithNutrients()
        {
            policy.role = LocalSignalPolicyV1.Role.Silent;
            field.Deposit("nutrient-A", Vector2.zero, 1f);
            policy.Decide(1f);
            Assert.AreEqual(0, policy.SuccessfulSignals);
        }

        [Test]
        public void ReceiverFollowsOnlyWhenSignalExceedsThreshold()
        {
            var motility = organism.AddComponent<CellMotility>();
            motility.metabolism = energy;
            policy.motility = motility;
            policy.role = LocalSignalPolicyV1.Role.FollowSignal;
            signaling.detectionThreshold = .25f;
            policy.Decide(1f);
            Assert.IsFalse(policy.FollowEnabled);
            Assert.AreEqual(0f, motility.signalAttractantGain);
            field.Deposit("signal-S", Vector2.zero, .1f);
            policy.Decide(1f);
            Assert.IsTrue(policy.FollowEnabled);
            Assert.AreEqual(policy.signalChemotaxisGain, motility.signalAttractantGain);
        }

        [Test]
        public void EnergyThresholdPreventsEmission()
        {
            field.Deposit("nutrient-A", Vector2.zero, 1f);
            policy.minimumEnergy = 11f;
            policy.Decide(1f);
            Assert.AreEqual(0f, field.Total("signal-S"), 1e-6f);
        }
    }
}
#endif
