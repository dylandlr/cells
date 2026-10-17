#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Cells.Substances;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class CellChemicalSignalingTests
    {
        GameObject world, cell;
        SubstanceDefinition signal;
        MultiChemicalField2D field;
        CellMetabolism metabolism;
        CellChemicalSignaling signaling;

        [SetUp]
        public void SetUp()
        {
            world = new GameObject("chemical world");
            signal = ScriptableObject.CreateInstance<SubstanceDefinition>();
            signal.stableId = "signal-S";
            signal.classification = SubstanceClass.Signal;
            signal.diffusionCoefficient = .1f;
            field = world.AddComponent<MultiChemicalField2D>();
            field.width = 15;
            field.height = 15;
            field.cellSize = .5f;
            field.species = new[] { signal };
            field.Initialize();

            cell = new GameObject("signaler");
            metabolism = cell.AddComponent<CellMetabolism>();
            metabolism.energy = 2f;
            signaling = cell.AddComponent<CellChemicalSignaling>();
            signaling.field = field;
            signaling.metabolism = metabolism;
            signaling.energyPerUnitMass = 2f;
            signaling.maximumEmissionPerPulse = .5f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cell);
            Object.DestroyImmediate(world);
            Object.DestroyImmediate(signal);
        }

        [Test]
        public void EmissionCreatesChemicalMassAndDebitsEnergy()
        {
            float mass = signaling.Emit(.25f);
            Assert.That(mass, Is.EqualTo(.25f).Within(1e-6f));
            Assert.That(field.Total("signal-S"), Is.EqualTo(.25f).Within(1e-6f));
            Assert.That(metabolism.energy, Is.EqualTo(1.5f).Within(1e-6f));
            Assert.That(signaling.CumulativeEnergySpent, Is.EqualTo(.5f).Within(1e-6f));
        }

        [Test]
        public void OutOfBoundsEmissionCannotSpendEnergy()
        {
            cell.transform.position = new Vector3(999f, 999f);
            Assert.That(signaling.Emit(.25f), Is.EqualTo(0f));
            Assert.That(metabolism.energy, Is.EqualTo(2f).Within(1e-6f));
            Assert.That(field.Total("signal-S"), Is.EqualTo(0f).Within(1e-6f));
        }

        [Test]
        public void InvalidOrUnknownSubstanceEmissionCannotSpendEnergy()
        {
            signaling.signalId = "missing-substance";
            Assert.That(signaling.Emit(.25f), Is.EqualTo(0f));
            signaling.signalId = "signal-S";
            Assert.That(signaling.Emit(float.NaN), Is.EqualTo(0f));
            Assert.That(metabolism.energy, Is.EqualTo(2f).Within(1e-6f));
        }

        [Test]
        public void CannotEmitMoreThanAvailableEnergyPermits()
        {
            metabolism.energy = .2f;
            Assert.That(signaling.Emit(.5f), Is.EqualTo(.1f).Within(1e-6f));
            Assert.That(metabolism.energy, Is.EqualTo(0f).Within(1e-6f));
            Assert.IsFalse(metabolism.alive);
            Assert.That(field.Total("signal-S"), Is.EqualTo(.1f).Within(1e-6f));
        }

        [Test]
        public void ChemicalSignalDiffusesAndReceiverReadsLocalConcentration()
        {
            Assert.That(signaling.Emit(.3f), Is.EqualTo(.3f).Within(1e-6f));
            var receiver = new GameObject("receiver");
            try
            {
                receiver.transform.position = new Vector3(.5f, 0f);
                var reception = receiver.AddComponent<CellMetabolism>();
                var sensor = receiver.AddComponent<CellChemicalSignaling>();
                sensor.metabolism = reception;
                sensor.field = field;
                sensor.detectionThreshold = .0001f;
                Assert.That(sensor.ReadLocal(), Is.EqualTo(0f));
                field.Step(.1f);
                Assert.Greater(sensor.ReadLocal(), 0f);
                Assert.IsTrue(sensor.LastDetected);
                Assert.That(field.Total("signal-S"), Is.EqualTo(.3f).Within(.0001f));
            }
            finally
            {
                Object.DestroyImmediate(receiver);
            }
        }

        [Test]
        public void EmissionLimitApplies()
        {
            Assert.That(signaling.Emit(100f), Is.EqualTo(.5f).Within(1e-6f));
            Assert.That(field.Total("signal-S"), Is.EqualTo(.5f).Within(1e-6f));
        }
    }
}
#endif
