using FracturedChorus.Combat.Timeline;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [System.Serializable]
    public class TimelineNoteVisualCatalog
    {
        private const string ResourceRoot = "UI/Combat/Timeline/";

        public Sprite NoteRed;
        public Sprite NoteBlue;
        public Sprite NotePurple;
        public Sprite DropGhostValid;
        public Sprite DropGhostInvalid;
        public Sprite CoverPerfect;
        public Sprite CoverMiss;
        public Sprite BeatFrameEmpty;
        public Sprite BeatFrameImpact;
        public Sprite BeatFrameWindup;

        public float NoteDisplaySize = 40f;
        public float GhostDisplaySize = 42f;
        public float CoverDisplaySize = 48f;
        [Range(0.35f, 1f)] public float NoteAlpha = 0.78f;
        public float NoteRedSizeScale = 1f;
        public float NoteBlueSizeScale = 1f;
        public float NotePurpleSizeScale = 1f;

        public Sprite NoteForTier(BossNoteTier tier)
        {
            return tier switch
            {
                BossNoteTier.Purple => NotePurple != null ? NotePurple : NoteRed,
                BossNoteTier.Blue => NoteBlue != null ? NoteBlue : NoteRed,
                _ => NoteRed
            };
        }

        public float NoteSizeScaleForTier(BossNoteTier tier)
        {
            return tier switch
            {
                BossNoteTier.Purple => NotePurpleSizeScale > 0f ? NotePurpleSizeScale : 1f,
                BossNoteTier.Blue => NoteBlueSizeScale > 0f ? NoteBlueSizeScale : 1f,
                _ => NoteRedSizeScale > 0f ? NoteRedSizeScale : 1f
            };
        }

        public Sprite DropGhost(bool valid) =>
            valid ? DropGhostValid : DropGhostInvalid;

        public Sprite Cover(bool valid) =>
            valid ? CoverPerfect : CoverMiss;

        public Sprite BeatFrame(bool hasTelegraph, bool isWindup)
        {
            if (!hasTelegraph)
            {
                return BeatFrameEmpty;
            }

            return isWindup
                ? (BeatFrameWindup != null ? BeatFrameWindup : BeatFrameImpact)
                : (BeatFrameImpact != null ? BeatFrameImpact : BeatFrameEmpty);
        }

        public void EnsureDefaultsLoaded()
        {
            if (NoteRed == null)
            {
                NoteRed = Resources.Load<Sprite>(ResourceRoot + "note_tier_red_v1");
            }

            if (NoteBlue == null)
            {
                NoteBlue = Resources.Load<Sprite>(ResourceRoot + "note_tier_blue_v1");
            }

            if (NotePurple == null)
            {
                NotePurple = Resources.Load<Sprite>(ResourceRoot + "note_tier_purple_v1");
            }

            if (DropGhostValid == null)
            {
                DropGhostValid = Resources.Load<Sprite>(ResourceRoot + "drop_ghost_valid_v1");
            }

            if (DropGhostInvalid == null)
            {
                DropGhostInvalid = Resources.Load<Sprite>(ResourceRoot + "drop_ghost_invalid_v1");
            }

            if (CoverPerfect == null)
            {
                CoverPerfect = Resources.Load<Sprite>(ResourceRoot + "cover_perfect_v1");
            }

            if (CoverMiss == null)
            {
                CoverMiss = Resources.Load<Sprite>(ResourceRoot + "cover_miss_v1");
            }

            if (BeatFrameEmpty == null)
            {
                BeatFrameEmpty = Resources.Load<Sprite>(ResourceRoot + "beat_frame_empty_v1");
            }

            if (BeatFrameImpact == null)
            {
                BeatFrameImpact = Resources.Load<Sprite>(ResourceRoot + "beat_frame_impact_v1");
            }

            if (BeatFrameWindup == null)
            {
                BeatFrameWindup = Resources.Load<Sprite>(ResourceRoot + "beat_frame_windup_v1");
            }
        }
    }
}
