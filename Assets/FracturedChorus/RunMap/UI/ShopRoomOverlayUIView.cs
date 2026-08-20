using System;
using FracturedChorus.RunMap;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.RunMap.UI
{
    public sealed class ShopRoomOverlayUIView : MonoBehaviour
    {
        public const string BackgroundResourcePath = "UI/RunMap/shop_room_bg_v1";
        public const string BackgroundAssetPath =
            "Assets/FracturedChorus/Art/UI/RunMap/Shop/shop_room_bg_v1.png";

        private const int OverlaySortOrder = UiCanvasLayers.Popup;
        private const int MaxCards = 5;
        private const float CardWidth = 210f;
        private const float CardHeight = 280f;
        private const float CardSpacing = 228f;
        private const float CardY = 168f;

        private static readonly Color TitleColor = new Color(0.86f, 0.72f, 1f, 1f);

        [SerializeField] private Image background;
        [SerializeField] private Text titleText;
        [SerializeField] private Text hintText;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Text leaveLabel;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private bool preserveSceneLayout = true;

        private readonly ChoiceCard[] _cards = new ChoiceCard[MaxCards];
        private Action<ShopChoiceOffer> _onPicked;
        private bool _resolved;

        public static ShopRoomOverlayUIView EnsureOnCanvas(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var existing = canvasRoot.GetComponentInChildren<ShopRoomOverlayUIView>(true);
            if (existing != null)
            {
                existing.WireSceneReferences();
                return existing;
            }

            var go = new GameObject("ShopRoomOverlay", typeof(RectTransform), typeof(ShopRoomOverlayUIView));
            go.transform.SetParent(canvasRoot, false);
            var view = go.GetComponent<ShopRoomOverlayUIView>();
            view.preserveSceneLayout = false;
            view.BuildDefaultHierarchy();
            view.ApplyBackground();
            go.SetActive(false);
            return view;
        }

        public void WireSceneReferences()
        {
            background ??= FindChildImage("Background");
            titleText ??= FindChildText("Title");
            hintText ??= FindChildText("Hint");
            if (leaveButton == null)
            {
                var leave = transform.Find("LeaveButton");
                leaveButton = leave != null ? leave.GetComponent<Button>() : null;
                leaveLabel = leave != null ? leave.Find("Label")?.GetComponent<Text>() : null;
            }

            for (var i = 0; i < MaxCards; i++)
            {
                var child = transform.Find($"ChoiceCard_{i}");
                if (child == null)
                {
                    continue;
                }

                HologramChoiceCardChrome.Apply(child);
                _cards[i] ??= ChoiceCard.FromRoot(child);
            }

            EnsureOverlayCanvas();
        }

        public void Show(ShopChoiceOffer[] offers, Action<ShopChoiceOffer> onPicked)
        {
            WireSceneReferences();
            _onPicked = onPicked;
            _resolved = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            EnsureOverlayCanvas();
            ApplyBackground();

            if (titleText != null)
            {
                titleText.text = "SHOP";
            }

            if (hintText != null)
            {
                var notes = FracturedChorus.Meta.GameMetaSession.HasSession
                    ? FracturedChorus.Meta.GameMetaSession.Current.Wallet.Notes
                    : 0;
                hintText.text = $"Chọn 1 món · {notes} Notes";
            }

            var count = offers != null ? Mathf.Min(offers.Length, MaxCards) : 0;
            var startX = -CardSpacing * (count - 1) / 2f;
            for (var i = 0; i < MaxCards; i++)
            {
                var card = EnsureCard(i);
                if (card == null)
                {
                    continue;
                }

                if (i >= count)
                {
                    card.Root.gameObject.SetActive(false);
                    continue;
                }

                card.Root.gameObject.SetActive(true);
                if (!preserveSceneLayout)
                {
                    ApplyCardRect(card.Root, startX + i * CardSpacing);
                }

                card.Bind(offers[i], OnCardClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveAllListeners();
                leaveButton.onClick.AddListener(() => OnCardClicked(ShopChoiceCatalog.LeaveOffer()));
                leaveButton.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            _onPicked = null;
            _resolved = true;
            gameObject.SetActive(false);
        }

        public void ShowEditPreview()
        {
            Show(ShopChoiceCatalog.CreateOffers(previewAllAvailable: true), _ => { });
        }

        public void BuildDefaultHierarchy()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            background = CreateImage("Background", root, Color.white, true);
            background.raycastTarget = true;

            titleText = CreateLabel("Title", root, "SHOP", 52, TextAnchor.MiddleCenter, TitleColor);
            ApplyRect(titleText.rectTransform, new Vector2(0.5f, 0.9f), Vector2.zero, new Vector2(900f, 72f));
            UiFontCatalog.Apply(titleText, UiFontRole.Display, 52);

            hintText = CreateLabel("Hint", root, "Chọn 1 món", 24, TextAnchor.MiddleCenter, FcColorTokens.Brand.TextMuted);
            ApplyRect(hintText.rectTransform, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(900f, 40f));
            UiFontCatalog.ApplyAutomatic(hintText);

            for (var i = 0; i < MaxCards; i++)
            {
                var go = HologramChoiceCardChrome.Create($"ChoiceCard_{i}", root, true);
                ApplyCardRect(go.transform as RectTransform, 0f);
                _cards[i] = ChoiceCard.FromRoot(go.transform);
                _cards[i].Root.gameObject.SetActive(false);
            }

            var leaveGo = new GameObject("LeaveButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            leaveGo.transform.SetParent(root, false);
            leaveButton = leaveGo.GetComponent<Button>();
            var leaveImage = leaveGo.GetComponent<Image>();
            leaveImage.color = new Color(0.12f, 0.08f, 0.2f, 0.92f);
            ApplyRect((RectTransform)leaveGo.transform, new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(220f, 48f));
            leaveLabel = CreateLabel("Label", leaveGo.transform, "Leave", 22, TextAnchor.MiddleCenter, Color.white);
            ApplyRect(leaveLabel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 40f));
            UiFontCatalog.ApplyAutomatic(leaveLabel);
            UiButtonHoverFeedback.Ensure(leaveGo);

            EnsureOverlayCanvas();
        }

        public void ApplyBackground()
        {
            if (backgroundSprite == null)
            {
                backgroundSprite = Resources.Load<Sprite>(BackgroundResourcePath);
            }

#if UNITY_EDITOR
            if (backgroundSprite == null)
            {
                backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundAssetPath);
            }
#endif

            if (background == null)
            {
                return;
            }

            if (backgroundSprite != null)
            {
                background.sprite = backgroundSprite;
                background.color = Color.white;
                background.preserveAspect = false;
            }
            else
            {
                background.sprite = null;
                background.color = new Color(0.12f, 0.04f, 0.22f, 1f);
            }
        }

        private void OnCardClicked(ShopChoiceOffer offer)
        {
            if (_resolved || !offer.Available)
            {
                return;
            }

            _resolved = true;
            var callback = _onPicked;
            callback?.Invoke(offer);
        }

        private ChoiceCard EnsureCard(int index)
        {
            if (_cards[index] != null)
            {
                return _cards[index];
            }

            var child = transform.Find($"ChoiceCard_{index}");
            if (child != null)
            {
                HologramChoiceCardChrome.Apply(child);
                _cards[index] = ChoiceCard.FromRoot(child);
                return _cards[index];
            }

            var go = HologramChoiceCardChrome.Create($"ChoiceCard_{index}", (RectTransform)transform, true);
            ApplyCardRect(go.transform as RectTransform, 0f);
            _cards[index] = ChoiceCard.FromRoot(go.transform);
            return _cards[index];
        }

        private static void ApplyCardRect(RectTransform rt, float x)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, CardY);
            rt.sizeDelta = new Vector2(CardWidth, CardHeight);
        }

        private static void ApplyRect(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static Image CreateImage(string name, RectTransform parent, Color color, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            var rt = image.rectTransform;
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            return image;
        }

        private static Text CreateLabel(string name, Transform parent, string content, int size, TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.alignment = align;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = UiFontCatalog.Body != null ? UiFontCatalog.Body : Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
        }

        private void EnsureOverlayCanvas()
        {
            var parentCanvas = GetComponentInParent<Canvas>();
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            if (parentCanvas != null && parentCanvas != canvas)
            {
                canvas.renderMode = parentCanvas.renderMode;
                canvas.worldCamera = parentCanvas.worldCamera;
                canvas.planeDistance = parentCanvas.planeDistance;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySortOrder;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private Image FindChildImage(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private Text FindChildText(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private sealed class ChoiceCard
        {
            public ChoiceCard(Transform root, Button button, CanvasGroup group, Text kind, Text title, Text body)
            {
                Root = root as RectTransform;
                Button = button;
                Group = group;
                Kind = kind;
                Title = title;
                Body = body;
            }

            public RectTransform Root { get; }
            public Button Button { get; }
            public CanvasGroup Group { get; }
            public Text Kind { get; }
            public Text Title { get; }
            public Text Body { get; }

            public static ChoiceCard FromRoot(Transform root)
            {
                if (root == null)
                {
                    return null;
                }

                return new ChoiceCard(
                    root,
                    root.GetComponent<Button>(),
                    root.GetComponent<CanvasGroup>() ?? root.gameObject.AddComponent<CanvasGroup>(),
                    root.Find("Kind")?.GetComponent<Text>(),
                    root.Find("Title")?.GetComponent<Text>(),
                    root.Find("Body")?.GetComponent<Text>());
            }

            public void Bind(ShopChoiceOffer offer, Action<ShopChoiceOffer> onClicked)
            {
                if (Kind != null)
                {
                    Kind.text = offer.KindLabel;
                }

                if (Title != null)
                {
                    Title.text = offer.Title;
                }

                if (Body != null)
                {
                    Body.text = offer.Description;
                }

                if (Group != null)
                {
                    Group.alpha = offer.Available ? 1f : 0.38f;
                    Group.interactable = offer.Available;
                    Group.blocksRaycasts = true;
                }

                if (Button == null)
                {
                    return;
                }

                Button.onClick.RemoveAllListeners();
                Button.interactable = offer.Available;
                if (offer.Available)
                {
                    Button.onClick.AddListener(() => onClicked?.Invoke(offer));
                }
            }
        }
    }
}
