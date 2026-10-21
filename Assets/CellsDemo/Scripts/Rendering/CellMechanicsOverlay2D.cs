using UnityEngine;
using Cells.Physiology;

namespace Cells.Rendering
{
    /// <summary>
    /// Optional microscope diagnostic overlays. Every arrow derives from a
    /// measured membrane node velocity or strain, never a generated force.
    /// Dashed-looking line segments are rendered in local coordinates.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(SoftCellBody2D))]
    public sealed class CellMechanicsOverlay2D : MonoBehaviour
    {
        public SoftCellBody2D body;
        public bool showMotion = true;
        public bool showStress = true;
        [Range(.02f,1.5f)] public float velocityScale = .18f;
        [Range(.01f,.4f)] public float maximumArrowLength = .20f;
        [Range(.001f,.05f)] public float strokeWidth = .012f;
        [Range(1,8)] public int sectorStride = 4;
        readonly LineRenderer[] vectors = new LineRenderer[SoftCellBody2D.Nodes];
        readonly LineRenderer[] stresses = new LineRenderer[SoftCellBody2D.Nodes];
        Material shared;
        void Awake()
        {
            if (!body) body = GetComponent<SoftCellBody2D>();
            var shader = Shader.Find("Sprites/Default");
            if (!shader) shader = Shader.Find("Unlit/Transparent");
            if (shader) shared = new Material(shader);
            for (int i=0;i<SoftCellBody2D.Nodes;i++)
            {
                vectors[i] = MakeLine("Measured membrane motion " + i,14);
                stresses[i] = MakeLine("Measured membrane strain " + i,15);
            }
        }
        LineRenderer MakeLine(string label,int order)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform,false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.widthMultiplier = strokeWidth;
            line.sortingOrder = order;
            if (shared) line.sharedMaterial = shared;
            return line;
        }
        void LateUpdate()
        {
            if (!body || !body.Initialized) return;
            for (int i=0;i<SoftCellBody2D.Nodes;i++)
            {
                bool sampled = i%Mathf.Max(1,sectorStride)==0;
                Vector2 pos = body.Node(i);
                if (vectors[i])
                {
                    Vector2 v = Vector2.ClampMagnitude(body.NodeVelocity(i)*velocityScale,
                        maximumArrowLength);
                    vectors[i].enabled = sampled && showMotion && v.sqrMagnitude>1e-7f;
                    vectors[i].widthMultiplier=strokeWidth;
                    vectors[i].startColor=vectors[i].endColor=new Color(.34f,.78f,1f,.75f);
                    vectors[i].SetPosition(0,new Vector3(pos.x,pos.y,-.03f));
                    vectors[i].SetPosition(1,new Vector3(pos.x+v.x,pos.y+v.y,-.03f));
                }
                if (stresses[i])
                {
                    float s = body.StressAt(i);
                    Vector2 normal = pos-body.Core;
                    if(normal.sqrMagnitude>1e-7f) normal.Normalize();
                    stresses[i].enabled = sampled && showStress && s>.015f;
                    stresses[i].widthMultiplier=strokeWidth*(1f+s);
                    stresses[i].startColor=stresses[i].endColor=Color.Lerp(
                        new Color(.96f,.77f,.3f,.45f),new Color(1f,.17f,.11f,.95f),s);
                    stresses[i].SetPosition(0,new Vector3(pos.x,pos.y,-.04f));
                    Vector2 tip = pos+normal*Mathf.Min(maximumArrowLength,s*.22f);
                    stresses[i].SetPosition(1,new Vector3(tip.x,tip.y,-.04f));
                }
            }
        }
        void OnDestroy(){ if(shared) Destroy(shared); }
    }
}
