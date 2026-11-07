#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class FieldVisualizationTests
    {
        [Test]
        public void SamplingDoesNotChangeSubstanceMass()
        {
            var go = new GameObject("field");
            var substance = ScriptableObject.CreateInstance<SubstanceDefinition>();
            try
            {
                substance.stableId = "test";
                var field = go.AddComponent<MultiChemicalField2D>();
                field.width = 8; field.height = 8;
                field.species = new[] { substance };
                field.Initialize();
                field.Deposit("test", Vector2.zero, 10f);
                float before = field.Total("test");
                for (int i=0;i<64;i++) field.SampleCell("test",i%8,i/8);
                Assert.That(field.Total("test"),Is.EqualTo(before).Within(.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(substance);
            }
        }
    }
}
#endif
