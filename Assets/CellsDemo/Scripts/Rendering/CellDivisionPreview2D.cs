using UnityEngine;
using Cells.Physiology;

namespace Cells.Rendering
{
    /// <summary>
    /// A geometry-following *division readiness preview*, not a mechanical
    /// cytokinesis solver. It visualizes a constriction plane and candidate
    /// compartment segregation along the currently observed membrane.
    /// </summary>
    [RequireComponent(typeof(SoftCellBody2D))]
    public sealed class CellDivisionPreview2D : MonoBehaviour
    {
        public SoftCellBody2D body;
        public InternalCompartments2D compartments;
        [Range(0f,1f)] public float progress;
        public bool visible;
        public Color septumTint = new Color(1f,.80f,.40f,.9f);
        LineRenderer septum, geneticTrack;
        Material material;

        void Awake()
        {
            if (!body) body=GetComponent<SoftCellBody2D>();
            if (!compartments) compartments=GetComponent<InternalCompartments2D>();
            var shader=Shader.Find("Sprites/Default");
            if (!shader)shader=Shader.Find("Unlit/Transparent");
            if(shader)material=new Material(shader);
            septum=Create("Prospective division septum",.028f,16);
            geneticTrack=Create("Genetic compartment displacement guide",.013f,16);
        }
        LineRenderer Create(string label,float width,int order)
        {
            var obj=new GameObject(label);
            obj.transform.SetParent(transform,false);
            var line=obj.AddComponent<LineRenderer>();
            line.useWorldSpace=false;
            line.positionCount=2;
            line.startWidth=line.endWidth=width;
            line.sortingOrder=order;
            if(material)line.sharedMaterial=material;
            return line;
        }
        void LateUpdate()
        {
            if(!septum||!geneticTrack||!body||!body.Initialized)return;
            septum.enabled=visible;
            geneticTrack.enabled=visible;
            if(!visible)return;
            // Opposed points on the observed, deformed membrane; no fake
            // circular silhouette and no force applied to membrane nodes.
            int half=SoftCellBody2D.Nodes/2;
            Vector2 a=body.Node(half/2);
            Vector2 b=body.Node(half/2+half);
            Vector2 midpoint=(a+b)*.5f;
            float widthScale=1f-.72f*Mathf.SmoothStep(0f,1f,progress);
            Vector2 endpointA=Vector2.Lerp(midpoint,a,widthScale);
            Vector2 endpointB=Vector2.Lerp(midpoint,b,widthScale);
            septum.startColor=septum.endColor=septumTint;
            septum.SetPosition(0,new Vector3(endpointA.x,endpointA.y,-.05f));
            septum.SetPosition(1,new Vector3(endpointB.x,endpointB.y,-.05f));
            // Measured nucleoid location to prospective segregation target.
            Vector2 nucleus=compartments&&compartments.Initialized
                ? compartments.Position(0) : body.Core;
            Vector2 offset=Vector2.Perpendicular(b-a).normalized*
                (body.CurrentRadius*.22f*progress);
            geneticTrack.startColor=geneticTrack.endColor=new Color(.87f,.72f,1f,.8f);
            geneticTrack.SetPosition(0,new Vector3(nucleus.x,nucleus.y,-.055f));
            geneticTrack.SetPosition(1,new Vector3(nucleus.x+offset.x,nucleus.y+offset.y,-.055f));
        }
        void OnDestroy(){if(material)Destroy(material);}
    }
}
