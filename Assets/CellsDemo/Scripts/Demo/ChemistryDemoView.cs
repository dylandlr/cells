using System.Collections.Generic;
using UnityEngine;
using Cells.Substances;
using Cells.Physiology;
using Cells.Rendering;

namespace Cells.Demo
{
    /// <summary>Human-only microscope/debug controls; never feeds metadata to an agent.</summary>
    public sealed class ChemistryDemoView : MonoBehaviour
    {
        public ChemistryDemoBootstrap demo;
        public CellMetabolism Selected { get; private set; }
        public int PipetteSector { get; private set; }
        public void SetPipetteSector(int sector) => PipetteSector = Mathf.Clamp(sector, 0, MembraneIntegrity.SegmentCount - 1);
        public Vector2 InspectionPosition
        {
            get
            {
                if (!Selected) return Vector2.zero;
                var exposure = Selected.GetComponent<CellExposureSystem>();
                float radius = exposure ? exposure.membraneRadius : .9f;
                var membrane = Selected.GetComponent<MembraneIntegrity>();
                return Selected.transform.TransformPoint(membrane ? membrane.SurfacePoint(PipetteSector, radius)
                    : MembraneIntegrity.DirectionOf(PipetteSector) * radius);
            }
        }
        Camera microscope;
        bool follow;
        bool chemicalOverlay = true;
        float zoom = 5f;
        readonly List<ChemicalEvent> history = new List<ChemicalEvent>();
        Rect Panel => new Rect(12, 12, Mathf.Min(330, Screen.width - 24), Mathf.Max(100, Screen.height - 24));
        Vector2 scroll;

        public void Select(CellMetabolism cell) => Selected = cell;
        public void SetZoom(float size) => zoom = Mathf.Clamp(size, 1f, 12f);
        public void SetChemicalOverlay(bool visible)
        {
            chemicalOverlay = visible;
            if (!demo) return;
            foreach (var overlay in demo.GetComponentsInChildren<ChemicalFieldRenderer>())
                overlay.GetComponent<SpriteRenderer>().enabled = visible;
        }
        public void SetPaused(bool paused) => Time.timeScale = paused ? 0f : 1f;
        public float Dose(string substance, float mass)
        {
            if (!demo || !demo.Field) return 0f;
            Vector2 position = InspectionPosition;
            float added = demo.Field.Deposit(substance, position, mass);
            if (added > 0f) demo.Events.Record(Time.fixedTime, 0, substance,
                ChemicalEventKind.Added, added, position, "microscope.pipette");
            return added;
        }

        void Update()
        {
            if (!microscope) microscope = Camera.main;
            if (!microscope) return;
            Vector2 guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            bool overPanel = Panel.Contains(guiMouse);
            // Reserve the sidebar's screen space so it cannot conceal a cell at normal zoom.
            float sidebar = Mathf.Clamp01((Panel.xMax + 12f) / Mathf.Max(1, Screen.width));
            microscope.rect = new Rect(sidebar, 0f, Mathf.Max(.1f, 1f - sidebar), 1f);
            if (!overPanel && Input.mouseScrollDelta.y != 0f)
                zoom = Mathf.Clamp(zoom * Mathf.Pow(.86f, Input.mouseScrollDelta.y), 1f, 12f);
            microscope.orthographicSize = Mathf.Lerp(microscope.orthographicSize, zoom, Time.unscaledDeltaTime * 8f);
            if (Input.GetKeyDown(KeyCode.Space)) SetPaused(Time.timeScale > 0f);
            if (Input.GetKeyDown(KeyCode.Escape)) { Select(null); follow = false; }
            if (Input.GetKeyDown(KeyCode.F)) follow = !follow;
            if (!overPanel && Input.GetMouseButtonDown(0) && demo)
            {
                Vector2 mouse = microscope.ScreenToWorldPoint(Input.mousePosition);
                float best = 1f;
                CellMetabolism nearest = null;
                foreach (var candidate in demo.GetComponentsInChildren<CellMetabolism>())
                {
                    float distance = Vector2.Distance(candidate.transform.position, mouse);
                    if (distance < best) { best = distance; nearest = candidate; }
                }
                Select(nearest);
            }
            if (follow && Selected)
            {
                var next = Selected.transform.position; next.z = -10f;
                microscope.transform.position = Vector3.Lerp(microscope.transform.position, next, Time.unscaledDeltaTime * 4f);
            }
            else
            {
                Vector2 pan = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                if (pan.sqrMagnitude > 1f) pan.Normalize();
                if (pan.sqrMagnitude > 0f)
                {
                    Vector3 next = microscope.transform.position + (Vector3)(pan * zoom * .75f * Time.unscaledDeltaTime);
                    next.z = -10f;
                    microscope.transform.position = next;
                }
            }
        }

        void OnGUI()
        {
            if (!demo || !demo.Field) return;
            GUILayout.BeginArea(Panel, GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("CELLS / CAUSAL MICROSCOPE");
            GUILayout.Label("Seed " + demo.seed + " | t = " + Time.fixedTime.ToString("F1") + " s");
            GUILayout.Label("Scroll: zoom | Click: inspect | F: follow | WASD: pan");
            if (GUILayout.Button(Time.timeScale > 0f ? "Pause [Space]" : "Resume [Space]"))
                SetPaused(Time.timeScale > 0f);
            bool overlayVisible = GUILayout.Toggle(chemicalOverlay, "Show false-color chemical concentrations");
            if (overlayVisible != chemicalOverlay) SetChemicalOverlay(overlayVisible);
            GUILayout.Label("Click any cell to inspect / key organisms:");
            GUILayout.BeginHorizontal();
            foreach (var cell in demo.GetComponentsInChildren<CellMetabolism>())
                if ((cell.name == "Secretor" || cell.name == "Grazer") && GUILayout.Button(cell.name)) Select(cell);
            GUILayout.EndHorizontal();
            Vector2 location = InspectionPosition;
            if (Selected)
            {
                var membrane = Selected.GetComponent<MembraneIntegrity>();
                GUILayout.Label(Selected.name + " / " + (Selected.alive ? "Alive" : "Dead"));
                GUILayout.Label("Energy: " + Selected.energy.ToString("F3"));
                GUILayout.Label("Membrane: " + membrane.integrity.ToString("P1") + (membrane.ruptured ? " / RUPTURED" : ""));
                GUILayout.Label("Weakest region: " + membrane.WeakestIntegrity.ToString("P1"));
                var motility = Selected.GetComponent<CellMotility>();
                if (motility)
                {
                    GUILayout.Label("Speed: " + motility.Velocity.magnitude.ToString("F3") + " units/s");
                    GUILayout.Label("Nutrient gradient: " + motility.LastAttractantGradient.ToString("F3"));
                    GUILayout.Label("Toxin gradient: " + motility.LastRepellentGradient.ToString("F3"));
                    var motor = Selected.GetComponent<CellFlagellarActuator>();
                    if (motor)
                    {
                        GUILayout.Label("Flagellar thrust: " + motor.EffectiveThrust.ToString("F3"));
                        GUILayout.Label("Beat frequency: " + motor.beatFrequencyHz.ToString("F2") + " Hz");
                        GUILayout.Label("Motor integrity: " + motor.structuralIntegrity.ToString("P0"));
                    }
                }
                var policy = Selected.GetComponent<LocalSignalPolicyV1>();
                if (policy)
                {
                    GUILayout.Label("Agent policy: " + policy.role);
                    GUILayout.Label("Decisions: " + policy.Decisions);
                    GUILayout.Label("Local nutrient cue: " + policy.LastNutrientConcentration.ToString("F4"));
                    GUILayout.Label("Successful signals: " + policy.SuccessfulSignals);
                    GUILayout.Label("Follow signal: " + (policy.FollowEnabled ? "active" : "inactive"));
                }
                var signaling = Selected.GetComponent<CellChemicalSignaling>();
                if (signaling)
                {
                    GUILayout.Label("Signal concentration: " + signaling.ReadLocal().ToString("F4"));
                    GUILayout.Label("Signal detected: " + (signaling.LastDetected ? "yes" : "no"));
                    GUILayout.Label("Signal mass emitted: " + signaling.CumulativeEmittedMass.ToString("F4"));
                    GUILayout.Label("Signal energy cost: " + signaling.CumulativeEnergySpent.ToString("F4"));
                    if (Selected.alive && GUILayout.Button("Emit signaling molecule (uses energy)"))
                        signaling.Emit(signaling.maximumEmissionPerPulse);
                }
                var exchange = Selected.GetComponent<CellChemicalExchange>();
                var leakage = Selected.GetComponent<MembraneLeakageSystem>();
                if (exchange) GUILayout.Label("Internal nutrient mass: " + exchange.nutrientReserve.ToString("F4"));
                if (leakage) GUILayout.Label("Leaked nutrient mass: " + leakage.TotalLeaked.ToString("F4"));
                GUI.enabled = Selected.alive && !membrane.ruptured;
                if (GUILayout.Button("Repair membrane (uses energy)")) membrane.Repair(1f, Selected);
                GUI.enabled = true;
            }
            GUILayout.Space(8);
            GUILayout.Label("Dissolved chemistry / local concentration");
            foreach (var species in demo.Field.species)
            {
                if (!ChemicalInspectorQuery.TryRead(demo.Field, demo.Registry, species.stableId, location, out var state)) continue;
                GUI.contentColor = state.displayColor;
                GUILayout.Label(state.name + ": " + state.localConcentration.ToString("F3"));
                GUI.contentColor = Color.white;
                GUILayout.Label("  Total mass: " + state.totalQuantity.ToString("F3"));
            }
            GUILayout.Label("Suspended food: " + demo.Particles.Particles.Count + " packets / " + demo.Particles.TotalMass("food-particle").ToString("F3") + " mass");
            GUILayout.Label("Neutralized mass: " + demo.Reaction.ReactedMass.ToString("F3"));
            if (demo.Fluid) GUILayout.Label("Fluid mean |divergence|: " + demo.Fluid.MeanAbsoluteDivergence().ToString("F4"));
            GUILayout.Label("Pipette at " + (Selected ? Selected.name + " membrane" : "field center"));
            if (Selected)
            {
                SetPipetteSector(Mathf.RoundToInt(GUILayout.HorizontalSlider(PipetteSector, 0, MembraneIntegrity.SegmentCount - 1)));
                GUILayout.Label("Region " + PipetteSector + " / " + (PipetteSector * 360f / MembraneIntegrity.SegmentCount).ToString("F0") + " degrees (0 = right)");
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Food")) Dose("nutrient-A", .25f);
            if (GUILayout.Button("+ Toxin")) Dose("toxin-B", .1f);
            if (GUILayout.Button("+ Reagent")) Dose("neutralizer-C", .25f);
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            GUILayout.Label("Recent causal events (mass / injury)");
            int id = Selected ? Selected.GetComponent<CellChemicalExchange>().organismId : 0;
            demo.Events.QueryOrganism(id, history);
            for (int i = Mathf.Max(0, history.Count - 6); i < history.Count; i++)
            {
                var e = history[i];
                GUILayout.Label(e.simulationTime.ToString("F2") + "s " + e.kind + " " + e.substanceId + " " + e.quantity.ToString("F4"));
                GUILayout.Label("  " + e.sourceStructure);
            }
            GUILayout.Label("Illustrative model / arbitrary units.\nNot calibrated biological toxicology.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void OnDisable() => Time.timeScale = 1f;
    }
}
