using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Explicit conserved effective membrane-material reserve. Units are
    /// abstract material equivalents, not measured bilayer molecules.
    /// Neither geometric perimeter nor surface area is itself mass.
    /// </summary>
    public sealed class CellMembraneMaterial : MonoBehaviour
    {
        [Min(0f)] public float material = .20f;
        [Min(0f)] public float minimumViableMaterial = .04f;
        public bool IsValid => float.IsFinite(material) && material >= 0f &&
            float.IsFinite(minimumViableMaterial) && minimumViableMaterial > 0f;

        public bool TryPartition(float fraction, out float parent, out float daughter)
        {
            parent = daughter = 0f;
            if (!IsValid || !float.IsFinite(fraction) ||
                fraction <= 0f || fraction >= 1f) return false;
            daughter = material*fraction;
            parent = material-daughter;
            if (daughter < minimumViableMaterial ||
                parent < minimumViableMaterial)
            {
                parent=daughter=0f;
                return false;
            }
            return true;
        }
    }
}
