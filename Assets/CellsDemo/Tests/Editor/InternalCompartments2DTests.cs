using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class InternalCompartments2DTests
    {
        GameObject cell;
        SoftCellBody2D body;
        InternalCompartments2D compartments;

        [SetUp]
        public void SetUp()
        {
            cell=new GameObject("internal compartments test");
            cell.AddComponent<MembraneIntegrity>();
            body=cell.AddComponent<SoftCellBody2D>();
            body.Initialize();
            compartments=cell.AddComponent<InternalCompartments2D>();
            compartments.body=body;
            compartments.Initialize();
        }

        [TearDown]
        public void TearDown()=>Object.DestroyImmediate(cell);

        [Test]
        public void InternalCompartmentPositionsRemainFiniteAndBounded()
        {
            for(int k=0;k<300;k++)
            {
                body.Tick(.02f);
                compartments.Tick(.02f);
            }
            for(int i=0;i<InternalCompartments2D.Count;i++)
            {
                Vector2 p=compartments.Position(i);
                Assert.IsTrue(float.IsFinite(p.x)&&float.IsFinite(p.y));
                Assert.Less(Vector2.Distance(p,body.Core),body.restRadius*1.2f);
            }
        }

        [Test]
        public void InternalCompartmentsAreMutuallyExcluded()
        {
            for(int k=0;k<100;k++)
            {
                body.Tick(.02f);
                compartments.Tick(.02f);
            }
            for(int i=0;i<InternalCompartments2D.Count;i++)
            for(int j=i+1;j<InternalCompartments2D.Count;j++)
                Assert.GreaterOrEqual(
                    Vector2.Distance(compartments.Position(i),compartments.Position(j)),
                    compartments.Radius(i)+compartments.Radius(j)-.01f);
        }

        [Test]
        public void MembraneContactQueueIsBoundedAndConsumed()
        {
            // Directly probe the same reaction queue used by organelle contacts.
            Vector2 before=body.Node(0);
            body.QueueContact(0,new Vector2(100f,100f));
            body.QueueContact(0,new Vector2(100f,100f));
            body.Tick(.02f);
            Assert.Greater(body.LastContactMagnitude,0f);
            Assert.LessOrEqual(body.LastContactMagnitude,.0351f);
            Vector2 after=body.Node(0);
            Assert.IsTrue(float.IsFinite(after.x) && float.IsFinite(after.y));
            Assert.Greater((after-before).sqrMagnitude,1e-9f);
            body.Tick(.02f);
            Assert.AreEqual(0f,body.LastContactMagnitude,1e-7f);
        }

        [Test]
        public void InjuryDoesNotProduceUnboundedInternalMotion()
        {
            cell.GetComponent<MembraneIntegrity>().ApplyExposureToSegment(5,.7f);
            for(int k=0;k<250;k++)
            {
                body.Tick(.02f);
                compartments.Tick(.02f);
            }
            for(int i=0;i<InternalCompartments2D.Count;i++)
                Assert.Less(compartments.Position(i).magnitude,1.5f);
        }
    }
}
