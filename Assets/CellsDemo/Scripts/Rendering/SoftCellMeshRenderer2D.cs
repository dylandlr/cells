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
        public InternalCompartments2D compartments;
        public MembraneIntegrity membrane;
        public Color cytoplasm = new Color(.16f,.68f,.59f,.79f);
        public Color edgeColor = new Color(.55f,.98f,.85f,.95f);
        public bool showStress = true;
        public Color highStressColor = new Color(1f,.36f,.12f,1f);
        Mesh mesh;
        MeshFilter filter;
        MeshRenderer surface;
        Material material;
        readonly Vector3[] vertices = new Vector3[SoftCellBody2D.Nodes+1];
        readonly Color[] colors = new Color[SoftCellBody2D.Nodes+1];
        readonly int[] triangles = new int[SoftCellBody2D.Nodes*3];
        LineRenderer rim;
        LineRenderer halo;
        readonly Transform[] organelleNodes = new Transform[InternalCompartments2D.Count];
        Sprite organelleSprite;
        Texture2D organelleTexture;
        readonly Transform[] granules = new Transform[12];
        readonly float[] granuleAngles = new float[12];
        readonly float[] granuleRadii = new float[12];

        void Awake()
        {
            if(!body) body=GetComponent<SoftCellBody2D>();
            if(!membrane) membrane=GetComponent<MembraneIntegrity>();
            if(!compartments) compartments=GetComponent<InternalCompartments2D>();
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

            var haloObject=new GameObject("Phase contrast halo");
            haloObject.transform.SetParent(transform,false);
            halo=haloObject.AddComponent<LineRenderer>();
            halo.useWorldSpace=false;
            halo.loop=true;
            halo.positionCount=SoftCellBody2D.Nodes;
            halo.startWidth=halo.endWidth=.095f;
            halo.sortingOrder=11;
            halo.startColor=halo.endColor=new Color(.08f,.12f,.11f,.24f);
            if(material)halo.sharedMaterial=material;

            organelleSprite=MakeOrganelleSprite();
            organelleTexture=organelleSprite.texture;
            string[] names={"Nucleoid mass", "Storage vacuole", "Signal vesicle"};
            Color[] tints={new Color(.90f,.96f,.80f,.92f),
                new Color(.66f,.89f,.98f,.72f),new Color(.99f,.72f,.42f,.86f)};
            for(int i=0;i<organelleNodes.Length;i++)
            {
                var organelle=new GameObject(names[i]+" / physically constrained");
                organelle.transform.SetParent(transform,false);
                organelleNodes[i]=organelle.transform;
                var visual=organelle.AddComponent<SpriteRenderer>();
                visual.sprite=organelleSprite;
                visual.color=tints[i];
                visual.sortingOrder=13;
            }

            var random=new System.Random(Mathf.Abs(name.GetHashCode())+631);
            for(int i=0;i<granules.Length;i++)
            {
                granuleAngles[i]=(float)(random.NextDouble()*Mathf.PI*2f);
                granuleRadii[i]=.24f+(float)random.NextDouble()*.48f;
                var grain=new GameObject("Cytoplasm granule "+(i+1));
                grain.transform.SetParent(transform,false);
                granules[i]=grain.transform;
                var visual=grain.AddComponent<SpriteRenderer>();
                visual.sprite=organelleSprite;
                float v=.66f+(float)random.NextDouble()*.20f;
                visual.color=new Color(v*.72f,v*.94f,v*.84f,.24f);
                visual.sortingOrder=13;
                float size=.025f+(float)random.NextDouble()*.030f;
                grain.transform.localScale=new Vector3(size,size,1f);
            }
            UpdateGeometry();
        }

        static Sprite MakeOrganelleSprite()
        {
            const int side=32;
            var tex=new Texture2D(side,side,TextureFormat.RGBA32,false);
            tex.filterMode=FilterMode.Point;
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
                Color baseColor=Color.Lerp(injured,healthy,integrity);
                colors[i+1]=showStress ? Color.Lerp(baseColor,highStressColor,body.StressAt(i)) : baseColor;
                triangles[i*3]=0;
                triangles[i*3+1]=i+1;
                triangles[i*3+2]=(i+1)%SoftCellBody2D.Nodes+1;
                if(rim)rim.SetPosition(i,new Vector3(p.x,p.y,-.015f));
                if(halo)halo.SetPosition(i,new Vector3(p.x,p.y,.01f));
            }
            mesh.Clear();
            mesh.vertices=vertices;
            mesh.colors=colors;
            mesh.triangles=triangles;
            mesh.RecalculateBounds();
            for(int i=0;i<organelleNodes.Length;i++)
            {
                if(!organelleNodes[i])continue;
                Vector2 p=compartments && compartments.Initialized
                    ? compartments.Position(i) : body.Core;
                float radius=compartments ? compartments.Radius(i) : .16f;
                organelleNodes[i].localPosition=new Vector3(p.x,p.y,-.02f);
                organelleNodes[i].localScale=new Vector3(radius*2,radius*2,1f);
            }
            for(int i=0;i<granules.Length;i++)
            {
                if(!granules[i])continue;
                float normalized=granuleAngles[i]/(Mathf.PI*2f);
                int segment=Mathf.RoundToInt(normalized*SoftCellBody2D.Nodes)%SoftCellBody2D.Nodes;
                Vector2 edge=body.Node(segment);
                Vector2 p=Vector2.Lerp(body.Core,edge,granuleRadii[i]);
                granules[i].localPosition=new Vector3(p.x,p.y,-.012f);
            }
        }
        void OnDestroy()
        {
            if(mesh)Destroy(mesh);
            if(material)Destroy(material);
            if(organelleSprite)Destroy(organelleSprite);
            if(organelleTexture)Destroy(organelleTexture);

        }
    }
}
