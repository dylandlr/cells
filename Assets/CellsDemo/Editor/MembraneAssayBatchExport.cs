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
            var output=Environment.GetEnvironmentVariable("CELLS_ASSAY_OUTPUT");
            if(string.IsNullOrEmpty(output))
                output=Path.Combine(Directory.GetCurrentDirectory(),"artifacts","membrane-assay.json");
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
