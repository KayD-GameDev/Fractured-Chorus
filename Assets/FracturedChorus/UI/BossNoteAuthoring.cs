using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Scene-authored boss attack note — edit RemainingHits in the Inspector.
    /// Survives edit-mode seed; Play hides these and rebuilds from telegraphs (or merges when none).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossNoteAuthoring : MonoBehaviour
    {
        [Tooltip("Absolute beat index this note belongs to.")]
        [SerializeField] private int beatIndex = 1;

        [Tooltip("Hits required to cancel this enemy attack note.")]
        [SerializeField] [Min(0)] private int remainingHits = 3;

        [SerializeField] private BossNoteTier displayTier = BossNoteTier.Red;

        [SerializeField] private Text numberLabel;

        public int BeatIndex => beatIndex;
        public int RemainingHits => Mathf.Max(0, remainingHits);
        public BossNoteTier DisplayTier => displayTier;

        public void SetBeatIndex(int beat) => beatIndex = Mathf.Max(0, beat);

        public void SetRemainingHits(int hits)
        {
            remainingHits = Mathf.Max(0, hits);
            RefreshNumberLabel();
        }

        public void RefreshNumberLabel()
        {
            if (numberLabel == null)
            {
                var child = transform.Find("NoteNum");
                if (child != null)
                {
                    numberLabel = child.GetComponent<Text>();
                }
            }

            if (numberLabel == null)
            {
                return;
            }

            numberLabel.text = RemainingHits <= 0
                ? string.Empty
                : Mathf.Clamp(RemainingHits, 0, 9).ToString();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RefreshNumberLabel();
        }
#endif
    }
}
