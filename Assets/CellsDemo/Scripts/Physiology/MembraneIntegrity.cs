using System;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>32 material sectors around a circular membrane. Integrity is their mean;
    /// local failure can rupture an otherwise healthy cell. Units are phenomenological.</summary>
    public sealed class MembraneIntegrity : MonoBehaviour
    {
        public const int SegmentCount = 32;
        [Range(0f, 1f)] public float integrity = 1f;
        [Range(0f, 1f)] public float chemicalResistance = .25f;
        [Min(0f)] public float repairEnergyPerUnit = 3f;
        [Range(0f, 1f)] public float ruptureThreshold = .05f;
        readonly float[] segments = new float[SegmentCount];
        float synchronizedIntegrity = float.NaN;
        int revision;
        public int Revision { get { EnsureSegments(); return revision; } }
        public float WeakestIntegrity
        {
            get
            {
                EnsureSegments();
                float weakest = 1f;
                foreach (float value in segments) weakest = Mathf.Min(weakest, value);
                return weakest;
            }
        }
        public bool ruptured => WeakestIntegrity <= Mathf.Clamp01(ruptureThreshold);
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        void Awake() => EnsureSegments();

        // Preserve existing serialized scalar values and explicit scenario resets.
        void EnsureSegments()
        {
            if (integrity == synchronizedIntegrity) return;
            integrity = Finite(integrity) ? Mathf.Clamp01(integrity) : 1f;
            for (int i = 0; i < SegmentCount; i++) segments[i] = integrity;
            synchronizedIntegrity = integrity;
            revision++;
        }

        void UpdateMean()
        {
            float sum = 0f;
            foreach (float value in segments) sum += value;
            integrity = synchronizedIntegrity = sum / SegmentCount;
            revision++;
        }

        public static Vector2 DirectionOf(int segment)
        {
            float angle = segment * 2f * Mathf.PI / SegmentCount;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public static int SegmentAt(Vector2 localDirection)
        {
            int index = Mathf.RoundToInt(Mathf.Atan2(localDirection.y, localDirection.x) * SegmentCount / (2f * Mathf.PI));
            return (index % SegmentCount + SegmentCount) % SegmentCount;
        }

        public float GetIntegrity(int segment)
        {
            if (segment < 0 || segment >= SegmentCount) throw new ArgumentOutOfRangeException(nameof(segment));
            EnsureSegments();
            return segments[segment];
        }

        /// <summary>Uniform exposure, retained for whole-cell challenges. Returns mean integrity lost.</summary>
        public float ApplyExposure(float incomingDamage)
        {
            EnsureSegments();
            if (!Finite(incomingDamage) || incomingDamage <= 0f) return 0f;
            float effective = incomingDamage * (1f - Mathf.Clamp01(chemicalResistance));
            float lost = 0f;
            for (int i = 0; i < SegmentCount; i++)
            {
                float amount = Mathf.Min(segments[i], effective);
                segments[i] -= amount; lost += amount;
            }
            UpdateMean();
            return lost / SegmentCount;
        }

        /// <summary>Damage is in local-sector integrity units; result is whole-membrane mean loss.</summary>
        public float ApplyExposureToSegment(int segment, float incomingLocalDamage)
        {
            float current = GetIntegrity(segment);
            if (!Finite(incomingLocalDamage) || incomingLocalDamage <= 0f) return 0f;
            float loss = Mathf.Min(current, incomingLocalDamage * (1f - Mathf.Clamp01(chemicalResistance)));
            segments[segment] -= loss;
            if (loss > 0f) UpdateMean();
            return loss / SegmentCount;
        }

        public float Repair(float desiredIntegrity, CellMetabolism metabolism)
        {
            if (!metabolism || !metabolism.alive || ruptured || !Finite(desiredIntegrity)) return 0f;
            float target = Mathf.Clamp01(desiredIntegrity);
            float missing = 0f;
            for (int i = 0; i < SegmentCount; i++) missing += Mathf.Max(0f, target - segments[i]);
            missing /= SegmentCount;
            if (missing <= 0f) return 0f;
            float restored = repairEnergyPerUnit <= 0f ? missing : metabolism.Spend(missing * repairEnergyPerUnit) / repairEnergyPerUnit;
            float fraction = restored / missing;
            for (int i = 0; i < SegmentCount; i++) segments[i] += Mathf.Max(0f, target - segments[i]) * fraction;
            UpdateMean();
            return restored;
        }
    }
}
