using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class SoftFlagellum2DTests
    {
        GameObject cell;
        CellFlagellarActuator motor;
        SoftFlagellum2D tail;
        [SetUp]
        public void SetUp()
        {
            cell=new GameObject("flagellum segment test");
            var metabolism=cell.AddComponent<CellMetabolism>();
            metabolism.energy=30f;
            motor=cell.AddComponent<CellFlagellarActuator>();
            tail=cell.AddComponent<SoftFlagellum2D>();
            tail.motor=motor;
            tail.Initialize();
        }
        [TearDown]
        public void TearDown()=>Object.DestroyImmediate(cell);

        [Test]
        public void MotorCurveDrivesConstrainedChainWithoutNaN()
        {
            Vector2 basePosition=tail.Node(0);
            Vector2 tipBefore=tail.Node(SoftFlagellum2D.NodeCount-1);
            for(int i=0;i<250;i++)
            {
                motor.Tick(.02f);
                tail.Tick(.02f);
            }
            Assert.Less(Vector2.Distance(basePosition,tail.Node(0)),1e-4f);
            Assert.Greater(Vector2.Distance(tipBefore,tail.Node(SoftFlagellum2D.NodeCount-1)),.001f);
            for(int i=0;i<SoftFlagellum2D.NodeCount;i++)
            {
                Vector2 p=tail.Node(i);
                Assert.IsTrue(float.IsFinite(p.x) && float.IsFinite(p.y));
                Assert.Less((p-basePosition).magnitude,4f);
            }
        }
    }
}
