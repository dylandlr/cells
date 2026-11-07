using UnityEngine;
using Cells.Physiology;
using Cells.Rendering;

namespace Cells.Demo
{
    /// <summary>
    /// Experimental pre-division constriction driver. Affects actual
    /// SoftCellBody2D boundary nodes; does not execute scission or spawn.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    [RequireComponent(typeof(SoftCellBody2D))]
    public sealed class ExperimentalConstrictionCycle2D : MonoBehaviour
    {
        public SoftCellBody2D body;
        public CellBiomassGrowth growth;
        public CellMetabolism metabolism;
        public CellDivisionPreview2D preview;
        public GenomeCopySegregation2D segregation;
        [Min(.01f)] public float activationBiomass = 1.7f;
        [Min(0f)] public float minimumEnergy = 1f;
        [Min(.1f)] public float rampSeconds = 12f;
        [Range(0f,1f)] public float maximumConstriction = .85f;
        public float Progress { get; private set; }
        public bool ReadyForScission => Progress >= .999f;

        void Awake()
        {
            if (!body) body = GetComponent<SoftCellBody2D>();
            if (!growth) growth = GetComponent<CellBiomassGrowth>();
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!preview) preview = GetComponent<CellDivisionPreview2D>();
            if (!segregation) segregation = GetComponent<GenomeCopySegregation2D>();
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (!body || !growth || !metabolism || !float.IsFinite(dt) || dt <= 0f) return;
            if (!segregation) segregation = GetComponent<GenomeCopySegregation2D>();
            bool eligible = segregation && segregation.Complete &&
                metabolism.alive && metabolism.energy >= minimumEnergy &&
                growth.structuralBiomass >= activationBiomass;
            if (eligible) Progress = Mathf.Clamp01(Progress + dt / Mathf.Max(.1f,rampSeconds));
            else Progress = Mathf.MoveTowards(Progress,0f,dt/Mathf.Max(.1f,rampSeconds));
            body.divisionConstriction = Progress * maximumConstriction;
            if (preview)
            {
                preview.visible = Progress > 0f;
                preview.progress = Progress;
            }
        }

        public void ResetCycle()
        {
            Progress = 0f;
            if (body) body.divisionConstriction = 0f;
            if (preview) { preview.visible = false; preview.progress = 0f; }
        }
    }
}
