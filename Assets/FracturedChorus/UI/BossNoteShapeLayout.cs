using System;
using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Per-shape layout. Hierarchy: NoteSimulator / Knob / (RailAnchor, NoteNum).
    /// Knob places the belly region; RailAnchor + NoteNum are children (usually centered).
    /// </summary>
    [Serializable]
    public struct BossNoteShapeLayout
    {
        [Tooltip("Tab name in Note Simulator. Empty = V0, V1, …")]
        public string displayName;

        [Tooltip("Knob local pos from NoteSimulator center (belly region).")]
        public Vector2 knobLocal;

        [Tooltip("Knob size — space for NoteNum.")]
        public Vector2 knobSize;

        [Tooltip("RailAnchor local pos from Knob center (usually 0,0 = centered).")]
        public Vector2 railAnchorLocal;

        [Tooltip("NoteNum local pos from Knob center (usually 0,0 = centered).")]
        public Vector2 noteNumLocal;

        [Tooltip("Sprite for this shape. Empty = use timeline catalog fallback.")]
        public Sprite sprite;

        public string TabLabel(int index)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            return "V" + index;
        }

        /// <summary>Pin point in NoteSimulator space (onto BorderTop).</summary>
        public Vector2 PinInNoteSpace => knobLocal + railAnchorLocal;

        public bool HasData =>
            sprite != null
            || knobSize.x > 0.5f
            || knobSize.y > 0.5f
            || !Mathf.Approximately(knobLocal.sqrMagnitude, 0f)
            || !Mathf.Approximately(railAnchorLocal.sqrMagnitude, 0f)
            || !Mathf.Approximately(noteNumLocal.sqrMagnitude, 0f);

        public static BossNoteShapeLayout FromKnob(
            Vector2 knobLocal,
            Vector2 knobSize,
            Vector2 railLocal,
            Vector2 numLocal,
            Sprite sprite = null,
            string displayName = null) =>
            new()
            {
                displayName = displayName,
                knobLocal = knobLocal,
                knobSize = knobSize.x > 0.5f ? knobSize : new Vector2(24f, 24f),
                railAnchorLocal = railLocal,
                noteNumLocal = numLocal,
                sprite = sprite
            };

        /// <summary>Legacy migration: old layouts stored pin/num in note space.</summary>
        public static BossNoteShapeLayout FromLegacyNoteSpace(Vector2 railInNote, Vector2 numInNote)
        {
            var knob = !Mathf.Approximately(numInNote.sqrMagnitude, 0f) ? numInNote : railInNote;
            return FromKnob(knob, new Vector2(24f, 24f), railInNote - knob, Vector2.zero);
        }
    }
}
