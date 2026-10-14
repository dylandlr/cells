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
            var grazer = demo.transform.Find("Grazer").GetComponent<MembraneIntegrity>();
            yield return new WaitForSeconds(.4f);
            Assert.That(demo.Particles.TotalMass("food-particle"), Is.LessThan(mass));
            Assert.That(grazer.integrity, Is.LessThan(1f));
            Assert.That(demo.Field.Total("toxin-B"), Is.GreaterThan(0f));
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
            var camera = Camera.main;
            Assert.IsNotNull(camera);
            var target = new RenderTexture(960, 540, 24);
            var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var previousRect = camera.rect;
            try
            {
                camera.orthographicSize = 2.5f;
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
                File.WriteAllBytes("Artifacts/microscope-camera.png", image.EncodeToPNG());
                File.WriteAllText("Artifacts/microscope-camera.txt", "Actual Unity PlayMode Camera.Render output; excludes IMGUI HUD.\nEditor: " + Application.unityVersion + "\nSeed: " + demo.seed + "\nSimulation time: " + Time.fixedTime + "\n");
            }
            finally
            {
                camera.targetTexture = previousTarget; camera.rect = previousRect; RenderTexture.active = previous;
                target.Release(); Object.Destroy(target); Object.Destroy(image);
            }
        }
    }
}
