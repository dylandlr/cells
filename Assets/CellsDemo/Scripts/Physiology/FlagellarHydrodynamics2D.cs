using UnityEngine;
using Cells.Fluid;

namespace Cells.Physiology
{
    /// <summary>
    /// Coarse, overdamped resistive-force estimate from independently moving
    /// flagellar nodes. Computes force-free relative swimming velocity and
    /// a local, zero-net-force flow dipole. This is neither an immersed-boundary
    /// solver nor a calibrated 3-D Stokes mobility matrix.
    /// </summary>
    [RequireComponent(typeof(SoftFlagellum2D))]
    [DefaultExecutionOrder(-22)]
    public sealed class FlagellarHydrodynamics2D : MonoBehaviour
    {
        public SoftFlagellum2D tail;
        public CellFlagellarActuator motor;
        public FluidGrid2D fluid;
        [Min(.01f)] public float parallelDrag = .48f;
        [Min(.01f)] public float perpendicularDrag = .95f;
        [Min(.01f)] public float bodyDrag = 3.5f;
        [Min(0f)] public float couplingGain = .65f;
        [Min(0f)] public float maximumSwimSpeed = .65f;
        [Min(0f)] public float fluidReactionGain = .45f;
        public Vector2 LocalSwimmingVelocity { get; private set; }
        public float SegmentDissipation { get; private set; }
        public float StrokeMagnitude { get; private set; }

        readonly Vector2[] previous = new Vector2[SoftFlagellum2D.NodeCount];
        bool hasHistory;

        void Awake()
        {
            if (!tail) tail = GetComponent<SoftFlagellum2D>();
            if (!motor) motor = GetComponent<CellFlagellarActuator>();
        }

        public void ResetHistory()
        {
            hasHistory = false;
            LocalSwimmingVelocity = Vector2.zero;
            SegmentDissipation = StrokeMagnitude = 0f;
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (!tail) tail = GetComponent<SoftFlagellum2D>();
            if (!motor) motor = GetComponent<CellFlagellarActuator>();
            if (!tail || !tail.Initialized || dt <= 0f || !float.IsFinite(dt)) return;
            if (!hasHistory)
            {
                for (int i=0; i<previous.Length; i++) previous[i]=tail.Node(i);
                hasHistory=true;
                LocalSwimmingVelocity=Vector2.zero;
                return;
            }

            bool active = motor && motor.ActualActivation > 0f
                && motor.structuralIntegrity > 0f && motor.EffectiveThrust > 0f;
            Vector2 dragWeightedStroke=Vector2.zero;
            float dragX=bodyDrag, dragY=bodyDrag, dragXY=0f;
            SegmentDissipation=0f;
            StrokeMagnitude=0f;
            float segmentLength = motor ? motor.length/(SoftFlagellum2D.NodeCount-1) : .06f;
            for (int i=1; i<SoftFlagellum2D.NodeCount; i++)
            {
                Vector2 p=tail.Node(i);
                Vector2 change=(p-previous[i])/dt;
                previous[i]=p;
                Vector2 tangent=tail.Node(i)-tail.Node(i-1);
                float magnitude=tangent.magnitude;
                if (magnitude<1e-5f) continue;
                tangent/=magnitude;
                Vector2 normal=new Vector2(-tangent.y,tangent.x);
                float along=Vector2.Dot(change,tangent);
                float across=Vector2.Dot(change,normal);
                Vector2 resistance=(parallelDrag*along*tangent+
                    perpendicularDrag*across*normal)*segmentLength;
                dragWeightedStroke+=resistance;
                dragX+=(parallelDrag*tangent.x*tangent.x+
                    perpendicularDrag*normal.x*normal.x)*segmentLength;
                dragY+=(parallelDrag*tangent.y*tangent.y+
                    perpendicularDrag*normal.y*normal.y)*segmentLength;
                dragXY+=(parallelDrag*tangent.x*tangent.y+
                    perpendicularDrag*normal.x*normal.y)*segmentLength;
                SegmentDissipation+=Mathf.Max(0f,
                    (parallelDrag*along*along+perpendicularDrag*across*across)*segmentLength*dt);
                StrokeMagnitude+=change.magnitude*segmentLength;
            }
            previous[0]=tail.Node(0);
            // For a force-free swimmer, body drag balances the distributed
            // resistive forces of the deforming appendage. The fluid is used
            // independently by CellMotility for advection; do not count it twice.
            if (!active)
            {
                LocalSwimmingVelocity=Vector2.zero;
                SegmentDissipation=StrokeMagnitude=0f;
                return;
            }
            // Inclined segments couple X and Y drag; diagonal division is not
            // rotation invariant. Solve the complete symmetric 2x2 tensor.
            PlanarDragBalance.Solve(dragX,dragXY,dragY,
                dragWeightedStroke.x,dragWeightedStroke.y,out double ux,out double uy);
            Vector2 predicted=new Vector2((float)ux,(float)uy)*couplingGain;
            LocalSwimmingVelocity=Vector2.ClampMagnitude(predicted,maximumSwimSpeed);
            if (!float.IsFinite(LocalSwimmingVelocity.x) || !float.IsFinite(LocalSwimmingVelocity.y))
                LocalSwimmingVelocity=Vector2.zero;
            // Effective equal/opposite flow forcing; never insert a net fluid
            // momentum source. No legacy motor stresslet when this component exists.
            if (fluid && fluidReactionGain>0f)
            {
                Vector2 front=transform.TransformPoint(new Vector3(.45f,0f));
                Vector2 rear=transform.TransformPoint(tail.Node(SoftFlagellum2D.NodeCount-1));
                Vector2 world=transform.TransformDirection(
                    (Vector3)(-dragWeightedStroke*fluidReactionGain));
                fluid.AddForceDipole(front,rear,world);
            }
        }
    }
}
