using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class TemporalChemotaxisSensorTests
    {
        [Test]
        public void SteadyReceptorInputAdaptsToZeroResponse()
        {
            var sensor = new TemporalChemotaxisSensor();
            for(int i=0;i<200;i++) sensor.Tick(1f,.02f);
            Assert.AreEqual(0f,sensor.Response,1e-5f);
        }

        [Test]
        public void ImprovingConditionsReduceTurningRelativeToWorseningConditions()
        {
            var improving = new TemporalChemotaxisSensor();
            var worsening = new TemporalChemotaxisSensor();
            improving.Tick(1f,.02f);
            worsening.Tick(1f,.02f);
            improving.Tick(2f,.02f);
            worsening.Tick(0f,.02f);
            Assert.Greater(improving.Response,0f);
            Assert.Less(worsening.Response,0f);
            Assert.Less(improving.TurnRateDegrees,worsening.TurnRateDegrees);
        }

        [Test]
        public void InvalidSamplesDoNotCorruptState()
        {
            var sensor = new TemporalChemotaxisSensor();
            sensor.Tick(1f,.02f);
            sensor.Tick(float.NaN,.02f);
            sensor.Tick(2f,0f);
            Assert.IsTrue(float.IsFinite(sensor.Response));
            Assert.IsTrue(float.IsFinite(sensor.TurnRateDegrees));
        }
    }
}
