using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Marks the feet / ground contact point used to snap the unit onto grid cell centers.
    /// No collider — only Transform, so child does not steal pointer hits from body BoxCollider2D.
    /// </summary>
    public class UnitFeetAnchor : MonoBehaviour
    {
        [SerializeField] private Vector2 gizmoSize = new(0.2f, 0.1f);

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
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(transform.position, gizmoSize);
        }
#endif
    }
}
