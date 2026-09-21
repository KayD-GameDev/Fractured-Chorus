using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.VFX
{
    public sealed class ButterflyWingAnimator : MonoBehaviour
    {
        [SerializeField] private Image butterflySprite;
        [SerializeField] private Sprite[] wingFrames;
        [SerializeField] private float wingAnimationSpeed = 8f;

        private float _clock;
        private int _step;
        private int[] _cycle;
        private bool _playing;

        public void Bind(Image image, Sprite[] frames)
        {
            if (image != null)
            {
                butterflySprite = image;
            }

            if (frames != null && frames.Length > 0)
            {
                wingFrames = frames;
            }

            BuildCycle();
        }

        public void SetSpeed(float fps)
        {
            wingAnimationSpeed = Mathf.Clamp(fps, 4f, 12f);
        }

        public void Play()
        {
            BuildCycle();
            _clock = 0f;
            _step = 0;
            _playing = true;
            ApplyFrame();
        }

        public void Stop()
        {
            _playing = false;
        }

        public void Tick(float deltaTime, float speedMultiplier)
        {
            if (!_playing || wingFrames == null || wingFrames.Length == 0)
            {
                return;
            }

            var fps = Mathf.Max(0.1f, wingAnimationSpeed) * Mathf.Max(0.15f, speedMultiplier);
            _clock += deltaTime * fps;
            while (_clock >= 1f)
            {
                _clock -= 1f;
                _step = (_step + 1) % _cycle.Length;
                ApplyFrame();
            }
        }

        private void BuildCycle()
        {
            if (wingFrames == null || wingFrames.Length == 0)
            {
                _cycle = new[] { 0 };
                return;
            }

            if (wingFrames.Length == 1)
            {
                _cycle = new[] { 0 };
                return;
            }

            var count = wingFrames.Length * 2 - 2;
            _cycle = new int[count];
            var index = 0;
            for (var i = 0; i < wingFrames.Length; i++)
            {
                _cycle[index++] = i;
            }

            for (var i = wingFrames.Length - 2; i >= 1; i--)
            {
                _cycle[index++] = i;
            }
        }

        private void ApplyFrame()
        {
            if (butterflySprite == null || wingFrames == null || wingFrames.Length == 0)
            {
                return;
            }

            var frameIndex = _cycle[_step % _cycle.Length];
            var sprite = wingFrames[Mathf.Clamp(frameIndex, 0, wingFrames.Length - 1)];
            if (sprite != null)
            {
                butterflySprite.sprite = sprite;
            }
        }
    }
}
