#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(CombatExecuteOverlayUIView))]
    public sealed class CombatExecuteOverlayUIViewEditor : UnityEditor.Editor
    {
        private bool _previewHover;
        private bool _sceneHover;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var view = (CombatExecuteOverlayUIView)target;
            view.WireReferences();
            var feedback = ResolveFeedback(view);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Hover Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play: hover bằng pointer trên nút scene.\n" +
                "Edit: bật Preview Hover, hoặc rê chuột lên nút trong Scene view (khi object này đang chọn).",
                MessageType.Info);

            var nextPreview = EditorGUILayout.Toggle("Preview Hover", _previewHover);
            if (nextPreview != _previewHover)
            {
                _previewHover = nextPreview;
                feedback?.SetHovered(_previewHover);
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGUI()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var view = (CombatExecuteOverlayUIView)target;
            view.WireReferences();
            var rect = view.ButtonRect;
            var feedback = ResolveFeedback(view);
            if (rect == null || feedback == null)
            {
                return;
            }

            var over = IsMouseOver(rect);
            if (over == _sceneHover && !_previewHover)
            {
                return;
            }

            _sceneHover = over;
            if (!_previewHover)
            {
                feedback.SetHovered(over);
            }

            HandleUtility.Repaint();
        }

        private void OnDisable()
        {
            if (Application.isPlaying || _previewHover)
            {
                return;
            }

            var view = target as CombatExecuteOverlayUIView;
            if (view == null)
            {
                return;
            }

            ResolveFeedback(view)?.SetHovered(false);
        }

        private static UiButtonHoverFeedback ResolveFeedback(CombatExecuteOverlayUIView view)
        {
            var button = view.ExecuteButton;
            var host = button != null ? button.gameObject : null;
            return host != null ? UiButtonHoverFeedback.Ensure(host) : null;
        }

        private static bool IsMouseOver(RectTransform rect)
        {
            var sceneView = SceneView.currentDrawingSceneView;
            if (sceneView == null || Event.current == null)
            {
                return false;
            }

            var canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }

            var screen = HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition);
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screen, cam);
        }
    }
}
#endif
