using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Cells.Physiology;
using Cells.Experiments;

namespace Cells.Demo
{
    /// <summary>
    /// Sequential Play Mode runner. Fresh generated field/cell hierarchy each trial;
    /// the results CSV is written only for trials which actually finished.
    /// Attach to an empty object in a dedicated assay scene, not ChemistryDemo.
    /// </summary>
    public sealed class ChemotaxisBatchRunner : MonoBehaviour
    {
        public bool runOnStart = true;
        [Min(1)] public int replicates = 5;
        public int firstSeed = 631;
        [Min(.1f)] public float horizonSeconds = 30f;
        public string outputFile = "chemotaxis-outcomes-v1.csv";
        public bool finished;
        public string LastOutputPath { get; private set; }
        readonly List<ChemotaxisAssayPlanV1.Outcome> completed =
            new List<ChemotaxisAssayPlanV1.Outcome>();

        IEnumerator Start()
        {
            if (runOnStart) yield return Run();
        }

        public IEnumerator Run()
        {
            if (finished) yield break;
            var schedule = ChemotaxisAssayPlanV1.Build(firstSeed,replicates);
            for (int index=0;index<schedule.Length;index++)
            {
                var trial=schedule[index];
                var root=new GameObject("Assay " + trial.ordinal + " " + trial.controller);
                var demo=root.AddComponent<ChemistryDemoBootstrap>();
                demo.autoBuild=false;
                demo.assayMode=true;
                demo.seed=trial.seed;
                demo.Build();
                var swimmer=demo.AssaySwimmer;
                var recorder=demo.AssayRecorder;
                if (!swimmer || !recorder)
                {
                    Destroy(root);
                    throw new InvalidOperationException("Assay bootstrap did not produce focal swimmer and recorder");
                }
                swimmer.useTemporalChemotaxis=trial.controller != ChemotaxisAssayPlanV1.Spatial;
                swimmer.stochasticTumbles=trial.controller == ChemotaxisAssayPlanV1.Stochastic;
                recorder.controllerLabel=trial.controller;
                recorder.BeginTrial();
                int steps=Mathf.CeilToInt(horizonSeconds/Time.fixedDeltaTime);
                for(int step=0;step<steps;step++)
                    yield return new WaitForFixedUpdate();
                completed.Add(new ChemotaxisAssayPlanV1.Outcome {
                    trial=trial,
                    elapsedSeconds=recorder.ElapsedSeconds,
                    pathLength=recorder.PathLength,
                    nutrientAbsorbed=recorder.NutrientAbsorbed,
                    nutrientDigested=recorder.NutrientDigested,
                    motorEnergySpent=recorder.MotorEnergySpent,
                    finalEnergy=recorder.LastEnergy,
                    tumbles=recorder.TumbleCount,
                    alive=recorder.Alive
                });
                Destroy(root);
                // Destroy is deferred; wait until the next frame before rebuilding
                // to prevent transient overlap between independent assay worlds.
                yield return null;
            }
            string safeName=Path.GetFileName(outputFile);
            if (string.IsNullOrEmpty(safeName)) safeName="chemotaxis-outcomes-v1.csv";
            LastOutputPath=Path.Combine(Application.persistentDataPath,safeName);
            File.WriteAllText(LastOutputPath,ChemotaxisAssayPlanV1.OutcomeCsv(completed));
            finished=true;
            Debug.Log("CELL_ASSAY_COMPLETE "+LastOutputPath+" trials="+completed.Count);
        }
    }
}
