using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Explicit geometric handoff for the experimental spawn-based division.
    /// Two independent existing soft-body solvers are positioned according to
    /// their assigned biomass-dependent radii. This is NOT a topological
    /// scission of a shared membrane mesh.
    /// </summary>
    public static class DivisionGeometryHandoff2D
    {
        public static bool TrySeparate(SoftCellBody2D parent, SoftCellBody2D daughter,
            float clearanceFraction = .08f)
        {
            if (!parent || !daughter || parent == daughter ||
                !parent.Initialized || !daughter.Initialized ||
                !float.IsFinite(clearanceFraction) || clearanceFraction < 0f ||
                clearanceFraction > 1f) return false;
            float parentRadius=parent.CurrentRadius;
            float childRadius=daughter.CurrentRadius;
            if (!float.IsFinite(parentRadius) || !float.IsFinite(childRadius) ||
                parentRadius <= 0f || childRadius <= 0f) return false;
            Vector2 axis=parent.transform.up;
            if (axis.sqrMagnitude<.001f) axis=Vector2.up;
            axis.Normalize();
            Vector2 midpoint=((Vector2)parent.transform.position+
                (Vector2)daughter.transform.position)*.5f;
            // Measured contour extents are more robust than nominal radius:
            // injured/deformed membranes can bulge outside their rest circles.
            float parentExtent=MeasuredExtent(parent);
            float daughterExtent=MeasuredExtent(daughter);
            if (!float.IsFinite(parentExtent) || !float.IsFinite(daughterExtent) ||
                parentExtent <= 0f || daughterExtent <= 0f) return false;
            float spacing=(Mathf.Max(parentRadius,parentExtent)+
                Mathf.Max(childRadius,daughterExtent))*(1f+clearanceFraction);
            parent.transform.position=midpoint-axis*(spacing*.5f);
            daughter.transform.position=midpoint+axis*(spacing*.5f);
            parent.divisionConstriction=0f;
            daughter.divisionConstriction=0f;
            return true;
        }
        public static float MeasuredExtent(SoftCellBody2D body)
        {
            if (!body || !body.Initialized) return 0f;
            float maximum=0f;
            for (int i=0;i<SoftCellBody2D.Nodes;i++)
            {
                Vector2 node=body.Node(i);
                if (!float.IsFinite(node.x) || !float.IsFinite(node.y))
                    return float.NaN;
                maximum=Mathf.Max(maximum,node.magnitude);
            }
            return maximum;
        }
    }
}
