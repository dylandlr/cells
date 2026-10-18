using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;
using Cells.Demo;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class ChemotaxisTrialRecorderTests
    {
        GameObject cell;
        CellMetabolism metabolism;
        CellFlagellarActuator motor;
        CellMotility motility;
        ChemotaxisTrialRecorder recorder;

        [SetUp]
        public void Setup()
        {
            cell = new GameObject("recorded swimmer");
            metabolism = cell.AddComponent<CellMetabolism>();
            metabolism.energy = 10f;
            motor = cell.AddComponent<CellFlagellarActuator>();
            motor.metabolism = metabolism;
            motility = cell.AddComponent<CellMotility>();
            motility.metabolism = metabolism;
            motility.flagellum = motor;
            motility.exploratoryBias = 0f;
            recorder = cell.AddComponent<ChemotaxisTrialRecorder>();
            recorder.motor = motor;
            recorder.metabolism = metabolism;
            recorder.motility = motility;
            recorder.samplePeriod = 100f;
            recorder.BeginTrial();
        }

        [TearDown] public void Cleanup() { Object.DestroyImmediate(cell); }

        [Test]
        public void RecorderTracksDistanceAndMotorEnergyIndependently()
        {
            const float dt=.02f;
            float before = metabolism.energy;
            motor.Tick(dt);
            motility.Tick(dt);
            recorder.RecordStep(dt);
            Assert.AreEqual(dt,recorder.ElapsedSeconds,1e-6f);
            Assert.AreEqual(1,recorder.RecordedSteps);
            Assert.AreEqual(motility.LastDisplacementVelocity.magnitude*dt,recorder.PathLength,1e-5f);
            Assert.AreEqual(motor.CumulativeMotorEnergySpent,recorder.MotorEnergySpent,1e-6f);
            Assert.AreEqual(metabolism.energy-before,recorder.LastEnergy-recorder.InitialEnergy,1e-5f);
            Assert.IsTrue(recorder.Alive);
        }

        [Test]
        public void UptakeAndDigestionAreSeparateCounters()
        {
            var fieldGo = new GameObject("nutrient field");
            fieldGo.transform.SetParent(cell.transform);
            var field = fieldGo.AddComponent<MultiChemicalField2D>();
            var food = ScriptableObject.CreateInstance<SubstanceDefinition>();
            food.stableId = "nutrient-A";
            var waste = ScriptableObject.CreateInstance<SubstanceDefinition>();
            waste.stableId = "metabolic-waste-E";
            try
            {
                field.species = new[] { food, waste };
                field.Initialize();
                field.Deposit("nutrient-A", cell.transform.position, 2f);
                var exchange = cell.AddComponent<CellChemicalExchange>();
                exchange.metabolism = metabolism;
                exchange.field = field;
                exchange.secretionEnabled = false;
                exchange.feedingEnabled = true;
                exchange.feedingRate = 1f;
                exchange.digestionRate = .5f;
                recorder.exchange = exchange;
                recorder.field = field;
                recorder.BeginTrial();
                exchange.Tick(.2f);
                Assert.Greater(recorder.NutrientAbsorbed,0f);
                Assert.GreaterOrEqual(recorder.NutrientDigested,0f);
                Assert.LessOrEqual(recorder.NutrientDigested,recorder.NutrientAbsorbed+1e-6f);
                recorder.BeginTrial();
                Assert.AreEqual(0f,recorder.NutrientAbsorbed,1e-6f);
                Assert.AreEqual(0f,recorder.NutrientDigested,1e-6f);
            }
            finally
            {
                Object.DestroyImmediate(food);
                Object.DestroyImmediate(waste);
            }
        }

        [Test]
        public void RestartEstablishesFreshMotorBaseline()
        {
            motor.Tick(.02f);
            recorder.BeginTrial();
            Assert.AreEqual(0,recorder.RecordedSteps);
            Assert.AreEqual(0f,recorder.MotorEnergySpent,1e-7f);
            motor.Tick(.02f);
            Assert.Greater(recorder.MotorEnergySpent,0f);
        }
    }
}
