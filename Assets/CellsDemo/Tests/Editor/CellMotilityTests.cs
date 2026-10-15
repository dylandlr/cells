using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class CellMotilityTests
    {
        GameObject root;
        SubstanceDefinition nutrient;
        SubstanceDefinition toxin;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("motility-test");
            nutrient = ScriptableObject.CreateInstance<SubstanceDefinition>();
            nutrient.stableId = "nutrient-A";
            toxin = ScriptableObject.CreateInstance<SubstanceDefinition>();
            toxin.stableId = "toxin-B";
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(nutrient);
            Object.DestroyImmediate(toxin);
        }

        [Test]
        public void NutrientGradientTurnsCellTowardHigherConcentration()
        {
            var fieldGo = new GameObject("field");
            fieldGo.transform.SetParent(root.transform);
            var field = fieldGo.AddComponent<MultiChemicalField2D>();
            field.species = new[] { nutrient, toxin };
            field.Initialize();
            field.Deposit("nutrient-A", new Vector2(1f, 0f), 2f);

            var cell = new GameObject("cell");
            cell.transform.SetParent(root.transform);
            cell.transform.position = Vector2.zero;
            cell.transform.rotation = Quaternion.Euler(0,0,90);
            var metabolism = cell.AddComponent<CellMetabolism>();
            metabolism.energy = 10f;
            var motility = cell.AddComponent<CellMotility>();
            motility.field = field;
            motility.metabolism = metabolism;
            motility.exploratoryBias = 0f;
            motility.drag = 0f;
            motility.sensorOffset = 1f;

            for (int i=0;i<20;i++) motility.Tick(.02f);

            Assert.Greater(cell.transform.right.x, 0f);
            Assert.Greater(motility.Velocity.x, 0f);
        }

        [Test]
        public void MotilityConsumesEnergy()
        {
            var fieldGo = new GameObject("field");
            fieldGo.transform.SetParent(root.transform);
            var field = fieldGo.AddComponent<MultiChemicalField2D>();
            field.species = new[] { nutrient, toxin };
            field.Initialize();
            field.Deposit("nutrient-A", new Vector2(1f, 0f), 2f);

            var cell = new GameObject("cell");
            cell.transform.SetParent(root.transform);
            var metabolism = cell.AddComponent<CellMetabolism>();
            metabolism.energy = 3f;
            var motility = cell.AddComponent<CellMotility>();
            motility.field = field;
            motility.metabolism = metabolism;
            motility.exploratoryBias = 0f;
            motility.sensorOffset = 1f;
            motility.movementCostPerUnit = .5f;

            float before = metabolism.energy;
            for (int i=0;i<25;i++) motility.Tick(.02f);
            Assert.Less(metabolism.energy, before);
        }

        [Test]
        public void ToxinGradientProducesRepellentSteering()
        {
            var fieldGo = new GameObject("field");
            fieldGo.transform.SetParent(root.transform);
            var field = fieldGo.AddComponent<MultiChemicalField2D>();
            field.species = new[] { nutrient, toxin };
            field.Initialize();
            field.Deposit("toxin-B", new Vector2(1f, 0f), 2f);

            var cell = new GameObject("cell");
            cell.transform.SetParent(root.transform);
            var metabolism = cell.AddComponent<CellMetabolism>();
            metabolism.energy = 10f;
            var motility = cell.AddComponent<CellMotility>();
            motility.field = field;
            motility.metabolism = metabolism;
            motility.exploratoryBias = 0f;
            motility.sensorOffset = 1f;

            motility.Tick(.02f);
            Assert.Less(motility.DesiredDirection.x, 0f);
        }
    }
}
