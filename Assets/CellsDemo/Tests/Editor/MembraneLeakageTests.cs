using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class MembraneLeakageTests
    {
        GameObject cell, substrate;
        SubstanceDefinition nutrient;
        MultiChemicalField2D field;
        CellMetabolism metabolism;
        MembraneIntegrity membrane;
        CellChemicalExchange exchange;
        MembraneLeakageSystem leakage;

        [SetUp]
        public void Setup()
        {
            cell = new GameObject("cell"); substrate = new GameObject("field");
            nutrient = ScriptableObject.CreateInstance<SubstanceDefinition>(); nutrient.stableId = "nutrient-A";
            field = substrate.AddComponent<MultiChemicalField2D>(); field.width = field.height = 16;
            field.species = new[] { nutrient }; field.Initialize();
            metabolism = cell.AddComponent<CellMetabolism>();
            membrane = cell.AddComponent<MembraneIntegrity>(); membrane.chemicalResistance = 0f;
            exchange = cell.AddComponent<CellChemicalExchange>();
            exchange.metabolism = metabolism; exchange.field = field;
            exchange.secretionEnabled = exchange.feedingEnabled = false;
            exchange.nutrientReserve = 1f;
            exchange.events = substrate.AddComponent<ChemicalEventLog>();
            leakage = cell.AddComponent<MembraneLeakageSystem>();
            leakage.exchange = exchange; leakage.membrane = membrane;
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(cell); Object.DestroyImmediate(substrate); Object.DestroyImmediate(nutrient);
        }

        [Test]
        public void IntactMembraneRetainsNutrients()
        {
            leakage.Tick(10f);
            Assert.That(exchange.nutrientReserve, Is.EqualTo(1f));
            Assert.That(field.Total(nutrient.stableId), Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LeakTransfersActualMassAndNeverChangesEnergy(bool dead)
        {
            membrane.ApplyExposureToSegment(0, .5f);
            if (dead) metabolism.Injure(metabolism.energy);
            float energy = metabolism.energy;
            leakage.Tick(.5f);
            Assert.That(field.Total(nutrient.stableId), Is.GreaterThan(0f));
            Assert.That(exchange.nutrientReserve + field.Total(nutrient.stableId), Is.EqualTo(1f).Within(1e-6f));
            Assert.That(metabolism.energy, Is.EqualTo(energy));
            Assert.That(metabolism.alive, Is.EqualTo(!dead));
            var history = new List<ChemicalEvent>(); exchange.events.QueryOrganism(0, history);
            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(history[0].kind, Is.EqualTo(ChemicalEventKind.Leaked));
            Assert.That(history[0].quantity, Is.EqualTo(leakage.TotalLeaked));
            Assert.That(history[0].sourceStructure, Is.EqualTo("membrane.sector[0]"));
        }

        [Test]
        public void ConcentratedExternalNutrientStopsOutwardLeak()
        {
            membrane.ApplyExposureToSegment(0, 1f);
            field.Deposit(nutrient.stableId, Vector2.right * leakage.membraneRadius, 1f);
            leakage.Tick(10f);
            Assert.That(exchange.nutrientReserve, Is.EqualTo(1f));
            Assert.That(leakage.TotalLeaked, Is.Zero);
        }

        [Test]
        public void LargeStepApproachesContactEquilibriumWithoutOvershoot()
        {
            membrane.ApplyExposureToSegment(0, 1f);
            leakage.Tick(10000f);
            float internalArea = Mathf.PI * leakage.membraneRadius * leakage.membraneRadius;
            float outside = field.Sample(nutrient.stableId, Vector2.right * leakage.membraneRadius);
            Assert.That(exchange.nutrientReserve / internalArea, Is.EqualTo(outside).Within(1e-5f));
            Assert.That(exchange.nutrientReserve, Is.GreaterThan(0f));
            Assert.That(exchange.nutrientReserve + field.Total(nutrient.stableId), Is.EqualTo(1f).Within(1e-6f));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RejectedDepositCannotDestroyInternalMass(bool outsideDomain)
        {
            membrane.ApplyExposure(1f);
            if (outsideDomain) cell.transform.position = Vector3.right * 100f;
            else exchange.nutrientId = "missing";
            leakage.Tick(1f);
            Assert.That(exchange.nutrientReserve, Is.EqualTo(1f));
            Assert.That(leakage.TotalLeaked, Is.Zero);
        }

        [Test]
        public void DiffusionAndLeakageConserveCombinedMassOverManySteps()
        {
            nutrient.diffusionCoefficient = .1f;
            membrane.ApplyExposure(.6f);
            for (int i = 0; i < 500; i++) { leakage.Tick(.02f); field.Step(.02f); }
            Assert.That(leakage.TotalLeaked, Is.GreaterThan(.01f));
            Assert.That(exchange.nutrientReserve, Is.GreaterThanOrEqualTo(0f));
            Assert.That(exchange.nutrientReserve + field.Total(nutrient.stableId), Is.EqualTo(1f).Within(1e-4f));
        }

        [TestCase("missing")]
        [TestCase("nutrient-A")]
        public void InvalidDigestionProductCannotConsumeFoodOrProduceEnergy(string product)
        {
            exchange.digestionProductId = product;
            float energy = metabolism.energy;
            exchange.Tick(1f);
            Assert.That(exchange.nutrientReserve, Is.EqualTo(1f));
            Assert.That(metabolism.energy, Is.EqualTo(energy));
        }

        [Test]
        public void ParticleIngestionRequiresReserveAndTransfersMassBeforeDigestion()
        {
            var world = substrate.AddComponent<TrackedParticleWorld>();
            world.Spawn("food", Vector2.zero, 2f, .1f);
            var feeder = cell.AddComponent<ParticleFeedingSite>();
            feeder.world = world; feeder.metabolism = metabolism; feeder.edibleSubstanceId = "food";
            feeder.exchange = null;
            feeder.Tick();
            Assert.That(world.TotalMass("food"), Is.EqualTo(2f));
            float energy = metabolism.energy;
            feeder.exchange = exchange;
            feeder.Tick();
            Assert.That(world.TotalMass("food"), Is.Zero);
            Assert.That(exchange.nutrientReserve, Is.EqualTo(3f));
            Assert.That(metabolism.energy, Is.EqualTo(energy));
        }
    }
}
