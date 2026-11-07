using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests.Editor
{
    public sealed class BoundaryConstraintSolver2DTests
    {
        static BoundaryLoop2D Square() => new BoundaryLoop2D(new [] {
            new Vector2(-1,-1),new Vector2(1,-1),
            new Vector2(1,1),new Vector2(-1,1)
        });

        [Test]
        public void PerturbedBoundaryMovesTowardRestGeometry()
        {
            var solver=new BoundaryConstraintSolver2D(Square());
            solver.positions[1]+=new Vector2(.3f,.15f);
            float before=Mathf.Abs(new BoundaryLoop2D(solver.positions).SignedArea-4f);
            Assert.IsTrue(solver.Step(20,.55f,.55f));
            float after=Mathf.Abs(new BoundaryLoop2D(solver.positions).SignedArea-4f);
            Assert.Less(after,before);
            Assert.Greater(new BoundaryLoop2D(solver.positions).Perimeter,0f);
            Assert.IsTrue(new BoundaryLoop2D(solver.positions).Valid);
        }

        [Test]
        public void InvalidSettingsDoNotAdvance()
        {
            var solver=new BoundaryConstraintSolver2D(Square());
            Vector2 original=solver.positions[0];
            Assert.IsFalse(solver.Step(0));
            Assert.AreEqual(original,solver.positions[0]);
        }
    }
}
