#if UNITY_EDITOR
using FracturedChorus.VFX;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(ButterflyBezierFlight))]
    public sealed class ButterflyBezierFlightEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var flight = (ButterflyBezierFlight)target;
            var canvas = flight.CanvasRect != null ? flight.CanvasRect : flight.transform as RectTransform;
            if (canvas == null)
            {
                return;
            }

            var controller = flight.GetComponent<ButterflyTransitionController>();
            var points = controller != null
                ? new[] { controller.StartPoint, controller.ControlPointA, controller.ControlPointB, controller.EndPoint }
                : new[] { flight.StartPoint, flight.ControlPointA, flight.ControlPointB, flight.EndPoint };
            Handles.color = new Color(0f, 0.83f, 1f, 0.95f);
            for (var i = 0; i < points.Length; i++)
            {
                var world = canvas.TransformPoint(flight.ToLocal(points[i]));
                var size = HandleUtility.GetHandleSize(world) * 0.08f;
                EditorGUI.BeginChangeCheck();
                var next = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(flight, "Move butterfly bezier point");
                    var local = canvas.InverseTransformPoint(next);
                    var normalized = flight.ToNormalized(new Vector2(local.x, local.y));
                    flight.EditorSetPoint(i, normalized);
                    if (controller != null)
                    {
                        Undo.RecordObject(controller, "Move butterfly bezier point");
                        var nextPoints = new[]
                        {
                            controller.StartPoint,
                            controller.ControlPointA,
                            controller.ControlPointB,
                            controller.EndPoint
                        };
                        nextPoints[i] = normalized;
                        controller.SetFlightPoints(nextPoints[0], nextPoints[1], nextPoints[2], nextPoints[3]);
                        EditorUtility.SetDirty(controller);
                    }

                    EditorUtility.SetDirty(flight);
                }
            }

            Handles.color = new Color(0.55f, 0.85f, 1f, 0.7f);
            var prev = canvas.TransformPoint(flight.ToLocal(points[0]));
            for (var i = 1; i <= 24; i++)
            {
                var t = i / 24f;
                var p = canvas.TransformPoint(flight.EvaluateLocal(t));
                Handles.DrawLine(prev, p);
                prev = p;
            }
        }
    }

    [CustomEditor(typeof(ButterflyTransitionController))]
    public sealed class ButterflyTransitionControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var controller = (ButterflyTransitionController)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Create missing children"))
            {
                ButterflyTransitionHierarchy.EnsureMissing(controller);
                EditorUtility.SetDirty(controller);
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play"))
                {
                    controller.Play();
                }

                if (GUILayout.Button("Stop"))
                {
                    controller.Stop();
                }

                if (GUILayout.Button("Reset"))
                {
                    controller.ResetTransition();
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif
