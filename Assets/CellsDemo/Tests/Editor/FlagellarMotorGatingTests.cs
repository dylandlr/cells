using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class FlagellarMotorGatingTests
    {
        GameObject organism;
        CellMetabolism metabolism;
        CellFlagellarActuator motor;

        [SetUp]
        public void Setup()
        {
            organism = new GameObject("motor gating assay");
            metabolism = organism.AddComponent<CellMetabolism>();
            metabolism.energy = 20f;
            motor = organism.AddComponent<CellFlagellarActuator>();
            motor.metabolism = metabolism;
            motor.motorActivation = 1f;
            motor.motorCostPerSecond = 1f;
        }

        [TearDown]
        public void Cleanup() { Object.DestroyImmediate(organism); }

        [Test]
        public void TumbleReducesMotorATPAndThrustThenRecovers()
        {
            const float dt=.02f;
            motor.behavioralMotorMultiplier = 1f;
            float initial=metabolism.energy;
            motor.Tick(dt);
            float runningCost=initial-metabolism.energy;
            float runningThrust=motor.EffectiveThrust;

            var tumble = new TumbleMotorState();
            tumble.Begin();
            motor.behavioralMotorMultiplier=tumble.MotorMultiplier;
            float before=metabolism.energy;
            motor.Tick(dt);
            float tumbleCost=before-metabolism.energy;

            Assert.Greater(runningCost,0f);
            Assert.Greater(tumbleCost,0f);
            Assert.AreEqual(runningCost*tumble.tumbleMotorMultiplier,tumbleCost,1e-5f);
            Assert.Less(motor.EffectiveThrust,runningThrust);
            Assert.AreEqual(tumble.tumbleMotorMultiplier,motor.ActualActivation,1e-5f);

            tumble.Advance(1f);
            motor.behavioralMotorMultiplier=tumble.MotorMultiplier;
            motor.Tick(dt);
            Assert.AreEqual(runningThrust,motor.EffectiveThrust,1e-5f);
        }

        [Test]
        public void ZeroMotorCommandHasZeroATPAndZeroThrust()
        {
            motor.behavioralMotorMultiplier=0f;
            float before=metabolism.energy;
            motor.Tick(.02f);
            Assert.AreEqual(before,metabolism.energy,1e-6f);
            Assert.AreEqual(0f,motor.ActualActivation,1e-6f);
            Assert.AreEqual(0f,motor.EffectiveThrust,1e-6f);
        }

        [Test]
        public void InsufficientEnergyCannotCreateFreeThrust()
        {
            metabolism.energy=.001f;
            motor.Tick(.02f);
            Assert.Greater(motor.ActualActivation,0f);
            Assert.Less(motor.ActualActivation,1f);
            Assert.LessOrEqual(motor.EffectiveThrust,motor.ActualActivation+1e-5f);
            Assert.IsFalse(metabolism.alive);
        }
    }
}
