using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Runs several independent disposable membrane solvers to expose
    /// timestep sensitivity. Never reuses a mutated membrane between trials.
    /// A narrowing result is geometric evidence, not membrane scission.
    /// </summary>
    public static class MembraneConvergenceSweep2D
    {
        [Serializable]
        public struct Trial
        {
            public float timestep;
            public string status;
            public float minimumRatio, finalRatio, finalArea;
            public float controlFinalRatio, controlFinalArea, actuationEffect;
            public bool controlValid;
            public int invalidSamples, narrowedSamples;
        }

        [Serializable]
        public sealed class Report
        {
            public List<Trial> trials = new List<Trial>();
            public bool allValid;
            public float maximumFinalRatioDifference;
            public float maximumFinalAreaRelativeDifference;
        }

        public static Report Run(float duration=20f, float neckThreshold=.70f)
        {
            var report=new Report { allValid=true };
            float[] steps = { .02f, .01f, .005f };
            float firstRatio=0f,firstArea=0f;
            for(int index=0;index<steps.Length;index++)
            {
                var go=new GameObject("Disposable convergence trial "+index);
                try
                {
                    go.SetActive(false);
                    go.AddComponent<MembraneIntegrity>();
                    var body=go.AddComponent<SoftCellBody2D>();
                    body.Initialize();
                    var trace=MembraneConstrictionAssay2D.Run(
                        body,duration,steps[index],neckThreshold,
                        Mathf.Max(1,Mathf.RoundToInt(.2f/steps[index])));
                    // Matched fresh no-actuation control at the same dt.
                    var controlObject=new GameObject("Convergence control "+index);
                    MembraneConstrictionAssay2D.Report control;
                    try
                    {
                        controlObject.SetActive(false);
                        controlObject.AddComponent<MembraneIntegrity>();
                        var controlBody=controlObject.AddComponent<SoftCellBody2D>();
                        controlBody.Initialize();
                        control=MembraneConstrictionAssay2D.Run(
                            controlBody,duration,steps[index],neckThreshold,
                            Mathf.Max(1,Mathf.RoundToInt(.2f/steps[index])),
                            maximumActuation:0f);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(controlObject); }
                    var controlLast=control.samples[control.samples.Count-1];
                    var last=trace.samples[trace.samples.Count-1];
                    var trial=new Trial {
                        timestep=steps[index],status=trace.status,
                        minimumRatio=trace.minimumRatio,
                        finalRatio=trace.lastRatio,finalArea=last.area,
                        controlFinalRatio=control.lastRatio,
                        controlFinalArea=controlLast.area,
                        controlValid=control.invalidSamples==0 && controlLast.valid,
                        actuationEffect=control.lastRatio-trace.lastRatio,
                        invalidSamples=trace.invalidSamples,
                        narrowedSamples=trace.narrowedSamples
                    };
                    report.trials.Add(trial);
                    if(trace.invalidSamples>0 || !last.valid || !trial.controlValid)
                        report.allValid=false;
                    if(index==0) { firstRatio=trial.finalRatio;firstArea=trial.finalArea; }
                    else
                    {
                        report.maximumFinalRatioDifference=Mathf.Max(
                            report.maximumFinalRatioDifference,
                            Mathf.Abs(trial.finalRatio-firstRatio));
                        report.maximumFinalAreaRelativeDifference=Mathf.Max(
                            report.maximumFinalAreaRelativeDifference,
                            Mathf.Abs(trial.finalArea-firstArea)/Mathf.Max(.0001f,firstArea));
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            return report;
        }
    }
}
