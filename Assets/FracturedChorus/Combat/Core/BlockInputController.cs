using FracturedChorus.Audio;
using FracturedChorus.Combat.Block;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Combat.Core
{
    public class BlockInputController : MonoBehaviour
    {
        [SerializeField] private BeatTimelineUIView timelineView;
        [SerializeField] private CombatSfxController sfx;

        private BlockBarrierTracker _barriers;
        private BeatTimelineEngine _timeline;

        public BlockBarrierTracker Barriers => _barriers;

        public void Initialize(
            BeatTimelineUIView timelineUi,
            BlockBarrierTracker barriers,
            BeatTimelineEngine timeline = null,
            CombatSfxController sfxController = null)
        {
            timelineView = timelineUi;
            _barriers = barriers;
            _timeline = timeline;
            if (sfxController != null)
            {
                sfx = sfxController;
            }
        }

        private void Update()
        {
            if (_barriers == null || timelineView == null)
            {
                return;
            }

            if (!ReadSpacePressedThisFrame() || !timelineView.IsPlaybackActive)
            {
                return;
            }

            var beatIndex = timelineView.GetCurrentScanBeatIndex();
            if (_timeline != null && CombatCounterResolver.HasCounterOnBeat(_timeline, beatIndex))
            {
                Debug.Log($"[Block] Space locked — counter owns beat {beatIndex}");
                return;
            }

            if (!_barriers.TryPlaceBarrier(beatIndex, _timeline))
            {
                Debug.Log($"[Block] Space ignored @ beat {beatIndex} (need impact note ±1, free cell)");
                return;
            }

            Debug.Log($"[Block] Barrier placed @ beat {beatIndex}");
            PlayPlaceSfx();
        }

        private void PlayPlaceSfx()
        {
            if (sfx == null)
            {
                sfx = FindAnyObjectByType<CombatSfxController>();
            }

            if (sfx == null)
            {
                Debug.LogWarning("[Block] Perfect SFX — no CombatSfxController");
                return;
            }

            sfx.PlayPerfectBlock(-1d);
        }

        private static bool ReadSpacePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}
