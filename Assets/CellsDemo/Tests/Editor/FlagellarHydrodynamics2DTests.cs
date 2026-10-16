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

        // Controlled parameter sweep: produces physically interpretable diagnostics
        // without asserting an unvalidated monotonic speed law.
        [TestCase(1.1f,.12f,1f)]
        [TestCase(2.2f,.24f,1f)]
        [TestCase(3.3f,.36f,1f)]
        [TestCase(2.2f,.24f,.35f)]
        [TestCase(2.2f,.24f,0f)]
        public void ParameterSweepReportsSignedTravelAndEnergetics(
            float frequency,float amplitude,float integrity)
        {
            const float dt=.02f;
            motor.beatFrequencyHz=frequency;
            motor.waveAmplitude=amplitude;
            motor.structuralIntegrity=integrity;
            motor.motorActivation=1f;
            motor.metabolism=organism.GetComponent<CellMetabolism>();
            motor.metabolism.energy=80f;
            hydro.fluidReactionGain=0f; // Avoid time-varying field back-reaction in this isolate.
            tail.Initialize();
            hydro.ResetHistory();
            hydro.Tick(dt);

            float travelX=0f,travelY=0f,absoluteTravel=0f;
            float dissipated=0f;
            float initialEnergy=motor.metabolism.energy;
            int samples=0;
            for(int i=0;i<500;i++)
            {
                motor.Tick(dt);
                tail.Tick(dt);
                hydro.Tick(dt);
                if(i<100)continue; // warm-up removes startup transients
                Vector2 velocity=hydro.LocalSwimmingVelocity;
                Assert.IsTrue(float.IsFinite(velocity.x)&&float.IsFinite(velocity.y));
                Assert.IsTrue(float.IsFinite(hydro.SegmentDissipation));
                Assert.LessOrEqual(velocity.magnitude,hydro.maximumSwimSpeed+1e-4f);
                travelX+=velocity.x*dt; travelY+=velocity.y*dt;
                absoluteTravel+=velocity.magnitude*dt;
                dissipated+=hydro.SegmentDissipation;
                samples++;
            }
            Assert.AreEqual(400,samples);
            Assert.GreaterOrEqual(absoluteTravel,0f);
            Assert.GreaterOrEqual(dissipated,0f);
            Assert.LessOrEqual(motor.metabolism.energy,initialEnergy);
            if(integrity==0f)
            {
                Assert.Less(absoluteTravel,1e-6f);
                Assert.Less(dissipated,1e-6f);
            }
            TestContext.Out.WriteLine(
                $"frequency_hz={frequency:R},amplitude={amplitude:R},"+
                $"integrity={integrity:R},signed_x={travelX:R},signed_y={travelY:R},"+
                $"absolute_path={absoluteTravel:R},stroke_dissipation={dissipated:R},"+
                $"atp_used={initialEnergy-motor.metabolism.energy:R}");
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
