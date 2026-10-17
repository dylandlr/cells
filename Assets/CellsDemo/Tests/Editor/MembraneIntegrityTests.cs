#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class MembraneIntegrityTests
    {
        [Test]
        public void ResistanceReducesDamageAndRepairConsumesEnergy()
        {
            var go = new GameObject("test-cell");
            try
            {
                var metabolism = go.AddComponent<CellMetabolism>();
                metabolism.energy = 2f;
                var membrane = go.AddComponent<MembraneIntegrity>();
                membrane.chemicalResistance = .5f;
                Assert.That(membrane.ApplyExposure(.4f), Is.EqualTo(.2f).Within(.00001f));
                Assert.That(membrane.integrity, Is.EqualTo(.8f).Within(.00001f));
                float restored = membrane.Repair(1f, metabolism);
                Assert.That(restored, Is.EqualTo(.2f).Within(.00001f));
                Assert.That(metabolism.energy, Is.EqualTo(1.4f).Within(.00001f));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
#endif
