using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests.Editor
{
    public sealed class MembraneConvergenceSweepTests
    {
        [Test]
        public void FreshTrialsProduceThreeIndependentFiniteReports()
        {
            var report=MembraneConvergenceSweep2D.Run(duration=.4f);
            Assert.AreEqual(3,report.trials.Count);
            Assert.AreEqual(.02f,report.trials[0].timestep,1e-6f);
            Assert.AreEqual(.01f,report.trials[1].timestep,1e-6f);
            Assert.AreEqual(.005f,report.trials[2].timestep,1e-6f);
            Assert.IsTrue(float.IsFinite(report.maximumFinalRatioDifference));
            Assert.IsTrue(float.IsFinite(report.maximumFinalAreaRelativeDifference));
            foreach(var trial in report.trials)
            {
                Assert.Greater(trial.finalArea,0f);
                Assert.IsTrue(float.IsFinite(trial.finalRatio));
            }
        }
    }
}
