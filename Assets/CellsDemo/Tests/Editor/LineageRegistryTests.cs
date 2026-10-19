using System;
using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class LineageRegistryTests
    {
        [Test]
        public void FounderAndDescendantPreserveIdentityAndExactGenomeWithoutMutation()
        {
            var registry = new LineageRegistry();
            var founderGenome = new OrganismGenome { assimilationRate = .045f };
            var founder = registry.Founder(founderGenome, "Cellaria prima");
            var descendant = registry.Descendant(founder.individualId, new Random(7), 0, .1);
            Assert.AreEqual(2, registry.Count);
            Assert.AreEqual(0, founder.generation);
            Assert.AreEqual(1, descendant.generation);
            Assert.AreEqual(founder.individualId, descendant.parentId);
            Assert.AreEqual(founder.lineageId, descendant.lineageId);
            Assert.AreEqual(founder.taxonLabel, descendant.taxonLabel);
            Assert.AreEqual(.045f, descendant.genome.assimilationRate);
            founderGenome.assimilationRate = .1f;
            Assert.AreEqual(.045f, founder.genome.assimilationRate);
        }

        [Test]
        public void MutationIsSeededAndDoesNotAutomaticallyProduceSpecies()
        {
            var original = new OrganismGenome();
            var left = original.Inherit(new Random(12345), 1, .2);
            var right = original.Inherit(new Random(12345), 1, .2);
            Assert.AreEqual(left.assimilationRate, right.assimilationRate);
            Assert.AreEqual(left.feedingRate, right.feedingRate);
            Assert.GreaterOrEqual(left.assimilationRate, .005f);
            Assert.LessOrEqual(left.assimilationRate, .5f);

            var registry = new LineageRegistry();
            var parent = registry.Founder(original, "Cellaria prima");
            var descendant = registry.Descendant(parent.individualId, new Random(4), 1, .25);
            Assert.AreEqual(parent.taxonLabel, descendant.taxonLabel);
            Assert.AreEqual(parent.lineageId, descendant.lineageId);
        }

        [Test]
        public void RegistryRejectsUnknownParentAndInvalidMutationProbability()
        {
            var registry = new LineageRegistry();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                registry.Descendant(999, new Random(4)));
            var genome = new OrganismGenome();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                genome.Inherit(new Random(4), -1, .1));
        }
    }
}
