using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Marks the note-head contact point used to pin a boss timeline note onto BorderTop (note rail).
    /// No collider — Transform only, same role as <see cref="UnitFeetAnchor"/> for units.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossNoteRailAnchor : MonoBehaviour
    {
        [SerializeField] private Vector2 gizmoSize = new(12f, 6f);

        public Vector2 GizmoSize
        {
            get => gizmoSize;
            set => gizmoSize = value;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.35f, 0.95f, 1f, 0.95f);
            Gizmos.DrawWireCube(transform.position, gizmoSize);
        }

        private void OnDrawGizmos()
        {
            // Dimmer when not selected so rail pin stays visible while tuning notes.
            Gizmos.color = new Color(0.35f, 0.95f, 1f, 0.35f);
            Gizmos.DrawWireCube(transform.position, gizmoSize * 0.85f);
        }
#endif
    }
}
