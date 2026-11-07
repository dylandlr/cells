using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests.Editor
{
    public sealed class BoundaryConstrictionTests
    {
        static BoundaryLoop2D Ellipse()
        {
            var points=new Vector2[32];
            for(int i=0;i<points.Length;i++)
            {
                float angle=2f*Mathf.PI*i/points.Length;
                points[i]=new Vector2(Mathf.Cos(angle),1.35f*Mathf.Sin(angle));
            }
            return new BoundaryLoop2D(points);
        }

        [Test]
        public void BoundedConstrictionPreservesSimpleLoop()
        {
            var solver=new BoundaryConstraintSolver2D(Ellipse());
            var baseline=new BoundaryLoop2D(solver.positions);
            Assert.IsTrue(solver.TryConstrict(.3f,.40f,.04f));
            var after=new BoundaryLoop2D(solver.positions);
            Assert.IsTrue(after.Valid);
            Assert.AreEqual(Mathf.Sign(baseline.SignedArea),
                Mathf.Sign(after.SignedArea));
            Assert.IsTrue(float.IsFinite(solver.RelativeAreaError));
            Assert.Less(solver.RelativeAreaError,.15f);
        }

        [Test]
        public void InvalidActuationIsRejectedWithoutMutation()
        {
            var solver=new BoundaryConstraintSolver2D(Ellipse());
            var original=(Vector2[])solver.positions.Clone();
            Assert.IsFalse(solver.TryConstrict(float.NaN,.4f));
            Assert.IsFalse(solver.TryConstrict(.2f,0f));
            for(int i=0;i<original.Length;i++)
                Assert.AreEqual(original[i],solver.positions[i]);
        }
    }
}
