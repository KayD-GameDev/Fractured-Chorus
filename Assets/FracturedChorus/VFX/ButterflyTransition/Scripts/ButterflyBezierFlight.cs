using UnityEngine;

namespace FracturedChorus.VFX
{
    public sealed class ButterflyBezierFlight : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private RectTransform target;
        [SerializeField] private Vector2 startPoint = DefaultStartPoint;
        [SerializeField] private Vector2 controlPointA = DefaultControlA;
        [SerializeField] private Vector2 controlPointB = DefaultControlB;
        [SerializeField] private Vector2 endPoint = DefaultEndPoint;
        [SerializeField] private AnimationCurve speedCurve = DefaultSpeedCurve();
        [SerializeField] private float rotationLimit = 20f;
        [SerializeField] private float oscillationAmplitude = 18f;
        [SerializeField] private float oscillationFrequency = 1.15f;

        private void OnEnable()
        {
            if (canvasRect == null)
            {
                canvasRect = transform as RectTransform;
            }
        }
        public static Vector2 DefaultStartPoint => new Vector2(0.08f, 0.10f);
        public static Vector2 DefaultControlA => new Vector2(0.28f, 0.22f);
        public static Vector2 DefaultControlB => new Vector2(0.55f, 0.52f);
        public static Vector2 DefaultEndPoint => new Vector2(0.88f, 0.86f);

        public Vector2 StartPoint => startPoint;
        public Vector2 ControlPointA => controlPointA;
        public Vector2 ControlPointB => controlPointB;
        public Vector2 EndPoint => endPoint;
        public float RotationLimit => rotationLimit;
        public RectTransform CanvasRect => canvasRect;
        public RectTransform Target => target;

        public void Bind(RectTransform boundCanvas, RectTransform boundTarget)
        {
            if (boundCanvas != null)
            {
                canvasRect = boundCanvas;
            }

            if (boundTarget != null)
            {
                target = boundTarget;
            }
        }

        public void BindTuning(AnimationCurve curve, float rotLimit)
        {
            if (curve != null && curve.length > 0)
            {
                speedCurve = curve;
            }

            rotationLimit = Mathf.Clamp(rotLimit, 0f, 45f);
        }

        public void SetNormalizedPoints(Vector2 start, Vector2 controlA, Vector2 controlB, Vector2 end)
        {
            startPoint = start;
            controlPointA = controlA;
            controlPointB = controlB;
            endPoint = end;
        }

        public void Apply(float normalizedTime, float elapsed, float oscillationScale = 1f)
        {
            if (target == null || canvasRect == null)
            {
                return;
            }

            var t = EvaluateSpeed(Mathf.Clamp01(normalizedTime));
            var local = EvaluateLocal(t);
            var tangent = EvaluateTangent(t);
            var perpendicular = new Vector2(-tangent.y, tangent.x);
            if (perpendicular.sqrMagnitude > 0.0001f)
            {
                perpendicular.Normalize();
            }

            var wave = Mathf.Sin(elapsed * oscillationFrequency * Mathf.PI * 2f)
                       * oscillationAmplitude
                       * Mathf.Max(0f, oscillationScale);
            target.anchoredPosition = local + perpendicular * wave;
            var angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            target.localRotation = Quaternion.Euler(0f, 0f, ClampVisualRotation(angle, rotationLimit));
        }

        public Vector2 EvaluateLocal(float t)
        {
            return ToLocal(EvaluateNormalized(t));
        }

        public Vector2 EvaluateNormalized(float t)
        {
            return EvaluateCubic(startPoint, controlPointA, controlPointB, endPoint, t);
        }

        public Vector2 EvaluateTangent(float t)
        {
            var tangent = EvaluateCubicDerivative(startPoint, controlPointA, controlPointB, endPoint, t);
            var local = ToLocal(startPoint + tangent) - ToLocal(startPoint);
            if (local.sqrMagnitude < 0.0001f)
            {
                return Vector2.right;
            }

            return local.normalized;
        }

        public float EvaluateSpeed(float normalizedTime)
        {
            if (speedCurve == null || speedCurve.length == 0)
            {
                return normalizedTime;
            }

            return Mathf.Clamp01(speedCurve.Evaluate(normalizedTime));
        }

        public Vector2 ToLocal(Vector2 normalized)
        {
            var rect = PixelRect();
            var anchorMin = target != null ? target.anchorMin : new Vector2(0.5f, 0.5f);
            var anchorMax = target != null ? target.anchorMax : new Vector2(0.5f, 0.5f);
            return NormalizedToAnchored(normalized, rect, anchorMin, anchorMax);
        }

        public Vector2 ToNormalized(Vector2 local)
        {
            var rect = PixelRect();
            var anchorMin = target != null ? target.anchorMin : new Vector2(0.5f, 0.5f);
            var anchorMax = target != null ? target.anchorMax : new Vector2(0.5f, 0.5f);
            return AnchoredToNormalized(local, rect, anchorMin, anchorMax);
        }

        public Rect PixelRect()
        {
            var rect = canvasRect != null ? canvasRect.rect : FallbackCanvasRect;
            if (rect.width < 1f || rect.height < 1f)
            {
                return FallbackCanvasRect;
            }

            return rect;
        }

        public static Rect FallbackCanvasRect => new Rect(-960f, -540f, 1920f, 1080f);

        public static Vector2 NormalizedToAnchored(Vector2 normalized, Rect rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            var local = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));
            var anchor = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, (anchorMin.x + anchorMax.x) * 0.5f),
                Mathf.Lerp(rect.yMin, rect.yMax, (anchorMin.y + anchorMax.y) * 0.5f));
            return local - anchor;
        }

        public static Vector2 AnchoredToNormalized(Vector2 anchored, Rect rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            var anchor = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, (anchorMin.x + anchorMax.x) * 0.5f),
                Mathf.Lerp(rect.yMin, rect.yMax, (anchorMin.y + anchorMax.y) * 0.5f));
            var local = anchored + anchor;
            var x = rect.width > 0.001f ? (local.x - rect.xMin) / rect.width : 0.5f;
            var y = rect.height > 0.001f ? (local.y - rect.yMin) / rect.height : 0.5f;
            return new Vector2(x, y);
        }

        public static Vector2 EvaluateCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            t = Mathf.Clamp01(t);
            var u = 1f - t;
            return (u * u * u * p0)
                   + (3f * u * u * t * p1)
                   + (3f * u * t * t * p2)
                   + (t * t * t * p3);
        }

        public static Vector2 EvaluateCubicDerivative(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            t = Mathf.Clamp01(t);
            var u = 1f - t;
            return (3f * u * u * (p1 - p0))
                   + (6f * u * t * (p2 - p1))
                   + (3f * t * t * (p3 - p2));
        }

        public static float ClampVisualRotation(float angle, float limit)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }
            else if (angle < -180f)
            {
                angle += 360f;
            }

            var facing = angle;
            if (facing > 90f)
            {
                facing -= 180f;
            }
            else if (facing < -90f)
            {
                facing += 180f;
            }

            return Mathf.Clamp(facing, -limit, limit);
        }

        public static AnimationCurve DefaultSpeedCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0.85f),
                new Keyframe(0.18f, 0.16f, 0.7f, 0.7f),
                new Keyframe(0.55f, 0.58f, 1f, 1f),
                new Keyframe(1f, 1f, 0.55f, 0f));
        }

#if UNITY_EDITOR
        public void EditorSetPoint(int index, Vector2 normalized)
        {
            switch (index)
            {
                case 0:
                    startPoint = normalized;
                    break;
                case 1:
                    controlPointA = normalized;
                    break;
                case 2:
                    controlPointB = normalized;
                    break;
                default:
                    endPoint = normalized;
                    break;
            }
        }
#endif
    }
}
