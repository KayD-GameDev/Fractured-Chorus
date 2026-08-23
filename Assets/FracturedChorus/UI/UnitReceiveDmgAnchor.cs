using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Child object on a unit that marks where incoming VFX / damage lands.
    /// Transform only — no collider.
    /// </summary>
    public class UnitReceiveDmgAnchor : MonoBehaviour
    {
        [SerializeField] private UnitMarkerShape gizmoShape = UnitMarkerShape.Diamond;
        [SerializeField] private Color gizmoColor = new(1f, 0.28f, 0.32f, 0.95f);
        [SerializeField] [Min(0.02f)] private float gizmoSize = 0.16f;
        [SerializeField] private bool gizmoAlwaysVisible = true;

        public void WireReferences()
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
