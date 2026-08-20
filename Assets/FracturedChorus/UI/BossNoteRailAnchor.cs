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
    }
}
