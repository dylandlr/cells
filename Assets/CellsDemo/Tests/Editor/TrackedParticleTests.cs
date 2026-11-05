#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class TrackedParticleTests
    {
        [Test]
        public void ZeroDiffusionAndDriftPreservesPositionAndMass()
        {
            var go = new GameObject("particles");
            try
            {
                var world = go.AddComponent<TrackedParticleWorld>();
                world.diffusivity = 0;
                world.fluidDrift = Vector2.zero;
                world.Spawn("nutrient-A", Vector2.zero, 3f, .1f);
                world.Step(.2f);
                Assert.That(world.Particles[0].position, Is.EqualTo(Vector2.zero));
                Assert.That(world.TotalMass("nutrient-A"), Is.EqualTo(3f));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void IngestionRemovesPreciselyOneMassPacket()
        {
            var go = new GameObject("particles");
            try
            {
                var world = go.AddComponent<TrackedParticleWorld>();
                world.Spawn("nutrient-A", Vector2.zero, 2f, .1f);
                world.Spawn("toxin-B", Vector2.zero, 4f, .1f);
                Assert.IsTrue(world.TryConsume("nutrient-A",Vector2.zero,.2f,out var p));
                Assert.That(p.mass, Is.EqualTo(2f));
                Assert.That(world.TotalMass("nutrient-A"), Is.EqualTo(0f));
                Assert.That(world.TotalMass("toxin-B"), Is.EqualTo(4f));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
#endif
