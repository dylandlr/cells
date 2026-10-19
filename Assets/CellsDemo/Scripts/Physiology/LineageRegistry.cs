using System;
using System.Collections.Generic;

namespace Cells.Physiology
{
    /// <summary>In-memory ancestry record. Taxon labels are fictional research labels,
    /// not an automatic biological species concept or official binomial nomenclature.</summary>
    public sealed class LineageRegistry
    {
        [Serializable]
        public sealed class Record
        {
            public int individualId;
            public int parentId;
            public int lineageId;
            public int generation;
            public string taxonLabel;
            public OrganismGenome genome;
        }

        readonly Dictionary<int, Record> individuals = new Dictionary<int, Record>();
        int nextIndividualId = 1;
        int nextLineageId = 1;
        public int Count => individuals.Count;
        public IEnumerable<Record> Records => individuals.Values;

        public Record Founder(OrganismGenome genome, string provisionalTaxon)
        {
            if (genome == null) throw new ArgumentNullException(nameof(genome));
            var record = new Record {
                individualId = nextIndividualId++, parentId = 0,
                lineageId = nextLineageId++, generation = 0,
                taxonLabel = provisionalTaxon ?? "Unclassified",
                genome = genome.Copy()
            };
            individuals.Add(record.individualId, record);
            return record;
        }

        public Record Descendant(int parentId, System.Random random,
            double mutationProbability = .01, double relativeMutationSigma = .05)
        {
            if (!individuals.TryGetValue(parentId, out var parent))
                throw new ArgumentOutOfRangeException(nameof(parentId), "Parent must exist.");
            var record = new Record {
                individualId = nextIndividualId++, parentId = parentId,
                lineageId = parent.lineageId, generation = parent.generation + 1,
                taxonLabel = parent.taxonLabel,
                genome = parent.genome.Inherit(random, mutationProbability, relativeMutationSigma)
            };
            individuals.Add(record.individualId, record);
            return record;
        }

        public bool TryGet(int id, out Record record) => individuals.TryGetValue(id, out record);

        // Explicit annotation only; mutation does not trigger naming/speciation.
        public void AnnotateLineage(int lineageId, string provisionalTaxon)
        {
            if (string.IsNullOrWhiteSpace(provisionalTaxon))
                throw new ArgumentException("Taxon label required", nameof(provisionalTaxon));
            foreach (var record in individuals.Values)
                if (record.lineageId == lineageId) record.taxonLabel = provisionalTaxon;
        }
    }
}
