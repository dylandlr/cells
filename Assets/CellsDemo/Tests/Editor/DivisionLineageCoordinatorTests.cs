using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class DivisionLineageCoordinatorTests
    {
        [Test]
        public void FailedBirthDoesNotRecordAChildOrMutation()
        {
            var coordinator = new DivisionLineageCoordinator(99);
            coordinator.mutations.eventProbability = 1;
            var founder = coordinator.lineage.Founder(new OrganismGenome(),"Cellaria prima");
            bool ok = coordinator.TryRecordDivision(founder.individualId,
                new RegulatoryGenome(),2.4f,.8f,6f,.5f,
                split => false,out var child,out var childRegulation);
            Assert.IsFalse(ok);
            Assert.IsNull(child);
            Assert.IsNull(childRegulation);
            Assert.AreEqual(1,coordinator.lineage.Count);
            Assert.AreEqual(0,coordinator.mutations.events.Count);
        }

        [Test]
        public void PreparedFactoryReceivesGenomeBeforeAnyCommit()
        {
            var coordinator = new DivisionLineageCoordinator(127);
            coordinator.mutations.eventProbability = 1;
            var founder = coordinator.lineage.Founder(new OrganismGenome(),"Cellaria prima");
            RegulatoryGenome prepared = null;
            int countDuringPreparation = -1;
            bool first = coordinator.TryPrepareAndRecordDivision(
                founder.individualId,new RegulatoryGenome(),3f,1f,8f,.5f,
                (budget, genes, regulation) => {
                    prepared = regulation.Copy();
                    countDuringPreparation = coordinator.lineage.Count;
                    Assert.AreEqual(1.5f,budget.parentBiomass,1e-6f);
                    Assert.AreEqual(founder.genome.feedingRate,genes.feedingRate);
                    return false;
                },out var failedChild,out var failedRegulation);
            Assert.IsFalse(first);
            Assert.AreEqual(1,countDuringPreparation);
            Assert.IsNull(failedChild);
            Assert.AreEqual(0,coordinator.mutations.events.Count);

            bool second = coordinator.TryPrepareAndRecordDivision(
                founder.individualId,new RegulatoryGenome(),3f,1f,8f,.5f,
                (budget, genes, regulation) => {
                    Assert.AreEqual(prepared.nutrientToMotor,regulation.nutrientToMotor);
                    Assert.AreEqual(prepared.motorBias,regulation.motorBias);
                    return true;
                },out var child,out var childRegulation);
            Assert.IsTrue(second);
            Assert.AreEqual(2,coordinator.lineage.Count);
            Assert.AreEqual(child.individualId,coordinator.mutations.events[0].descendantId);
            Assert.AreEqual(childRegulation.motorBias,prepared.motorBias);
        }

        [Test]
        public void SuccessfulBirthRecordsOneDescendantAndMutationOpportunity()
        {
            var coordinator = new DivisionLineageCoordinator(99);
            coordinator.mutations.eventProbability = 1;
            var founder = coordinator.lineage.Founder(new OrganismGenome(),"Cellaria prima");
            CellDivisionBudget.Partition observed = default(CellDivisionBudget.Partition);
            bool ok = coordinator.TryRecordDivision(founder.individualId,
                new RegulatoryGenome(),2.4f,.8f,6f,.5f,
                split => { observed=split; return true; },
                out var child,out var childRegulation);
            Assert.IsTrue(ok);
            Assert.NotNull(child);
            Assert.NotNull(childRegulation);
            Assert.AreEqual(2,coordinator.lineage.Count);
            Assert.AreEqual(founder.individualId,child.parentId);
            Assert.AreEqual(1,child.generation);
            Assert.AreEqual(1,coordinator.mutations.events.Count);
            Assert.AreEqual(2.4f,observed.parentBiomass+observed.daughterBiomass,1e-5f);
        }
    }
}
