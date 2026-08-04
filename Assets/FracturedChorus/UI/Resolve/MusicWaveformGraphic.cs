using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class MusicWaveformGraphic : MaskableGraphic
    {
        [SerializeField] private Color waveColor = new Color(0.15f, 1f, 0.95f, 1f);
        [SerializeField] private Color glowColor = new Color(1f, 0.18f, 0.82f, 0.55f);
        [SerializeField] private Color magentaColor = new Color(1f, 0.2f, 0.85f, 1f);
        [SerializeField] private Color cyanColor = new Color(0.2f, 0.95f, 1f, 1f);
        [SerializeField] [Range(16, 256)] private int sampleCount = 96;
        [SerializeField] private float amplitude = 78f;
        [SerializeField] private float glowAmplitude = 96f;
        [SerializeField] private float lineThickness = 3.2f;
        [SerializeField] private float glowThickness = 8f;
        [SerializeField] private bool flipVertical;
        [SerializeField] [Range(0f, 0.9f)] private float smoothing = 0.04f;
        [SerializeField] private float spectrumGain = 48f;
        [SerializeField] private float punchExponent = 0.42f;
        [SerializeField] private bool drawBars = true;
        [SerializeField] private bool pixelBlocks = true;
        [SerializeField] private float pixelSize = 4.2f;
        [SerializeField] private float pixelGap = 1.1f;
        [SerializeField] private bool drawSineRibbons = true;
        [SerializeField] private bool drawBaseline = true;
        [SerializeField] private bool drawPeakSparks = true;
        [SerializeField] private float barWidthRatio = 0.72f;
        [SerializeField] private bool mirrorSpectrum = true;
        [SerializeField] private bool spectrumHue = true;
        [SerializeField] [Range(0f, 1f)] private float hueStart = 0.48f;
        [SerializeField] [Range(0f, 1f)] private float hueSpan = 0.42f;
        [SerializeField] [Range(0.5f, 1f)] private float barAlphaFloor = 0.9f;

        private float[] _display;
        private float[] _scratch;
        private float _phase;

        public void Configure(
            Color wave,
            Color glow,
            float amp,
            float glowAmp,
            bool flip,
            float gain = 48f,
            float smooth = 0.04f,
            bool bars = true,
            bool hueSpectrum = true)
        {
            waveColor = wave;
            glowColor = glow;
            cyanColor = wave;
            magentaColor = new Color(glow.r, glow.g, glow.b, 1f);
            amplitude = amp;
            glowAmplitude = glowAmp;
            flipVertical = flip;
            spectrumGain = gain;
            smoothing = smooth;
            drawBars = bars;
            spectrumHue = hueSpectrum;
            pixelBlocks = true;
            drawSineRibbons = true;
            drawBaseline = true;
            drawPeakSparks = true;
            SetVerticesDirty();
        }

        public void ConfigureHologramStyle(
            Color cyan,
            Color magenta,
            float amp,
            float glowAmp,
            bool flip,
            float gain,
            float smooth,
            int bars = 96)
        {
            cyanColor = cyan;
            magentaColor = magenta;
            waveColor = cyan;
            glowColor = new Color(magenta.r, magenta.g, magenta.b, 0.55f);
            amplitude = amp;
            glowAmplitude = glowAmp;
            flipVertical = flip;
            spectrumGain = gain;
            smoothing = smooth;
            sampleCount = Mathf.Clamp(bars, 16, 256);
            drawBars = true;
            pixelBlocks = true;
            drawSineRibbons = true;
            drawBaseline = true;
            drawPeakSparks = true;
            spectrumHue = false;
            EnsureBuffers(sampleCount);
            SetVerticesDirty();
        }

        public void SetSamples(float[] samples)
        {
            if (samples == null || samples.Length == 0)
            {
                return;
            }

            _phase += Time.unscaledDeltaTime * 3.2f;
            EnsureBuffers(sampleCount);
            var half = mirrorSpectrum ? Mathf.Max(2, sampleCount / 2) : sampleCount;
            var step = Mathf.Max(1, samples.Length / half);
            for (var i = 0; i < half; i++)
            {
                var idx = Mathf.Min(i * step, samples.Length - 1);
                var neighbor = Mathf.Min(idx + 1, samples.Length - 1);
                var raw = samples[idx] * 0.65f + samples[neighbor] * 0.35f;
                var mid = samples[Mathf.Min(samples.Length / 4, samples.Length - 1)];
                raw = Mathf.Max(raw, mid * 0.35f);
                var boosted = Mathf.Pow(Mathf.Clamp01(raw * spectrumGain), punchExponent);
                var jitter = 0.86f + 0.28f * Mathf.Abs(Mathf.Sin(i * 1.7f + _phase * 2.8f));
                _scratch[i] = Mathf.Clamp01(boosted * jitter);
            }

            if (mirrorSpectrum)
            {
                for (var i = 0; i < half; i++)
                {
                    var mirrorIndex = sampleCount - 1 - i;
                    if (mirrorIndex >= half)
                    {
                        _scratch[mirrorIndex] = _scratch[i];
                    }
                }

                if ((sampleCount & 1) == 1)
                {
                    _scratch[half] = _scratch[Mathf.Max(0, half - 1)];
                }
            }

            var lerp = 1f - Mathf.Clamp01(smoothing);
            for (var i = 0; i < sampleCount; i++)
            {
                var target = _scratch[i];
                if (target > _display[i])
                {
                    _display[i] = Mathf.Lerp(_display[i], target, Mathf.Max(lerp, 0.82f));
                }
                else
                {
                    _display[i] = Mathf.Lerp(_display[i], target, lerp * 0.62f);
                }
            }

            SetVerticesDirty();
        }

        public float GetAverageLevel()
        {
            EnsureBuffers(sampleCount);
            if (_display == null || _display.Length == 0)
            {
                return 0f;
            }

            var sum = 0f;
            for (var i = 0; i < _display.Length; i++)
            {
                sum += _display[i];
            }

            return sum / _display.Length;
        }

        public void ClearWave()
        {
            if (this == null)
            {
                return;
            }

            EnsureBuffers(sampleCount);
            for (var i = 0; i < _display.Length; i++)
            {
                _display[i] = 0f;
            }

            if (canvasRenderer != null)
            {
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            EnsureBuffers(sampleCount);
            var rect = rectTransform.rect;
            if (rect.width < 2f || rect.height < 2f)
            {
                return;
            }

            if (drawBars)
            {
                if (pixelBlocks)
                {
                    DrawPixelBars(vh, rect, glow: true);
                    DrawPixelBars(vh, rect, glow: false);
                }
                else
                {
                    DrawBars(vh, rect, glowColor, glowAmplitude, glowThickness * 0.85f, useHue: false, softAlpha: true);
                    DrawBars(vh, rect, waveColor, amplitude, 1f, useHue: spectrumHue, softAlpha: false);
                    DrawBars(vh, rect, Color.white, amplitude * 0.55f, 0.42f, useHue: false, softAlpha: false);
                }
            }
            else
            {
                DrawRibbon(vh, rect, glowColor, glowAmplitude, glowThickness);
                DrawRibbon(vh, rect, waveColor, amplitude, lineThickness);
            }

            if (drawBaseline)
            {
                DrawBaseline(vh, rect);
            }

            if (drawSineRibbons)
            {
                DrawSineRibbon(vh, rect, cyanColor, 0.55f, 2.4f, 1.7f, 0f);
                DrawSineRibbon(vh, rect, magentaColor, 0.48f, 2.1f, 2.3f, 1.4f);
            }

            if (drawPeakSparks)
            {
                DrawPeakSparks(vh, rect);
            }
        }

        private void DrawPixelBars(VertexHelper vh, Rect rect, bool glow)
        {
            var count = sampleCount;
            if (count < 2)
            {
                return;
            }

            var baseline = flipVertical ? rect.yMax : rect.yMin;
            var dir = flipVertical ? -1f : 1f;
            var slot = rect.width / count;
            var barW = Mathf.Max(2f, slot * barWidthRatio * (glow ? 1.25f : 1f));
            var block = Mathf.Max(2.5f, pixelSize * (glow ? 1.15f : 1f));
            var gap = Mathf.Max(0.4f, pixelGap);
            var amp = glow ? glowAmplitude : amplitude;

            for (var i = 0; i < count; i++)
            {
                var level = _display[i];
                if (level < 0.02f)
                {
                    continue;
                }

                var h = Mathf.Max(block, level * amp);
                var blocks = Mathf.Max(1, Mathf.FloorToInt(h / (block + gap)));
                var x = rect.xMin + (i + 0.5f) * slot;
                var x0 = x - barW * 0.5f;
                var x1 = x + barW * 0.5f;
                var color = ResolveHologramColor(i, count, level, glow);

                for (var b = 0; b < blocks; b++)
                {
                    var y0 = baseline + dir * b * (block + gap);
                    var y1 = y0 + dir * block;
                    var fade = 1f - b / (float)Mathf.Max(1, blocks) * 0.35f;
                    var c = color;
                    c.a *= fade * (glow ? 0.35f + level * 0.35f : Mathf.Lerp(barAlphaFloor, 1f, level));
                    AddQuad(vh, x0, Mathf.Min(y0, y1), x1, Mathf.Max(y0, y1), c);
                }
            }
        }

        private Color ResolveHologramColor(int index, int count, float level, bool glow)
        {
            var alternate = ((index / 2) & 1) == 0;
            var baseCol = alternate ? cyanColor : magentaColor;
            if (glow)
            {
                baseCol = Color.Lerp(baseCol, Color.white, 0.15f);
                baseCol.a = glowColor.a;
            }

            if (spectrumHue)
            {
                var t = count <= 1 ? 0f : index / (float)(count - 1);
                var hue = Mathf.Repeat(hueStart + t * hueSpan, 1f);
                var c = Color.HSVToRGB(hue, Mathf.Lerp(0.78f, 1f, level), Mathf.Lerp(0.92f, 1f, level));
                c.a = baseCol.a;
                return c;
            }

            var hot = Color.Lerp(baseCol, Color.white, level * 0.45f);
            hot.a = baseCol.a;
            return hot;
        }

        private void DrawBaseline(VertexHelper vh, Rect rect)
        {
            var baseline = flipVertical ? rect.yMax : rect.yMin;
            var half = 1.6f;
            var y0 = baseline - half;
            var y1 = baseline + half;
            var core = new Color(0.85f, 1f, 1f, 0.95f);
            var glow = new Color(magentaColor.r, cyanColor.g, 1f, 0.35f);
            AddQuad(vh, rect.xMin, y0 - 2.2f, rect.xMax, y1 + 2.2f, glow);
            AddQuad(vh, rect.xMin, y0, rect.xMax, y1, core);
        }

        private void DrawSineRibbon(
            VertexHelper vh,
            Rect rect,
            Color color,
            float ampScale,
            float thickness,
            float freq,
            float phaseOffset)
        {
            var segments = Mathf.Clamp(sampleCount, 32, 128);
            var baseline = flipVertical ? rect.yMax : rect.yMin;
            var dir = flipVertical ? -1f : 1f;
            var amp = amplitude * ampScale;
            var half = thickness * 0.5f;
            var startIndex = vh.currentVertCount;
            var avg = GetAverageLevel();

            for (var i = 0; i < segments; i++)
            {
                var t = i / (float)(segments - 1);
                var x = Mathf.Lerp(rect.xMin, rect.xMax, t);
                var sample = _display[Mathf.Clamp(Mathf.RoundToInt(t * (sampleCount - 1)), 0, sampleCount - 1)];
                var wave = Mathf.Sin((t * freq + _phase + phaseOffset) * Mathf.PI * 2f);
                var y = baseline + dir * (wave * amp * (0.35f + sample * 0.65f) * (0.55f + avg));
                var c = color;
                c.a *= 0.55f + sample * 0.4f;
                vh.AddVert(new Vector3(x, y - half, 0f), c, Vector2.zero);
                vh.AddVert(new Vector3(x, y + half, 0f), c, Vector2.zero);
            }

            for (var i = 0; i < segments - 1; i++)
            {
                var i0 = startIndex + i * 2;
                vh.AddTriangle(i0, i0 + 1, i0 + 3);
                vh.AddTriangle(i0, i0 + 3, i0 + 2);
            }
        }

        private void DrawPeakSparks(VertexHelper vh, Rect rect)
        {
            var baseline = flipVertical ? rect.yMax : rect.yMin;
            var dir = flipVertical ? -1f : 1f;
            var slot = rect.width / sampleCount;
            var size = Mathf.Max(1.8f, pixelSize * 0.55f);

            for (var i = 0; i < sampleCount; i++)
            {
                if (_display[i] < 0.62f || ((i + Mathf.FloorToInt(_phase * 7f)) % 5) != 0)
                {
                    continue;
                }

                var x = rect.xMin + (i + 0.5f) * slot;
                var y = baseline + dir * (_display[i] * amplitude + size * 1.5f);
                var c = ((i & 1) == 0) ? cyanColor : magentaColor;
                c.a = 0.75f;
                AddQuad(vh, x - size * 0.5f, y - size * 0.5f, x + size * 0.5f, y + size * 0.5f, c);
            }
        }

        private void DrawBars(
            VertexHelper vh,
            Rect rect,
            Color color,
            float amp,
            float widthScale,
            bool useHue,
            bool softAlpha)
        {
            var count = sampleCount;
            if (count < 2)
            {
                return;
            }

            var baseline = flipVertical ? rect.yMax : rect.yMin;
            var dir = flipVertical ? -1f : 1f;
            var slot = rect.width / count;
            var barW = Mathf.Max(1.4f, slot * barWidthRatio * widthScale);

            for (var i = 0; i < count; i++)
            {
                var x = rect.xMin + (i + 0.5f) * slot;
                var h = Mathf.Max(2f, _display[i] * amp);
                var y0 = baseline;
                var y1 = baseline + dir * h;
                var x0 = x - barW * 0.5f;
                var x1 = x + barW * 0.5f;
                var a = useHue ? ResolveBarColor(color, i, count) : color;
                if (softAlpha)
                {
                    a.a *= 0.35f + _display[i] * 0.4f;
                }
                else
                {
                    a.a *= Mathf.Lerp(barAlphaFloor, 1f, _display[i]);
                }

                AddQuad(vh, x0, Mathf.Min(y0, y1), x1, Mathf.Max(y0, y1), a);
            }
        }

        private Color ResolveBarColor(Color color, int index, int count)
        {
            var t = count <= 1 ? 0f : index / (float)(count - 1);
            var hue = Mathf.Repeat(hueStart + t * hueSpan, 1f);
            var sat = Mathf.Lerp(0.78f, 1f, _display[index]);
            var val = Mathf.Lerp(0.92f, 1f, _display[index]);
            var c = Color.HSVToRGB(hue, sat, val);
            c.a = color.a;
            return c;
        }

        private void DrawRibbon(
            VertexHelper vh,
            Rect rect,
            Color color,
            float amp,
            float thickness)
        {
            var count = sampleCount;
            if (count < 2)
            {
                return;
            }

            var baseline = flipVertical ? rect.yMax : rect.yMin;
            var dir = flipVertical ? -1f : 1f;
            var half = thickness * 0.5f;
            var startIndex = vh.currentVertCount;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)(count - 1);
                var x = Mathf.Lerp(rect.xMin, rect.xMax, t);
                var y = baseline + dir * _display[i] * amp;
                var c = spectrumHue ? ResolveBarColor(color, i, count) : color;
                vh.AddVert(new Vector3(x, y - half, 0f), c, Vector2.zero);
                vh.AddVert(new Vector3(x, y + half, 0f), c, Vector2.zero);
            }

            for (var i = 0; i < count - 1; i++)
            {
                var i0 = startIndex + i * 2;
                vh.AddTriangle(i0, i0 + 1, i0 + 3);
                vh.AddTriangle(i0, i0 + 3, i0 + 2);
            }
        }

        private static void AddQuad(VertexHelper vh, float x0, float y0, float x1, float y1, Color color)
        {
            var i = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(x1, y1, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(x1, y0, 0f), color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        private void EnsureBuffers(int count)
        {
            count = Mathf.Clamp(count, 16, 256);
            if (_display != null && _display.Length == count)
            {
                return;
            }

            _display = new float[count];
            _scratch = new float[count];
            sampleCount = count;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            EnsureBuffers(sampleCount);
            SetVerticesDirty();
        }
#endif
    }
}
