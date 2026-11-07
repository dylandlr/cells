using UnityEngine;

namespace Cells.Substances
{
    public enum HazardGlyph { None, Skull, Warning, Unknown }

    /// <summary>
    /// Human-only semantic overlay policy. Does NOT affect simulation chemistry or controllers.
    /// A skull means predicted lethality to the particular selected target over the given window,
    /// not that a purple particle is universally lethal.
    /// </summary>
    public static class HazardAnnotationPolicy
    {
        public static HazardGlyph ForTarget(
            ExposureProfile exposure, float targetEnergy, float horizonSeconds,
            bool hasVerifiedExposureModel)
        {
            if (!hasVerifiedExposureModel) return HazardGlyph.Unknown;
            if (exposure.dose <= 0f || exposure.uptakePerSecond <= 0f) return HazardGlyph.None;
            if (exposure.IsLethalRisk(targetEnergy, horizonSeconds)) return HazardGlyph.Skull;
            return exposure.NetEnergyRate() < 0f ? HazardGlyph.Warning : HazardGlyph.None;
        }

        public static bool ShowAtZoom(float zoom, bool hovered, bool selected, bool severe)
            => hovered || selected || (severe && zoom <= 12f);
    }
}
