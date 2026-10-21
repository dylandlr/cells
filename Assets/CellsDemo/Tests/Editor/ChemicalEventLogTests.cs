#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class ChemicalEventLogTests
    {
        [Test]
        public void LogPreservesSequenceAndOrganismProvenance()
        {
            var go = new GameObject("log");
            try
            {
                var log = go.AddComponent<ChemicalEventLog>();
                log.Record(1f, 11, "toxin-B", ChemicalEventKind.Secreted, .1f, Vector2.zero, "pore");
                log.Record(2f, 12, "nutrient-A", ChemicalEventKind.Absorbed, .2f, Vector2.one, "mouth");
                log.Record(3f, 11, "toxin-B", ChemicalEventKind.Secreted, .3f, Vector2.right, "pore");
                var results = new List<ChemicalEvent>();
                log.QueryOrganism(11, results);
                Assert.AreEqual(2, results.Count);
                Assert.Less(results[0].sequence, results[1].sequence);
                Assert.AreEqual("toxin-B", results[1].substanceId);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
#endif
