using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Cells.Substances;
using Cells.Physiology;
using Object = UnityEngine.Object;

namespace Cells.Tests
{
    public sealed class ChemistryIntegrationTests
    {
        GameObject root, cell;
        SubstanceDefinition a, b, product;
        MultiChemicalField2D field;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("chemistry test");
            cell = new GameObject("cell");
            a = ScriptableObject.CreateInstance<SubstanceDefinition>(); a.stableId = "a";
            b = ScriptableObject.CreateInstance<SubstanceDefinition>(); b.stableId = "b";
            product = ScriptableObject.CreateInstance<SubstanceDefinition>(); product.stableId = "p";
            field = root.AddComponent<MultiChemicalField2D>();
            field.width = field.height = 5; field.cellSize = .5f;
            field.species = new[] { a, b, product }; field.Initialize();
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(cell); Object.DestroyImmediate(root);
            Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(product);
        }

        [Test]
        public void ConcentrationUsesVoxelAreaAndOutsideSamplesDoNotClampToEdge()
        {
            field.Deposit("a", Vector2.zero, 2f);
            Assert.That(field.Sample("a", Vector2.zero), Is.EqualTo(8f));
            Assert.That(field.Deposit("a", new Vector2(100, 0), 1f), Is.Zero);
            Assert.That(field.Sample("a", new Vector2(100, 0)), Is.Zero);
            Assert.That(field.Withdraw("a", new Vector2(100, 0), 10f), Is.Zero);
            Assert.That(field.Deposit("a", Vector2.zero, float.NaN), Is.Zero);
            Assert.That(field.Total("a"), Is.EqualTo(2f));
        }

        [Test]
        public void DecayIsExponentialAndAccountedFor()
        {
            a.decayRate = .7f; a.diffusionCoefficient = 3f;
            field.Deposit("a", Vector2.zero, 4f); field.Step(1f);
            Assert.That(field.Total("a"), Is.EqualTo(4f * Mathf.Exp(-.7f)).Within(1e-5f));
            Assert.That(field.Total("a") + field.DecayedMass, Is.EqualTo(4f).Within(1e-5f));
        }

        [TestCase(1f, 1f, 1f)]
        [TestCase(1f, 2f, 100f)]
        [TestCase(2f, 1f, 100f)]
        public void NeutralizationConservesMassAndNeverOverdraws(float left, float right, float dt)
        {
            field.Deposit("a", Vector2.zero, left); field.Deposit("b", Vector2.zero, right);
            float extent = field.React("a", "b", "p", .5f, dt);
            Assert.That(extent, Is.GreaterThan(0f).And.LessThanOrEqualTo(Mathf.Min(left, right)));
            Assert.That(field.Total("p"), Is.EqualTo(2f * extent).Within(1e-5f));
            Assert.That(field.Total("a") + field.Total("b") + field.Total("p"), Is.EqualTo(left + right).Within(1e-5f));
            Assert.That(field.Total("a"), Is.GreaterThanOrEqualTo(0f));
            Assert.That(field.Total("b"), Is.GreaterThanOrEqualTo(0f));
            if (left == right) Assert.That(field.Total("a"), Is.EqualTo(1f / 3f).Within(1e-5f));
        }

        [Test]
        public void ReactionIsLocalAndMissingProductCannotConsumeReactants()
        {
            field.Deposit("a", new Vector2(-.5f, 0), 1f);
            field.Deposit("b", new Vector2(.5f, 0), 1f);
            Assert.That(field.React("a", "b", "p", 10f, 10f), Is.Zero);
            field.Deposit("b", new Vector2(-.5f, 0), 1f);
            Assert.That(field.React("a", "b", "missing", 10f, 10f), Is.Zero);
            Assert.That(field.Total("a"), Is.EqualTo(1f));
        }

        [Test]
        public void ReactionIntegrationIsIndependentOfTimePartition()
        {
            field.Deposit("a", Vector2.zero, 1f); field.Deposit("b", Vector2.zero, 2f);
            field.React("a", "b", "p", .3f, 1f);
            float expected = field.Total("p");
            field.Initialize(); field.Deposit("a", Vector2.zero, 1f); field.Deposit("b", Vector2.zero, 2f);
            for (int i = 0; i < 10; i++) field.React("a", "b", "p", .3f, .1f);
            Assert.That(field.Total("p"), Is.EqualTo(expected).Within(1e-5f));
        }

        [Test]
        public void DissolvedFeedingTransfersMassBeforeEnergyAndSecretionPaysItsCost()
        {
            var metabolism = cell.AddComponent<CellMetabolism>(); metabolism.energy = 5f;
            var exchange = cell.AddComponent<CellChemicalExchange>(); exchange.field = field;
            exchange.metabolism = metabolism;
            exchange.nutrientId = "a"; exchange.toxinId = "b";
            exchange.feedingRate = 1f; exchange.digestionRate = .5f; exchange.usableEnergyPerMass = 2f;
            exchange.toxinReserve = 2f; exchange.toxinRate = .5f; exchange.secretionEnergyCost = 1f;
            field.Deposit("a", Vector2.zero, 1f); exchange.Tick(1f);
            Assert.That(field.Total("a"), Is.Zero);
            Assert.That(exchange.nutrientReserve, Is.EqualTo(.5f));
            Assert.That(exchange.toxinReserve + field.Total("b"), Is.EqualTo(2f));
            Assert.That(metabolism.energy, Is.EqualTo(5.5f));
        }

        [Test]
        public void ExposureDamagesRupturesKillsAndLogsOnlyOneDeath()
        {
            var metabolism = cell.AddComponent<CellMetabolism>();
            var membrane = cell.AddComponent<MembraneIntegrity>(); membrane.chemicalResistance = 0f;
            var exposure = cell.AddComponent<CellExposureSystem>();
            exposure.metabolism = metabolism; exposure.membrane = membrane;
            exposure.membraneRadius = 0f;
            exposure.field = field; exposure.substanceId = "a"; exposure.uptakeRate = .1f;
            exposure.damagePerUnit = 20f; exposure.events = root.AddComponent<ChemicalEventLog>();
            field.Deposit("a", Vector2.zero, 1f); exposure.Tick(1f); exposure.Tick(1f);
            Assert.That(membrane.integrity, Is.Zero); Assert.IsFalse(metabolism.alive);
            Assert.That(field.Total("a"), Is.EqualTo(.6f).Within(1e-5f));
            Assert.That(membrane.Repair(1f, metabolism), Is.Zero);
            var log = new List<ChemicalEvent>(); exposure.events.QueryOrganism(0, log);
            Assert.That(log.FindAll(e => e.kind == ChemicalEventKind.Died).Count, Is.EqualTo(1));
        }

        [Test]
        public void ParticleResetReplaysTheSameBrownianTrajectory()
        {
            var particles = root.AddComponent<TrackedParticleWorld>();
            particles.ResetSimulation(123); particles.Spawn("a", Vector2.zero, 1f, .1f); particles.Step(.2f);
            var first = particles.Particles[0].position;
            particles.ResetSimulation(123); particles.Spawn("a", Vector2.zero, 1f, .1f); particles.Step(.2f);
            Assert.That(particles.Particles[0].position, Is.EqualTo(first));
        }

        [Test]
        public void RegistryCanBeInitializedAfterRuntimeDefinitionsAreAssigned()
        {
            var registry = root.AddComponent<SubstanceRegistry>();
            registry.definitions = field.species; registry.Initialize();
            Assert.IsTrue(registry.TryGet("a", out var definition)); Assert.AreSame(a, definition);
        }
    }
}
