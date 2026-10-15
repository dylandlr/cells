using UnityEngine;
using Cells.Physiology;

namespace Cells.Rendering
{
    /// <summary>Follows the authoritative flagellar actuator phase; does not control physics.</summary>
    [RequireComponent(typeof(CellFlagellarActuator))]
    public sealed class FlagellumRenderer : MonoBehaviour
    {
        public CellFlagellarActuator actuator;
        public int samples = 28;
        public float width = .035f;
        LineRenderer filament;
        Material material;

        void Awake()
        {
            if (!actuator) actuator = GetComponent<CellFlagellarActuator>();
            var child = new GameObject("Flagellum / driven by actuator");
            child.transform.SetParent(transform, false);
            filament = child.AddComponent<LineRenderer>();
            filament.useWorldSpace = false;
            filament.loop = false;
            filament.numCapVertices = 2;
            filament.numCornerVertices = 2;
            filament.startWidth = filament.endWidth = width;
            filament.sortingOrder = 9;
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader);
                filament.sharedMaterial = material;
            }
            filament.startColor = new Color(.25f,.34f,.28f,.94f);
            filament.endColor = new Color(.40f,.52f,.42f,.65f);
        }

        void LateUpdate()
        {
            if (!filament || !actuator) return;
            int count = Mathf.Clamp(samples, 4, 128);
            if (filament.positionCount != count) filament.positionCount = count;
            filament.enabled = actuator.structuralIntegrity > 0f;
            for (int i = 0; i < count; i++)
            {
                Vector2 local = actuator.SampleCenterline(i / (float)(count - 1));
                filament.SetPosition(i, new Vector3(local.x, local.y, 0f));
            }
        }

        void OnDestroy()
        {
            if (material) Destroy(material);
            if (filament) Destroy(filament.gameObject);
        }
    }
}
