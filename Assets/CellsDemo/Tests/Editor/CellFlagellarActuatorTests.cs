using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class CellFlagellarActuatorTests
    {
        GameObject cell;
        CellMetabolism metabolism;
        CellFlagellarActuator actuator;

        [SetUp]
        public void Setup()
        {
            cell = new GameObject("flagellum");
            metabolism = cell.AddComponent<CellMetabolism>();
            metabolism.energy = 5f;
            actuator = cell.AddComponent<CellFlagellarActuator>();
            actuator.metabolism = metabolism;
        }

        [TearDown] public void Cleanup() => Object.DestroyImmediate(cell);

        [Test]
        public void ActiveBeatAdvancesPhaseAndConsumesEnergy()
        {
            float before = metabolism.energy;
            actuator.Tick(.02f);
            Assert.Greater(actuator.PhaseRadians, 0f);
            Assert.Greater(actuator.EffectiveThrust, 0f);
            Assert.Less(metabolism.energy, before);
        }

        [Test]
        public void DamagedMotorHasNoPropulsion()
        {
            actuator.structuralIntegrity = 0f;
            actuator.Tick(.02f);
            Assert.AreEqual(0f, actuator.EffectiveThrust);
            Assert.AreEqual(0f, actuator.ActualActivation);
        }

        [Test]
        public void BeatDeformsFlagellumButMaintainsBaseAttachment()
        {
            Vector2 baseBefore = actuator.SampleCenterline(0f);
            Vector2 tipBefore = actuator.SampleCenterline(1f);
            actuator.Tick(.05f);
            Vector2 baseAfter = actuator.SampleCenterline(0f);
            Vector2 tipAfter = actuator.SampleCenterline(1f);
            Assert.Less((baseAfter - baseBefore).sqrMagnitude, 1e-8f);
            Assert.Greater((tipAfter - tipBefore).sqrMagnitude, 1e-8f);
        }

        [Test]
        public void DepletedEnergyStopsMotor()
        {
            metabolism.energy = 0f;
            metabolism.alive = false;
            actuator.Tick(.02f);
            Assert.AreEqual(0f, actuator.EffectiveThrust);
        }
    }
}
