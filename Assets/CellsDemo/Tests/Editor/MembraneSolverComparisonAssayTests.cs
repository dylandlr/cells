using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests.Editor
{
    public sealed class MembraneSolverComparisonAssayTests
    {
        [Test]
        public void TwoSolversStartFromIdenticalGeometryAndRecordTraces()
        {
            var report=MembraneSolverComparisonAssay2D.Run(steps:4,dt:.02f);
            Assert.Greater(report.samples.Count,0);
            var first=report.samples[0];
            Assert.AreEqual(0,first.step);
            Assert.IsTrue(first.legacy.valid);
            Assert.IsTrue(first.boundary.valid);
            Assert.AreEqual(first.legacy.absoluteArea,
                first.boundary.absoluteArea,1e-5f);
            Assert.AreEqual(first.legacy.neckRatio,
                first.boundary.neckRatio,1e-5f);
            Assert.AreEqual(first.legacy.perimeter,
                first.boundary.perimeter,1e-5f);
            Assert.IsTrue(report.status=="completed" ||
                report.status=="boundary-rejected" ||
                report.status=="invalid-geometry");
        }
    }
}
