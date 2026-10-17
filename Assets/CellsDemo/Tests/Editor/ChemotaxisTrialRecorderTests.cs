using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;
using Cells.Demo;

namespace Cells.Tests
{
    public sealed class ChemotaxisTrialRecorderTests
    {
        GameObject cell;
        CellMetabolism metabolism;
        CellFlagellarActuator motor;
        CellMotility motility;
        ChemotaxisTrialRecorder recorder;

        [SetUp]
        public void Setup()
        {
            cell = new GameObject("recorded swimmer");
            metabolism = cell.AddComponent<CellMetabolism>();
            metabolism.energy = 10f;
            motor = cell.AddComponent<CellFlagellarActuator>();
            motor.metabolism = metabolism;
            motility = cell.AddComponent<CellMotility>();
            motility.metabolism = metabolism;
            motility.flagellum = motor;
            motility.exploratoryBias = 0f;
            recorder = cell.AddComponent<ChemotaxisTrialRecorder>();
            recorder.motor = motor;
            recorder.metabolism = metabolism;
            recorder.motility = motility;
            recorder.samplePeriod = 100f;
            recorder.BeginTrial();
        }

        [TearDown] public void Cleanup() { Object.DestroyImmediate(cell); }

        [Test]
        public void RecorderTracksDistanceAndMotorEnergyIndependently()
        {
            const float dt=.02f;
            float before = metabolism.energy;
            motor.Tick(dt);
            motility.Tick(dt);
            recorder.RecordStep(dt);
            Assert.AreEqual(dt,recorder.ElapsedSeconds,1e-6f);
            Assert.AreEqual(motility.LastDisplacementVelocity.magnitude*dt,recorder.PathLength,1e-5f);
            Assert.AreEqual(motor.CumulativeMotorEnergySpent,recorder.MotorEnergySpent,1e-6f);
            Assert.AreEqual(metabolism.energy-before,recorder.LastEnergy-recorder.InitialEnergy,1e-5f);
            Assert.IsTrue(recorder.Alive);
        }

        [Test]
        public void RestartEstablishesFreshMotorBaseline()
        {
            motor.Tick(.02f);
            recorder.BeginTrial();
            Assert.AreEqual(0f,recorder.MotorEnergySpent,1e-7f);
            motor.Tick(.02f);
            Assert.Greater(recorder.MotorEnergySpent,0f);
        }
    }
}
