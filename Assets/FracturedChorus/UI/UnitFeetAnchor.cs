using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Feet / ground contact point — snap unit onto grid cell centers. Transform only, no collider.
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
