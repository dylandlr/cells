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
        public void ContactStressIsObservableBoundedAndDecays()
        {
            body.QueueContact(3,new Vector2(.012f,.009f));
            body.Tick(.02f);
            float first=body.StressAt(3);
            Assert.Greater(first,0f);
            Assert.LessOrEqual(first,1f);
            Assert.GreaterOrEqual(body.MaximumStress,first);
            for(int i=0;i<200;i++)body.Tick(.02f);
            Assert.IsTrue(float.IsFinite(body.StressAt(3)));
            Assert.GreaterOrEqual(body.StressAt(3),0f);
            Assert.LessOrEqual(body.StressAt(3),1f);
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
        public void MicroscopeEnergyShadeTracksActualMetabolicReserve()
        {
            var metabolism=cell.AddComponent<CellMetabolism>();
            metabolism.energy=10f;
            var renderer=cell.AddComponent<SoftCellMeshRenderer2D>();
            renderer.metabolism=metabolism;
            renderer.body=body;
            renderer.fullEnergyReference=10f;
            renderer.UpdateGeometry();
            Color healthy=renderer.ObservedCytoplasmColor;
            var filter=cell.GetComponentInChildren<MeshFilter>();
            Color vertexHealthy=filter.sharedMesh.colors[0];

            metabolism.Spend(8f);
            renderer.UpdateGeometry();
            Color depleted=renderer.ObservedCytoplasmColor;
            Color vertexDepleted=filter.sharedMesh.colors[0];
            Assert.Less(depleted.g,healthy.g);
            Assert.Less(vertexDepleted.g,vertexHealthy.g);
            Assert.AreEqual(.2f,renderer.ObservedEnergyFraction,1e-4f);

            metabolism.Feed(8f);
            renderer.UpdateGeometry();
            Assert.AreEqual(healthy.g,renderer.ObservedCytoplasmColor.g,1e-5f);
            Assert.AreEqual(vertexHealthy.g,filter.sharedMesh.colors[0].g,1e-5f);
        }

        [Test]
        public void MicroscopeDeathDoesNotRequireInventedAnimation()
        {
            var metabolism=cell.AddComponent<CellMetabolism>();
            metabolism.energy=1f;
            var renderer=cell.AddComponent<SoftCellMeshRenderer2D>();
            renderer.metabolism=metabolism;
            metabolism.Spend(1f);
            renderer.UpdateGeometry();
            Assert.IsFalse(metabolism.alive);
            Assert.AreEqual(0f,renderer.ObservedEnergyFraction);
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
