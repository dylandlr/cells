using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;
using Cells.Rendering;

namespace Cells.Tests
{
    public sealed class SoftCellBody2DTests
    {
        GameObject cell;
        SoftCellBody2D body;
        MembraneIntegrity membrane;
        [SetUp]
        public void SetUp()
        {
            cell=new GameObject("soft cell test");
            membrane=cell.AddComponent<MembraneIntegrity>();
            body=cell.AddComponent<SoftCellBody2D>();
            body.membrane=membrane;
            body.Initialize();
        }
        [TearDown]
        public void TearDown()=>Object.DestroyImmediate(cell);

        [Test]
        public void MeshNodesRemainFiniteUnderRepeatedIntegration()
        {
            for(int i=0;i<750;i++)body.Tick(.02f);
            Assert.AreEqual(SoftCellBody2D.Nodes,MembraneIntegrity.SegmentCount);
            Assert.Greater(body.SignedArea(),.1f);
            for(int i=0;i<SoftCellBody2D.Nodes;i++)
            {
                Vector2 p=body.Node(i);
                Assert.IsTrue(float.IsFinite(p.x) && float.IsFinite(p.y));
                Assert.Less(p.magnitude,body.restRadius*2.5f);
            }
        }

        [Test]
        public void StressAndMembraneInjuryDoNotExplodeConstraints()
        {
            membrane.ApplyExposureToSegment(4,.6f);
            for(int i=0;i<250;i++)body.Tick(.02f);
            Assert.Greater(Mathf.Abs(body.SignedArea()),.1f);
            for(int i=0;i<SoftCellBody2D.Nodes;i++)
                Assert.Less(body.Node(i).magnitude,body.restRadius*2.5f);
        }

        [Test]
        public void MeshRendererUsesLivePhysicsVertices()
        {
            var meshRenderer=cell.AddComponent<SoftCellMeshRenderer2D>();
            meshRenderer.body=body;
            meshRenderer.UpdateGeometry();
            var filter=cell.GetComponentInChildren<MeshFilter>();
            Assert.NotNull(filter);
            Assert.AreEqual(SoftCellBody2D.Nodes+1,filter.sharedMesh.vertexCount);
            Assert.AreEqual(SoftCellBody2D.Nodes*3,filter.sharedMesh.triangles.Length);
        }
    }
}
