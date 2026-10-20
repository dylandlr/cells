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
        public CellMetabolism metabolism;
        public CellBiomassGrowth growth;
        [Min(.001f)] public float fullEnergyReference = 10f;
        [Range(0f,1f)] public float depletionDarkening = .65f;
        public float ObservedEnergyFraction => metabolism && metabolism.alive
            ? Mathf.Clamp01(metabolism.energy / Mathf.Max(.001f,fullEnergyReference)) : 0f;
        public Color ObservedCytoplasmColor => Color.Lerp(
            new Color(cytoplasm.r*(1f-depletionDarkening),
                      cytoplasm.g*(1f-depletionDarkening),
                      cytoplasm.b*(1f-depletionDarkening),cytoplasm.a),
            cytoplasm,ObservedEnergyFraction);
        public Color cytoplasm = new Color(.16f,.68f,.59f,.79f);
        public Color edgeColor = new Color(.55f,.98f,.85f,.95f);
        public bool showStress = true;
        public Color highStressColor = new Color(1f,.36f,.12f,1f);
        // Ear-clipped fill is preferred for valid concave outlines.
        // The legacy center fan remains a defensive rendering fallback.
        public bool useBoundaryTriangulation = true;
        public bool LastBoundaryTriangulationSucceeded { get; private set; }
        public int BoundaryTriangulationFailures { get; private set; }
        public int BoundaryTriangulationSuccesses { get; private set; }
        public bool LastContourWasValid { get; private set; }
        public int RenderTriangleCount => surface && surface.enabled && mesh
            ? mesh.triangles.Length / 3 : 0;
        public bool ShellVisible => surface && surface.enabled;
        readonly Vector2[] boundaryPoints = new Vector2[SoftCellBody2D.Nodes];
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
            if(!metabolism) metabolism=GetComponent<CellMetabolism>();
            if(!growth) growth=GetComponent<CellBiomassGrowth>();
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
            // The soft-body solver now owns biomass-dependent geometric expansion.
            const float size=1f;
            vertices[0]=new Vector3(body.Core.x*size,body.Core.y*size,-.005f);
            Color liveCytoplasm=ObservedCytoplasmColor;
            colors[0]=liveCytoplasm;
            for(int i=0;i<SoftCellBody2D.Nodes;i++)
            {
                Vector2 p=body.Node(i)*size;
                vertices[i+1]=new Vector3(p.x,p.y,0f);
                float integrity=membrane?membrane.GetIntegrity(i):1f;
                Color healthy=Color.Lerp(liveCytoplasm,edgeColor,.60f);
                healthy=Color.Lerp(liveCytoplasm,healthy,ObservedEnergyFraction);
                Color injured=new Color(.95f,.22f,.12f,.82f);
                Color baseColor=Color.Lerp(injured,healthy,integrity);
                colors[i+1]=showStress ? Color.Lerp(baseColor,highStressColor,body.StressAt(i)) : baseColor;
                boundaryPoints[i]=p;
                triangles[i*3]=0;
                triangles[i*3+1]=i+1;
                triangles[i*3+2]=(i+1)%SoftCellBody2D.Nodes+1;
                if(rim)rim.SetPosition(i,new Vector3(p.x,p.y,-.015f));
                if(halo)halo.SetPosition(i,new Vector3(p.x,p.y,.01f));
            }
            // The boundary remains owned by the physical solver. This only
            // changes how its cytoplasm is filled visually, not its forces.
            var observedLoop = new BoundaryLoop2D(boundaryPoints);
            LastContourWasValid = observedLoop.Valid;
            // A self-intersecting or nonfinite contour is a solver fault,
            // not an invitation to draw an invalid center-fan mesh.
            if (!LastContourWasValid)
            {
                LastBoundaryTriangulationSucceeded = false;
                if (useBoundaryTriangulation) BoundaryTriangulationFailures++;
                if (surface) surface.enabled = false;
                if (rim) rim.enabled = false;
                if (halo) halo.enabled = false;
                return;
            }
            if (surface) surface.enabled = true;
            if (rim) rim.enabled = true;
            if (halo) halo.enabled = true;
            int[] boundaryTriangles = null;
            LastBoundaryTriangulationSucceeded = useBoundaryTriangulation &&
                BoundaryEarClip2D.TryTriangulate(observedLoop,
                    out boundaryTriangles);
            if (useBoundaryTriangulation)
            {
                if (LastBoundaryTriangulationSucceeded) BoundaryTriangulationSuccesses++;
                else BoundaryTriangulationFailures++;
            }
            int[] fillTriangles=triangles;
            if (LastBoundaryTriangulationSucceeded)
            {
                fillTriangles=new int[boundaryTriangles.Length];
                for(int i=0;i<boundaryTriangles.Length;i++)
                    fillTriangles[i]=boundaryTriangles[i]+1;
            }
            mesh.Clear();
            mesh.vertices=vertices;
            mesh.colors=colors;
            mesh.triangles=fillTriangles;
            mesh.RecalculateBounds();
            for(int i=0;i<organelleNodes.Length;i++)
            {
                if(!organelleNodes[i])continue;
                Vector2 p=compartments && compartments.Initialized
                    ? compartments.Position(i) : body.Core;
                float radius=compartments ? compartments.Radius(i) : .16f;
                organelleNodes[i].localPosition=new Vector3(p.x*size,p.y*size,-.02f);
                organelleNodes[i].localScale=new Vector3(radius*2*size,radius*2*size,1f);
            }
            for(int i=0;i<granules.Length;i++)
            {
                if(!granules[i])continue;
                float normalized=granuleAngles[i]/(Mathf.PI*2f);
                int segment=Mathf.RoundToInt(normalized*SoftCellBody2D.Nodes)%SoftCellBody2D.Nodes;
                Vector2 edge=body.Node(segment);
                Vector2 p=Vector2.Lerp(body.Core,edge,granuleRadii[i]);
                granules[i].localPosition=new Vector3(p.x*size,p.y*size,-.012f);
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
