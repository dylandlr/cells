using UnityEngine;
using Cells.Physiology;

namespace Cells.Rendering
{
    /// <summary>
    /// Live triangulated body shell driven by SoftCellBody2D, with interior
    /// mass/node visuals and 2.5D-style shading. Geometry follows physics only.
    /// </summary>
    [RequireComponent(typeof(SoftCellBody2D))]
    public sealed class SoftCellMeshRenderer2D : MonoBehaviour
    {
        public SoftCellBody2D body;
        public MembraneIntegrity membrane;
        public Color cytoplasm = new Color(.16f,.68f,.59f,.79f);
        public Color edgeColor = new Color(.55f,.98f,.85f,.95f);
        Mesh mesh;
        MeshFilter filter;
        MeshRenderer surface;
        Material material;
        readonly Vector3[] vertices = new Vector3[SoftCellBody2D.Nodes+1];
        readonly Color[] colors = new Color[SoftCellBody2D.Nodes+1];
        readonly int[] triangles = new int[SoftCellBody2D.Nodes*3];
        LineRenderer rim;
        Transform nucleus;
        SpriteRenderer nucleusVisual;

        void Awake()
        {
            if(!body) body=GetComponent<SoftCellBody2D>();
            if(!membrane) membrane=GetComponent<MembraneIntegrity>();
            var skin=new GameObject("Live membrane mesh / driven by physics");
            skin.transform.SetParent(transform,false);
            filter=skin.AddComponent<MeshFilter>();
            surface=skin.AddComponent<MeshRenderer>();
            mesh=new Mesh {name="Deformable cell shell"};
            mesh.MarkDynamic();
            filter.sharedMesh=mesh;
            var shader=Shader.Find("Sprites/Default");
            if(shader==null)shader=Shader.Find("Unlit/Transparent");
            if(shader!=null)
            {
                material=new Material(shader);
                surface.sharedMaterial=material;
            }
            surface.sortingOrder=12;
            var edge=new GameObject("Membrane tension contour");
            edge.transform.SetParent(transform,false);
            rim=edge.AddComponent<LineRenderer>();
            rim.useWorldSpace=false;
            rim.loop=true;
            rim.positionCount=SoftCellBody2D.Nodes;
            rim.startWidth=rim.endWidth=.029f;
            rim.sortingOrder=14;
            rim.startColor=rim.endColor=edgeColor;
            if(material)rim.sharedMaterial=material;
            var organelle=new GameObject("Interior density node");
            organelle.transform.SetParent(transform,false);
            nucleus=organelle.transform;
            nucleusVisual=organelle.AddComponent<SpriteRenderer>();
            nucleusVisual.sprite=MakeOrganelleSprite();
            nucleusVisual.color=new Color(.88f,.95f,.75f,.84f);
            nucleusVisual.sortingOrder=13;
            nucleus.localScale=new Vector3(.28f,.28f,1f);
            UpdateGeometry();
        }

        static Sprite MakeOrganelleSprite()
        {
            const int side=32;
            var tex=new Texture2D(side,side,TextureFormat.RGBA32,false);
            tex.filterMode=FilterMode.Bilinear;
            var pixels=new Color[side*side];
            for(int y=0;y<side;y++)
            for(int x=0;x<side;x++)
            {
                float r=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f;
                float alpha=Mathf.Clamp01((1f-r)*3f);
                pixels[y*side+x]=new Color(.91f,.98f,.86f,alpha);
            }
            tex.SetPixels(pixels);tex.Apply();
            return Sprite.Create(tex,new Rect(0,0,side,side),new Vector2(.5f,.5f),side);
        }

        void LateUpdate()=>UpdateGeometry();
        public void UpdateGeometry()
        {
            if(!body || !body.Initialized || !mesh)return;
            vertices[0]=new Vector3(body.Core.x,body.Core.y,-.005f);
            colors[0]=cytoplasm;
            for(int i=0;i<SoftCellBody2D.Nodes;i++)
            {
                Vector2 p=body.Node(i);
                vertices[i+1]=new Vector3(p.x,p.y,0f);
                float integrity=membrane?membrane.GetIntegrity(i):1f;
                Color healthy=Color.Lerp(cytoplasm,edgeColor,.60f);
                Color injured=new Color(.95f,.22f,.12f,.82f);
                colors[i+1]=Color.Lerp(injured,healthy,integrity);
                triangles[i*3]=0;
                triangles[i*3+1]=i+1;
                triangles[i*3+2]=(i+1)%SoftCellBody2D.Nodes+1;
                if(rim)rim.SetPosition(i,new Vector3(p.x,p.y,-.015f));
            }
            mesh.Clear();
            mesh.vertices=vertices;
            mesh.colors=colors;
            mesh.triangles=triangles;
            mesh.RecalculateBounds();
            if(nucleus)nucleus.localPosition=new Vector3(body.Core.x,body.Core.y,-.02f);
        }
        void OnDestroy()
        {
            if(mesh)Destroy(mesh);
            if(material)Destroy(material);
            if(nucleusVisual && nucleusVisual.sprite)
            {
                var sprite=nucleusVisual.sprite;
                var tex=sprite.texture;
                Destroy(sprite);
                if(tex)Destroy(tex);
            }
        }
    }
}
