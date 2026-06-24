using FracturedChorus.Combat.Grid;
using UnityEngine;

namespace FracturedChorus.UI
{
    public static class HexSpriteUtil
    {
        private static Sprite _hexagonFlatTop;

        public const float DefaultScaleX = 1.5f;
        public const float DefaultScaleY = 1.5f;

        public static Sprite ResolveHexagonFlatTop()
        {
            if (_hexagonFlatTop != null)
            {
                return _hexagonFlatTop;
            }

            var template = GameObject.Find("Hexagon Flat Top");
            if (template != null && template.TryGetComponent<SpriteRenderer>(out var templateRenderer) &&
                templateRenderer.sprite != null)
            {
                _hexagonFlatTop = templateRenderer.sprite;
                return _hexagonFlatTop;
            }

            _hexagonFlatTop = Resources.Load<Sprite>("HexagonFlatTop");
            if (_hexagonFlatTop != null)
            {
                return _hexagonFlatTop;
            }

#if UNITY_EDITOR
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("Hexagon Flat Top t:Sprite"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    _hexagonFlatTop = sprite;
                    return _hexagonFlatTop;
                }
            }
#endif

            _hexagonFlatTop = CreateProceduralFallback();
            return _hexagonFlatTop;
        }

        private static Sprite CreateProceduralFallback()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2(size * 0.5f, size * 0.5f);
            var radius = size * 0.46f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, IsInsideFlatTopHex(new Vector2(x, y), center, radius)
                        ? Color.white
                        : Color.clear);
                }
            }

            tex.Apply();
            var diameter = HexBoardLayout.HexRadius * 2f;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / diameter);
        }

        private static bool IsInsideFlatTopHex(Vector2 point, Vector2 center, float radius)
        {
            var verts = HexBoardLayout.GetHexOutlineVertices(radius);
            var inside = false;
            for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
            {
                var pi = center + verts[i];
                var pj = center + verts[j];
                if ((pi.y > point.y) != (pj.y > point.y) &&
                    point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y + 0.0001f) + pi.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
