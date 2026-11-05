using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests.Editor
{
    public sealed class MembraneComparisonMetricsTests
    {
        [Test]
        public void SamePolygonProducesSameMetricsFromIndependentCopies()
        {
            var nodes=new Vector2[32];
            for(int i=0;i<32;i++)
            {
                float angle=i*2f*Mathf.PI/32f;
                nodes[i]=new Vector2(Mathf.Cos(angle),1.25f*Mathf.Sin(angle));
            }
            var area=Mathf.Abs(new BoundaryLoop2D(nodes).SignedArea);
            var first=MembraneComparisonMetrics2D.Measure(nodes,area);
            var second=MembraneComparisonMetrics2D.Measure((Vector2[])nodes.Clone(),area);
            Assert.IsTrue(first.valid);
            Assert.AreEqual(first.neckRatio,second.neckRatio,1e-6f);
            Assert.AreEqual(first.perimeter,second.perimeter,1e-6f);
            Assert.AreEqual(0f,first.normalizedAreaDrift,1e-6f);
            nodes[0].x=float.NaN;
            Assert.IsFalse(MembraneComparisonMetrics2D.Measure(nodes,area).valid);
        }
    }
}
