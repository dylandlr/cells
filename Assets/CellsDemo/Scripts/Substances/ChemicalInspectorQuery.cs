using System;
using UnityEngine;

namespace Cells.Substances
{
    [Serializable]
    public struct InspectableChemicalState
    {
        public string id;
        public string name;
        public SubstanceClass substanceClass;
        public MaterialForm form;
        public float localConcentration;
        public float totalQuantity;
        public Color displayColor;
        public string particleShapeId;
        public string glyphId;
    }

    /// <summary>Human-only snapshot; never serialize this into an agent's observations.</summary>
    public static class ChemicalInspectorQuery
    {
        public static bool TryRead(MultiChemicalField2D field, SubstanceRegistry registry,
            string id, Vector2 location, out InspectableChemicalState result)
        {
            result = default;
            if (!field || !registry || !registry.TryGet(id, out var definition)) return false;
            result = new InspectableChemicalState
            {
                id = definition.stableId,
                name = definition.displayName,
                substanceClass = definition.classification,
                form = definition.materialForm,
                localConcentration = field.Sample(id, location),
                totalQuantity = field.Total(id),
                displayColor = definition.visualization.baseColor,
                particleShapeId = definition.visualization.particleShapeId,
                glyphId = definition.visualization.glyphId
            };
            return true;
        }
    }
}
