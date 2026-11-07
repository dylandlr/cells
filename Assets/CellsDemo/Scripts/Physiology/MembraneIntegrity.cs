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
        [Range(0f, .2f)] public float contourAmplitude;
        public float contourPhase;
        readonly float[] stretch = new float[SegmentCount];
        bool deformed;
        public int GeometryRevision { get; private set; }

        public float GetStretch(int segment) => stretch[segment] == 0f ? 1f : stretch[segment];
        public void SetStretch(int segment, float value)
        {
            if (segment < 0 || segment >= SegmentCount) throw new ArgumentOutOfRangeException(nameof(segment));
            if (!Finite(value)) return;
            value = Mathf.Clamp(value, .72f, 1.08f);
            if (GetStretch(segment) == value) return;
            stretch[segment] = value;
            deformed = true;
            GeometryRevision++;
        }

        public float StretchAt(Vector2 direction)
        {
            float index = Mathf.Repeat(Mathf.Atan2(direction.y, direction.x) * SegmentCount / (2f * Mathf.PI), SegmentCount);
            int left = Mathf.FloorToInt(index);
            return Mathf.Lerp(GetStretch(left), GetStretch((left + 1) % SegmentCount), index - left);
        }

        /// <summary>Static radial morphology shared by contact sampling and rendering.</summary>
        public float ContourScale(Vector2 direction)
        {
            float a = Mathf.Clamp(contourAmplitude, 0f, .2f);
            float angle = Mathf.Atan2(direction.y, direction.x) - contourPhase;
            return 1f + a * (.65f * Mathf.Cos(2f * angle) + .25f * Mathf.Sin(3f * angle) + .1f * Mathf.Cos(5f * angle));
        }

        public Vector2 SurfacePoint(int segment, float radius) => DirectionOf(segment) * (radius * ContourScale(DirectionOf(segment)) * GetStretch(segment));

        public float EnclosedArea(float radius)
        {
            if (deformed)
            {
                // Polar quadrature uses the same interpolated contour as the optical shader.
                float area = 0f;
                const int samples = 128;
                for (int i = 0; i < samples; i++)
                {
                    float angle = (i + .5f) * 2f * Mathf.PI / samples;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    float r = radius * ContourScale(direction) * StretchAt(direction);
                    area += r * r * Mathf.PI / samples;
                }
                return area;
            }
            float a = Mathf.Clamp(contourAmplitude, 0f, .2f);
            // Analytic polar area: integral r(theta)^2 / 2. Harmonics are orthogonal.
            return Mathf.PI * radius * radius * (1f + .2475f * a * a);
        }

        public float ContactLength(int segment, float radius)
        {
            if (contourAmplitude <= 0f && !deformed) return 2f * Mathf.PI * radius / SegmentCount;
            float angle = segment * 2f * Mathf.PI / SegmentCount;
            float half = Mathf.PI / SegmentCount;
            Vector2 left = new Vector2(Mathf.Cos(angle - half), Mathf.Sin(angle - half));
            Vector2 right = new Vector2(Mathf.Cos(angle + half), Mathf.Sin(angle + half));
            return Vector2.Distance(left * (ContourScale(left) * StretchAt(left)), right * (ContourScale(right) * StretchAt(right))) * radius;
        }
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
