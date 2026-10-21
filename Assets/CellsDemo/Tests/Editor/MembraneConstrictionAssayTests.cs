using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests.Editor
{
    public sealed class MembraneConstrictionAssayTests
    {
        [Test]
        public void FixedStepAssayProducesFiniteTraceAndRestoresActuation()
        {
            var go=new GameObject("constriction assay");
            try
            {
                go.AddComponent<MembraneIntegrity>();
                var body=go.AddComponent<SoftCellBody2D>();
                body.Initialize();
                body.divisionConstriction=.17f;
                var report=MembraneConstrictionAssay2D.Run(
                    body,duration=2f,timestep=.02f,neckThreshold=.7f,sampleEvery=5);
                Assert.AreEqual(100,report.steps);
                Assert.AreEqual(21,report.samples.Count);
                Assert.AreEqual(.17f,body.divisionConstriction,1e-6f);
                Assert.IsTrue(report.samples[0].valid);
                Assert.AreEqual(0f,report.samples[0].time,1e-6f);
                Assert.AreEqual(2f,report.samples[report.samples.Count-1].time,1e-5f);
                Assert.IsTrue(float.IsFinite(report.lastRatio));
                Assert.AreEqual(0,report.invalidSamples,
                    "The physical solver must preserve simple finite geometry.");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
