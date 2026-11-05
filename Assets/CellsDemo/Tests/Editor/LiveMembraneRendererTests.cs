using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;
using Cells.Rendering;

namespace Cells.Tests.Editor
{
    public sealed class LiveMembraneRendererTests
    {
        [Test]
        public void HealthyCellUsesBoundaryTriangulationAndExposesFallback()
        {
            var go=new GameObject("live membrane render test");
            try
            {
                go.AddComponent<MembraneIntegrity>();
                var body=go.AddComponent<SoftCellBody2D>();
                body.Initialize();
                var renderer=go.AddComponent<SoftCellMeshRenderer2D>();
                renderer.UpdateGeometry();
                Assert.IsTrue(renderer.LastContourWasValid);
                Assert.IsTrue(renderer.ShellVisible);
                Assert.IsTrue(renderer.LastBoundaryTriangulationSucceeded);
                Assert.Greater(renderer.BoundaryTriangulationSuccesses,0);
                Assert.AreEqual(SoftCellBody2D.Nodes-2,renderer.RenderTriangleCount);
                var mesh = go.GetComponentInChildren<MeshFilter>().sharedMesh;
                int childCount = go.transform.childCount;
                renderer.UpdateGeometry();
                Assert.AreSame(mesh, go.GetComponentInChildren<MeshFilter>().sharedMesh);
                Assert.AreEqual(childCount, go.transform.childCount,
                    "Repeated updates must reuse rendering resources.");
                renderer.useBoundaryTriangulation=false;
                renderer.UpdateGeometry();
                Assert.IsFalse(renderer.LastBoundaryTriangulationSucceeded);
                Assert.AreEqual(SoftCellBody2D.Nodes,renderer.RenderTriangleCount);
                Assert.IsTrue(renderer.ShellVisible);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
