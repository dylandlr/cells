using NUnit.Framework;
using UnityEngine;
using Cells.Fluid;

namespace Cells.Tests
{
    public sealed class FluidGrid2DTests
    {
        GameObject go;
        FluidGrid2D fluid;

        [SetUp]
        public void Setup()
        {
            go = new GameObject("fluid-test");
            fluid = go.AddComponent<FluidGrid2D>();
            fluid.width = 32;
            fluid.height = 24;
            fluid.spacing = .25f;
            fluid.Initialize();
        }

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(go);

        [Test]
        public void FluidStartsAtRest()
        {
            Assert.Less(fluid.Sample(Vector2.zero).sqrMagnitude, 1e-8f);
            Assert.AreEqual(0f, fluid.MeanAbsoluteDivergence(), 1e-6f);
        }

        [Test]
        public void LocalForceCreatesVelocity()
        {
            for(int i=0;i<5;i++)
            {
                fluid.AddForce(Vector2.zero, Vector2.right * 12f);
                fluid.Step(.02f);
            }
            Assert.Greater(fluid.Sample(Vector2.zero).magnitude, 1e-5f);
        }

        [Test]
        public void OutOfBoundsForcingDoesNotDestabilizeGrid()
        {
            fluid.AddForce(new Vector2(999f,999f), new Vector2(100f,100f));
            fluid.Step(.02f);
            Assert.AreEqual(0f, fluid.MeanAbsoluteDivergence(), 1e-6f);
        }

        [Test]
        public void ProjectedFlowHasFiniteDivergence()
        {
            for(int i=0;i<30;i++)
            {
                fluid.AddForce(Vector2.zero, new Vector2(4f,3f));
                fluid.Step(.02f);
            }
            float divergence = fluid.MeanAbsoluteDivergence();
            Assert.IsFalse(float.IsNaN(divergence) || float.IsInfinity(divergence));
            Assert.Less(divergence, 2f);
        }
    }
}
