using System;
using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class GeneticRegulationTests
    {
        [Test]
        public void SameInheritedGraphProducesDeterministicBoundedResponses()
        {
            var genome = new RegulatoryGenome();
            var a = new RegulatoryNetwork();
            var b = new RegulatoryNetwork();
            for(int i=0;i<500;i++)
            {
                a.Tick(genome,.7f,.4f,.02f);
                b.Tick(genome,.7f,.4f,.02f);
            }
            Assert.AreEqual(a.motorOutput,b.motorOutput,1e-7f);
            Assert.AreEqual(a.uptakeOutput,b.uptakeOutput,1e-7f);
            Assert.Greater(a.motorOutput,0f);
            Assert.Less(a.motorOutput,1f);
            Assert.Greater(a.uptakeOutput,0f);
            Assert.Less(a.uptakeOutput,1f);
        }

        [Test]
        public void RegulationChangesWithNutrientAndRelaxesOverTime()
        {
            var genome = new RegulatoryGenome { nutrientToMotor=4f, energyToMotor=0f,
                motorBias=-2f, responseSeconds=2f };
            var state = new RegulatoryNetwork();
            for(int i=0;i<200;i++)state.Tick(genome,0f,.5f,.02f);
            float absent = state.motorOutput;
            for(int i=0;i<200;i++)state.Tick(genome,1f,.5f,.02f);
            Assert.Greater(state.motorOutput,absent);
        }

        [Test]
        public void MutationOccursOnlyOnReplicateAndIsRepeatable()
        {
            var a=new GeneticMutationScheduler(41) {eventProbability=1};
            var b=new GeneticMutationScheduler(41) {eventProbability=1};
            var original=new RegulatoryGenome();
            var childA=a.Replicate(original,1,2,1);
            var childB=b.Replicate(original,1,2,1);
            Assert.AreEqual(1,a.events.Count);
            Assert.AreEqual(1,b.events.Count);
            Assert.AreEqual(a.events[0].target,b.events[0].target);
            Assert.AreEqual(a.events[0].newValue,b.events[0].newValue);
            Assert.AreNotSame(original,childA);
            Assert.AreEqual(a.events[0].newValue,b.events[0].newValue);
            Assert.AreEqual(0f,original.motorBias);
            Assert.AreEqual(1,a.events[0].generation);
        }

        [Test]
        public void ZeroMutationSettingCopiesWithoutLogging()
        {
            var scheduler=new GeneticMutationScheduler(2) {eventProbability=0};
            var original=new RegulatoryGenome();
            var child=scheduler.Replicate(original,1,2,1);
            Assert.AreEqual(0,scheduler.events.Count);
            Assert.AreNotSame(original,child);
            Assert.AreEqual(original.nutrientToMotor,child.nutrientToMotor);
        }

        [Test]
        public void MutationRateValidationRejectsInvalidValues()
        {
            var scheduler=new GeneticMutationScheduler(1) {eventProbability=2};
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                scheduler.Replicate(new RegulatoryGenome(),1,2,1));
        }
    }
}
