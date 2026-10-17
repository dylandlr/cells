using System;
using UnityEngine;

namespace Cells.Experiments
{
    /// <summary>Reproducible, fixed-mass food placement for paired microscope assays.</summary>
    public static class SeededNutrientPatches
    {
        public const int Count = 5;
        public static Vector2[] Positions(int seed)
        {
            var rng = new System.Random(seed);
            var output = new Vector2[Count];
            for (int i=0;i<Count;i++)
                output[i] = new Vector2(
                    -3.5f+(float)rng.NextDouble()*7f,
                    -2.5f+(float)rng.NextDouble()*5f);
            return output;
        }
    }
}
