using System;

namespace Cells.Physiology
{
    /// <summary>
    /// A two-phase division transaction. First compute a resource budget,
    /// then let the caller create a daughter through a validated factory,
    /// only then commit ancestry and genetic changes.
    /// No Unity objects are spawned by this class.
    /// </summary>
    public sealed class DivisionLineageCoordinator
    {
        public readonly LineageRegistry lineage = new LineageRegistry();
        public readonly GeneticMutationScheduler mutations;
        readonly Random quantitativeRandom;

        public DivisionLineageCoordinator(int seed)
        {
            mutations = new GeneticMutationScheduler(seed);
            quantitativeRandom = new Random(unchecked(seed + 317));
        }

        // New callers should use TryPrepareAndRecordDivision so the daughter
        // receives its inherited regulatory genome BEFORE spawn confirmation.
        public bool TryPrepareAndRecordDivision(int parentIndividualId, RegulatoryGenome parentRegulation,
            float biomass, float nutrients, float energy, float minimumDaughterBiomass,
            Func<CellDivisionBudget.Partition, OrganismGenome, RegulatoryGenome, bool> preparedFactory,
            out LineageRegistry.Record descendant, out RegulatoryGenome childRegulation)
        {
            descendant = null;
            childRegulation = null;
            if (preparedFactory == null || parentRegulation == null ||
                !lineage.TryGet(parentIndividualId,out var parent)) return false;
            if (!CellDivisionBudget.TryPartition(biomass,nutrients,energy,
                minimumDaughterBiomass,.5f,out var budget)) return false;

            // The predicted ID is reserved logically, but the lineage registry is
            // changed only after the prepared factory reports atomic success.
            int predictedId = lineage.PeekNextIndividualId;
            int generation = parent.generation + 1;
            var candidateRegulation = mutations.Preview(parentRegulation,
                parentIndividualId,predictedId,generation,out var proposedMutation);
            var candidateGenome = parent.genome.Copy();
            if (!preparedFactory(budget,candidateGenome,candidateRegulation)) return false;
            descendant = lineage.Descendant(parentIndividualId,quantitativeRandom,0,0);
            if (descendant.individualId != predictedId)
                throw new InvalidOperationException("Lineage registration diverged from prepared ID.");
            mutations.CommitPreview(proposedMutation);
            childRegulation = candidateRegulation;
            return true;
        }

        public bool TryRecordDivision(int parentIndividualId, RegulatoryGenome parentRegulation,
            float biomass, float nutrients, float energy, float minimumDaughterBiomass,
            Func<CellDivisionBudget.Partition,bool> daughterFactory,
            out LineageRegistry.Record descendant,
            out RegulatoryGenome childRegulation)
        {
            descendant = null;
            childRegulation = null;
            if (daughterFactory == null || parentRegulation == null ||
                !lineage.TryGet(parentIndividualId,out var parent)) return false;
            if (!CellDivisionBudget.TryPartition(biomass,nutrients,energy,
                minimumDaughterBiomass,.5f,out var budget)) return false;

            // The factory MUST be an atomic spawn/initialization operation,
            // returning true only when the prepared daughter is viable.
            if (!daughterFactory(budget)) return false;
            // Do not mutate both old trait genome and regulatory genome per birth.
            descendant = lineage.Descendant(parentIndividualId,quantitativeRandom,0,0);
            childRegulation = mutations.Replicate(parentRegulation,parentIndividualId,
                descendant.individualId,descendant.generation);
            return true;
        }
    }
}
