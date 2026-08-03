using FracturedChorus.Combat.Formation;
using FracturedChorus.Combat.Grid;
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
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropButton;

        public bool IsVisible =>
            root != null && root.activeSelf && gameObject.activeSelf;

        public static DeployFormationHintView EnsureOnCanvas(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var existing = canvasRoot.GetComponentInChildren<DeployFormationHintView>(true);
            if (existing != null)
            {
                existing.EnsureBuilt();
                existing.ApplyLaneBadgeStyles();
                existing.BindCloseHandlers();
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
            ApplyLaneBadgeStyles();
            BindCloseHandlers();

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (root != null)
            {
                root.SetActive(true);
            }

            transform.SetAsLastSibling();

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

            gameObject.SetActive(false);
        }

        private void BindCloseHandlers()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }

            if (backdropButton != null)
            {
                backdropButton.onClick.RemoveListener(Hide);
                backdropButton.onClick.AddListener(Hide);
            }
        }

        private void EnsureBuilt()
        {
            if (root != null && transform.Find("Panel/CloseButton") != null)
            {
                WireExistingBadges();
                return;
            }

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            root = null;
            frontBadge = null;
            midBadge = null;
            backBadge = null;
            pressureLabel = null;
            closeButton = null;
            backdropButton = null;
            BuildDefaultHierarchy();
        }

        private void WireExistingBadges()
        {
            if (frontBadge == null)
            {
                frontBadge = transform.Find("Panel/BadgeRow/FrontBadge/Label")?.GetComponent<Text>();
            }

            if (midBadge == null)
            {
                midBadge = transform.Find("Panel/BadgeRow/MidBadge/Label")?.GetComponent<Text>();
            }

            if (backBadge == null)
            {
                backBadge = transform.Find("Panel/BadgeRow/BackBadge/Label")?.GetComponent<Text>();
            }

            if (pressureLabel == null)
            {
                pressureLabel = transform.Find("Panel/PressureLabel")?.GetComponent<Text>();
            }

            if (closeButton == null)
            {
                closeButton = transform.Find("Panel/CloseButton")?.GetComponent<Button>();
            }

            if (backdropButton == null)
            {
                backdropButton = transform.Find("Backdrop")?.GetComponent<Button>();
            }
        }

        private void ApplyLaneBadgeStyles()
        {
            WireExistingBadges();
            StyleBadge(frontBadge, "FRONT  −15% dmg taken", FormationLaneVisuals.FrontBadge,
                FormationLaneVisuals.LoadLaneIcon(PositionalModifiers.FrontColumnIndex));
            StyleBadge(midBadge, "MID  +dmg", FormationLaneVisuals.MidBadge,
                FormationLaneVisuals.LoadLaneIcon(FormationLaneVisuals.MidColumnIndex));
            StyleBadge(backBadge, "BACK  +buff / né", FormationLaneVisuals.BackBadge,
                FormationLaneVisuals.LoadLaneIcon(PositionalModifiers.BackColumnIndex));
        }

        private static void StyleBadge(Text label, string copy, Color color, Sprite icon)
        {
            if (label == null)
            {
                return;
            }

            label.text = copy;
            label.color = color;
            var badgeBg = label.transform.parent != null
                ? label.transform.parent.GetComponent<Image>()
                : null;
            if (badgeBg != null)
            {
                badgeBg.color = new Color(color.r, color.g, color.b, 0.22f);
            }

            EnsureBadgeIcon(label.transform.parent, icon);
        }

        private static void EnsureBadgeIcon(Transform badgeRoot, Sprite icon)
        {
            if (badgeRoot == null || icon == null)
            {
                return;
            }

            var iconTf = badgeRoot.Find("Icon");
            Image image;
            if (iconTf == null)
            {
                var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(badgeRoot, false);
                image = go.GetComponent<Image>();
                Stretch(go.GetComponent<RectTransform>(), new Vector2(0.08f, 0.18f), new Vector2(0.32f, 0.82f),
                    Vector2.zero, Vector2.zero);
            }
            else
            {
                image = iconTf.GetComponent<Image>();
            }

            if (image == null)
            {
                return;
            }

            image.sprite = icon;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;

            var label = badgeRoot.Find("Label") as RectTransform;
            if (label != null)
            {
                Stretch(label, new Vector2(0.34f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
            }
        }

        private void BuildDefaultHierarchy()
        {
            var host = gameObject;
            var hostRect = host.GetComponent<RectTransform>();
            Stretch(hostRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var canvas = host.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = host.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = UiCanvasLayers.Modal;

            if (host.GetComponent<GraphicRaycaster>() == null)
            {
                host.AddComponent<GraphicRaycaster>();
            }

            root = host;

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGo.transform.SetParent(host.transform, false);
            Stretch(backdropGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var backdropImage = backdropGo.GetComponent<Image>();
            backdropImage.color = new Color(0.02f, 0.03f, 0.08f, 0.72f);
            backdropImage.raycastTarget = true;
            backdropButton = backdropGo.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(host.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 280f);
            panelRect.anchoredPosition = Vector2.zero;
            var panelBg = panelGo.GetComponent<Image>();
            panelBg.color = FcColorTokens.Surface.Modal;
            panelBg.raycastTarget = true;

            var title = CreateText(panelGo.transform, "Title", "Formation lanes", 22, TextAnchor.MiddleCenter);
            Stretch(title.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.85f, 0.96f), Vector2.zero, Vector2.zero);
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;

            closeButton = CreateCloseButton(panelGo.transform);

            var badgeRow = new GameObject("BadgeRow", typeof(RectTransform));
            badgeRow.transform.SetParent(panelGo.transform, false);
            Stretch(badgeRow.GetComponent<RectTransform>(), new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.74f),
                Vector2.zero, Vector2.zero);

            frontBadge = CreateBadge(badgeRow.transform, "FrontBadge", "FRONT  −15% dmg taken", 0f, 0.32f);
            midBadge = CreateBadge(badgeRow.transform, "MidBadge", "MID  +dmg", 0.34f, 0.66f);
            backBadge = CreateBadge(badgeRow.transform, "BackBadge", "BACK  +buff / né", 0.68f, 1f);
            ApplyLaneBadgeStyles();

            pressureLabel = CreateText(panelGo.transform, "PressureLabel", string.Empty, 18, TextAnchor.MiddleCenter);
            Stretch(pressureLabel.rectTransform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.26f), Vector2.zero,
                Vector2.zero);
            pressureLabel.color = FcColorTokens.Brand.Cyan;
            pressureLabel.fontStyle = FontStyle.Italic;
            pressureLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            BindCloseHandlers();
        }

        private static Button CreateCloseButton(Transform panel)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -10f);
            rect.sizeDelta = new Vector2(40f, 40f);

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.12f);
            image.raycastTarget = true;

            var label = CreateText(go.transform, "Label", "✕", 22, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.raycastTarget = false;
            label.color = Color.white;

            return go.GetComponent<Button>();
        }

        private static Text CreateBadge(Transform parent, string name, string copy, float xMin, float xMax)
        {
            var badgeGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(parent, false);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            Stretch(badgeRect, new Vector2(xMin, 0f), new Vector2(xMax, 1f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
            var badgeBg = badgeGo.GetComponent<Image>();
            badgeBg.raycastTarget = false;

            var label = CreateText(badgeGo.transform, "Label", copy, 16, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, new Vector2(0.34f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
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

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
