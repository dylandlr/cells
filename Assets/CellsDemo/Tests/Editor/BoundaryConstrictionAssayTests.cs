using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests.Editor
{
    public sealed class BoundaryConstrictionAssayTests
    {
        [Test]
        public void RepeatedConstrictionEmitsOrderedGeometryMeasurements()
        {
            var report=BoundaryConstrictionAssay2D.Run(steps:8);
            Assert.AreEqual(0,report.observations[0].iteration);
            Assert.Greater(report.observations.Count,1);
            Assert.LessOrEqual(report.completedSteps,8);
            Assert.IsTrue(report.status=="completed" ||
                report.status=="solver-rejected" ||
                report.status=="invalid-geometry");
            for(int i=0;i<report.observations.Count;i++)
            {
                var measurement=report.observations[i];
                Assert.AreEqual(i,measurement.iteration);
                Assert.IsTrue(float.IsFinite(measurement.neckWidth));
                Assert.IsTrue(float.IsFinite(measurement.signedArea));
                Assert.Greater(measurement.shoulderWidth,0f);
            }
        }
    }
}
