using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OffBeatWaveformView : MaskableGraphic
    {
        private const int SampleSize = 512;
        private const int PointCount = 96;

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Color waveA = new Color(0f, 0.75f, 1f, 0.95f);
        [SerializeField] private Color waveB = new Color(0.15f, 0.45f, 0.95f, 0.75f);
        [SerializeField] private Color centerLine = new Color(0f, 0.7f, 1f, 0.12f);
        [SerializeField] private Color panelColor = new Color(0.97f, 0.94f, 0.98f, 0f);
        [SerializeField] [Range(1f, 8f)] private float lineThickness = 2.4f;
        [SerializeField] [Range(0.5f, 12f)] private float sensitivity = 4.5f;
        [SerializeField] [Range(0.1f, 1f)] private float amplitude = 0.55f;
        [SerializeField] [Range(1f, 30f)] private float smoothSpeed = 14f;
        [SerializeField] [Range(0.2f, 3f)] private float idleSpeed = 1.1f;
        [SerializeField] private bool fillPanel = false;

        private readonly float[] _raw = new float[SampleSize];
        private readonly float[] _waveA = new float[PointCount];
        private readonly float[] _waveB = new float[PointCount];
        private readonly List<UIVertex> _verts = new List<UIVertex>(1024);
        private readonly List<int> _indices = new List<int>(2048);
        private bool _bound;

        public override Texture mainTexture => s_WhiteTexture;

        public void Bind(AudioSource source)
        {
            audioSource = source;
            _bound = true;
            HideLegacyBars();
            color = Color.white;
            SetAllDirty();
        }

        public void CollectBarsFromChildren()
        {
            HideLegacyBars();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            HideLegacyBars();
            if (color.a < 0.01f)
            {
                color = Color.white;
            }
        }

        private void LateUpdate()
        {
            if (!_bound && audioSource == null)
            {
                return;
            }

            var playing = audioSource != null && audioSource.isPlaying && audioSource.clip != null;
            if (playing)
            {
                audioSource.GetOutputData(_raw, 0);
                SampleToWaves(_raw, live: true);
            }
            else
            {
                SampleIdleWaves();
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            if (rect.width < 2f || rect.height < 2f)
            {
                return;
            }

            _verts.Clear();
            _indices.Clear();

            if (fillPanel)
            {
                AddQuad(
                    new Vector2(rect.xMin, rect.yMin),
                    new Vector2(rect.xMax, rect.yMin),
                    new Vector2(rect.xMax, rect.yMax),
                    new Vector2(rect.xMin, rect.yMax),
                    panelColor);
            }

            var midY = rect.center.y;
            AddLine(
                new Vector2(rect.xMin, midY),
                new Vector2(rect.xMax, midY),
                Mathf.Max(1f, lineThickness * 0.35f),
                centerLine);

            AppendWave(rect, _waveA, waveA, lineThickness);
            AppendWave(rect, _waveB, waveB, lineThickness * 0.92f);

            vh.AddUIVertexStream(_verts, _indices);
        }

        private void SampleToWaves(float[] samples, bool live)
        {
            var step = Mathf.Max(1, samples.Length / PointCount);
            var lerp = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
            for (var i = 0; i < PointCount; i++)
            {
                var start = i * step;
                var end = Mathf.Min(samples.Length, start + step);
                var peak = 0f;
                for (var s = start; s < end; s++)
                {
                    var v = Mathf.Abs(samples[s]);
                    if (v > peak)
                    {
                        peak = v;
                    }
                }

                var signed = samples[Mathf.Min(samples.Length - 1, start + (end - start) / 2)];
                var shaped = Mathf.Sign(signed) * Mathf.Pow(Mathf.Clamp01(peak * sensitivity), 0.65f);
                shaped = Mathf.Clamp(shaped * amplitude, -1f, 1f);

                var bIndex = (start + PointCount / 5) % samples.Length;
                var bSigned = samples[bIndex];
                var bPeak = Mathf.Abs(bSigned);
                var shapedB = Mathf.Sign(bSigned) * Mathf.Pow(Mathf.Clamp01(bPeak * sensitivity * 0.92f), 0.7f);
                shapedB = Mathf.Clamp(shapedB * amplitude * 0.88f, -1f, 1f);

                if (live)
                {
                    _waveA[i] = Mathf.Lerp(_waveA[i], shaped, lerp);
                    _waveB[i] = Mathf.Lerp(_waveB[i], shapedB, lerp);
                }
                else
                {
                    _waveA[i] = shaped;
                    _waveB[i] = shapedB;
                }
            }
        }

        private void SampleIdleWaves()
        {
            var t = Time.unscaledTime * idleSpeed;
            var lerp = 1f - Mathf.Exp(-smoothSpeed * 0.5f * Time.unscaledDeltaTime);
            for (var i = 0; i < PointCount; i++)
            {
                var x = i / (float)(PointCount - 1);
                var a = Mathf.Sin(x * 18f + t * 2.2f) * 0.22f
                        + Mathf.Sin(x * 41f - t * 3.1f) * 0.12f
                        + Mathf.Sin(x * 7f + t) * 0.08f;
                var b = Mathf.Sin(x * 15f - t * 1.7f + 1.3f) * 0.2f
                        + Mathf.Sin(x * 33f + t * 2.6f) * 0.14f
                        + Mathf.Sin(x * 9f - t * 0.8f) * 0.07f;
                a = Mathf.Clamp(a * amplitude, -1f, 1f);
                b = Mathf.Clamp(b * amplitude * 0.9f, -1f, 1f);
                _waveA[i] = Mathf.Lerp(_waveA[i], a, lerp);
                _waveB[i] = Mathf.Lerp(_waveB[i], b, lerp);
            }
        }

        private void AppendWave(Rect rect, float[] samples, Color col, float thickness)
        {
            var midY = rect.center.y;
            var halfH = rect.height * 0.5f * 0.9f;
            for (var i = 0; i < samples.Length - 1; i++)
            {
                var t0 = i / (float)(samples.Length - 1);
                var t1 = (i + 1) / (float)(samples.Length - 1);
                var p0 = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, t0), midY + samples[i] * halfH);
                var p1 = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, t1), midY + samples[i + 1] * halfH);
                AddLine(p0, p1, thickness, col);
            }
        }

        private void AddLine(Vector2 a, Vector2 b, float thickness, Color col)
        {
            var dir = b - a;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            dir.Normalize();
            var normal = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);
            AddQuad(a - normal, b - normal, b + normal, a + normal, col);
        }

        private void AddQuad(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, Color col)
        {
            var start = _verts.Count;
            _verts.Add(MakeVert(v0, col));
            _verts.Add(MakeVert(v1, col));
            _verts.Add(MakeVert(v2, col));
            _verts.Add(MakeVert(v3, col));
            _indices.Add(start);
            _indices.Add(start + 1);
            _indices.Add(start + 2);
            _indices.Add(start);
            _indices.Add(start + 2);
            _indices.Add(start + 3);
        }

        private static UIVertex MakeVert(Vector2 pos, Color col)
        {
            var v = UIVertex.simpleVert;
            v.position = pos;
            v.color = col;
            v.uv0 = Vector2.zero;
            return v;
        }

        private void HideLegacyBars()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Bar_"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }
}
