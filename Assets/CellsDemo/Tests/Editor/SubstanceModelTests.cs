#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class SubstanceModelTests
    {
        [Test]
        public void ResistanceChangesExposureOutcome()
        {
            var vulnerable = new ExposureProfile {
                dose = 2f, uptakePerSecond = 1f,
                membraneResistance = 0f, toxicityCoefficient = 2f
            };
            var resistant = vulnerable;
            resistant.membraneResistance = 1f;
            Assert.Less(vulnerable.NetEnergyRate(), resistant.NetEnergyRate());
            Assert.AreEqual(HazardGlyph.Skull,
                HazardAnnotationPolicy.ForTarget(vulnerable, 3f, 1f, true));
            Assert.AreEqual(HazardGlyph.None,
                HazardAnnotationPolicy.ForTarget(resistant, 3f, 1f, true));
        }

        [Test]
        public void UnverifiedToxicityMustNeverShowCertainSkull()
        {
            var exposure = new ExposureProfile {
                dose = 100f, uptakePerSecond = 1f, toxicityCoefficient = 99f
            };
            Assert.AreEqual(HazardGlyph.Unknown,
                HazardAnnotationPolicy.ForTarget(exposure, 1f, 5f, false));
        }

        [Test]
        public void AgentObservationContainsOnlyReceptorValues()
        {
            var observation = AgentChemicalSensor.Sample(
                new Vector3(1f, 2f, 3f), Vector2.right,
                new Vector3(1f, .5f, 0f), 1f, .25f);
            Assert.AreEqual(1f, observation.receptorA);
            Assert.AreEqual(1f, observation.receptorB);
            Assert.AreEqual(0f, observation.receptorC);
            Assert.AreEqual(1f, observation.gradientX);
            Assert.IsNull(typeof(ChemicalObservation).GetField("displayName"));
            Assert.IsNull(typeof(ChemicalObservation).GetField("glyphId"));
        }
    }
}
#endif
