using FracturedChorus.Combat.Formation;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public sealed class DeployFormationHintView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text frontBadge;
        [SerializeField] private Text midBadge;
        [SerializeField] private Text backBadge;
        [SerializeField] private Text pressureLabel;

        public static DeployFormationHintView EnsureOnCanvas(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var existing = canvasRoot.GetComponentInChildren<DeployFormationHintView>(true);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("DeployFormationHint", typeof(RectTransform), typeof(DeployFormationHintView));
            go.transform.SetParent(canvasRoot, false);
            var view = go.GetComponent<DeployFormationHintView>();
            view.BuildDefaultHierarchy();
            go.SetActive(false);
            return view;
        }

        public void ShowForDeploy(BossFormationProfileSO profile)
        {
            EnsureBuilt();
            if (root != null)
            {
                root.SetActive(true);
            }

            if (pressureLabel != null)
            {
                var summary = profile != null ? profile.pressureSummary : string.Empty;
                pressureLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(summary));
                pressureLabel.text = summary ?? string.Empty;
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void EnsureBuilt()
        {
            if (root != null)
            {
                return;
            }

            BuildDefaultHierarchy();
        }

        private void BuildDefaultHierarchy()
        {
            var panel = gameObject;
            var panelRect = panel.GetComponent<RectTransform>();
            Stretch(panelRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-420f, 24f), new Vector2(420f, 148f));

            var panelBg = panel.GetComponent<Image>();
            if (panelBg == null)
            {
                panelBg = panel.AddComponent<Image>();
            }

            panelBg.color = FcColorTokens.WithAlpha(FcColorTokens.Surface.Dim, 0.82f);
            panelBg.raycastTarget = false;

            var canvas = panel.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = panel.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = UiCanvasLayers.Panel;

            root = panel;

            var badgeRow = new GameObject("BadgeRow", typeof(RectTransform));
            badgeRow.transform.SetParent(panel.transform, false);
            Stretch(badgeRow.GetComponent<RectTransform>(), new Vector2(0.02f, 0.42f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);

            frontBadge = CreateBadge(badgeRow.transform, "FrontBadge", "FRONT  −15% dmg taken", FcColorTokens.Brand.SaturdayLabel, 0f, 0.32f);
            midBadge = CreateBadge(badgeRow.transform, "MidBadge", "MID", FcColorTokens.Brand.CyanHover, 0.34f, 0.66f);
            backBadge = CreateBadge(badgeRow.transform, "BackBadge", "BACK  +15% dmg dealt", FcColorTokens.Brand.MagentaAccent, 0.68f, 1f);

            pressureLabel = CreateText(panel.transform, "PressureLabel", string.Empty, 18, TextAnchor.MiddleCenter);
            Stretch(pressureLabel.rectTransform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.38f), Vector2.zero, Vector2.zero);
            pressureLabel.color = FcColorTokens.Brand.Cyan;
            pressureLabel.fontStyle = FontStyle.Italic;
            pressureLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private static Text CreateBadge(Transform parent, string name, string copy, Color color, float xMin, float xMax)
        {
            var badgeGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(parent, false);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            Stretch(badgeRect, new Vector2(xMin, 0f), new Vector2(xMax, 1f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
            var badgeBg = badgeGo.GetComponent<Image>();
            badgeBg.color = new Color(color.r, color.g, color.b, 0.18f);
            badgeBg.raycastTarget = false;

            var label = CreateText(badgeGo.transform, "Label", copy, 16, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 4f), new Vector2(-6f, -4f));
            label.color = color;
            label.fontStyle = FontStyle.Bold;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            UiFontCatalog.Apply(text, UiFontRole.Body, fontSize);
            text.text = content;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
