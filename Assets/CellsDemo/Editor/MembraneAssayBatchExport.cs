using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Cells.Physiology;

namespace Cells.EditorTools
{
    /// <summary>Runs a disposable geometry experiment from an Editor menu or
    /// -executeMethod. Writes the actual membrane solver trace as JSON.</summary>
    public static class MembraneAssayBatchExport
    {
        [MenuItem("Cells/Research/Export Membrane Constriction Trace")]
        public static void Export()
        {
            // GameCI may invoke the Editor from a working directory other
            // than the Unity project root. Resolve artifacts against the
            // project itself, not the process's current directory.
            var projectRoot=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            var requested=Environment.GetEnvironmentVariable("CELLS_ASSAY_OUTPUT");
            if(string.IsNullOrWhiteSpace(requested))
                requested=Path.Combine("artifacts","membrane-assay.json");
            var output=Path.IsPathRooted(requested) ? requested :
                Path.Combine(projectRoot,requested);
            GameObject go=null;
            try
            {
                go=new GameObject("Temporary membrane experiment");
                go.AddComponent<MembraneIntegrity>();
                var body=go.AddComponent<SoftCellBody2D>();
                body.Initialize();
                var report=MembraneConstrictionAssay2D.Run(
                    body,20f,.02f,.70f,10);
                var absolute=Path.GetFullPath(output);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute));
                File.WriteAllText(absolute,JsonUtility.ToJson(report,true));
                var convergence=MembraneConvergenceSweep2D.Run(20f,.70f);
                var convergencePath=Path.Combine(Path.GetDirectoryName(absolute),
                    "membrane-convergence.json");
                File.WriteAllText(convergencePath,JsonUtility.ToJson(convergence,true));
                var comparison=MembraneSolverComparisonAssay2D.Run(100,.02f);
                var comparisonPath=Path.Combine(Path.GetDirectoryName(absolute),
                    "membrane-comparison.json");
                File.WriteAllText(comparisonPath,JsonUtility.ToJson(comparison,true));
                Debug.Log("cells membrane comparison: "+comparison.status+
                    "; observations="+comparison.samples.Count+
                    "; JSON="+comparisonPath);
                Debug.Log("cells membrane convergence: valid="+convergence.allValid+
                    "; maximum ratio difference="+convergence.maximumFinalRatioDifference+
                    "; report="+convergencePath);
                Debug.Log("cells membrane assay: "+report.status+
                    "; ratio="+report.lastRatio+
                    "; invalid samples="+report.invalidSamples+
                    "; JSON="+absolute);
                // Failing the geometry check is not treated as a successful
                // cytokinesis result. Export remains useful for diagnosis.
            }
            catch(Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
            finally
            {
                if(go)UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
