using UnityEngine;
using Cells.Substances;
using Cells.Physiology;
using Cells.Rendering;

namespace Cells.Demo
{
    /// <summary>Standalone, deterministic chemistry demonstration in an otherwise empty Unity 2D scene.</summary>
    public sealed class ChemistryDemoBootstrap : MonoBehaviour
    {
        public int seed = 631;
        public float initialToxin = 5f;
        public float initialFood = 4f;
        public bool autoBuild = true;
        MultiChemicalField2D field;
        ChemicalEventLog eventLog;

        void Start() { if (autoBuild) Build(); }

        public void Build()
        {
            if (field) return;
            var fieldObject = new GameObject("Chemical substrate");
            field = fieldObject.AddComponent<MultiChemicalField2D>();
            var food = ScriptableObject.CreateInstance<SubstanceDefinition>();
            food.stableId = "nutrient-A"; food.displayName = "Organic nutrient";
            food.classification = SubstanceClass.Nutrient;
            food.materialForm = MaterialForm.Suspension;
            food.diffusionCoefficient = .18f;
            var poison = ScriptableObject.CreateInstance<SubstanceDefinition>();
            poison.stableId = "toxin-B"; poison.displayName = "Membrane-active toxin";
            poison.classification = SubstanceClass.Toxin;
            poison.materialForm = MaterialForm.Dissolved;
            poison.diffusionCoefficient = .12f;
            field.species = new[] { food, poison };
            field.Initialize();

            eventLog = new GameObject("Causal chemical log").AddComponent<ChemicalEventLog>();
            field.Deposit("nutrient-A", new Vector2(0f, 0f), initialFood);
            field.Deposit("toxin-B", new Vector2(1f, 0f), initialToxin);
            CreateCell("Secretor", 1, new Vector2(-1.5f, 0f), true, 0.65f);
            CreateCell("Grazer", 2, new Vector2(1f, 0f), false, 0.1f);
            CreateCamera();
            AddChemicalOverlay("Nutrients", "nutrient-A", MaterialForm.Suspension, new Color(.48f,.87f,.33f), -22);
            AddChemicalOverlay("Toxins", "toxin-B", MaterialForm.Dissolved, new Color(.72f,.31f,.94f), -21);
        }

        void CreateCell(string label, int id, Vector2 position, bool secretor, float resistance)
        {
            var go = new GameObject(label);
            go.transform.position = position;
            var metabolism = go.AddComponent<CellMetabolism>();
            metabolism.energy = secretor ? 10f : 2f;
            var membrane = go.AddComponent<MembraneIntegrity>();
            membrane.chemicalResistance = resistance;
            var visual = go.AddComponent<SpriteRenderer>();
            visual.sprite = CreateSprite();
            visual.sortingOrder = 10;
            var membraneVisual = go.AddComponent<MembraneStateRenderer>();
            membraneVisual.membrane = membrane;
            visual.color = secretor ? new Color(.66f, .9f, .82f) : new Color(.95f, .75f, .55f);

            var exchange = go.AddComponent<CellChemicalExchange>();
            exchange.organismId = id;
            exchange.field = field;
            exchange.events = eventLog;
            exchange.metabolism = metabolism;
            exchange.secretionEnabled = secretor;
            exchange.feedingEnabled = true;
            exchange.toxinReserve = secretor ? 2f : 0f;
            exchange.toxinRate = .4f;

            if (!secretor)
            {
                var exposure = go.AddComponent<CellExposureSystem>();
                exposure.organismId = id;
                exposure.field = field;
                exposure.events = eventLog;
                exposure.metabolism = metabolism;
                exposure.membrane = membrane;
                exposure.substanceId = "toxin-B";
                exposure.uptakeRate = .4f;
                exposure.damagePerUnit = 3f;
            }
        }

        void AddChemicalOverlay(string label, string id, MaterialForm form, Color color, int order)
        {
            var go = new GameObject(label + " field visualization");
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = order;
            var renderer = go.AddComponent<ChemicalFieldRenderer>();
            renderer.field = field;
            renderer.substanceId = id;
            renderer.form = form;
            renderer.tint = color;
        }

        static Sprite CreateSprite()
        {
            const int s = 48;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + .5f - s*.5f)/(s*.5f);
                float dy = (y + .5f - s*.5f)/(s*.5f);
                float r = Mathf.Sqrt(dx*dx+dy*dy);
                Color c = Color.clear;
                if (r < .95f) c = new Color(.65f,.83f,.76f,1f);
                if (r < .85f) c = new Color(.35f,.65f,.59f,1f);
                if (r < .3f) c = new Color(.22f,.3f,.35f,1f);
                tex.SetPixel(x,y,c);
            }
            tex.Apply();
            return Sprite.Create(tex,new Rect(0,0,s,s),new Vector2(.5f,.5f),24f);
        }

        static void CreateCamera()
        {
            if (Camera.main) return;
            var obj = new GameObject("Microscope camera");
            obj.tag = "MainCamera";
            var camera = obj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(.045f,.035f,.08f);
            obj.transform.position = new Vector3(0,0,-10);
        }
    }
}
