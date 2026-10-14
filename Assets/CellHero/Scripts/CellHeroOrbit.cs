using UnityEngine;
namespace Cells.Hero
{
    public sealed class CellHeroOrbit : MonoBehaviour
    {
        public Transform target;
        public Vector3 focusOffset = new Vector3(0f, 0f, -0.7f);
        public float radius = 7f;
        public float elevation = 5.5f;
        public float degreesPerSecond = 7f;
        private float angle;
        private void LateUpdate()
        {
            if (target == null) return;
            angle += degreesPerSecond * Mathf.Deg2Rad * Time.deltaTime;
            var focus = target.position + focusOffset;
            transform.position = focus + new Vector3(Mathf.Sin(angle) * radius, elevation, -Mathf.Cos(angle) * radius);
            transform.LookAt(focus);
        }
    }
}
