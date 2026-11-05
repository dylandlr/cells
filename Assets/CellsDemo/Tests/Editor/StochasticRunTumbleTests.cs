using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class StochasticRunTumbleTests
    {
        [Test]
        public void SameSeedReproducesIdenticalTumbleSequence()
        {
            var a=new StochasticRunTumble(631);
            var b=new StochasticRunTumble(631);
            for(int i=0;i<2000;i++)
            {
                bool aa=a.Step(-.7,.02,out double ra);
                bool bb=b.Step(-.7,.02,out double rb);
                Assert.AreEqual(aa,bb);
                Assert.AreEqual(ra,rb,1e-12);
            }
            Assert.Greater(a.TumbleCount,0);
        }

        [Test]
        public void WorseningInputRaisesInstantaneousHazard()
        {
            var sensor=new StochasticRunTumble(31);
            sensor.Step(1,.02,out _);
            double favorable=sensor.LastHazard;
            sensor.Step(-1,.02,out _);
            Assert.Greater(sensor.LastHazard,favorable);
        }

        [Test]
        public void ZeroHazardNeverTumbles()
        {
            var sensor=new StochasticRunTumble(7);
            sensor.baselineRate=0;
            sensor.unfavorableRateGain=0;
            for(int i=0;i<1000;i++)
                Assert.IsFalse(sensor.Step(-1,.02,out _));
            Assert.AreEqual(0,sensor.TumbleCount);
        }

        [Test]
        public void InvalidTimeDoesNotGenerateTumbles()
        {
            var sensor=new StochasticRunTumble(7);
            Assert.IsFalse(sensor.Step(-1,0,out _));
            Assert.IsFalse(sensor.Step(double.NaN,.02,out _));
            Assert.AreEqual(0,sensor.TumbleCount);
        }
    }
}
