using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class SpatialMembraneTests
    {
        GameObject cell, substrate;
        MembraneIntegrity membrane;
        CellMetabolism metabolism;
        SubstanceDefinition toxin;
        MultiChemicalField2D field;
        CellExposureSystem exposure;

        [SetUp]
        public void Setup()
        {
            cell = new GameObject("cell"); substrate = new GameObject("substrate");
            metabolism = cell.AddComponent<CellMetabolism>();
            membrane = cell.AddComponent<MembraneIntegrity>(); membrane.chemicalResistance = 0f;
            toxin = ScriptableObject.CreateInstance<SubstanceDefinition>(); toxin.stableId = "test-toxin";
            field = substrate.AddComponent<MultiChemicalField2D>();
            field.width = field.height = 16; field.cellSize = .25f;
            field.species = new[] { toxin }; field.Initialize();
            exposure = cell.AddComponent<CellExposureSystem>();
            exposure.metabolism = metabolism; exposure.membrane = membrane;
            exposure.field = field; exposure.substanceId = toxin.stableId;
            exposure.events = substrate.AddComponent<ChemicalEventLog>();
            exposure.uptakeRate = .01f; exposure.damagePerUnit = 2f;
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(cell); Object.DestroyImmediate(substrate); Object.DestroyImmediate(toxin);
        }

        [Test]
        public void LocalLesionCanRuptureMostlyHealthyMembrane()
        {
            float lost = membrane.ApplyExposureToSegment(0, 1f);
            Assert.That(lost, Is.EqualTo(1f / MembraneIntegrity.SegmentCount));
            Assert.That(membrane.GetIntegrity(16), Is.EqualTo(1f));
            Assert.That(membrane.integrity, Is.GreaterThan(.96f));
            Assert.IsTrue(membrane.ruptured);
            Assert.That(membrane.Repair(1f, metabolism), Is.Zero);
            exposure.Tick(.02f);
            Assert.IsFalse(metabolism.alive, "A breached cell must die even without a fresh toxin uptake.");
            exposure.Tick(.02f);
            var events = new List<ChemicalEvent>(); exposure.events.QueryOrganism(0, events);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].kind, Is.EqualTo(ChemicalEventKind.Died));
        }

        [Test]
        public void RepairSpendsEnergyOnDamagedSectorsOnly()
        {
            membrane.ApplyExposureToSegment(0, .4f);
            metabolism.energy = .015f;
            float restored = membrane.Repair(1f, metabolism);
            Assert.That(restored, Is.EqualTo(.005f).Within(1e-6f));
            Assert.That(membrane.GetIntegrity(0), Is.EqualTo(.76f).Within(1e-5f));
            Assert.That(membrane.GetIntegrity(16), Is.EqualTo(1f));
            Assert.That(metabolism.energy, Is.Zero);
        }

        [Test]
        public void DirectionalExposureRespectsCellRotationAndRecordsContactSite()
        {
            cell.transform.rotation = Quaternion.Euler(0, 0, 90);
            Vector2 contact = exposure.SurfacePosition(0);
            field.Deposit(toxin.stableId, contact, .1f);
            exposure.Tick(.1f);
            Assert.That(membrane.GetIntegrity(0), Is.LessThan(1f));
            Assert.That(membrane.GetIntegrity(16), Is.EqualTo(1f));
            var events = new List<ChemicalEvent>(); exposure.events.QueryOrganism(0, events);
            var injury = events.Find(e => e.kind == ChemicalEventKind.Damaged && e.sourceStructure == "membrane.sector[0]");
            Assert.That(Vector2.Distance(injury.location, contact), Is.LessThan(1e-6f));
            float absorbed = 0f;
            foreach (var e in events) if (e.kind == ChemicalEventKind.Absorbed) absorbed += e.quantity;
            Assert.That(field.Total(toxin.stableId) + absorbed, Is.EqualTo(.1f).Within(1e-6f));
        }

        [Test]
        public void DamagedRegionHasGreaterUptakeAndSamplingDoesNotUseCellCenter()
        {
            field.Deposit(toxin.stableId, Vector2.zero, .1f);
            exposure.Tick(.1f);
            Assert.That(membrane.integrity, Is.EqualTo(1f));
            Assert.That(field.Total(toxin.stableId), Is.EqualTo(.1f));
            Vector2 contact = exposure.SurfacePosition(0);
            field.Initialize(); field.Deposit(toxin.stableId, contact, .1f);
            exposure.damagePerUnit = 0f;
            exposure.Tick(.1f);
            float healthyAbsorbed = .1f - field.Total(toxin.stableId);
            field.Initialize(); field.Deposit(toxin.stableId, contact, .1f);
            membrane.ApplyExposureToSegment(0, .5f);
            exposure.Tick(.1f);
            Assert.That(.1f - field.Total(toxin.stableId), Is.GreaterThan(healthyAbsorbed));
        }

        [Test]
        public void UniformDamageAndLegacyScalarResetRemainConsistent()
        {
            membrane.ApplyExposure(.2f);
            for (int i = 0; i < MembraneIntegrity.SegmentCount; i++)
                Assert.That(membrane.GetIntegrity(i), Is.EqualTo(.8f).Within(1e-6f));
            membrane.integrity = .5f;
            Assert.That(membrane.WeakestIntegrity, Is.EqualTo(.5f));
            Assert.That(MembraneIntegrity.SegmentAt(Vector2.right), Is.Zero);
            Assert.That(MembraneIntegrity.SegmentAt(Vector2.up), Is.EqualTo(8));
            Assert.That(MembraneIntegrity.SegmentAt(Vector2.left), Is.EqualTo(16));
        }
    }
}
