using UnityEngine;
using Cells.Substances;
using Cells.Physiology;
using Cells.Rendering;
using System;

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
        readonly System.Collections.Generic.List<UnityEngine.Object> ownedAssets = new System.Collections.Generic.List<UnityEngine.Object>();
        public MultiChemicalField2D Field => field;
        public ChemicalEventLog Events => eventLog;
        public SubstanceRegistry Registry { get; private set; }
        public TrackedParticleWorld Particles { get; private set; }
        public ChemicalReaction2D Reaction { get; private set; }

        GameObject Child(string label)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(transform, false);
            return obj;
        }

        void Start() { if (autoBuild) Build(); }

        public void Build()
        {
            if (field) return;
            var fieldObject = Child("Chemical substrate");
            field = fieldObject.AddComponent<MultiChemicalField2D>();
            var food = ScriptableObject.CreateInstance<SubstanceDefinition>();
            food.stableId = "nutrient-A"; food.displayName = "Organic nutrient";
            food.classification = SubstanceClass.Nutrient;
            food.materialForm = MaterialForm.Dissolved;
            food.diffusionCoefficient = .18f;
            var poison = ScriptableObject.CreateInstance<SubstanceDefinition>();
            poison.stableId = "toxin-B"; poison.displayName = "Membrane-active toxin";
            poison.classification = SubstanceClass.Toxin;
            poison.materialForm = MaterialForm.Dissolved;
            poison.diffusionCoefficient = .12f;
            food.visualization.baseColor = new Color(.48f,.87f,.33f);
            poison.visualization.baseColor = new Color(.72f,.31f,.94f);
            var neutralizer = ScriptableObject.CreateInstance<SubstanceDefinition>();
            neutralizer.stableId = "neutralizer-C"; neutralizer.displayName = "Neutralizing reagent";
            neutralizer.classification = SubstanceClass.Reactive; neutralizer.diffusionCoefficient = .15f;
            neutralizer.visualization.baseColor = new Color(.3f,.75f,1f);
            var waste = ScriptableObject.CreateInstance<SubstanceDefinition>();
            waste.stableId = "waste-D"; waste.displayName = "Neutralization product";
            waste.classification = SubstanceClass.Waste; waste.diffusionCoefficient = .1f;
            waste.visualization.baseColor = new Color(.6f,.6f,.65f);
            field.species = new[] { food, poison, neutralizer, waste };
            ownedAssets.AddRange(field.species);
            field.Initialize();
            Registry = fieldObject.AddComponent<SubstanceRegistry>();
            Registry.definitions = field.species;
            Registry.Initialize();
            Reaction = fieldObject.AddComponent<ChemicalReaction2D>();
            Reaction.field = field;

            eventLog = Child("Causal chemical log").AddComponent<ChemicalEventLog>();
            field.Deposit("nutrient-A", new Vector2(0f, 0f), initialFood);
            // Spread the challenge dose over several voxels rather than an instantaneous point overdose.
            for (int x = -2; x <= 2; x++)
            for (int y = -2; y <= 2; y++)
                field.Deposit("toxin-B", new Vector2(1f + x * field.cellSize, y * field.cellSize), initialToxin / 25f);
            CreateCell("Secretor", 1, new Vector2(-1.5f, 0f), true, 0.65f);
            CreateCell("Grazer", 2, new Vector2(1f, 0f), false, 0.1f);
            CreateParticleDemo();
            CreateCamera();
            AddChemicalOverlay("Nutrients", "nutrient-A", MaterialForm.Dissolved, food.visualization.baseColor, -22);
            AddChemicalOverlay("Toxins", "toxin-B", MaterialForm.Dissolved, new Color(.72f,.31f,.94f), -21);
            AddChemicalOverlay("Neutralizer", "neutralizer-C", MaterialForm.Dissolved, neutralizer.visualization.baseColor, -20);
            AddChemicalOverlay("Waste", "waste-D", MaterialForm.Dissolved, waste.visualization.baseColor, -23);
            var view = GetComponent<ChemistryDemoView>();
            if (!view) view = gameObject.AddComponent<ChemistryDemoView>();
            view.demo = this;
        }

        void CreateParticleDemo()
        {
            var particleObject = Child("Tracked particulate substrate");
            var particles = particleObject.AddComponent<TrackedParticleWorld>();
            Particles = particles;
            particles.ResetSimulation(seed);
            particles.diffusivity = .012f;
            var random = new System.Random(seed);
            for (int i = 0; i < 120; i++)
            {
                float x = -.6f + (float)random.NextDouble() * 1.2f;
                float y = -.8f + (float)random.NextDouble() * 1.6f;
                particles.Spawn("food-particle", new Vector2(x, y), .025f, .025f);
            }
            var display = Child("Actual nutrient particle display");
            var particleRenderer = display.AddComponent<TrackedParticleRenderer>();
            particleRenderer.world = particles;
            particleRenderer.substanceId = "food-particle";
            particleRenderer.color = new Color(.75f, .97f, .43f);
            var grazer = transform.Find("Grazer");
            if (grazer)
            {
                var mouth = new GameObject("Particulate intake");
                mouth.transform.SetParent(grazer.transform);
                mouth.transform.localPosition = Vector3.left * .45f;
                var feeder = mouth.AddComponent<ParticleFeedingSite>();
                feeder.organismId = 2;
                feeder.world = particles;
                feeder.events = eventLog;
                feeder.metabolism = grazer.GetComponent<CellMetabolism>();
                feeder.edibleSubstanceId = "food-particle";
                feeder.captureRadius = .45f;
            }
        }

        void CreateCell(string label, int id, Vector2 position, bool secretor, float resistance)
        {
            var go = Child(label);
            go.transform.position = position;
            var metabolism = go.AddComponent<CellMetabolism>();
            metabolism.energy = secretor ? 10f : 2f;
            var membrane = go.AddComponent<MembraneIntegrity>();
            membrane.chemicalResistance = resistance;
            var visual = go.AddComponent<SpriteRenderer>();
            visual.sprite = CreateSprite();
            ownedAssets.Add(visual.sprite.texture);
            ownedAssets.Add(visual.sprite);
            visual.sortingOrder = 10;
            var membraneVisual = go.AddComponent<MembraneStateRenderer>();
            membraneVisual.membrane = membrane;
            visual.color = secretor ? new Color(.66f, .9f, .82f) : new Color(.95f, .75f, .55f);
            membraneVisual.healthy = visual.color;

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
                exposure.uptakeRate = .025f;
                exposure.damagePerUnit = 3f;
            }
        }

        void AddChemicalOverlay(string label, string id, MaterialForm form, Color color, int order)
        {
            var go = Child(label + " field visualization");
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

        void CreateCamera()
        {
            if (Camera.main) return;
            var obj = Child("Microscope camera");
            obj.tag = "MainCamera";
            var camera = obj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.045f,.035f,.08f);
            obj.transform.position = new Vector3(0,0,-10);
        }

        void OnDestroy()
        {
            foreach (var asset in ownedAssets)
                if (asset) Destroy(asset);
        }
    }
}
