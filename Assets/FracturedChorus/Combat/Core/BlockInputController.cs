using FracturedChorus.Combat.Block;
using FracturedChorus.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Combat.Core
{
    /// <summary>
    /// Space (edge) places a block barrier snapped to the current integer beat while timeline scan runs.
    /// </summary>
    public class BlockInputController : MonoBehaviour
    {
        [SerializeField] private BeatTimelineUIView timelineView;

        private BlockBarrierTracker _barriers;

        public BlockBarrierTracker Barriers => _barriers;

        public void Initialize(BeatTimelineUIView timeline, BlockBarrierTracker barriers)
        {
            timelineView = timeline;
            _barriers = barriers;
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
            _barriers.TryPlaceBarrier(beatIndex);
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
