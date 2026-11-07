using System;
using UnityEngine;

namespace Cells.Substances
{
    public enum SubstanceClass { Nutrient, Toxin, Signal, Waste, Reactive, Inert }
    public enum MaterialForm { Dissolved, Suspension, Colloid, Droplet, VesicleBound, Gel, Crystal, GasBubble, MembraneBound }
    public enum TransportMode { Diffusion, Advection, Settling, Bound }

    [Serializable]
    public struct SubstanceVisualSignature
    {
        public Color baseColor;
        public string patternId;
        public string particleShapeId;
        public string glyphId;
        [Range(0, 1)] public float shimmer;
    }

    // Data-only registry entry. No visual/UI affordance is part of the cell's sensory observation.
    [CreateAssetMenu(fileName = "Substance", menuName = "cells/Simulation/Substance")]
    public sealed class SubstanceDefinition : ScriptableObject
    {
        public string stableId;
        public string displayName;
        public SubstanceClass classification;
        public MaterialForm materialForm;
        public TransportMode transport;
        [Min(0)] public float diffusionCoefficient;
        [Min(0)] public float decayRate;
        [Min(0)] public float settlingSpeed;
        public SubstanceVisualSignature visualization;
        [TextArea] public string inspectableNotes;

        void OnValidate()
        {
            diffusionCoefficient = Mathf.Max(0, diffusionCoefficient);
            decayRate = Mathf.Max(0, decayRate);
            settlingSpeed = Mathf.Max(0, settlingSpeed);
        }
    }
}
