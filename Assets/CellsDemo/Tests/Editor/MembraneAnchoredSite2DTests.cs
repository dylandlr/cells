using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class MembraneAnchoredSite2DTests
    {
        GameObject cell;

        [TearDown]
        public void TearDown()
        {
            if (cell) Object.DestroyImmediate(cell);
        }

        [Test]
        public void PoreFollowsLiveSoftBodySurfaceAfterGrowth()
        {
            cell = new GameObject("port test");
            var metabolism = cell.AddComponent<CellMetabolism>();
            metabolism.energy = 8f;
            var membrane = cell.AddComponent<MembraneIntegrity>();
            var growth = cell.AddComponent<CellBiomassGrowth>();
            growth.metabolism = metabolism;
            growth.assimilationRate = 1f;
            var body = cell.AddComponent<SoftCellBody2D>();
            body.membrane = membrane;
            body.growth = growth;
            body.Initialize();

            var pore = new GameObject("pore");
            pore.transform.SetParent(cell.transform, false);
            var anchor = pore.AddComponent<MembraneAnchoredSite2D>();
            anchor.body = body;
            anchor.membrane = membrane;
            anchor.sector = 16;
            anchor.UpdatePosition();
            float initial = pore.transform.localPosition.magnitude;

            float reserve = 1f;
            Assert.Greater(growth.AssimilateFrom(ref reserve, 1f), 0f);
            for (int i = 0; i < 150; ++i) body.Tick(.02f);
            anchor.UpdatePosition();

            Assert.Greater(pore.transform.localPosition.magnitude, initial);
            Assert.AreEqual(body.SurfacePoint(16).x, pore.transform.localPosition.x, 1e-5f);
            Assert.AreEqual(body.SurfacePoint(16).y, pore.transform.localPosition.y, 1e-5f);
        }

        [Test]
        public void WithoutSoftBodyPoreUsesMembraneContour()
        {
            cell = new GameObject("fallback");
            var membrane = cell.AddComponent<MembraneIntegrity>();
            var pore = new GameObject("pore");
            pore.transform.SetParent(cell.transform, false);
            var anchor = pore.AddComponent<MembraneAnchoredSite2D>();
            anchor.body = null;
            anchor.membrane = membrane;
            anchor.sector = 4;
            anchor.UpdatePosition();
            var expected = membrane.SurfacePoint(4, anchor.legacyRadius);
            Assert.AreEqual(expected.x, pore.transform.localPosition.x, 1e-5f);
            Assert.AreEqual(expected.y, pore.transform.localPosition.y, 1e-5f);
        }
    }
}
