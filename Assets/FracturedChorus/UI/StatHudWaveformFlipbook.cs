using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    [RequireComponent(typeof(Image))]
    public sealed class StatHudWaveformFlipbook : MonoBehaviour
    {
        [SerializeField] private Image target;
        [SerializeField] private Image blendTarget;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 24f;
        [SerializeField] private bool crossfade = true;

        private float _t;
        private int _index = -1;

        public void Bind(Sprite[] boundFrames)
        {
            frames = boundFrames;
            if (target == null)
            {
                target = GetComponent<Image>();
            }

            ApplyDiscrete(0);
        }

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<Image>();
            }

            ApplyDiscrete(0);
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || target == null)
            {
                return;
            }

            _t += Time.unscaledDeltaTime * framesPerSecond;
            var count = frames.Length;
            var f = _t % count;
            if (f < 0f)
            {
                f += count;
            }

            var i0 = Mathf.FloorToInt(f);
            if (!crossfade || blendTarget == null || count < 2)
            {
                ApplyDiscrete(i0);
                return;
            }

            var i1 = i0 + 1;
            if (i1 >= count)
            {
                i1 = 0;
            }

            var frac = f - i0;
            ApplySprite(target, frames[i0], 1f - frac);
            ApplySprite(blendTarget, frames[i1], frac);
            _index = i0;
        }

        private void ApplyDiscrete(int i)
        {
            if (frames == null || frames.Length == 0 || target == null)
            {
                return;
            }

            if (i == _index)
            {
                return;
            }

            _index = i;
            ApplySprite(target, frames[i], 1f);
            if (blendTarget != null)
            {
                blendTarget.enabled = false;
            }
        }

        private static void ApplySprite(Image image, Sprite sprite, float alpha)
        {
            if (image == null)
            {
                return;
            }

            image.enabled = true;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
