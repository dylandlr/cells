#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Cells.Substances;
using Cells.Fluid;

namespace Cells.Tests
{
    public sealed class MultiChemicalFieldTests
    {
        GameObject obj;
        SubstanceDefinition nutrient, toxin;
        MultiChemicalField2D grid;

        [SetUp]
        public void Setup()
        {
            obj = new GameObject("field");
            nutrient = ScriptableObject.CreateInstance<SubstanceDefinition>();
            toxin = ScriptableObject.CreateInstance<SubstanceDefinition>();
            nutrient.stableId = "nutrient-A";
            toxin.stableId = "toxin-B";
            nutrient.diffusionCoefficient = .1f;
            toxin.diffusionCoefficient = .06f;
            grid = obj.AddComponent<MultiChemicalField2D>();
            grid.width = 11; grid.height = 11; grid.cellSize = .5f;
            grid.species = new[] { nutrient, toxin };
            grid.Initialize();
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(obj);
            Object.DestroyImmediate(nutrient);
            Object.DestroyImmediate(toxin);
        }

        [Test]
        public void SeparateChemicalsDoNotIntermix()
        {
            grid.Deposit("toxin-B", Vector2.zero, 10f);
            Assert.AreEqual(0f, grid.Total("nutrient-A"));
            grid.Step(.1f);
            Assert.AreEqual(0f, grid.Total("nutrient-A"));
            Assert.Greater(grid.Total("toxin-B"), 0f);
        }

        [Test]
        public void DiffusionApproximatelyConservesQuantity()
        {
            grid.Deposit("nutrient-A", Vector2.zero, 10f);
            for (int i = 0; i < 100; i++) grid.Step(.02f);
            Assert.That(grid.Total("nutrient-A"), Is.EqualTo(10f).Within(.002f));
        }

        [Test]
        public void ZeroFlowAdvectionLeavesTheFieldUnchanged()
        {
            var fluid = obj.AddComponent<FluidGrid2D>();
            fluid.width = grid.width;
            fluid.height = grid.height;
            fluid.spacing = grid.cellSize;
            fluid.Initialize();
            grid.fluid = fluid;
            grid.Deposit("nutrient-A", Vector2.zero, 7f);
            nutrient.diffusionCoefficient = 0f;
            float beforeCenter = grid.Sample("nutrient-A", Vector2.zero);
            grid.Step(.5f);
            Assert.That(grid.Total("nutrient-A"), Is.EqualTo(7f).Within(.0001f));
            Assert.That(grid.Sample("nutrient-A", Vector2.zero), Is.EqualTo(beforeCenter).Within(.0001f));
        }

        [Test]
        public void FluidAdvectionMovesMassWithoutCreatingOrDestroyingIt()
        {
            var fluid = obj.AddComponent<FluidGrid2D>();
            fluid.width = grid.width;
            fluid.height = grid.height;
            fluid.spacing = grid.cellSize;
            fluid.Initialize();
            grid.fluid = fluid;
            nutrient.diffusionCoefficient = 0f;
            toxin.diffusionCoefficient = 0f;
            grid.Deposit("nutrient-A", Vector2.zero, 8f);
            grid.Deposit("toxin-B", new Vector2(.5f, 0f), 3f);
            for (int n = 0; n < 8; n++)
            {
                fluid.AddForce(Vector2.zero, Vector2.right * 30f);
                fluid.Step(.02f);
                grid.Step(.02f);
            }
            Assert.That(grid.Total("nutrient-A"), Is.EqualTo(8f).Within(.002f));
            Assert.That(grid.Total("toxin-B"), Is.EqualTo(3f).Within(.002f));
            for (int y = 0; y < grid.height; y++)
            for (int x = 0; x < grid.width; x++)
            {
                Assert.GreaterOrEqual(grid.SampleCell("nutrient-A", x, y), -1e-5f);
                Assert.GreaterOrEqual(grid.SampleCell("toxin-B", x, y), -1e-5f);
            }
        }

        [Test]
        public void UptakeCannotWithdrawMoreThanAvailable()
        {
            grid.Deposit("nutrient-A", Vector2.zero, 3f);
            Assert.That(grid.Withdraw("nutrient-A", Vector2.zero, 10f), Is.EqualTo(3f));
            Assert.AreEqual(0f, grid.Total("nutrient-A"));
        }
    }
}
#endif
