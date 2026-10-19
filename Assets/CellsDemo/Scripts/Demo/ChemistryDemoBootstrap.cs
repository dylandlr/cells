using UnityEngine;
using Cells.Substances;
using Cells.Physiology;
using Cells.Rendering;
using Cells.Fluid;
using Cells.Experiments;
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
        public bool cultureView;
        public bool assayMode;
        public CellMotility AssaySwimmer { get; private set; }
        public ChemotaxisTrialRecorder AssayRecorder { get; private set; }
        MultiChemicalField2D field;
        ChemicalEventLog eventLog;
        readonly System.Collections.Generic.List<UnityEngine.Object> ownedAssets = new System.Collections.Generic.List<UnityEngine.Object>();
        public MultiChemicalField2D Field => field;
        public ChemicalEventLog Events => eventLog;
        public SubstanceRegistry Registry { get; private set; }
        public TrackedParticleWorld Particles { get; private set; }
        public ChemicalReaction2D Reaction { get; private set; }
        public FluidGrid2D Fluid { get; private set; }
        public float AssayNutrientDeposited { get; private set; }

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
            var metabolicWaste = ScriptableObject.CreateInstance<SubstanceDefinition>();
            metabolicWaste.stableId = "metabolic-waste-E"; metabolicWaste.displayName = "Metabolic byproduct";
            metabolicWaste.classification = SubstanceClass.Waste; metabolicWaste.diffusionCoefficient = .14f;
            metabolicWaste.visualization.baseColor = new Color(.85f, .55f, .25f);
            var signal = ScriptableObject.CreateInstance<SubstanceDefinition>();
            signal.stableId = "signal-S";
            signal.displayName = "Diffusing signaling molecule";
            signal.classification = SubstanceClass.Signal;
            signal.materialForm = MaterialForm.Dissolved;
            signal.diffusionCoefficient = .22f;
            signal.decayRate = .13f;
            signal.visualization.baseColor = new Color(.95f, .76f, .22f);
            field.species = new[] { food, poison, neutralizer, waste, metabolicWaste, signal };
            ownedAssets.AddRange(field.species);
            field.Initialize();
            Registry = fieldObject.AddComponent<SubstanceRegistry>();
            Registry.definitions = field.species;
            Registry.Initialize();
            Reaction = fieldObject.AddComponent<ChemicalReaction2D>();
            Reaction.field = field;

            eventLog = Child("Causal chemical log").AddComponent<ChemicalEventLog>();
            Fluid = Child("Incompressible fluid substrate").AddComponent<FluidGrid2D>();
            Fluid.width = field.width;
            Fluid.height = field.height;
            Fluid.spacing = field.cellSize;
            Fluid.Initialize();
            field.fluid = Fluid;
            var fluidOverlay = Child("Measured fluid flow vectors").AddComponent<FluidFlowOverlay>();
            fluidOverlay.fluid = Fluid;
            // Reserve a small portion of the same finite food budget for the
            // secretor's local receptor. Without local food, the demonstrated
            // nutrient-dependent signaling policy may never emit a molecule.
            // This is an intentionally staged demo stimulus, not spontaneous
            // cooperative discovery or an additional resource source.
            float availableFood = Mathf.Max(0f, initialFood);
            if (assayMode)
            {
                // Same total nutrient mass for every seed and treatment.
                // Position jitter differs only by environment seed.
                foreach (Vector2 patch in SeededNutrientPatches.Positions(seed))
                    AssayNutrientDeposited += field.Deposit("nutrient-A", patch, availableFood / SeededNutrientPatches.Count);
            }
            else
            {
                float secretorPatch = availableFood * .15f;
                field.Deposit("nutrient-A", new Vector2(-1.5f, 0f), secretorPatch);
                field.Deposit("nutrient-A", new Vector2(0f, 0f), availableFood - secretorPatch);
            }
            // Spread the challenge dose over several voxels rather than an instantaneous point overdose.
            for (int x = -4; x <= 4; x++)
            for (int y = -4; y <= 4; y++)
                field.Deposit("toxin-B", new Vector2(1f + x * field.cellSize, y * field.cellSize), initialToxin / 81f);
            if (assayMode)
                CreateCell("Assay swimmer", 1, new Vector2(-1.5f, 0f), false, .65f, true);
            else
            {
                CreateCell("Secretor", 1, new Vector2(-1.5f, 0f), true, 0.65f, true);
                CreateCell("Grazer", 2, new Vector2(1f, 0f), false, 0.1f, true);
            }
            if (cultureView && !assayMode) CreateCulture();
            if (!assayMode) { CreateParticleDemo(); CreateCamera(); }
            AddChemicalOverlay("Nutrients", "nutrient-A", MaterialForm.Dissolved, food.visualization.baseColor, -22);
            AddChemicalOverlay("Toxins", "toxin-B", MaterialForm.Dissolved, new Color(.72f,.31f,.94f), -21);
            AddChemicalOverlay("Neutralizer", "neutralizer-C", MaterialForm.Dissolved, neutralizer.visualization.baseColor, -20);
            AddChemicalOverlay("Waste", "waste-D", MaterialForm.Dissolved, waste.visualization.baseColor, -23);
            AddChemicalOverlay("Metabolic byproduct", "metabolic-waste-E", MaterialForm.Dissolved, metabolicWaste.visualization.baseColor, -24);
            AddChemicalOverlay("Signaling molecule", "signal-S", MaterialForm.Dissolved, signal.visualization.baseColor, -19);
            var view = GetComponent<ChemistryDemoView>();
            if (!view) view = gameObject.AddComponent<ChemistryDemoView>();
            view.demo = this;
            view.SetZoom(cultureView ? 3.7f : 2.5f);
            view.SetChemicalOverlay(!cultureView);
            view.SetPlaybackRate(assayMode ? 1f : .25f);
        }

        void CreateCulture()
        {
            // Static adherent arrangement; every visible cell has metabolism, chemical exchange,
            // exposure and leakage. This does not imply beating, tissue mechanics or motility.
            var random = new System.Random(seed + 81);
            int id = 3;
            for (int row = -3; row <= 3; row++)
            for (int column = -4; column <= 4; column++)
            {
                Vector2 position = new Vector2(column * 1.72f + (row % 2) * .83f, row * 1.47f);
                position += new Vector2((float)random.NextDouble() - .5f, (float)random.NextDouble() - .5f) * .24f;
                if (Vector2.Distance(position, new Vector2(-1.5f, 0)) < 1.45f || Vector2.Distance(position, new Vector2(1f, 0)) < 1.45f) continue;
                CreateCell("Culture cell " + id, id, position, false, .25f, false);
                id++;
            }
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
            particleRenderer.color = new Color(.24f, .34f, .16f);
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
                feeder.exchange = grazer.GetComponent<CellChemicalExchange>();
                feeder.edibleSubstanceId = "food-particle";
                feeder.captureRadius = .45f;
            }
        }

        void CreateCell(string label, int id, Vector2 position, bool secretor, float resistance, bool motile)
        {
            var go = Child(label);
            go.transform.position = position;
            var metabolism = go.AddComponent<CellMetabolism>();
            metabolism.energy = secretor ? 10f : 2f;
            var growth = go.AddComponent<CellBiomassGrowth>();
            growth.metabolism = metabolism;
            growth.structuralBiomass = 1f;
            growth.referenceBiomass = 1f;
            var membrane = go.AddComponent<MembraneIntegrity>();
            membrane.chemicalResistance = resistance;
            membrane.contourAmplitude = .12f + .075f * Mathf.Abs(Mathf.Sin(id * 2.7f));
            membrane.contourPhase = id * 1.87f;
            var visual = go.AddComponent<SpriteRenderer>();
            visual.sprite = CreateSprite(id, membrane);
            ownedAssets.Add(visual.sprite.texture);
            ownedAssets.Add(visual.sprite);
            visual.sortingOrder = 10;
            var membraneVisual = go.AddComponent<MembraneStateRenderer>();
            membraneVisual.membrane = membrane;
            visual.color = Color.white;
            membraneVisual.healthy = new Color(.23f, .28f, .25f);
            membraneVisual.injured = new Color(.58f, .25f, .18f);

            var exchange = go.AddComponent<CellChemicalExchange>();
            exchange.organismId = id;
            exchange.field = field;
            exchange.events = eventLog;
            exchange.metabolism = metabolism;
            exchange.biomassGrowth = growth;
            exchange.secretionEnabled = secretor;
            exchange.feedingEnabled = true;
            exchange.toxinReserve = secretor ? 2f : 0f;
            exchange.toxinRate = .4f;
            exchange.nutrientReserve = .5f;
            exchange.digestionRate = .04f;
            exchange.feedingPore = CreatePore(go.transform, "Dissolved feeding pore", membrane.SurfacePoint(16, .9f));
            exchange.secretionPore = CreatePore(go.transform, "Toxin secretion pore", membrane.SurfacePoint(0, .9f));
            exchange.metabolicOutlet = CreatePore(go.transform, "Metabolic outlet", membrane.SurfacePoint(24, .9f));
            // Demo phenotype only: Secretor emits periodic real chemical pulses;
            // Grazer receives local concentration, without a behavioral policy yet.
            var signaling = go.AddComponent<CellChemicalSignaling>();
            signaling.field = field;
            signaling.metabolism = metabolism;
            signaling.demoAutoPulse = false; // Scripted agent policy owns the emission decision.
            signaling.demoPulsePeriod = 1.8f;
            signaling.maximumEmissionPerPulse = .035f;
            signaling.energyPerUnitMass = 2f;
            // Signal originates from actual local food; receiver reacts to local receptor levels.
            // Other culture cells stay silent as an observable comparison group.
            var policy = go.AddComponent<LocalSignalPolicyV1>();
            policy.metabolism = metabolism;
            policy.signaling = signaling;
            policy.role = secretor ? LocalSignalPolicyV1.Role.SignalWhenFed
                : (id == 2 ? LocalSignalPolicyV1.Role.FollowSignal : LocalSignalPolicyV1.Role.Silent);
            policy.nutrientThreshold = .15f;
            policy.minimumEnergy = 1f;
            policy.decisionInterval = 1.8f;
            policy.signalPulseMass = .035f;
            var leakage = go.AddComponent<MembraneLeakageSystem>();
            leakage.exchange = exchange;
            leakage.membrane = membrane;
            if (cultureView) go.AddComponent<CellShapeDynamics>();

            if (motile)
            {
                var actuator = go.AddComponent<CellFlagellarActuator>();
                actuator.metabolism = metabolism;
                actuator.fluid = Fluid;
                actuator.beatFrequencyHz = secretor ? 1.7f : 2.5f;
                actuator.waveAmplitude = secretor ? .17f : .25f;
                actuator.motorActivation = secretor ? .8f : .92f;
                var softTail = go.AddComponent<SoftFlagellum2D>();
                softTail.motor = actuator;
                softTail.Initialize();
                var hydro = go.AddComponent<FlagellarHydrodynamics2D>();
                hydro.tail = softTail;
                hydro.motor = actuator;
                hydro.fluid = Fluid;
                var motorVisual = go.AddComponent<FlagellumRenderer>();
                motorVisual.actuator = actuator;
                motorVisual.softFlagellum = softTail;
                var motility = go.AddComponent<CellMotility>();
                motility.flagellum = actuator;
                motility.hydrodynamics = hydro;
                motility.fluid = Fluid;
                motility.field = field;
                motility.metabolism = metabolism;
                motility.useTemporalChemotaxis = true;
                motility.tumbleSeed = seed + id * 1009;
                if (assayMode) AssaySwimmer = motility;
                var trial = go.AddComponent<ChemotaxisTrialRecorder>();
                trial.motility = motility;
                trial.metabolism = metabolism;
                trial.motor = actuator;
                trial.field = field;
                if (assayMode) { AssayRecorder = trial; trial.samplePeriod = 100000f; }
                trial.controllerLabel = motility.useTemporalChemotaxis
                    ? (motility.stochasticTumbles ? "temporal-stochastic" : "temporal-deterministic")
                    : "spatial-steering";
                policy.motility = motility;
                var telemetry = go.AddComponent<SwimmerTelemetryOverlay>();
                telemetry.swimmer = motility;
                telemetry.hydro = hydro;
                telemetry.fluid = Fluid;
                // Distinct phenotypes make the focal organisms visibly different without
                // granting them non-physical teleportation or perfect field knowledge.
                motility.maxSpeed = secretor ? .34f : .48f;
                motility.attractantGain = secretor ? .65f : 1.15f;
                motility.repellentGain = secretor ? 1.25f : 1.8f;
                motility.exploratoryBias = secretor ? .08f : .14f;
                go.transform.rotation = Quaternion.Euler(0f, 0f, secretor ? 18f : 205f);
            }

            // The two motile focal organisms use the new physically skinned
            // body; the large static culture still uses its existing sector
            // membrane so we do not run competing shape solvers on one cell.
            if (motile)
            {
                var softBody = go.AddComponent<SoftCellBody2D>();
                softBody.membrane = membrane;
                softBody.fluid = Fluid;
                softBody.flagellum = go.GetComponent<CellFlagellarActuator>();
                softBody.Initialize();
                var compartments = go.AddComponent<InternalCompartments2D>();
                compartments.body = softBody;
                compartments.Initialize();
                var meshView = go.AddComponent<SoftCellMeshRenderer2D>();
                meshView.body = softBody;
                meshView.membrane = membrane;
                meshView.compartments = compartments;
                // Preserve physiology and membrane integrity; replace just
                // the legacy sprite and its texture-based boundary rendering.
                membraneVisual.enabled = false;
                visual.enabled = false;
            }

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

        static Transform CreatePore(Transform cell, string label, Vector2 position)
        {
            var pore = new GameObject(label).transform;
            pore.SetParent(cell, false);
            pore.localPosition = position;
            return pore;
        }

        // Static illustrative microstructure, not simulated organelles or additional material.
        static Sprite CreateSprite(int cellId, MembraneIntegrity membrane)
        {
            const int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var random = new System.Random(631 + cellId);
            var inclusions = new Vector2[32];
            for (int i = 0; i < inclusions.Length; i++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2f;
                float d = .38f + (float)random.NextDouble() * .34f;
                inclusions[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
            }
            var pixels = new Color[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + .5f - s*.5f)/(s*.5f) * MembraneStateRenderer.TextureHalfExtent;
                float dy = (y + .5f - s*.5f)/(s*.5f) * MembraneStateRenderer.TextureHalfExtent;
                float r = Mathf.Sqrt(dx*dx+dy*dy) / membrane.ContourScale(new Vector2(dx, dy));
                if (r >= .9f) continue;
                float edge = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.85f, .895f, r));
                float grain = Mathf.PerlinNoise(x * .21f + cellId * 37f, y * .21f);
                float broad = Mathf.PerlinNoise(x * .037f, y * .037f + cellId * 11f);
                float light = .49f + .12f * (1f - r) + .10f * grain + .1f * broad;
                float alpha = .65f + .18f * r;
                // Slightly offset nucleus with a soft envelope and a nucleolus.
                float nx = dx + .16f * Mathf.Sin(cellId * 7.1f);
                float ny = dy - .14f * Mathf.Cos(cellId * 3.7f);
                float nucleus = Mathf.Sqrt(Mathf.Pow(nx / (.25f + .04f * Mathf.Sin(cellId)), 2f) + Mathf.Pow(ny / .21f, 2f));
                nucleus *= 1f + .065f * Mathf.Sin(Mathf.Atan2(ny, nx) * 3f);
                float envelope = Mathf.Exp(-Mathf.Pow((nucleus - 1f) / .075f, 2f));
                if (nucleus < 1f) { light *= .82f + .1f * grain; alpha = .78f; }
                light -= envelope * .09f;
                float nucleolus = new Vector2(nx + .065f, ny - .025f).magnitude;
                light *= 1f - .25f * Mathf.Exp(-nucleolus * nucleolus / .0015f);
                foreach (var inclusion in inclusions)
                {
                    float distance = new Vector2((dx - inclusion.x) * 1.4f, dy - inclusion.y).sqrMagnitude;
                    float size = .001f + .0008f * (1f + Mathf.Sin(inclusion.x * 29f));
                    // Phase-bright inclusion with a darker edge; illustrative static texture.
                    light += .20f * Mathf.Exp(-distance / size);
                    light -= .09f * Mathf.Exp(-Mathf.Pow((Mathf.Sqrt(distance) - Mathf.Sqrt(size) * 1.5f) / .012f, 2f));
                }
                pixels[y * s + x] = new Color(light * .98f, light, light * .95f, alpha * edge);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex,new Rect(0,0,s,s),new Vector2(.5f,.5f),s / (2f * MembraneStateRenderer.TextureHalfExtent), 0, SpriteMeshType.FullRect);
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
            camera.backgroundColor = new Color(.73f,.76f,.73f);
            obj.transform.position = new Vector3(0,0,-10);
        }

        void OnDestroy()
        {
            foreach (var asset in ownedAssets)
                if (asset) Destroy(asset);
        }
    }
}
