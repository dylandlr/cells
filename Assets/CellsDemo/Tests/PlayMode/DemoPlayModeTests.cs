using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Cells.Demo;
using Cells.Physiology;
using Cells.Rendering;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class DemoPlayModeTests
    {
        ChemistryDemoBootstrap demo;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("ChemistryDemo", LoadSceneMode.Single);
            yield return null;
            demo = Object.FindAnyObjectByType<ChemistryDemoBootstrap>();
            Assert.IsNotNull(demo);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            Object.Destroy(demo.gameObject);
            yield return null;
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SavedSceneBuildsAndSimulatesMaterialTransfers()
        {
            Assert.That(demo.GetComponentsInChildren<CellMetabolism>().Length, Is.EqualTo(2));
            Assert.IsNotNull(demo.GetComponent<ChemistryDemoView>());
            Assert.IsTrue(demo.Registry.TryGet("toxin-B", out _));
            float mass = demo.Particles.TotalMass("food-particle");
            float nutrientBudget = NutrientBudget();
            var grazer = demo.transform.Find("Grazer").GetComponent<MembraneIntegrity>();
            yield return new WaitForSeconds(.4f);
            Assert.That(demo.Particles.TotalMass("food-particle"), Is.LessThan(mass));
            Assert.That(grazer.integrity, Is.LessThan(1f));
            Assert.That(demo.Field.Total("toxin-B"), Is.GreaterThan(0f));
            Assert.That(NutrientBudget(), Is.EqualTo(nutrientBudget).Within(2e-4f),
                "Particle ingestion, uptake, digestion and leakage must conserve represented nutrient/byproduct mass.");
        }

        float NutrientBudget()
        {
            float total = demo.Field.Total("nutrient-A") + demo.Field.Total("metabolic-waste-E")
                + demo.Particles.TotalMass("food-particle");
            foreach (var exchange in demo.GetComponentsInChildren<CellChemicalExchange>()) total += exchange.nutrientReserve;
            return total;
        }

        [UnityTest]
        public IEnumerator MicroscopePauseSelectionDoseAndReactionUseRealState()
        {
            var view = demo.GetComponent<ChemistryDemoView>();
            var grazer = demo.transform.Find("Grazer").GetComponent<CellMetabolism>();
            view.Select(grazer); view.SetPaused(true);
            float time = Time.fixedTime;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(Time.fixedTime, Is.EqualTo(time));
            float before = demo.Field.Total("neutralizer-C");
            Assert.That(view.Dose("neutralizer-C", .25f), Is.EqualTo(.25f));
            Assert.That(demo.Field.Total("neutralizer-C"), Is.EqualTo(before + .25f).Within(1e-5f));
            view.SetPaused(false);
            yield return new WaitForSeconds(.2f);
            Assert.That(demo.Field.Total("waste-D"), Is.GreaterThan(0f));
            Assert.AreSame(grazer, view.Selected);
        }

        [UnityTest]
        public IEnumerator ParticleVisualsTrackPositionsAndFieldCanBeReenabled()
        {
            Time.timeScale = 0f;
            yield return null;
            yield return null;
            var display = demo.GetComponentInChildren<TrackedParticleRenderer>();
            Assert.That(display.transform.childCount, Is.GreaterThan(0));
            Assert.That(Vector2.Distance(display.transform.GetChild(0).position,
                demo.Particles.Particles[0].position), Is.LessThan(.01f));
            var overlay = demo.GetComponentInChildren<ChemicalFieldRenderer>();
            var sprite = overlay.GetComponent<SpriteRenderer>().sprite;
            Assert.IsNotNull(sprite);
            overlay.enabled = false; yield return null;
            overlay.enabled = true; yield return null;
            Assert.AreSame(sprite, overlay.GetComponent<SpriteRenderer>().sprite);
        }

        [UnityTest]
        public IEnumerator CameraRendersSimulationAndSavesEvidence()
        {
            yield return new WaitForSeconds(.2f);
            CaptureCamera("microscope-camera", "Default scene, live simulation.");
        }

        [UnityTest]
        public IEnumerator DirectionalToxinCreatesVisibleLocalRuptureWithoutChangingGeometry()
        {
            var view = demo.GetComponent<ChemistryDemoView>();
            var cell = demo.transform.Find("Grazer");
            var membrane = cell.GetComponent<MembraneIntegrity>();
            var renderer = cell.GetComponent<MembraneStateRenderer>();
            Vector3 initialScale = cell.localScale;
            // Isolate contact location from transport while the normal exposure FixedUpdate runs.
            view.SetPaused(true);
            demo.Field.Initialize(); demo.Field.enabled = false; demo.Reaction.enabled = false;
            membrane.integrity = 1f;
            view.Select(cell.GetComponent<CellMetabolism>()); view.SetPipetteSector(0);
            Assert.That(view.Dose("toxin-B", .5f), Is.EqualTo(.5f));
            view.SetPaused(false);
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(membrane.ruptured);
            Assert.IsFalse(cell.GetComponent<CellMetabolism>().alive);
            Assert.That(membrane.GetIntegrity(16), Is.EqualTo(1f));
            Assert.That(membrane.integrity, Is.GreaterThan(.8f));
            renderer.Refresh();
            var boundary = renderer.BoundaryRenderer.sprite.texture;
            float span = 2f * MembraneStateRenderer.TextureHalfExtent;
            var right = membrane.SurfacePoint(0, .9f) / span + Vector2.one * .5f;
            var left = membrane.SurfacePoint(16, .9f) / span + Vector2.one * .5f;
            Assert.That(boundary.GetPixelBilinear(right.x, right.y).a, Is.Zero, "Failed right-hand sector must leave a visible opening.");
            Assert.That(boundary.GetPixelBilinear(left.x, left.y).a, Is.GreaterThan(.6f), "Opposite membrane must remain visible through its antialiased edge.");
            Assert.That(cell.localScale, Is.EqualTo(initialScale), "Rendering must not alter physical sampling geometry.");
            CaptureCamera("membrane-local-rupture", "Directional toxin challenge: 0.5 mass at region 0; diffusion/reaction disabled; normal FixedUpdate exposure for 2 seconds.");
        }

        [UnityTest]
        public IEnumerator ShaderBoundaryFollowsAuthoritativeDeformation()
        {
            Time.timeScale = 0f;
            var cell = demo.transform.Find("Grazer");
            var membrane = cell.GetComponent<MembraneIntegrity>();
            membrane.integrity = 1f;
            var display = cell.GetComponent<MembraneStateRenderer>();
            display.Refresh();
            var boundary = display.BoundaryRenderer;
            var camera = Camera.main;
            var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var oldRect = camera.rect;
            var oldBackground = camera.backgroundColor;
            int oldMask = camera.cullingMask;
            try
            {
                boundary.gameObject.layer = 30;
                camera.cullingMask = 1 << 30;
                camera.backgroundColor = Color.clear;
                camera.orthographicSize = MembraneStateRenderer.TextureHalfExtent;
                camera.transform.position = cell.position + Vector3.back * 10f;
                camera.rect = new Rect(0, 0, 1, 1);
                camera.targetTexture = target;
                System.Func<Vector2, float> alphaAt = local =>
                {
                    camera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); image.Apply();
                    Vector3 uv = camera.WorldToViewportPoint(cell.TransformPoint(local));
                    return image.GetPixelBilinear(uv.x, uv.y).a;
                };
                Vector2 before = membrane.SurfacePoint(0, .9f);
                Assert.That(alphaAt(before), Is.GreaterThan(.4f));
                membrane.SetStretch(0, .75f);
                display.Refresh();
                Vector2 after = membrane.SurfacePoint(0, .9f);
                Assert.That(alphaAt(before), Is.LessThan(.05f), "Old boundary location must clear in the actual render.");
                Assert.That(alphaAt(after), Is.GreaterThan(.4f), "Optical rim must match the moved physical contact.");
            }
            finally
            {
                camera.targetTexture = oldTarget; camera.cullingMask = oldMask;
                camera.rect = oldRect; camera.backgroundColor = oldBackground;
                RenderTexture.active = oldActive; boundary.gameObject.layer = 0;
                target.Release(); Object.Destroy(target); Object.Destroy(image);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CultureSceneKeepsChemistryActiveWithoutBeatingOrChemicalOverlay()
        {
            yield return SceneManager.LoadSceneAsync("MicroscopeCulture", LoadSceneMode.Single);
            yield return null;
            demo = Object.FindAnyObjectByType<ChemistryDemoBootstrap>();
            var cells = demo.GetComponentsInChildren<CellMetabolism>();
            Assert.That(cells.Length, Is.GreaterThan(40));
            var positions = new Vector3[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                positions[i] = cells[i].transform.position;
                Assert.IsNotNull(cells[i].GetComponent<CellChemicalExchange>());
                Assert.IsNotNull(cells[i].GetComponent<MembraneLeakageSystem>());
            }
            float before = NutrientBudget();
            yield return new WaitForSeconds(.3f);
            Assert.That(NutrientBudget(), Is.EqualTo(before).Within(1e-3f));
            for (int i = 0; i < cells.Length; i++) Assert.That(cells[i].transform.position, Is.EqualTo(positions[i]));
            foreach (var overlay in demo.GetComponentsInChildren<ChemicalFieldRenderer>())
                Assert.IsFalse(overlay.GetComponent<SpriteRenderer>().enabled);
            Assert.That(cells[0].GetComponent<MembraneIntegrity>().GeometryRevision, Is.GreaterThan(0));
            Assert.IsTrue(cells[0].GetComponent<SpriteRenderer>().sharedMaterial.shader.isSupported);
            Assert.That(demo.Field.Total("metabolic-waste-E"), Is.GreaterThan(0f));
            CaptureCamera("microscope-culture", "Adherent culture with local deformation; active chemistry; false-color overlay hidden; no beating.", 3.7f);
        }

        void CaptureCamera(string name, string scenario, float orthographicSize = 2.5f)
        {
            var camera = Camera.main;
            Assert.IsNotNull(camera);
            var target = new RenderTexture(960, 540, 24);
            var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var previousRect = camera.rect;
            try
            {
                camera.orthographicSize = orthographicSize;
                camera.rect = new Rect(0, 0, 1, 1);
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
                var pixels = image.GetPixels32();
                int varied = 0;
                var background = pixels[0];
                foreach (var pixel in pixels)
                    if (Mathf.Abs(pixel.r - background.r) + Mathf.Abs(pixel.g - background.g) + Mathf.Abs(pixel.b - background.b) > 30) varied++;
                Assert.That(varied, Is.GreaterThan(1000), "Camera must draw actual objects, not just clear color.");
                Directory.CreateDirectory("Artifacts");
                File.WriteAllBytes("Artifacts/" + name + ".png", image.EncodeToPNG());
                File.WriteAllText("Artifacts/" + name + ".txt", "Actual Unity PlayMode Camera.Render output; excludes IMGUI HUD.\nEditor: " + Application.unityVersion + "\nSeed: " + demo.seed + "\nSimulation time: " + Time.fixedTime + "\nScenario: " + scenario + "\n");
            }
            finally
            {
                camera.targetTexture = previousTarget; camera.rect = previousRect; RenderTexture.active = previous;
                target.Release(); Object.Destroy(target); Object.Destroy(image);
            }
        }

        [UnityTest]
        public IEnumerator RupturedCellLeaksNutrientsWithoutFeedingOrLosingMass()
        {
            var view = demo.GetComponent<ChemistryDemoView>();
            view.SetPaused(true); demo.Field.Initialize();
            var cell = demo.transform.Find("Grazer");
            var exchange = cell.GetComponent<CellChemicalExchange>();
            exchange.nutrientReserve = 1f; exchange.digestionRate = 0f;
            exchange.feedingEnabled = exchange.secretionEnabled = false;
            cell.GetComponentInChildren<ParticleFeedingSite>().enabled = false;
            var membrane = cell.GetComponent<MembraneIntegrity>();
            membrane.chemicalResistance = 0f; membrane.ApplyExposureToSegment(0, 1f);
            float previouslyLeaked = cell.GetComponent<MembraneLeakageSystem>().TotalLeaked;
            view.SetPaused(false);
            yield return new WaitForSeconds(.25f);
            Assert.IsFalse(cell.GetComponent<CellMetabolism>().alive);
            Assert.That(cell.GetComponent<CellMetabolism>().energy, Is.Zero);
            Assert.That(cell.GetComponent<MembraneLeakageSystem>().TotalLeaked, Is.GreaterThan(previouslyLeaked));
            Assert.That(demo.Field.Total("nutrient-A"), Is.GreaterThan(0f));
            Assert.That(exchange.nutrientReserve + demo.Field.Total("nutrient-A"), Is.EqualTo(1f).Within(1e-4f));
        }
    }
}
