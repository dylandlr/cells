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
        public CellBiomassGrowth growth;
        public float EffectiveBodyDrag => bodyDrag * (growth ? growth.VisualRadiusScale : 1f);
        public float EffectiveRotationalBodyDrag => rotationalBodyDrag * Mathf.Pow(growth ? growth.VisualRadiusScale : 1f, 3f);
        [Min(.01f)] public float parallelDrag = .48f;
        [Min(.01f)] public float perpendicularDrag = .95f;
        [Min(.01f)] public float bodyDrag = 3.5f;
        [Min(0f)] public float couplingGain = .65f;
        [Min(0f)] public float maximumSwimSpeed = .65f;
        [Min(0f)] public float fluidReactionGain = .45f;
        [Min(.01f)] public float rotationalBodyDrag = 1.15f;
        [Min(0f)] public float maximumAngularSpeedDegrees = 55f;
        public Vector2 LocalSwimmingVelocity { get; private set; }
        public float SegmentDissipation { get; private set; }
        public float LocalAngularVelocityDegrees { get; private set; }
        public float StrokeMagnitude { get; private set; }

        readonly Vector2[] previous = new Vector2[SoftFlagellum2D.NodeCount];
        bool hasHistory;

        void Awake()
        {
            if (!tail) tail = GetComponent<SoftFlagellum2D>();
            if (!motor) motor = GetComponent<CellFlagellarActuator>();
            if (!growth) growth = GetComponent<CellBiomassGrowth>();
        }

        public void ResetHistory()
        {
            hasHistory = false;
            LocalSwimmingVelocity = Vector2.zero;
            LocalAngularVelocityDegrees = 0f;
            SegmentDissipation = StrokeMagnitude = 0f;
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);
        void OnDisable() => ResetHistory();

        public void Tick(float dt)
        {
            if (!tail) tail = GetComponent<SoftFlagellum2D>();
            if (!motor) motor = GetComponent<CellFlagellarActuator>();
            if (!tail || !tail.isActiveAndEnabled || !tail.Initialized || !motor
                || !motor.isActiveAndEnabled || dt <= 0f || !float.IsFinite(dt))
            {
                ResetHistory();
                return;
            }
            if (!hasHistory)
            {
                for (int i=0; i<previous.Length; i++) previous[i]=tail.Node(i);
                hasHistory=true;
                LocalSwimmingVelocity=Vector2.zero;
                LocalAngularVelocityDegrees=0f;
                return;
            }

            bool active = motor && motor.ActualActivation > 0f
                && motor.structuralIntegrity > 0f && motor.EffectiveThrust > 0f;
            Vector2 dragWeightedStroke=Vector2.zero;
            float strokeTorque=0f;
            float dragX=EffectiveBodyDrag, dragY=EffectiveBodyDrag, dragXY=0f;
            float dragXOmega=0f, dragYOmega=0f, dragOmega=EffectiveRotationalBodyDrag;
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
                Vector2 midpoint=.5f*(tail.Node(i)+tail.Node(i-1));
                Vector2 rotated=new Vector2(-midpoint.y,midpoint.x);
                float dxx=(parallelDrag*tangent.x*tangent.x+
                    perpendicularDrag*normal.x*normal.x)*segmentLength;
                float dxy=(parallelDrag*tangent.x*tangent.y+
                    perpendicularDrag*normal.x*normal.y)*segmentLength;
                float dyy=(parallelDrag*tangent.y*tangent.y+
                    perpendicularDrag*normal.y*normal.y)*segmentLength;
                dragXOmega+=dxx*rotated.x+dxy*rotated.y;
                dragYOmega+=dxy*rotated.x+dyy*rotated.y;
                dragOmega+=rotated.x*(dxx*rotated.x+dxy*rotated.y)
                    +rotated.y*(dxy*rotated.x+dyy*rotated.y);
                strokeTorque+=midpoint.x*resistance.y-midpoint.y*resistance.x;
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
                LocalAngularVelocityDegrees=0f;
                SegmentDissipation=StrokeMagnitude=0f;
                return;
            }
            // Integrate force and torque simultaneously; appendage geometry creates
            // off-diagonal translation/rotation couplings in the 3x3 resistance.
            PlanarMobilityBalance.Solve(
                dragX,dragXY,dragXOmega,dragY,dragYOmega,dragOmega,
                dragWeightedStroke.x,dragWeightedStroke.y,strokeTorque,
                out double ux,out double uy,out double omega);
            Vector2 predicted=new Vector2((float)ux,(float)uy)*couplingGain;
            LocalSwimmingVelocity=Vector2.ClampMagnitude(predicted,maximumSwimSpeed);
            if (!float.IsFinite(LocalSwimmingVelocity.x) || !float.IsFinite(LocalSwimmingVelocity.y))
                LocalSwimmingVelocity=Vector2.zero;
            LocalAngularVelocityDegrees = Mathf.Clamp(
                (float)(omega*Mathf.Rad2Deg)*couplingGain,
                -maximumAngularSpeedDegrees,maximumAngularSpeedDegrees);
            if (!float.IsFinite(LocalAngularVelocityDegrees))
                LocalAngularVelocityDegrees=0f;
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
