using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cells.Substances
{
    /// <summary>Independent finite-volume-like concentration grids; no visual metadata enters transport.</summary>
    public sealed class MultiChemicalField2D : MonoBehaviour
    {
        public SubstanceDefinition[] species = Array.Empty<SubstanceDefinition>();
        [Min(3)] public int width = 96;
        [Min(3)] public int height = 54;
        [Min(.1f)] public float cellSize = .25f;
        [Min(.001f)] public float fixedStep = .02f;
        readonly Dictionary<string, float[]> concentrations = new Dictionary<string, float[]>();
        readonly Dictionary<string, float[]> buffers = new Dictionary<string, float[]>();
        float accumulator;

        void Awake() => Initialize();

        public void Initialize()
        {
            concentrations.Clear(); buffers.Clear();
            foreach (var s in species)
            {
                if (!s || string.IsNullOrWhiteSpace(s.stableId)) continue;
                if (concentrations.ContainsKey(s.stableId))
                {
                    Debug.LogError("Duplicate substance ID: " + s.stableId, this);
                    continue;
                }
                concentrations.Add(s.stableId, new float[width * height]);
                buffers.Add(s.stableId, new float[width * height]);
            }
        }

        int Index(Vector2 world)
        {
            Vector2 local = transform.InverseTransformPoint(world);
            int x = Mathf.Clamp(Mathf.FloorToInt(local.x / cellSize + width * .5f), 0, width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(local.y / cellSize + height * .5f), 0, height - 1);
            return y * width + x;
        }

        public float Sample(string id, Vector2 world)
        {
            return concentrations.TryGetValue(id, out var values) ? values[Index(world)] : 0f;
        }

        public float Deposit(string id, Vector2 world, float amount)
        {
            if (!concentrations.TryGetValue(id, out var values) || amount <= 0f) return 0f;
            int i = Index(world);
            values[i] += amount;
            return amount;
        }

        public float Withdraw(string id, Vector2 world, float requested)
        {
            if (!concentrations.TryGetValue(id, out var values) || requested <= 0f) return 0f;
            int i = Index(world);
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
            accumulator += Time.fixedDeltaTime;
            int steps = 0;
            while (accumulator >= fixedStep && steps++ < 16)
            {
                Step(fixedStep);
                accumulator -= fixedStep;
            }
        }

        public void Step(float dt)
        {
            if (dt <= 0f) return;
            foreach (var speciesDefinition in species)
            {
                if (!speciesDefinition || !concentrations.TryGetValue(speciesDefinition.stableId, out var src)) continue;
                var dst = buffers[speciesDefinition.stableId];
                // Explicit 5-point diffusion: coefficient bounded to the 2D stability condition.
                // Excessive requested diffusion requires substepping; do not silently destabilize.
                float requested = speciesDefinition.diffusionCoefficient * dt / (cellSize * cellSize);
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
                float retention = Mathf.Exp(-speciesDefinition.decayRate * dt);
                for (int i = 0; i < src.Length; i++) src[i] *= retention;
                concentrations[speciesDefinition.stableId] = src;
                buffers[speciesDefinition.stableId] = dst;
            }
        }
    }
}
