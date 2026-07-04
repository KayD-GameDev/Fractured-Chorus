#if UNITY_EDITOR
using FracturedChorus.Data;
using FracturedChorus.RunMap.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    internal static class CadenceMapMaskScenePreview
    {
        public static void Draw(SceneView sceneView)
        {
            if (!CadenceMapMaskEditSession.PreviewEnabled || CadenceMapMaskEditSession.Layout == null)
            {
                return;
            }

            var macroView = Object.FindAnyObjectByType<CadenceMacroMapView>();
            if (macroView == null)
            {
                return;
            }

            var layer = macroView.TerritoryLayerRect;
            if (layer == null)
            {
                return;
            }

            var layout = CadenceMapMaskEditSession.Layout;
            var territories = layout.territories;
            if (territories == null || territories.Length == 0)
            {
                return;
            }

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            for (var t = 0; t < territories.Length; t++)
            {
                var entry = territories[t];
                var vertices = entry.normalizedVertices;
                if (vertices == null || vertices.Length < 2)
                {
                    continue;
                }

                var isSelected = t == CadenceMapMaskEditSession.SelectedTerritory;
                var lineColor = isSelected ? entry.highlightColor : WithAlpha(entry.territoryColor, 0.75f);
                var fillColor = WithAlpha(entry.highlightColor, isSelected ? 0.2f : 0.07f);

                var worldVerts = new Vector3[vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                {
                    worldVerts[i] = NormToWorld(layer, vertices[i]);
                }

                DrawPolygonFill(worldVerts, fillColor);

                Handles.color = lineColor;
                for (var i = 0; i < worldVerts.Length; i++)
                {
                    var next = (i + 1) % worldVerts.Length;
                    Handles.DrawLine(worldVerts[i], worldVerts[next], isSelected ? 3f : 1.5f);
                }

                if (!isSelected)
                {
                    continue;
                }

                for (var i = 0; i < worldVerts.Length; i++)
                {
                    var vertexSelected = i == CadenceMapMaskEditSession.SelectedVertex;
                    var handleSize = HandleUtility.GetHandleSize(worldVerts[i]) * (vertexSelected ? 0.08f : 0.055f);

                    Handles.color = vertexSelected ? Color.yellow : Color.white;
                    EditorGUI.BeginChangeCheck();
                    var newWorld = Handles.FreeMoveHandle(
                        worldVerts[i],
                        handleSize,
                        Vector3.zero,
                        Handles.SphereHandleCap);
                    if (EditorGUI.EndChangeCheck())
                    {
                        newWorld = SnapToLayer(layer, newWorld);
                        vertices[i] = WorldToNorm(layer, newWorld);
                        vertices[i].x = Mathf.Clamp01(vertices[i].x);
                        vertices[i].y = Mathf.Clamp01(vertices[i].y);
                        entry.normalizedVertices = vertices;
                        territories[t] = entry;
                        layout.territories = territories;
                        EditorUtility.SetDirty(layout);
                        CadenceMapMaskEditSession.NotifyLayoutChanged();
                    }

                    if (Event.current.type == EventType.MouseDown &&
                        Event.current.button == 0 &&
                        Vector3.Distance(HandleUtility.WorldToGUIPoint(newWorld), Event.current.mousePosition) < handleSize * 20f)
                    {
                        CadenceMapMaskEditSession.SelectedVertex = i;
                        sceneView.Repaint();
                    }
                }
            }

            HandleKeyboard(territories, layout);
            DrawSceneHud();
        }

        private static void DrawSceneHud()
        {
            Handles.BeginGUI();
            var rect = new Rect(12f, 12f, 420f, 48f);
            GUI.Box(rect, GUIContent.none);
            var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } };
            GUI.Label(new Rect(rect.x + 10f, rect.y + 6f, rect.width - 20f, 18f),
                "Cadence Mask Edit Mode — kéo sphere · click chọn điểm · Delete xóa điểm",
                style);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 26f, rect.width - 20f, 18f),
                "Đổi Finger bằng toolbar trong Layout Editor window",
                style);
            Handles.EndGUI();
        }

        private static void HandleKeyboard(
            CadenceMapLayoutSO.TerritoryEntry[] territories,
            CadenceMapLayoutSO layout)
        {
            if (Event.current.type != EventType.KeyDown || Event.current.keyCode != KeyCode.Delete)
            {
                return;
            }

            var t = CadenceMapMaskEditSession.SelectedTerritory;
            var v = CadenceMapMaskEditSession.SelectedVertex;
            if (t < 0 || t >= territories.Length || v < 0)
            {
                return;
            }

            var vertices = territories[t].normalizedVertices;
            if (vertices == null || vertices.Length <= 3 || v >= vertices.Length)
            {
                return;
            }

            var trimmed = new Vector2[vertices.Length - 1];
            var write = 0;
            for (var i = 0; i < vertices.Length; i++)
            {
                if (i == v)
                {
                    continue;
                }

                trimmed[write++] = vertices[i];
            }

            var entry = territories[t];
            entry.normalizedVertices = trimmed;
            territories[t] = entry;
            layout.territories = territories;
            CadenceMapMaskEditSession.SelectedVertex = Mathf.Clamp(v - 1, 0, trimmed.Length - 1);
            EditorUtility.SetDirty(layout);
            CadenceMapMaskEditSession.NotifyLayoutChanged();
            Event.current.Use();
        }

        private static Vector3 NormToWorld(RectTransform rectTransform, Vector2 norm)
        {
            var rect = rectTransform.rect;
            var local = new Vector3(
                rect.xMin + norm.x * rect.width,
                rect.yMin + norm.y * rect.height,
                0f);
            return rectTransform.TransformPoint(local);
        }

        private static Vector2 WorldToNorm(RectTransform rectTransform, Vector3 world)
        {
            var local = rectTransform.InverseTransformPoint(world);
            var rect = rectTransform.rect;
            return new Vector2(
                (local.x - rect.xMin) / rect.width,
                (local.y - rect.yMin) / rect.height);
        }

        private static Vector3 SnapToLayer(RectTransform rectTransform, Vector3 world)
        {
            var local = rectTransform.InverseTransformPoint(world);
            local.z = 0f;
            return rectTransform.TransformPoint(local);
        }

        private static void DrawPolygonFill(Vector3[] worldVerts, Color color)
        {
            if (worldVerts.Length < 3)
            {
                return;
            }

            var centroid = Vector3.zero;
            for (var i = 0; i < worldVerts.Length; i++)
            {
                centroid += worldVerts[i];
            }

            centroid /= worldVerts.Length;
            Handles.color = color;

            for (var i = 0; i < worldVerts.Length; i++)
            {
                var next = (i + 1) % worldVerts.Length;
                Handles.DrawAAConvexPolygon(centroid, worldVerts[i], worldVerts[next]);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
#endif
