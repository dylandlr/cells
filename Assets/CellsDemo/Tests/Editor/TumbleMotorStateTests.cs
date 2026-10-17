using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class TumbleMotorStateTests
    {
        [Test]
        public void TumbleTemporarilyReducesMotorCommand()
        {
            var state = new TumbleMotorState();
            Assert.AreEqual(1f,state.MotorMultiplier,1e-6f);
            state.Begin();
            Assert.IsTrue(state.IsTumbling);
            Assert.Less(state.MotorMultiplier,1f);
            state.Advance(.1f);
            Assert.IsTrue(state.IsTumbling);
            state.Advance(.2f);
            Assert.IsFalse(state.IsTumbling);
            Assert.AreEqual(1f,state.MotorMultiplier,1e-6f);
        }

        [Test]
        public void RestartAndResetHaveDefinedBehavior()
        {
            var state = new TumbleMotorState();
            state.tumbleDurationSeconds=.4f;
            state.Begin();
            state.Advance(.3f);
            state.Begin();
            Assert.AreEqual(.4f,state.RemainingSeconds,1e-6f);
            state.Reset();
            Assert.AreEqual(1f,state.MotorMultiplier,1e-6f);
            Assert.IsFalse(state.IsTumbling);
        }

        [Test]
        public void InvalidTimeDoesNotEndTumble()
        {
            var state = new TumbleMotorState();
            state.Begin();
            float remaining = state.RemainingSeconds;
            state.Advance(float.NaN);
            state.Advance(-1f);
            Assert.AreEqual(remaining,state.RemainingSeconds,1e-6f);
        }
    }
}
