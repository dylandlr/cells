using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Three movable, finite-radius internal compartments. Their positions and
    /// velocities obey overdamped local drag, core tethering, pairwise exclusion,
    /// and exclusion against every simulated membrane edge. No chemical mass is
    /// created by this visual/mechanical model; compartment labels are illustrative.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(SoftCellBody2D))]
    public sealed class InternalCompartments2D : MonoBehaviour
    {
        public const int Count = 3;
        public SoftCellBody2D body;
        [Range(0f,20f)] public float drag = 6f;
        [Range(0f,10f)] public float tetherStrength = 1.6f;
        [Range(1,12)] public int collisionIterations = 6;
        readonly Vector2[] locations = new Vector2[Count];
        readonly Vector2[] velocities = new Vector2[Count];
        readonly Vector2[] restOffsets = {
            new Vector2(.03f,.04f), new Vector2(-.36f,.19f), new Vector2(.30f,-.23f)
        };
        readonly float[] radii = { .20f,.115f,.12f };
        bool initialized;

        public bool Initialized => initialized;
        public Vector2 Position(int index) => locations[index];
        public float Radius(int index) => radii[index];
        public Vector2 Velocity(int index) => velocities[index];

        void Awake() => Initialize();
        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Initialize()
        {
            if(!body) body=GetComponent<SoftCellBody2D>();
            if(body && !body.Initialized)body.Initialize();
            for(int i=0;i<Count;i++)
            {
                locations[i]=(body?body.Core:Vector2.zero)+restOffsets[i];
                velocities[i]=Vector2.zero;
            }
            initialized=true;
        }

        public void Tick(float dt)
        {
            if(!(dt>0f) || float.IsNaN(dt) || float.IsInfinity(dt))return;
            if(!initialized)Initialize();
            if(!body || !body.Initialized)return;
            const float maxStep=.01f;
            int substeps=Mathf.CeilToInt(Mathf.Min(.25f,dt)/maxStep);
            float h=Mathf.Min(.25f,dt)/substeps;
            for(int sub=0;sub<substeps;sub++)
            {
                var before=new Vector2[Count];
                for(int i=0;i<Count;i++)
                {
                    before[i]=locations[i];
                    Vector2 target=body.Core+restOffsets[i];
                    velocities[i]+= (target-locations[i])*tetherStrength*h;
                    velocities[i]*=Mathf.Exp(-drag*h);
                    locations[i]+=velocities[i]*h;
                }
                for(int iter=0;iter<collisionIterations;iter++)
                {
                    // Compartment–compartment unilateral contact (no overlaps).
                    for(int i=0;i<Count;i++)
                    for(int j=i+1;j<Count;j++)
                    {
                        Vector2 between=locations[j]-locations[i];
                        float required=radii[i]+radii[j]+.012f;
                        float distance=between.magnitude;
                        if(distance>=required)continue;
                        Vector2 normal=distance>1e-7f?between/distance:Vector2.right;
                        Vector2 correction=normal*(required-distance)*.5f;
                        locations[i]-=correction;
                        locations[j]+=correction;
                    }

                    // Boundary segments come directly from the live soft mesh.
                    // For a counter-clockwise contour, the inward normal points left.
                    for(int i=0;i<Count;i++)
                    for(int j=0;j<SoftCellBody2D.Nodes;j++)
                    {
                        Vector2 a=body.Node(j);
                        Vector2 b=body.Node((j+1)%SoftCellBody2D.Nodes);
                        Vector2 edge=b-a;
                        float len=edge.magnitude;
                        if(len<1e-7f)continue;
                        Vector2 inward=new Vector2(-edge.y,edge.x)/len;
                        float signed=Vector2.Dot(locations[i]-a,inward);
                        // CCW membrane: constrain the compartment center inside
                        // the boundary by at least its finite collision radius.
                        if(signed<radii[i]+.015f)
                            locations[i]+=inward*(radii[i]+.015f-signed)*.7f;
                    }
                }
                for(int i=0;i<Count;i++)
                {
                    velocities[i]=(locations[i]-before[i])/h;
                    if(!float.IsFinite(locations[i].x) || !float.IsFinite(locations[i].y)
                        || (locations[i]-body.Core).sqrMagnitude>4f)
                    {
                        locations[i]=body.Core+restOffsets[i];
                        velocities[i]=Vector2.zero;
                    }
                }
            }
        }
    }
}
