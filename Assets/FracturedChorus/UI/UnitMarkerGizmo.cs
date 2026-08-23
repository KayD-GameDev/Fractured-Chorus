using UnityEngine;

namespace FracturedChorus.UI
{
    public enum UnitMarkerShape
    {
        Dot = 0,
        Triangle = 1,
        Square = 2,
        Line = 3,
        Diamond = 4,
        Cross = 5
    }

    /// <summary>
    /// Scene marker on unit children (FeetAnchor, ReceiveDmg, VFX preview). Pick shape in Inspector.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UnitMarkerGizmo : MonoBehaviour
    {
        [SerializeField] private UnitMarkerShape shape = UnitMarkerShape.Dot;
        [SerializeField] private Color color = new(1f, 0.85f, 0.2f, 0.95f);
        [SerializeField] [Min(0.02f)] private float size = 0.18f;
        [SerializeField] private bool alwaysVisible = true;

        public UnitMarkerShape Shape
        {
            get => shape;
            set => shape = value;
        }

        public Color Color
        {
            get => color;
            set => color = value;
        }

        public float Size
        {
            get => size;
            set => size = Mathf.Max(0.02f, value);
        }

        public static UnitMarkerGizmo EnsureOn(
            GameObject host,
            UnitMarkerShape defaultShape,
            Color defaultColor,
            float defaultSize = 0.18f)
        {
            if (host == null)
            {
                return null;
            }

            var marker = host.GetComponent<UnitMarkerGizmo>();
            if (marker != null)
            {
                return marker;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var captured = host;
                UnityEditor.EditorApplication.delayCall += () =>
                    AddIfMissing(captured, defaultShape, defaultColor, defaultSize);
                return null;
            }
#endif
            return AddIfMissing(host, defaultShape, defaultColor, defaultSize);
        }

        private static UnitMarkerGizmo AddIfMissing(
            GameObject host,
            UnitMarkerShape defaultShape,
            Color defaultColor,
            float defaultSize)
        {
            if (host == null)
            {
                return null;
            }

            var marker = host.GetComponent<UnitMarkerGizmo>();
            if (marker != null)
            {
                return marker;
            }

            marker = host.AddComponent<UnitMarkerGizmo>();
            marker.shape = defaultShape;
            marker.color = defaultColor;
            marker.size = defaultSize;
            marker.alwaysVisible = true;
            return marker;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (alwaysVisible)
            {
                Draw();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!alwaysVisible)
            {
                Draw();
            }
        }

        private void Draw()
        {
            DrawAt(transform.position, shape, color, size);
        }

        public static void DrawAt(
            Vector3 center,
            UnitMarkerShape markerShape,
            Color markerColor,
            float markerSize)
        {
            Gizmos.color = markerColor;
            var s = Mathf.Max(0.02f, markerSize);
            switch (markerShape)
            {
                case UnitMarkerShape.Dot:
                    Gizmos.DrawSphere(center, s * 0.45f);
                    break;
                case UnitMarkerShape.Square:
                    Gizmos.DrawWireCube(center, new Vector3(s, s, 0.01f));
                    break;
                case UnitMarkerShape.Line:
                    Gizmos.DrawLine(center + Vector3.left * s, center + Vector3.right * s);
                    Gizmos.DrawLine(center + Vector3.down * s * 0.15f, center + Vector3.up * s * 0.15f);
                    break;
                case UnitMarkerShape.Diamond:
                    DrawLoop(center, s, 4, 45f);
                    break;
                case UnitMarkerShape.Cross:
                    Gizmos.DrawLine(center + new Vector3(-s, -s, 0f), center + new Vector3(s, s, 0f));
                    Gizmos.DrawLine(center + new Vector3(-s, s, 0f), center + new Vector3(s, -s, 0f));
                    break;
                default:
                    DrawLoop(center, s, 3, 90f);
                    break;
            }
        }

        private static void DrawLoop(Vector3 center, float radius, int sides, float startDeg)
        {
            var prev = PointOnCircle(center, radius, startDeg);
            for (var i = 1; i <= sides; i++)
            {
                var next = PointOnCircle(center, radius, startDeg + 360f * i / sides);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        private static Vector3 PointOnCircle(Vector3 center, float radius, float deg)
        {
            var r = deg * Mathf.Deg2Rad;
            return center + new Vector3(Mathf.Cos(r) * radius, Mathf.Sin(r) * radius, 0f);
        }
#endif
    }
}
