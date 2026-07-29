using System;
using UnityEngine;
using UnityEngine.UI;
using FracturedChorus.UI;

namespace FracturedChorus.Tutorial
{
    public sealed class TutorialCoachView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextLabel;
        [SerializeField] private Image coachPortrait;
        [SerializeField] private Image panelImage;

        private Action _onNext;

        public bool IsVisible => root != null && root.activeSelf;

        public static TutorialCoachView Ensure(Transform host)
        {
            if (host == null)
            {
                return null;
            }

            var existing = host.GetComponentInChildren<TutorialCoachView>(true);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("TutorialCoach", typeof(RectTransform), typeof(TutorialCoachView));
            go.transform.SetParent(host, false);
            var view = go.GetComponent<TutorialCoachView>();
            view.BuildHierarchy();
            go.SetActive(false);
            return view;
        }

        public void Show(string bodyCopy, Action onNext)
        {
            Show(bodyCopy, onNext, null, null);
        }

        public void Show(string bodyCopy, Action onNext, Sprite portrait, Sprite panel)
        {
            EnsureBuilt();
            _onNext = onNext;
            if (bodyLabel != null)
            {
                bodyLabel.text = bodyCopy ?? string.Empty;
            }

            ApplySprite(coachPortrait, portrait, preserveAspect: true);
            ApplySprite(panelImage, panel, preserveAspect: true);

            if (bodyLabel != null)
            {
                if (panel != null)
                {
                    Stretch(bodyLabel.rectTransform, new Vector2(0.24f, 0.26f), new Vector2(0.96f, 0.5f), Vector2.zero, Vector2.zero);
                }
                else
                {
                    Stretch(bodyLabel.rectTransform, new Vector2(0.24f, 0.26f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
                }
            }

            if (root != null)
            {
                root.SetActive(true);
            }

            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            _onNext = null;
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

            BuildHierarchy();
        }

        private void BuildHierarchy()
        {
            var overlayRect = GetComponent<RectTransform>();
            Stretch(overlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dimmer = CreateImage(transform, "Dimmer", FcColorTokens.Surface.DimmerBlack);
            Stretch(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dimmer.raycastTarget = true;

            var panel = CreateImage(transform, "Panel", FcColorTokens.WithAlpha(FcColorTokens.Surface.Panel, 0.94f));
            Stretch(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-480f, -210f), new Vector2(480f, 210f));
            panel.raycastTarget = true;

            coachPortrait = CreateImage(panel.transform, "CoachPortrait", Color.white);
            Stretch(coachPortrait.rectTransform, new Vector2(0.02f, 0.18f), new Vector2(0.22f, 0.96f), Vector2.zero, Vector2.zero);
            coachPortrait.preserveAspect = true;
            coachPortrait.raycastTarget = false;
            coachPortrait.enabled = false;

            panelImage = CreateImage(panel.transform, "PanelImage", Color.white);
            Stretch(panelImage.rectTransform, new Vector2(0.24f, 0.52f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
            panelImage.preserveAspect = true;
            panelImage.raycastTarget = false;
            panelImage.enabled = false;

            bodyLabel = CreateText(panel.transform, "Body", string.Empty, 22, TextAnchor.UpperLeft);
            Stretch(bodyLabel.rectTransform, new Vector2(0.24f, 0.26f), new Vector2(0.96f, 0.5f), Vector2.zero, Vector2.zero);
            bodyLabel.color = Color.white;
            bodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyLabel.verticalOverflow = VerticalWrapMode.Overflow;

            nextButton = CreateButton(panel.transform, "NextButton", "Next", out nextLabel);
            Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.06f), new Vector2(0.96f, 0.2f), Vector2.zero, Vector2.zero);
            nextButton.onClick.AddListener(HandleNext);
            UiButtonHoverFeedback.Ensure(nextButton.gameObject);

            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = UiCanvasLayers.Tutorial;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            root = gameObject;
        }

        private static void ApplySprite(Image image, Sprite sprite, bool preserveAspect)
        {
            if (image == null)
            {
                return;
            }

            if (sprite == null)
            {
                image.sprite = null;
                image.enabled = false;
                return;
            }

            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.color = Color.white;
            image.enabled = true;
        }

        private void HandleNext()
        {
            var callback = _onNext;
            Hide();
            callback?.Invoke();
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            UiFontCatalog.Apply(text, UiFontRole.Body, fontSize);
            text.text = content;
            text.alignment = anchor;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, out Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.18f, 0.32f, 0.95f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            labelText = CreateText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            labelText.color = FcColorTokens.Brand.Cyan;
            labelText.fontStyle = FontStyle.Bold;
            return button;
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
