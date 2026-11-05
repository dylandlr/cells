using NUnit.Framework;
using UnityEngine;
using Cells.Experiments;

namespace Cells.Tests
{
    public sealed class SeededNutrientPatchesTests
    {
        [Test]
        public void EqualSeedsProduceIdenticalPositions()
        {
            var a=SeededNutrientPatches.Positions(631);
            var b=SeededNutrientPatches.Positions(631);
            Assert.AreEqual(SeededNutrientPatches.Count,a.Length);
            for(int i=0;i<a.Length;i++)
            {
                Assert.AreEqual(a[i].x,b[i].x,0f);
                Assert.AreEqual(a[i].y,b[i].y,0f);
            }
        }

        [Test]
        public void DifferentSeedsVaryTheEnvironment()
        {
            var a=SeededNutrientPatches.Positions(631);
            var b=SeededNutrientPatches.Positions(632);
            bool anyDifferent=false;
            for(int i=0;i<a.Length;i++)
                if((a[i]-b[i]).sqrMagnitude>1e-8f) anyDifferent=true;
            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void EveryPatchLiesInsideTheDeclaredAssayRegion()
        {
            var a=SeededNutrientPatches.Positions(633);
            foreach(var p in a)
            {
                Assert.GreaterOrEqual(p.x,-3.5f);
                Assert.LessOrEqual(p.x,3.5f);
                Assert.GreaterOrEqual(p.y,-2.5f);
                Assert.LessOrEqual(p.y,2.5f);
            }
        }
    }
}
