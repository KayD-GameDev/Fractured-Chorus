using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.VFX
{
    public sealed class ButterflyWaveformTrail : MaskableGraphic
    {
        [SerializeField] private float width = 38f;
        [SerializeField] private Sprite waveformSprite;
        [SerializeField] private bool persist = true;

        private readonly List<Vector2> _points = new();
        private readonly List<float> _ages = new();
        private float _fade = 1f;
        private float _length;

        public int PointCount => _points.Count;
        public bool Persist => persist;

        public void BindVisual(Sprite sprite, float trailWidth)
        {
            if (sprite != null)
            {
                waveformSprite = sprite;
            }

            if (trailWidth > 0.01f)
            {
                width = trailWidth;
            }

            raycastTarget = false;
            SetMaterialDirty();
            SetVerticesDirty();
        }

        public override Texture mainTexture
        {
            get
            {
                if (waveformSprite != null && waveformSprite.texture != null)
                {
                    return waveformSprite.texture;
                }

                return s_WhiteTexture;
            }
        }

        public void ClearPoints()
        {
            _points.Clear();
            _ages.Clear();
            _fade = 1f;
            _length = 0f;
            SetVerticesDirty();
        }

        public void AddPoint(Vector2 local, int maxPoints)
        {
            if (_points.Count > 0)
            {
                var last = _points[_points.Count - 1];
                var delta = local - last;
                var dist = delta.magnitude;
                if (dist < 2f)
                {
                    return;
                }

                var steps = Mathf.Max(1, Mathf.CeilToInt(dist / 28f));
                var cap = persist ? Mathf.Max(maxPoints, 256) : maxPoints;
                for (var s = 1; s <= steps; s++)
                {
                    Append(last + delta * (s / (float)steps), cap);
                }

                return;
            }

            Append(local, persist ? Mathf.Max(maxPoints, 256) : maxPoints);
        }

        public void Tick(float deltaTime, float fade)
        {
            _fade = fade;
            if (!persist)
            {
                for (var i = 0; i < _ages.Count; i++)
                {
                    _ages[i] += deltaTime;
                }
            }

            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetMaterialDirty();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < 2 || _fade <= 0.001f)
            {
                return;
            }

            var total = 0f;
            for (var i = 0; i < _points.Count - 1; i++)
            {
                total += (_points[i + 1] - _points[i]).magnitude;
            }

            if (total < 1f)
            {
                return;
            }

            var half = Mathf.Max(6f, width) * 0.5f;
            var vertIndex = 0;
            var travelled = 0f;
            for (var i = 0; i < _points.Count - 1; i++)
            {
                var a = _points[i];
                var b = _points[i + 1];
                var dir = b - a;
                var segLen = dir.magnitude;
                if (segLen < 0.5f)
                {
                    continue;
                }

                dir /= segLen;
                var normal = new Vector2(-dir.y, dir.x);
                var t0 = travelled / total;
                travelled += segLen;
                var t1 = travelled / total;
                var envA = Mathf.SmoothStep(0.28f, 1f, t0);
                var envB = Mathf.SmoothStep(0.28f, 1f, t1);
                var colorA = color;
                var colorB = color;
                colorA.a *= envA * _fade;
                colorB.a *= envB * _fade;
                if (!persist)
                {
                    colorA.a *= 1f - Mathf.Clamp01(_ages[i] / 2.4f);
                    colorB.a *= 1f - Mathf.Clamp01(_ages[i + 1] / 2.4f);
                }

                AddVert(vh, a - normal * half * envA, colorA, new Vector2(t0, 0f));
                AddVert(vh, a + normal * half * envA, colorA, new Vector2(t0, 1f));
                AddVert(vh, b + normal * half * envB, colorB, new Vector2(t1, 1f));
                AddVert(vh, b - normal * half * envB, colorB, new Vector2(t1, 0f));
                vh.AddTriangle(vertIndex, vertIndex + 1, vertIndex + 2);
                vh.AddTriangle(vertIndex, vertIndex + 2, vertIndex + 3);
                vertIndex += 4;
            }
        }

        private void Append(Vector2 local, int maxPoints)
        {
            if (_points.Count > 0)
            {
                _length += (local - _points[_points.Count - 1]).magnitude;
            }

            _points.Add(local);
            _ages.Add(0f);
            while (_points.Count > maxPoints)
            {
                _points.RemoveAt(0);
                _ages.RemoveAt(0);
            }

            SetVerticesDirty();
        }

        private static void AddVert(VertexHelper vh, Vector2 position, Color32 vertColor, Vector2 uv)
        {
            vh.AddVert(new Vector3(position.x, position.y, 0f), vertColor, uv);
        }
    }
}
