using UnityEngine;
using Cells.Fluid;

namespace Cells.Rendering
{
    /// <summary>Debug microscope flow markers sampled from the actual fluid velocity field.</summary>
    public sealed class FluidFlowOverlay : MonoBehaviour
    {
        public FluidGrid2D fluid;
        public int columns = 19;
        public int rows = 11;
        public float scale = .35f;
        public float minimumSpeed = .004f;
        LineRenderer[] lines;
        Material material;

        void Start()
        {
            if (!fluid) return;
            var shader = Shader.Find("Sprites/Default");
            if (shader) material = new Material(shader);
            columns = Mathf.Max(2,columns); rows = Mathf.Max(2,rows);
            lines = new LineRenderer[columns*rows];
            for(int i=0;i<lines.Length;i++)
            {
                var go = new GameObject("Flow sample");
                go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();
                line.useWorldSpace=true;
                line.positionCount=2;
                line.startWidth=.014f;
                line.endWidth=.006f;
                line.sortingOrder=8;
                line.startColor = new Color(.12f,.45f,.70f,.6f);
                line.endColor = new Color(.12f,.45f,.70f,.12f);
                if(material) line.sharedMaterial=material;
                lines[i]=line;
            }
        }

        void LateUpdate()
        {
            if(!fluid || lines==null) return;
            int index=0;
            for(int y=0;y<rows;y++)
            for(int x=0;x<columns;x++)
            {
                float fx=(x+.5f)/columns-.5f;
                float fy=(y+.5f)/rows-.5f;
                Vector2 local=new Vector2(fx*(fluid.width-2)*fluid.spacing,
                                           fy*(fluid.height-2)*fluid.spacing);
                Vector2 p=fluid.transform.TransformPoint(local);
                Vector2 v=fluid.Sample(p);
                var line=lines[index++];
                line.enabled=v.magnitude>minimumSpeed;
                line.SetPosition(0,p);
                line.SetPosition(1,p+Vector2.ClampMagnitude(v*scale,.3f));
            }
        }

        void OnDestroy()
        {
            if(material) Destroy(material);
        }
    }
}
