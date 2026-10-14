using UnityEngine;
using Cells.Substances;
using Cells.Physiology;

namespace Cells.Demo
{
    /// <summary>Development-only informative HUD and simple microscope focus control.</summary>
    public sealed class ChemistryDemoView : MonoBehaviour
    {
        Camera microscope;
        CellMetabolism selected;
        bool follow;
        float zoom = 5f;

        void Update()
        {
            if (!microscope) microscope = Camera.main;
            if (!microscope) return;
            float delta = Input.mouseScrollDelta.y;
            if (delta != 0f) zoom = Mathf.Clamp(zoom * Mathf.Pow(.86f, delta), 1f, 12f);
            microscope.orthographicSize = Mathf.Lerp(microscope.orthographicSize, zoom, Time.unscaledDeltaTime * 8f);
            if (Input.GetKeyDown(KeyCode.Escape)) { selected = null; follow = false; }
            if (Input.GetKeyDown(KeyCode.F)) follow = !follow;
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mouse = microscope.ScreenToWorldPoint(Input.mousePosition);
                float best = .9f;
                CellMetabolism nearest = null;
                foreach (var candidate in FindObjectsByType<CellMetabolism>(FindObjectsSortMode.None))
                {
                    float d = Vector2.Distance(candidate.transform.position, mouse);
                    if (d < best) { best = d; nearest = candidate; }
                }
                selected = nearest;
            }
            if (follow && selected)
            {
                var next = selected.transform.position; next.z = -10f;
                microscope.transform.position = Vector3.Lerp(microscope.transform.position, next, Time.unscaledDeltaTime * 4f);
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 340, 170), GUI.skin.box);
            GUILayout.Label("cells | Chemical causality demo");
            GUILayout.Label("Scroll: zoom   Click: inspect   F: follow   Esc: clear");
            if (selected)
            {
                var membrane = selected.GetComponent<MembraneIntegrity>();
                GUILayout.Label("Cell: " + selected.gameObject.name);
                GUILayout.Label("Energy: " + selected.energy.ToString("F3"));
                GUILayout.Label("Membrane integrity: " + (membrane ? membrane.integrity.ToString("F3") : "N/A"));
                GUILayout.Label("Status: " + (selected.alive ? "Alive" : "Dead"));
            }
            else GUILayout.Label("Select a cell for its physiological state.");
            GUILayout.EndArea();
        }
    }
}
