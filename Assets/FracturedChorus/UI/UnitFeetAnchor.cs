using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Marks the feet / ground contact point used to snap the unit onto grid cell centers.
    /// No collider — only Transform, so child does not steal pointer hits from body BoxCollider2D.
    /// </summary>
    public class UnitFeetAnchor : MonoBehaviour
    {
        [SerializeField] private UnitMarkerShape gizmoShape = UnitMarkerShape.Square;
        [SerializeField] private Color gizmoColor = new(1f, 0.85f, 0.2f, 0.95f);
        [SerializeField] [Min(0.02f)] private float gizmoSize = 0.2f;
        [SerializeField] private bool gizmoAlwaysVisible = true;

        public void WireReferences()
        {
            RemoveLegacyFeetCollider();
        }

        private void RemoveLegacyFeetCollider()
        {
            var col = GetComponent<Collider2D>();
            if (col == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(col);
            }
            else
            {
                DestroyImmediate(col);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (gizmoAlwaysVisible)
            {
                UnitMarkerGizmo.DrawAt(transform.position, gizmoShape, gizmoColor, gizmoSize);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!gizmoAlwaysVisible)
            {
                UnitMarkerGizmo.DrawAt(transform.position, gizmoShape, gizmoColor, gizmoSize);
            }
        }
#endif
    }
}
