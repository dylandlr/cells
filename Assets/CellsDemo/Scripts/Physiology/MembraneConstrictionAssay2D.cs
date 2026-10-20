using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Explicit fixed-step diagnostic experiment for the *existing* one-loop
    /// soft-body. Does not run automatically or alter the normal cell cycle.
    /// Records whether physically simulated necks are stable and sufficiently
    /// narrow to pass the separate geometric checkpoint.
    /// </summary>
    public static class MembraneConstrictionAssay2D
    {
        [Serializable]
        public struct Sample
        {
            public float time, actuation, equatorWidth, shoulderWidth, ratio,
                area, maxStress;
            public bool valid, narrowed;
        }

        [Serializable]
        public sealed class Report
        {
            public string status;
            public float baselineRatio, minimumRatio, lastRatio;
            public int steps, invalidSamples, narrowedSamples;
            public List<Sample> samples = new List<Sample>();
        }

        public static Report Run(SoftCellBody2D body, float duration=20f,
            float timestep=.02f, float neckThreshold=.70f,
            int sampleEvery=10)
        {
            if (!body || !body.Initialized ||
                !float.IsFinite(duration) || duration<=0f || duration>180f ||
                !float.IsFinite(timestep) || timestep<.001f || timestep>.05f ||
                !float.IsFinite(neckThreshold) || neckThreshold<=0f ||
                neckThreshold>=1f || sampleEvery<1)
                throw new ArgumentException("Invalid membrane assay configuration.");

            var report = new Report { status="not-narrowed",
                minimumRatio=float.PositiveInfinity };
            float oldActuation=body.divisionConstriction;
            try
            {
                int count=Mathf.CeilToInt(duration/timestep);
                report.steps=count;
                for (int step=0;step<=count;step++)
                {
                    float time=Mathf.Min(duration,step*timestep);
                    // A controlled linear ramp. Readiness timer and inherited
                    // genome state are intentionally excluded from this assay.
                    body.divisionConstriction=Mathf.Clamp01(time/duration);
                    if(step>0)body.Tick(timestep);
                    if(step%sampleEvery!=0 && step!=count)continue;
                    var geometry=MembraneNeckDiagnostic2D.Measure(body,neckThreshold);
                    var sample=new Sample {
                        time=time,actuation=body.divisionConstriction,
                        equatorWidth=geometry.equatorWidth,
                        shoulderWidth=geometry.shoulderWidth,
                        ratio=geometry.ratio,area=geometry.area,
                        maxStress=body.MaximumStress,
                        valid=geometry.valid,narrowed=geometry.narrowed
                    };
                    report.samples.Add(sample);
                    if(step==0)report.baselineRatio=sample.ratio;
                    report.lastRatio=sample.ratio;
                    if(!geometry.valid)report.invalidSamples++;
                    if(geometry.valid)
                    {
                        report.minimumRatio=Mathf.Min(report.minimumRatio,sample.ratio);
                        if(geometry.narrowed)report.narrowedSamples++;
                    }
                }
                report.status=report.invalidSamples>0 ? "invalid-geometry" :
                    report.narrowedSamples>0 ? "narrowed" : "not-narrowed";
                return report;
            }
            finally { body.divisionConstriction=oldActuation; }
        }
    }
}
