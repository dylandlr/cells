using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class CellMotorPolicyPortTests
    {
        GameObject cell;
        [TearDown] public void Cleanup() => Object.DestroyImmediate(cell);

        [Test]
        public void ActionsClampAndInvalidOutputStopsMotor()
        {
            cell = new GameObject("policy");
            var port = cell.AddComponent<CellMotorPolicyPort>();
            var motor = cell.GetComponent<CellFlagellarActuator>();
            port.ApplyMotorAction(2f);
            Assert.AreEqual(1f, motor.motorActivation);
            port.ApplyMotorAction(float.NaN);
            motor.Tick(.02f);
            Assert.AreEqual(0f, motor.ActualActivation);
            Assert.AreEqual(0f, motor.CumulativeMotorEnergySpent);
            cell.GetComponent<CellMetabolism>().alive = false;
            port.ApplyMotorAction(1f);
            Assert.AreEqual(0f, motor.motorActivation);
        }

        [Test]
        public void PolicyCannotUseFallbackPropulsionOrScriptedTurning()
        {
            cell = new GameObject("policy");
            var port = cell.AddComponent<CellMotorPolicyPort>();
            var motility = cell.GetComponent<CellMotility>();
            motility.flagellum = cell.GetComponent<CellFlagellarActuator>();
            motility.exploratoryBias = 1f;
            cell.transform.rotation = Quaternion.Euler(0, 0, 90);
            port.ApplyMotorAction(1f);
            motility.flagellum.Tick(.02f);
            motility.Tick(.02f);
            Assert.AreEqual(Vector2.zero, motility.LastActiveVelocity);
            Assert.That(Mathf.DeltaAngle(90f, cell.transform.eulerAngles.z), Is.EqualTo(0f).Within(.001f));
        }

        [Test]
        public void ObservationsAreFiniteAndDisablingRestoresBaseline()
        {
            cell = new GameObject("policy");
            var motor = cell.AddComponent<CellFlagellarActuator>();
            motor.motorActivation = .6f;
            var port = cell.AddComponent<CellMotorPolicyPort>();
            cell.GetComponent<CellMetabolism>().energy = float.NaN;
            var values = new float[CellMotorPolicyPort.ObservationCount];
            port.ReadObservations(values);
            foreach (float value in values) Assert.That(value, Is.InRange(0f, 1f));
            port.enabled = false;
            Assert.IsFalse(cell.GetComponent<CellMotility>().externalMotorControl);
            Assert.AreEqual(.6f, motor.motorActivation);
        }
    }
}
