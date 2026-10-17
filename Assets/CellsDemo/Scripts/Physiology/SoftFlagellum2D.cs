using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Overdamped segmented flagellum geometry. Motor phase sets preferred curvature;
    /// nodes relax under edge-length, bending and drag constraints, then drive rendering.
    /// Coupled thrust is still provided by CellFlagellarActuator's coarse dipole model,
    /// rather than calculated from a resolved fluid traction integral.
    /// </summary>
    [RequireComponent(typeof(CellFlagellarActuator))]
    [DefaultExecutionOrder(-25)]
    public sealed class SoftFlagellum2D : MonoBehaviour
    {
        public const int NodeCount = 18;
        public CellFlagellarActuator motor;
        [Range(.02f,1f)] public float motorStiffness = .2f;
        [Range(.02f,1f)] public float edgeStiffness = .7f;
        [Range(1,12)] public int iterations = 5;
        [Range(0f,30f)] public float drag = 7f;
        [Range(.001f,.03f)] public float maxSubstep = .01f;
        readonly Vector2[] nodes = new Vector2[NodeCount];
        readonly Vector2[] prev = new Vector2[NodeCount];
        readonly Vector2[] velocity = new Vector2[NodeCount];
        bool initialized;
        public bool Initialized => initialized;
        public Vector2 Node(int index) => nodes[Mathf.Clamp(index,0,NodeCount-1)];

        void Awake() => Initialize();
        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Initialize()
        {
            if(!motor)motor=GetComponent<CellFlagellarActuator>();
            for(int i=0;i<NodeCount;i++)
            {
                Vector2 p=Desired(i);
                nodes[i]=prev[i]=p;
                velocity[i]=Vector2.zero;
            }
            initialized=true;
        }

        Vector2 Desired(int i)
        {
            float u=i/(float)(NodeCount-1);
            return motor ? motor.SampleCenterline(u)
                : Vector2.left * (.78f+u);
        }

        public void Tick(float dt)
        {
            if(!(dt>0f)||float.IsNaN(dt)||float.IsInfinity(dt))return;
            if(!initialized)Initialize();
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Min(dt,.25f)/maxSubstep));
            float h=Mathf.Min(dt,.25f)/steps;
            for(int sub=0;sub<steps;sub++)
            {
                float damping=Mathf.Exp(-drag*h);
                nodes[0]=Desired(0);
                for(int i=1;i<NodeCount;i++)
                {
                    prev[i]=nodes[i];
                    velocity[i]*=damping;
                    nodes[i]+=velocity[i]*h;
                }
                for(int pass=0;pass<iterations;pass++)
                {
                    nodes[0]=Desired(0);
                    for(int i=1;i<NodeCount;i++)
                    {
                        Vector2 desired=Desired(i);
                        float integrity=motor?motor.structuralIntegrity:1f;
                        nodes[i]=Vector2.Lerp(nodes[i],desired,
                            motorStiffness*Mathf.Clamp01(integrity));
                        Vector2 delta=nodes[i]-nodes[i-1];
                        float distance=delta.magnitude;
                        float segmentLength=(motor?motor.length:1f)/(NodeCount-1);
                        if(distance>1e-6f)
                        {
                            Vector2 error=delta*((distance-segmentLength)/distance*edgeStiffness);
                            nodes[i]-=error*(i==1?1f:.5f);
                            if(i>1)nodes[i-1]+=error*.5f;
                        }
                    }
                }
                for(int i=1;i<NodeCount;i++)
                {
                    velocity[i]=(nodes[i]-prev[i])/h;
                    if(!float.IsFinite(nodes[i].x)||!float.IsFinite(nodes[i].y)
                       ||(nodes[i]-nodes[0]).sqrMagnitude>16f)
                    {
                        nodes[i]=Desired(i);
                        velocity[i]=Vector2.zero;
                    }
                }
            }
        }
    }
}
