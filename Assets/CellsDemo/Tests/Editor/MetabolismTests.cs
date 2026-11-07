#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class MetabolismTests
    {
        [Test]
        public void DamageCanKillButCannotCreateNegativeEnergy()
        {
            var go = new GameObject("metabolism");
            try
            {
                var m = go.AddComponent<CellMetabolism>();
                m.energy = 2f;
                m.Injure(3f);
                Assert.AreEqual(0f, m.energy);
                Assert.IsFalse(m.alive);
                m.Feed(100f);
                Assert.AreEqual(0f, m.energy);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void MetabolicSpendingIsBoundedByReserves()
        {
            var go = new GameObject("metabolism");
            try
            {
                var m = go.AddComponent<CellMetabolism>();
                m.energy = .25f;
                Assert.That(m.Spend(2f), Is.EqualTo(.25f));
                Assert.That(m.energy, Is.EqualTo(0f));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
#endif
