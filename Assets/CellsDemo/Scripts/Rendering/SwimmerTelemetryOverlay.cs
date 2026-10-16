using UnityEngine;
using Cells.Physiology;
using Cells.Fluid;

namespace Cells.Rendering
{
    /// <summary>
    /// Play Mode diagnostics only. Every arrow is sampled from authoritative
    /// velocity state, never drives the swimmer or fluid simulation.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SwimmerTelemetryOverlay : MonoBehaviour
    {
        public CellMotility swimmer;
        public FlagellarHydrodynamics2D hydro;
        public FluidGrid2D fluid;
        public bool showVelocity = true;
        public bool showFluid = true;
        public bool showRotation = true;
        [Min(0f)] public float velocityScale = 1.8f;
        [Min(0f)] public float fluidScale = 1.8f;
        [Min(.05f)] public float maxArrowLength = .85f;
        [Min(.01f)] public float angularScale = .008f;
        LineRenderer swimmingLine;
        LineRenderer fluidLine;
        LineRenderer yawLine;
        Material sharedMaterial;

        void Awake()
        {
            if (!swimmer) swimmer = GetComponent<CellMotility>();
            if (!hydro) hydro = GetComponent<FlagellarHydrodynamics2D>();
            if (!fluid && swimmer) fluid = swimmer.fluid;
            var shader = Shader.Find("Sprites/Default");
            if (shader) sharedMaterial = new Material(shader);
            swimmingLine = CreateArrow("Measured swimming velocity", new Color(.24f,.92f,.55f,.95f));
            fluidLine = CreateArrow("Measured local fluid advection", new Color(.26f,.68f,1f,.85f));
            yawLine = CreateArrow("Measured flagellar yaw", new Color(1f,.72f,.24f,.95f));
        }

        LineRenderer CreateArrow(string label, Color color)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform,false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = .024f;
            line.endWidth = .006f;
            line.sortingOrder = 30;
            line.startColor = line.endColor = color;
            if (sharedMaterial) line.sharedMaterial = sharedMaterial;
            return line;
        }

        void LateUpdate()
        {
            if (!swimmer) return;
            Vector2 p = transform.position;
            Vector2 v = swimmer.Velocity;
            SetVector(swimmingLine,p,v,velocityScale,showVelocity);
            Vector2 ambient = fluid ? fluid.Sample(p) * swimmer.flowEntrainment : Vector2.zero;
            SetVector(fluidLine,p,ambient,fluidScale,showFluid);
            float omega = hydro ? hydro.LocalAngularVelocityDegrees : 0f;
            bool valid = float.IsFinite(omega) && Mathf.Abs(omega)>1e-4f;
            Vector2 heading = transform.right;
            Vector2 tangent = new Vector2(-heading.y,heading.x);
            Vector2 origin = p + heading*.42f;
            SetVector(yawLine,origin,tangent*Mathf.Sign(omega),
                Mathf.Min(maxArrowLength,Mathf.Abs(omega)*angularScale),showRotation && valid);
        }

        void SetVector(LineRenderer line,Vector2 origin,Vector2 direction,float gain,bool enabled)
        {
            if (!line) return;
            bool valid = float.IsFinite(direction.x) && float.IsFinite(direction.y);
            Vector2 arrow = valid ? Vector2.ClampMagnitude(direction*gain,maxArrowLength) : Vector2.zero;
            line.enabled = enabled && valid && arrow.sqrMagnitude > 1e-8f;
            if (!line.enabled) return;
            line.SetPosition(0,new Vector3(origin.x,origin.y,-.1f));
            line.SetPosition(1,new Vector3(origin.x+arrow.x,origin.y+arrow.y,-.1f));
        }

        void OnDestroy()
        {
            if (sharedMaterial) Destroy(sharedMaterial);
        }
    }
}
