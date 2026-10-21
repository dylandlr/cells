using UnityEngine;
using Cells.Fluid;

namespace Cells.Physiology
{
    /// <summary>
    /// Local-frame, 32-node overdamped position-based membrane with an interior
    /// mass point. Boundary nodes have independent displacement, velocity, drag,
    /// rest-length and area constraints; integrity changes the local compliance.
    /// This is an effective mesoscale soft-body, NOT molecular dynamics or
    /// calibrated membrane hydrodynamics. The cell's locomotor transform remains
    /// owned by CellMotility so thrust cannot be counted twice.
    /// </summary>
    [DefaultExecutionOrder(-35)]
    [RequireComponent(typeof(MembraneIntegrity))]
    public sealed class SoftCellBody2D : MonoBehaviour
    {
        public const int Nodes = MembraneIntegrity.SegmentCount;
        public MembraneIntegrity membrane;
        public FluidGrid2D fluid;
        public CellFlagellarActuator flagellum;
        public CellBiomassGrowth growth;
        [Min(.1f)] public float restRadius = .9f;
        [Range(0f,1f)] public float divisionConstriction; // Experimental effective equatorial constriction
        [Range(.03f,.6f)] public float constrictionBand = .22f;
        public float ConstrictionSpan => Vector2.Distance(Node(0),Node(Nodes/2));
        [Range(.01f, 1f)] public float edgeCompliance = .16f;
        [Range(.01f, 1f)] public float areaCompliance = .12f;
        [Range(.01f, 1f)] public float radialCompliance = .18f;
        [Range(0f, 30f)] public float dragRate = 5f;
        [Range(1, 12)] public int constraintIterations = 5;
        [Range(.001f, .03f)] public float maxSubstep = .01f;

        readonly Vector2[] positions = new Vector2[Nodes];
        readonly Vector2[] previous = new Vector2[Nodes];
        readonly Vector2[] velocities = new Vector2[Nodes];
        readonly Vector2[] rest = new Vector2[Nodes];
        readonly Vector2[] pendingContact = new Vector2[Nodes];
        readonly float[] sectorStress = new float[Nodes];
        readonly float[] contactStress = new float[Nodes];
        public float MaximumStress { get; private set; }
        public float StressAt(int segment) => sectorStress[(segment % Nodes + Nodes) % Nodes];
        public float LastContactMagnitude { get; private set; }
        Vector2 core, previousCore, coreVelocity;
        float restArea;
        bool initialized;
        public bool Initialized => initialized;
        public Vector2 Core => core;
        public Vector2 Node(int i) => positions[(i % Nodes + Nodes) % Nodes];
        public Vector2 NodeVelocity(int i) => velocities[(i % Nodes + Nodes) % Nodes];
        public float CurrentRadius => restRadius * (growth ? growth.VisualRadiusScale : 1f);
        public Vector2 SurfacePoint(int i) => Node(i);

        /// <summary>Queues a bounded local membrane correction. This is coarse
        /// position-level contact coupling, not a momentum-conserving impulse.</summary>
        public void QueueContact(int segment, Vector2 displacement)
        {
            if (!initialized || segment<0 || segment>=Nodes ||
                !float.IsFinite(displacement.x) || !float.IsFinite(displacement.y)) return;
            pendingContact[segment] += Vector2.ClampMagnitude(displacement,.015f);
            contactStress[segment] = Mathf.Clamp01(contactStress[segment] + displacement.magnitude * 8f);
            pendingContact[segment] = Vector2.ClampMagnitude(pendingContact[segment],.035f);
        }

        void Awake() => Initialize();
        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Initialize()
        {
            if (!membrane) membrane = GetComponent<MembraneIntegrity>();
            if (!flagellum) flagellum = GetComponent<CellFlagellarActuator>();
            if (!growth) growth = GetComponent<CellBiomassGrowth>();
            if (fluid == null) fluid = GetComponentInParent<FluidGrid2D>();
            for (int i=0;i<Nodes;i++)
            {
                rest[i] = membrane ? membrane.SurfacePoint(i,restRadius)
                    : MembraneIntegrity.DirectionOf(i)*restRadius;
                positions[i]=previous[i]=rest[i];
                velocities[i]=Vector2.zero;
                pendingContact[i]=Vector2.zero;
                sectorStress[i]=contactStress[i]=0f;
            }
            LastContactMagnitude=0f;
            core=previousCore=coreVelocity=Vector2.zero;
            restArea=Mathf.Abs(SignedArea());
            initialized=true;
        }

        public float SignedArea()
        {
            float sum=0f;
            for(int i=0;i<Nodes;i++)
                sum+=Vector3.Cross(positions[i],positions[(i+1)%Nodes]).z;
            return sum*.5f;
        }

        public void Tick(float dt)
        {
            if(!initialized) Initialize();
            if(!(dt>0f) || float.IsNaN(dt) || float.IsInfinity(dt))return;
            int count=Mathf.CeilToInt(Mathf.Min(dt,.25f)/maxSubstep);
            float h=Mathf.Min(dt,.25f)/count;
            UpdateGrowthRestShape();
            LastContactMagnitude=0f;
            for(int i=0;i<Nodes;i++) LastContactMagnitude+=pendingContact[i].magnitude;
            for(int sub=0;sub<count;sub++) Step(h, sub==0);
            for(int i=0;i<Nodes;i++) pendingContact[i]=Vector2.zero;
            MeasureStress();
        }

        // Biomass changes the *authoritative* rest geometry instead of scaling the render.
        // This is a 2D area proxy and does not establish physical volume or material density.
        void UpdateGrowthRestShape()
        {
            float scale=growth ? growth.VisualRadiusScale : 1f;
            for(int i=0;i<Nodes;i++)
            {
                Vector2 shape=(membrane ? membrane.SurfacePoint(i,restRadius)
                    : MembraneIntegrity.DirectionOf(i)*restRadius)*scale;
                if(divisionConstriction>0f)
                {
                    float band=Mathf.Max(.03f,constrictionBand)*Mathf.Max(.1f,restRadius*scale);
                    float equatorialWeight=Mathf.Exp(-Mathf.Pow(shape.y/band,2f));
                    shape.x *= 1f - .65f*divisionConstriction*equatorialWeight;
                }
                rest[i]=shape;
            }
            float area=0f;
            for(int i=0;i<Nodes;i++)
                area+=Vector3.Cross(rest[i],rest[(i+1)%Nodes]).z;
            restArea=Mathf.Abs(area)*.5f;
        }

        // Strain is an observable diagnostic derived from actual deformed edges.
        // Contact memory decays over time; no mechanical injury is applied
        // until a calibrated force-to-damage contract exists.
        void MeasureStress()
        {
            MaximumStress=0f;
            for(int i=0;i<Nodes;i++)
            {
                int j=(i+1)%Nodes;
                float reference=(rest[j]-rest[i]).magnitude;
                float strain=reference>1e-6f
                    ? Mathf.Abs((positions[j]-positions[i]).magnitude/reference-1f):0f;
                sectorStress[i]=Mathf.Clamp01(strain*1.5f + contactStress[i]);
                MaximumStress=Mathf.Max(MaximumStress,sectorStress[i]);
                contactStress[i]*=.88f;
            }
        }

        void Step(float dt, bool firstSubstep)
        {
            float damp=Mathf.Exp(-dragRate*dt);
            previousCore=core;
            core+=coreVelocity*dt;
            for(int i=0;i<Nodes;i++)
            {
                previous[i]=positions[i];
                Vector2 localFlow=Vector2.zero;
                if(fluid)
                {
                    Vector2 worldPoint=transform.TransformPoint(positions[i]);
                    Vector2 worldFlow=fluid.Sample(worldPoint);
                    localFlow=transform.InverseTransformDirection(worldFlow);
                }
                velocities[i]=Vector2.Lerp(velocities[i],localFlow,1f-damp);
                positions[i]+=velocities[i]*dt;
                if(firstSubstep) positions[i]+=pendingContact[i];
                // Apply an explicit, localized constriction displacement at the
                // midplane (local y=0); this acts on membrane DOFs, not shaders.
                if(divisionConstriction>0f)
                {
                    float radius=Mathf.Max(.1f,CurrentRadius);
                    float band=Mathf.Max(.03f,constrictionBand)*radius;
                    float weight=Mathf.Exp(-Mathf.Pow(positions[i].y/band,2f));
                    // Actuation is rate-limited and resists restoration by
                    // the shape and area constraints below.
                    positions[i].x-=Mathf.Sign(positions[i].x)*Mathf.Min(
                        Mathf.Abs(positions[i].x),divisionConstriction*weight*.25f*dt);
                }
                if(flagellum && i>=Nodes/2-2 && i<=Nodes/2+2)
                {
                    // A localized moving posterior attachment deforms the shell;
                    // whole-body propulsion is computed separately by the motor.
                    float phase=flagellum.PhaseRadians+i*.26f;
                    positions[i].y+=Mathf.Sin(phase)*.017f*flagellum.ActualActivation*dt;
                }
            }
            for(int pass=0;pass<constraintIterations;pass++)
            {
                // A scalar pressure constraint acts over all material nodes.
                float currentArea=Mathf.Abs(SignedArea());
                float pressure=Mathf.Clamp((restArea-currentArea)/Mathf.Max(.01f,restArea),-.5f,.5f);
                for(int i=0;i<Nodes;i++)
                {
                    int j=(i+1)%Nodes;
                    float strength=membrane ? membrane.GetIntegrity(i):1f;
                    float localCompliance=edgeCompliance*Mathf.Max(.08f,strength);
                    Vector2 difference=positions[j]-positions[i];
                    float length=difference.magnitude;
                    float desired=(rest[j]-rest[i]).magnitude;
                    if(length>1e-6f)
                    {
                        Vector2 correction=difference*((length-desired)/length*.5f*localCompliance);
                        positions[i]+=correction; positions[j]-=correction;
                    }
                }
                for(int i=0;i<Nodes;i++)
                {
                    float integrity=membrane ? membrane.GetIntegrity(i):1f;
                    Vector2 desired=core+rest[i]*(.84f+.16f*integrity);
                    positions[i]=Vector2.Lerp(positions[i],desired,
                        radialCompliance*(.2f+.8f*integrity));
                    Vector2 fromCore=positions[i]-core;
                    positions[i]+=fromCore.normalized*pressure*areaCompliance*.025f;
                }
                // The core is a dynamic interior mesh node, connected through
                // radial constraints and damped toward the membrane centroid.
                Vector2 centroid=Vector2.zero;
                for(int i=0;i<Nodes;i++)centroid+=positions[i];
                core=Vector2.Lerp(core,centroid/Nodes,.12f);
            }
            coreVelocity=(core-previousCore)/dt;
            for(int i=0;i<Nodes;i++)
            {
                velocities[i]=(positions[i]-previous[i])/dt;
                // Prevent a degenerate mesh if injured nodes collapse.
                if(!float.IsFinite(positions[i].x)||!float.IsFinite(positions[i].y)
                    ||positions[i].sqrMagnitude>CurrentRadius*CurrentRadius*16f)
                {
                    positions[i]=rest[i];
                    velocities[i]=Vector2.zero;
                }
            }
        }
    }
}
