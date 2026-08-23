using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Child object on a unit that marks VFX spawn point A.
    /// Transform only — no collider, no combat sprite.
    /// </summary>
    public class UnitProjectileAnchor : MonoBehaviour
    {
        [SerializeField] private UnitMarkerShape gizmoShape = UnitMarkerShape.Triangle;
        [SerializeField] private Color gizmoColor = new(0.35f, 0.85f, 1f, 0.95f);
        [SerializeField] [Min(0.02f)] private float gizmoSize = 0.16f;
        [SerializeField] private bool gizmoAlwaysVisible = true;

        public void WireReferences()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(col);
                }
                else
                {
                    DestroyImmediate(col);
                }
            }

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(sr);
                }
                else
                {
                    DestroyImmediate(sr);
                }
            }

            var extraGizmo = GetComponent<UnitMarkerGizmo>();
            if (extraGizmo != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(extraGizmo);
                }
                else
                {
                    DestroyImmediate(extraGizmo);
                }
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
