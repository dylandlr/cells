using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;
using Cells.Fluid;

namespace Cells.Tests
{
    public sealed class FlagellarHydrodynamics2DTests
    {
        GameObject organism, fieldObject;
        CellFlagellarActuator motor;
        SoftFlagellum2D tail;
        FlagellarHydrodynamics2D hydro;
        FluidGrid2D fluid;

        [SetUp]
        public void Setup()
        {
            organism=new GameObject("segment force swimmer");
            fieldObject=new GameObject("fluid test");
            var metabolism=organism.AddComponent<CellMetabolism>();
            metabolism.energy=30f;
            motor=organism.AddComponent<CellFlagellarActuator>();
            tail=organism.AddComponent<SoftFlagellum2D>();
            hydro=organism.AddComponent<FlagellarHydrodynamics2D>();
            fluid=fieldObject.AddComponent<FluidGrid2D>();
            fluid.width=48;
            fluid.height=32;
            fluid.spacing=.25f;
            fluid.Initialize();
            motor.fluid=fluid;
            hydro.fluid=fluid;
            hydro.motor=motor;
            hydro.tail=tail;
            tail.motor=motor;
            tail.Initialize();
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(organism);
            Object.DestroyImmediate(fieldObject);
        }

        [Test]
        public void UnpoweredSegmentsDoNotProduceActiveSwimming()
        {
            motor.motorActivation=0f;
            hydro.Tick(.02f);
            for(int i=0;i<100;i++)
            {
                motor.Tick(.02f);
                tail.Tick(.02f);
                hydro.Tick(.02f);
            }
            Assert.Less(hydro.LocalSwimmingVelocity.sqrMagnitude,1e-10f);
            Assert.Less(fluid.QueuedForceSum().magnitude,1e-5f);
        }

        [Test]
        public void ActiveDeformationProducesBoundedSegmentBasedResponse()
        {
            motor.motorActivation=1f;
            hydro.Tick(.02f);
            float maxObserved=0f;
            for(int i=0;i<200;i++)
            {
                motor.Tick(.02f);
                tail.Tick(.02f);
                hydro.Tick(.02f);
                var u=hydro.LocalSwimmingVelocity;
                Assert.IsTrue(float.IsFinite(u.x)&&float.IsFinite(u.y));
                Assert.LessOrEqual(u.magnitude,hydro.maximumSwimSpeed+.0001f);
                Assert.GreaterOrEqual(hydro.SegmentDissipation,0f);
                maxObserved=Mathf.Max(maxObserved,u.magnitude);
            }
            Assert.Greater(maxObserved,1e-6f);
            // The paired fluid forcing never creates net grid force.
            Assert.Less(fluid.QueuedForceSum().magnitude,1e-4f);
        }

        [Test]
        public void DisabledMotorStopsVelocityEvenWithExistingTailHistory()
        {
            hydro.Tick(.02f);
            for(int i=0;i<20;i++)
            {
                motor.Tick(.02f); tail.Tick(.02f); hydro.Tick(.02f);
            }
            motor.structuralIntegrity=0f;
            motor.Tick(.02f);
            tail.Tick(.02f);
            hydro.Tick(.02f);
            Assert.AreEqual(0f,hydro.LocalSwimmingVelocity.magnitude,1e-7f);
        }
    }
}
