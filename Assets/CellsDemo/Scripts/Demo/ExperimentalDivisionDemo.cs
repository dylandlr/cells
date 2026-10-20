using Cells.Rendering;
using UnityEngine;
using Cells.Physiology;
using Cells.Substances;

namespace Cells.Demo
{
    /// <summary>
    /// Single opt-in reproduction assay on the Grazer. Not enabled by default,
    /// and not a general-purpose colony reproductive lifecycle.
    /// </summary>
    public sealed class ExperimentalDivisionDemo : MonoBehaviour
    {
        public ChemistryDemoBootstrap bootstrap;
        [Min(.1f)] public float minimumDaughterBiomass = .55f;
        [Min(.1f)] public float requiredEnergy = 1.1f;
        [Min(0f)] public float cooldownSeconds = 20f;
        public bool completed;
        public bool requireCompletedConstriction = true;
        public int daughterIndividualId;
        public int committedBirths => coordinator == null ? 0 : coordinator.lineage.Count-1;
        DivisionLineageCoordinator coordinator;
        int parentIndividualId;
        float nextAttempt;

        public void Initialize(int seed)
        {
            coordinator = new DivisionLineageCoordinator(seed);
            coordinator.mutations.eventProbability = .03;
            GameObject parent = GameObject.Find("Grazer");
            if (!parent || !parent.GetComponent<OrganismPhenotype>()) return;
            var founder = coordinator.lineage.Founder(
                parent.GetComponent<OrganismPhenotype>().genome, "Cellaria prima");
            parentIndividualId = founder.individualId;
        }

        void FixedUpdate()
        {
            if (!bootstrap || !bootstrap.enableExperimentalDivision || completed ||
                coordinator == null || parentIndividualId == 0 || Time.fixedTime < nextAttempt) return;
            TryDivide();
        }

        public bool TryDivide()
        {
            if (!bootstrap || !bootstrap.enableExperimentalDivision || completed ||
                coordinator == null || parentIndividualId == 0) return false;
            var parentTransform = bootstrap.transform.Find("Grazer");
            if (!parentTransform) return false;
            var parent = parentTransform.gameObject;
            var growth = parent.GetComponent<CellBiomassGrowth>();
            var metabolism = parent.GetComponent<CellMetabolism>();
            var exchange = parent.GetComponent<CellChemicalExchange>();
            var regulation = parent.GetComponent<GeneticRegulationController>();
            if (!growth || !metabolism || !exchange || !regulation || !metabolism.alive) return false;
            var cycle = parent.GetComponent<ExperimentalConstrictionCycle2D>();
            // A daughter cannot be spawned before its parent reaches the
            // demonstrated physical constriction stage. This is not scission.
            if (requireCompletedConstriction && (!cycle || !cycle.ReadyForScission)) return false;
            if (metabolism.energy < requiredEnergy) return false;
            // An actual growth-dependent biomass gate, not a timer-only reproduction.
            if (growth.structuralBiomass < 2f * minimumDaughterBiomass) return false;
            nextAttempt = Time.fixedTime + cooldownSeconds;
            GameObject staged = null;
            var ok = coordinator.TryPrepareAndRecordDivision(
                parentIndividualId,regulation.inherited,growth.structuralBiomass,
                exchange.nutrientReserve,metabolism.energy,minimumDaughterBiomass,
                (split,traits,genes) => {
                    staged = bootstrap.PrepareExperimentalDaughter(
                        parent,traits,genes,split,coordinator.lineage.PeekNextIndividualId);
                    if (!staged) return false;
                    // Only commit parent resources after staged daughter is ready.
                    // This demo uses a single-threaded main-thread transaction.
                    growth.structuralBiomass = split.parentBiomass;
                    exchange.nutrientReserve = split.parentNutrient;
                    metabolism.energy = split.parentEnergy;
                    staged.SetActive(true);
                    return true;
                }, out var child, out var daughterRegulation);
            if (!ok)
            {
                if (staged) Destroy(staged);
                return false;
            }
            daughterIndividualId = child.individualId;
            completed = true; // one generation until reproduction lifecycle validated
            Debug.Log("cells reproduction assay: parent " + parentIndividualId +
                      " -> daughter " + daughterIndividualId +
                      "; mutation events " + coordinator.mutations.events.Count);
            return true;
        }
    }
}
