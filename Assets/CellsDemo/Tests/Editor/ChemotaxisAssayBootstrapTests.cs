using NUnit.Framework;
using UnityEngine;
using Cells.Demo;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class ChemotaxisAssayBootstrapTests
    {
        [Test]
        public void AssayBuildCreatesSingleFocalSwimmer()
        {
            var root=new GameObject("assay bootstrap test");
            try
            {
                var demo=root.AddComponent<ChemistryDemoBootstrap>();
                demo.autoBuild=false;
                demo.assayMode=true;
                demo.seed=731;
                demo.Build();
                Assert.IsNotNull(demo.Field);
                Assert.IsNotNull(demo.AssaySwimmer);
                Assert.IsNotNull(demo.AssayRecorder);
                Assert.Greater(demo.AssayNutrientDeposited,0f);
                Assert.LessOrEqual(demo.AssayNutrientDeposited,demo.initialFood + 1e-4f);
                Assert.AreEqual(1,root.GetComponentsInChildren<CellMotility>().Length);
                Assert.AreEqual(1,root.GetComponentsInChildren<ChemotaxisTrialRecorder>().Length);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
