using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cells.Substances
{
    [Serializable]
    public struct TrackedParticle
    {
        public int id;
        public string substanceId;
        public Vector2 position;
        public Vector2 velocity;
        public float mass;
        public float radius;
    }

    /// <summary>
    /// Explicit suspended material, separate from the dissolved concentration grid.
    /// Positions change through imposed drift and stochastic kicks; this is a first-order
    /// Langevin-like approximation, not fully resolved fluid/particle mechanics.
    /// </summary>
    public sealed class TrackedParticleWorld : MonoBehaviour
    {
        public int seed = 631;
        [Min(0)] public float diffusivity = .015f;
        [Min(0)] public float velocityRelaxation = 8f;
        public Vector2 fluidDrift;
        [Min(1)] public int maxParticles = 5000;
        public Vector2 boundsMin = new Vector2(-12, -6);
        public Vector2 boundsMax = new Vector2(12, 6);
        readonly List<TrackedParticle> particles = new List<TrackedParticle>();
        System.Random random;
        int nextId = 1;
        public IReadOnlyList<TrackedParticle> Particles => particles;

        void Awake() => random = new System.Random(seed);

        public void ResetSimulation(int randomSeed)
        {
            seed = randomSeed;
            random = new System.Random(seed);
            particles.Clear();
            nextId = 1;
        }

        public int Spawn(string substanceId, Vector2 position, float mass, float radius)
        {
            if (particles.Count >= maxParticles || string.IsNullOrWhiteSpace(substanceId)
                || !MultiChemicalField2D.Finite(mass) || !MultiChemicalField2D.Finite(radius)
                || !MultiChemicalField2D.Finite(position.x) || !MultiChemicalField2D.Finite(position.y)
                || mass <= 0f || radius <= 0f) return -1;
            if (random == null) random = new System.Random(seed);
            int id = nextId++;
            particles.Add(new TrackedParticle {
                id = id, substanceId = substanceId, position = position,
                mass = mass, radius = radius, velocity = Vector2.zero
            });
            return id;
        }

        float Uniform() => (float)random.NextDouble();
        float Gaussian()
        {
            float a = Mathf.Max(1e-7f, Uniform());
            return Mathf.Sqrt(-2f * Mathf.Log(a)) * Mathf.Cos(2f * Mathf.PI * Uniform());
        }

        public void Step(float dt)
        {
            if (!MultiChemicalField2D.Finite(dt) || dt <= 0f) return;
            if (random == null) random = new System.Random(seed);
            float brownianSigma = Mathf.Sqrt(2f * Mathf.Max(0f, diffusivity) * dt);
            for (int i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                float relax = 1f - Mathf.Exp(-velocityRelaxation * dt);
                p.velocity += (fluidDrift - p.velocity) * relax;
                p.position += p.velocity * dt +
                    new Vector2(Gaussian(), Gaussian()) * brownianSigma;
                p.position.x = Reflect(p.position.x, boundsMin.x, boundsMax.x);
                p.position.y = Reflect(p.position.y, boundsMin.y, boundsMax.y);
                particles[i] = p;
            }
        }

        static float Reflect(float x, float min, float max)
        {
            if (max <= min) return min;
            float span = max - min;
            float phase = Mathf.Repeat(x - min, span * 2f);
            return min + (phase <= span ? phase : 2f * span - phase);
        }

        public bool TryConsume(string substanceId, Vector2 center, float reach,
            out TrackedParticle consumed)
        {
            consumed = default;
            if (!MultiChemicalField2D.Finite(reach) || reach < 0f) return false;
            float squared = reach * reach;
            for (int i = particles.Count-1; i>=0; i--)
            {
                var p = particles[i];
                if (p.substanceId != substanceId) continue;
                float contact = reach + p.radius;
                if ((p.position-center).sqrMagnitude > Mathf.Max(squared,contact*contact)) continue;
                consumed = p;
                particles.RemoveAt(i);
                return true;
            }
            consumed = default;
            return false;
        }

        public float TotalMass(string substanceId)
        {
            float sum = 0f;
            foreach (var p in particles) if (p.substanceId == substanceId) sum += p.mass;
            return sum;
        }

        void FixedUpdate() => Step(Time.fixedDeltaTime);
    }
}
