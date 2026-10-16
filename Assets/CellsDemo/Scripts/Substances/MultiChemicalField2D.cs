using System;
using System.Collections.Generic;
using UnityEngine;
using Cells.Fluid;

namespace Cells.Substances
{
    /// <summary>Mass per grid voxel, with concentration queries in mass / local area.
    /// No-flux diffusion and first-order decay use arbitrary simulation units.</summary>
    public sealed class MultiChemicalField2D : MonoBehaviour
    {
        public SubstanceDefinition[] species = Array.Empty<SubstanceDefinition>();
        [Tooltip("Optional velocity field. When connected, dissolved masses advect with the fluid.")]
        public FluidGrid2D fluid;
        [Range(.05f, .95f)] public float advectionCfl = .45f;
        [Min(3)] public int width = 96;
        [Min(3)] public int height = 54;
        [Min(.1f)] public float cellSize = .25f;
        [Min(.001f)] public float fixedStep = .02f;
        readonly Dictionary<string, float[]> concentrations = new Dictionary<string, float[]>();
        readonly Dictionary<string, float[]> buffers = new Dictionary<string, float[]>();
        float accumulator;
        public float CellArea => cellSize * cellSize;
        public float DecayedMass { get; private set; }

        void Awake() => Initialize();

        public void Initialize()
        {
            if (width < 3 || height < 3 || !Finite(cellSize) || cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Use a positive cell size and at least a 3x3 grid.");
            accumulator = 0f;
            DecayedMass = 0f;
            concentrations.Clear(); buffers.Clear();
            foreach (var s in species)
            {
                if (!s || string.IsNullOrWhiteSpace(s.stableId)) continue;
                if (concentrations.ContainsKey(s.stableId))
                {
                    throw new ArgumentException("Duplicate substance ID: " + s.stableId);
                }
                concentrations.Add(s.stableId, new float[width * height]);
                buffers.Add(s.stableId, new float[width * height]);
            }
        }

        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        int Index(Vector2 world)
        {
            Vector2 local = transform.InverseTransformPoint(world);
            if (!Finite(local.x) || !Finite(local.y)) return -1;
            int x = Mathf.FloorToInt(local.x / cellSize + width * .5f);
            int y = Mathf.FloorToInt(local.y / cellSize + height * .5f);
            if (x < 0 || x >= width || y < 0 || y >= height) return -1;
            return y * width + x;
        }

        // Read-only grid sampling for microscope rendering; no simulation state mutation.
        public float SampleCell(string id, int x, int y)
        {
            if (!concentrations.TryGetValue(id, out var values)) return 0f;
            if (x < 0 || x >= width || y < 0 || y >= height) return 0f;
            return values[y * width + x] / CellArea;
        }

        public float Sample(string id, Vector2 world)
        {
            int i = Index(world);
            return i >= 0 && concentrations.TryGetValue(id, out var values) ? values[i] / CellArea : 0f;
        }

        public float Deposit(string id, Vector2 world, float amount)
        {
            if (!concentrations.TryGetValue(id, out var values) || !Finite(amount) || amount <= 0f) return 0f;
            int i = Index(world);
            if (i < 0) return 0f;
            values[i] += amount;
            return amount;
        }

        public float Withdraw(string id, Vector2 world, float requested)
        {
            if (!concentrations.TryGetValue(id, out var values) || !Finite(requested) || requested <= 0f) return 0f;
            int i = Index(world);
            if (i < 0) return 0f;
            float amount = Mathf.Min(requested, values[i]);
            values[i] -= amount;
            return amount;
        }

        public float Total(string id)
        {
            if (!concentrations.TryGetValue(id, out var a)) return 0f;
            double sum = 0;
            foreach (float value in a) sum += value;
            return (float)sum;
        }

        void FixedUpdate()
        {
            if (!Finite(fixedStep) || fixedStep <= 0f) return;
            accumulator += Time.fixedDeltaTime;
            int steps = 0;
            while (accumulator >= fixedStep && steps++ < 16)
            {
                Step(fixedStep);
                accumulator -= fixedStep;
            }
        }

        /// <summary>
        /// Conservative first-order finite-volume upwind transport. Every interior face
        /// moves the exact same mass out of its donor and into its receiver.
        /// Domain boundaries are impermeable. CFL substeps keep donor mass nonnegative.
        /// This transports passive dissolved quantities; it does not solve fluid dynamics.
        /// </summary>
        public void Advect(float dt)
        {
            if (!fluid || !Finite(dt) || dt <= 0f) return;
            // Sample face speeds once, and choose a substep from the maximum total
            // outward flux of ANY donor voxel. This is stricter than a center-speed
            // estimate and gives monotone/nonnegative first-order upwind updates.
            float[] right = new float[width * height];
            float[] top = new float[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                if (x + 1 < width)
                {
                    Vector2 face = transform.TransformPoint(new Vector3(
                        (x + 1f - width * .5f) * cellSize,
                        (y + .5f - height * .5f) * cellSize));
                    right[i] = fluid.Sample(face).x;
                    if (!Finite(right[i])) return;
                }
                if (y + 1 < height)
                {
                    Vector2 face = transform.TransformPoint(new Vector3(
                        (x + .5f - width * .5f) * cellSize,
                        (y + 1f - height * .5f) * cellSize));
                    top[i] = fluid.Sample(face).y;
                    if (!Finite(top[i])) return;
                }
            }
            float maxOutflow = 0f;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                float outward = Mathf.Max(0f, right[i]) + Mathf.Max(0f, top[i]);
                if (x > 0) outward += Mathf.Max(0f, -right[i - 1]);
                if (y > 0) outward += Mathf.Max(0f, -top[i - width]);
                maxOutflow = Mathf.Max(maxOutflow, outward);
            }
            int count = Mathf.Max(1, Mathf.CeilToInt(maxOutflow * dt /
                (cellSize * Mathf.Max(.05f, advectionCfl))));
            float step = dt / count;
            foreach (var pair in concentrations)
            {
                float[] src = pair.Value;
                float[] dst = buffers[pair.Key];
                for (int pass = 0; pass < count; pass++)
                {
                    Array.Copy(src, dst, src.Length);
                    for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        int i = y * width + x;
                        if (x + 1 < width)
                        {
                            float vx = right[i];
                            int j = i + 1;
                            float flux = vx * step / cellSize *
                                (vx >= 0f ? src[i] : src[j]);
                            dst[i] -= flux;
                            dst[j] += flux;
                        }
                        if (y + 1 < height)
                        {
                            float vy = top[i];
                            int j = i + width;
                            float flux = vy * step / cellSize *
                                (vy >= 0f ? src[i] : src[j]);
                            dst[i] -= flux;
                            dst[j] += flux;
                        }
                    }
                    var tmp = src; src = dst; dst = tmp;
                }
                concentrations[pair.Key] = src;
                buffers[pair.Key] = dst;
            }
        }

        public void Step(float dt)
        {
            if (!Finite(dt) || dt <= 0f) return;
            Advect(dt);
            foreach (var speciesDefinition in species)
            {
                if (!speciesDefinition || !concentrations.TryGetValue(speciesDefinition.stableId, out var src)) continue;
                var dst = buffers[speciesDefinition.stableId];
                // Explicit 5-point diffusion: coefficient bounded to the 2D stability condition.
                // Excessive requested diffusion requires substepping; do not silently destabilize.
                float requested = Mathf.Max(0f, speciesDefinition.diffusionCoefficient) * dt / CellArea;
                int substeps = Mathf.Max(1, Mathf.CeilToInt(requested / .24f));
                float alpha = requested / substeps;
                for (int s = 0; s < substeps; s++)
                {
                    for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        int i = y * width + x;
                        // Reflective/no-flux boundary, preserving mass under diffusion.
                        float c = src[i];
                        float l = src[y * width + Mathf.Max(0, x - 1)];
                        float r = src[y * width + Mathf.Min(width - 1, x + 1)];
                        float b = src[Mathf.Max(0, y - 1) * width + x];
                        float t = src[Mathf.Min(height - 1, y + 1) * width + x];
                        dst[i] = Mathf.Max(0f, c + alpha * (l+r+b+t-4f*c));
                    }
                    var temp = src; src = dst; dst = temp;
                }
                float retention = Mathf.Exp(-Mathf.Max(0f, speciesDefinition.decayRate) * dt);
                for (int i = 0; i < src.Length; i++)
                {
                    DecayedMass += src[i] * (1f - retention);
                    src[i] *= retention;
                }
                concentrations[speciesDefinition.stableId] = src;
                buffers[speciesDefinition.stableId] = dst;
            }
        }

        /// <summary>Local equal-mass neutralization A + B -> 2 product.
        /// Exact integrated bimolecular rate for each voxel, avoiding negative reactants at large dt.</summary>
        public float React(string a, string b, string product, float rate, float dt)
        {
            if (a == b || a == product || b == product || !Finite(rate) || !Finite(dt) || rate <= 0f || dt <= 0f)
                return 0f;
            if (!concentrations.TryGetValue(a, out var left) || !concentrations.TryGetValue(b, out var right)
                || !concentrations.TryGetValue(product, out var output)) return 0f;
            float total = 0f;
            for (int i = 0; i < left.Length; i++)
            {
                double low = Math.Min(left[i], right[i]) / CellArea;
                double high = Math.Max(left[i], right[i]) / CellArea;
                if (low <= 0) continue;
                double difference = high - low;
                double remaining;
                if (difference < 1e-7 * high)
                    remaining = low / (1.0 + rate * low * dt);
                else
                {
                    double ratio = low / high * Math.Exp(-rate * difference * dt);
                    remaining = difference * ratio / (1.0 - ratio);
                }
                float extent = Mathf.Clamp((float)((low - remaining) * CellArea), 0f, Mathf.Min(left[i], right[i]));
                left[i] -= extent; right[i] -= extent; output[i] += 2f * extent;
                total += extent;
            }
            return total;
        }
    }
}
