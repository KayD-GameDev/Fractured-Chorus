#if UNITY_EDITOR
using System.Collections.Generic;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class FcColorTokensSceneApplyEditor
    {
        [MenuItem("Fractured Chorus/UI/Apply Color Tokens To Active Scene")]
        public static void ApplyToActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[FcColorTokens] No active scene loaded.");
                return;
            }

            var graphics = Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include);
            var changed = 0;

            foreach (var graphic in graphics)
            {
                if (graphic == null)
                {
                    continue;
                }

                var current = graphic.color;
                if (!TryResolveToken(graphic, current, out var token))
                {
                    continue;
                }

                if (ColorsApproximatelyEqual(current, token))
                {
                    continue;
                }

                Undo.RecordObject(graphic, "Apply FcColorTokens");
                graphic.color = token;
                EditorUtility.SetDirty(graphic);
                changed++;
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log($"[FcColorTokens] Applied tokens to {changed} Graphic(s) in '{scene.name}'. Save scene to persist.");
        }

        private static bool TryResolveToken(Graphic graphic, Color current, out Color token)
        {
            token = default;
            var name = graphic.gameObject.name;

            if (MatchesName(name, "RowSelected", "SlotSelected", "SelectedRow"))
            {
                token = FcColorTokens.Selection.RowBackground;
                return true;
            }

            if (MatchesName(name, "AgreeHighlight", "DisagreeHighlight") && current.a > 0.05f)
            {
                token = FcColorTokens.Selection.VnChoiceHighlight;
                return true;
            }

            if (MatchesName(name, "Selected", "Highlight", "RowSelected", "SlotSelected") && current.a > 0.05f)
            {
                token = FcColorTokens.Selection.Accent;
                return true;
            }

            if (MatchesName(name, "Fight", "EnterBattle", "Confirm") && MatchesName(name, "Button", "Btn", "Cta"))
            {
                token = FcColorTokens.Brand.RedSelection;
                return true;
            }

            if (LegacyMap.TryGetValue(RgbKey(current), out token))
            {
                return true;
            }

            return false;
        }

        private static bool MatchesName(string objectName, params string[] fragments)
        {
            foreach (var fragment in fragments)
            {
                if (objectName.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ColorsApproximatelyEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.004f
                && Mathf.Abs(a.g - b.g) < 0.004f
                && Mathf.Abs(a.b - b.b) < 0.004f
                && Mathf.Abs(a.a - b.a) < 0.004f;
        }

        private static (int r, int g, int b) RgbKey(Color color)
        {
            return (
                Mathf.RoundToInt(color.r * 1000f),
                Mathf.RoundToInt(color.g * 1000f),
                Mathf.RoundToInt(color.b * 1000f));
        }

        private static readonly Dictionary<(int r, int g, int b), Color> LegacyMap = BuildLegacyMap();

        private static Dictionary<(int r, int g, int b), Color> BuildLegacyMap()
        {
            return new Dictionary<(int, int, int), Color>
            {
                { (0, 831, 1000), FcColorTokens.Brand.Cyan },
                { (0, 550, 700), FcColorTokens.Brand.CyanDim },
                { (550, 850, 1000), FcColorTokens.Brand.CyanHover },
                { (200, 750, 1000), FcColorTokens.Brand.CyanSoft },
                { (350, 720, 1000), FcColorTokens.Selection.VnChoiceHighlight },
                { (950, 620, 250), FcColorTokens.Brand.CyanNeonBody },
                { (900, 490, 130), FcColorTokens.Brand.CyanNeonBody },
                { (920, 280, 220), FcColorTokens.Semantic.ElementRhythm },
                { (580, 280, 880), FcColorTokens.Semantic.ElementMelody },
                { (950, 820, 180), FcColorTokens.Semantic.ElementHarmony },
                { (20, 40, 120), FcColorTokens.Surface.Dim },
                { (30, 50, 140), FcColorTokens.Surface.Panel },
                { (39, 59, 180), FcColorTokens.Surface.Modal },
                { (60, 80, 240), FcColorTokens.Surface.Row },
                { (100, 140, 340), FcColorTokens.Surface.RowSelected },
            };
        }
    }
}
#endif
