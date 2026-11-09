using System;
using FracturedChorus.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.UI
{
    public sealed class ResonanceDiveButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public const string ObjectName = "ResonanceDiveButton";
        public const string ResourceFolder = "UI/ResonanceDive";
        public const string MotionResource = "UI/ResonanceDive/ResonanceDiveMotion";
        private const int ParticleCount = 14;
        private const float ParticleBitSize = 18f;

        [SerializeField] private Button button;
        [SerializeField] private Image shadow;
        [SerializeField] private Image baseLayer;
        [SerializeField] private Image gradient;
        [SerializeField] private Image glass;
        [SerializeField] private Image border;
        [SerializeField] private Image decoLeft;
        [SerializeField] private Image decoRight;
        [SerializeField] private Image textLayer;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text subtitleLabel;
        [SerializeField] private Image plate;
        [SerializeField] private Image glow;
        [SerializeField] private float glowIdleMin = 0.22f;
        [SerializeField] private float glowIdleMax = 0.4f;
        [SerializeField] private float glowHoverMin = 0.45f;
        [SerializeField] private float glowHoverMax = 0.7f;
        [SerializeField] private float pulseSpeed = 2.2f;
        [SerializeField] private float particleOrbitSpeed = 0.7f;

        private const string LockedNotice = "Resonance Dive can't be used during combat.";

        private Action _onActivate;
        private bool _listening;
        private bool _locked;
        private GameObject _lockRoot;
        private Text _noticeLabel;
        private float _noticeLeft;
        private bool _hovered;
        private bool _pressed;
        private float _phase;
        private CanvasGroup _group;
        private RectTransform _particleRoot;
        private RectTransform _layoutTemplate;
        private Sprite _stateNormal;
        private Sprite _stateHover;
        private Sprite _statePressed;
        private readonly Image[] _bits = new Image[ParticleCount];
        private readonly float[] _bitPhase = new float[ParticleCount];
        private readonly float[] _bitRadius = new float[ParticleCount];

        public static ResonanceDiveButton Ensure(Transform parent, Action onActivate, bool fillParent = false)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find(ObjectName)?.GetComponent<ResonanceDiveButton>();
            if (existing == null)
            {
                existing = parent.GetComponentInChildren<ResonanceDiveButton>(true);
            }

            if (existing == null)
            {
                existing = Create(parent, fillParent);
            }

            existing.Bind(onActivate);
            existing.ApplySprites();
            existing.ApplyFonts();
            return existing;
        }

        public void Bind(Action onActivate)
        {
            _onActivate = onActivate;
            _listening = onActivate != null;
            ResolveRefs();
            if (button == null)
            {
                return;
            }

            button.transition = Selectable.Transition.None;
            button.onClick.RemoveListener(Activate);
            button.interactable = _listening;
            if (button.targetGraphic != null)
            {
                button.targetGraphic.raycastTarget = _listening;
            }
            if (_listening)
            {
                button.onClick.AddListener(Activate);
            }
        }

        public void SetLocked(bool locked)
        {
            _locked = locked;
            EnsureLockBadge();
            if (_lockRoot != null)
            {
                _lockRoot.SetActive(locked);
                if (locked)
                {
                    _lockRoot.transform.SetAsLastSibling();
                }
            }

            if (!locked)
            {
                HideLockedNotice();
            }
        }

        public void SetListening(bool listening)
        {
            _listening = listening && _onActivate != null;
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(Activate);
            button.interactable = _listening;
            if (button.targetGraphic != null)
            {
                button.targetGraphic.raycastTarget = _listening;
            }
            if (_listening)
            {
                button.onClick.AddListener(Activate);
            }
        }

        public void SetMenuVisible(bool visible, float alpha)
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
            }

            gameObject.SetActive(visible);
            if (_group != null)
            {
                _group.alpha = visible ? alpha : 0f;
                _group.interactable = visible && alpha > 0.95f;
                _group.blocksRaycasts = visible && alpha > 0.95f;
            }

            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.raycastTarget = visible && alpha > 0.95f && _listening;
            }
        }

        private void Awake()
        {
            ResolveRefs();
            EnsurePack();
            ApplySprites();
            ApplyFonts();
            ApplyVisual(0f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            HideUnusedOverlays();
        }
#endif

        private void OnEnable()
        {
            _phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _hovered = false;
            _pressed = false;
            ApplyVisual(0f);
        }

        private void Update()
        {
            if (_listening && !_locked)
            {
                PollHotkey();
            }

            if (_noticeLeft > 0f)
            {
                _noticeLeft -= Time.unscaledDeltaTime;
                if (_noticeLeft <= 0f)
                {
                    HideLockedNotice();
                }
            }

            _phase += Time.unscaledDeltaTime;
            var pulse = (Mathf.Sin(_phase * pulseSpeed) + 1f) * 0.5f;
            ApplyVisual(pulse);
            OrbitParticles();
        }

        private void PollHotkey()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            {
                Activate();
            }
#endif
        }

        private void Activate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_locked)
            {
                ShowLockedNotice();
                return;
            }

            if (!_listening || _onActivate == null)
            {
                return;
            }

            _onActivate();
        }

        private void EnsureLockBadge()
        {
            if (_lockRoot != null)
            {
                return;
            }

            _lockRoot = new GameObject("LockBadge", typeof(RectTransform), typeof(Image));
            _lockRoot.transform.SetParent(transform, false);
            var rect = _lockRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(72f, 72f);
            var plateImage = _lockRoot.GetComponent<Image>();
            plateImage.color = new Color(0.12f, 0.12f, 0.14f, 0.88f);
            plateImage.raycastTarget = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(_lockRoot.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(40f, 40f);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = LockSprite;
            icon.color = new Color(0.82f, 0.82f, 0.86f, 1f);
            icon.raycastTarget = false;
            _lockRoot.SetActive(false);
        }

        private void ShowLockedNotice()
        {
            EnsureNotice();
            if (_noticeLabel == null)
            {
                return;
            }

            _noticeLabel.text = GameLoc.Tr(LockedNotice);
            _noticeLabel.gameObject.SetActive(true);
            _noticeLeft = 2.6f;
        }

        private void HideLockedNotice()
        {
            _noticeLeft = 0f;
            if (_noticeLabel != null)
            {
                _noticeLabel.gameObject.SetActive(false);
            }
        }

        private void EnsureNotice()
        {
            if (_noticeLabel != null)
            {
                return;
            }

            var go = new GameObject("LockNotice", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 12f);
            rect.sizeDelta = new Vector2(520f, 64f);
            _noticeLabel = go.GetComponent<Text>();
            _noticeLabel.font = UiFontCatalog.Body;
            _noticeLabel.fontSize = 22;
            _noticeLabel.alignment = TextAnchor.MiddleCenter;
            _noticeLabel.color = new Color(0.95f, 0.93f, 0.88f, 1f);
            _noticeLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _noticeLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _noticeLabel.raycastTarget = false;
            go.SetActive(false);
        }

        private static Sprite _lockSprite;

        private static Sprite LockSprite
        {
            get
            {
                if (_lockSprite != null)
                {
                    return _lockSprite;
                }

                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                var ink = new Color32(230, 230, 236, 255);
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var body = x >= 14 && x < 50 && y >= 6 && y < 34;
                        var dx = x - 31.5f;
                        var dy = y - 38f;
                        var dist = Mathf.Sqrt(dx * dx + dy * dy);
                        var shackle = y >= 34 && dist >= 10f && dist <= 16f;
                        pixels[y * size + x] = body || shackle ? ink : new Color32(0, 0, 0, 0);
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                tex.filterMode = FilterMode.Bilinear;
                _lockSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
                return _lockSprite;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
        }

        private void ApplyVisual(float pulse)
        {
            Assign(plate, ResolvePlateSprite());
            if (_locked)
            {
                SetRgb(plate, 0.42f, 0.42f, 0.46f, 1f);
                SetRgb(baseLayer, 0.45f, 0.45f, 0.48f, 1f);
                SetRgb(gradient, 0.35f, 0.35f, 0.38f, 0.12f);
                SetRgb(glass, 0.55f, 0.55f, 0.58f, 0.08f);
                SetAlpha(shadow, 0.55f);
                SetAlpha(border, 0.2f);
                SetAlpha(decoLeft, 0.35f);
                SetAlpha(decoRight, 0.35f);
                SetRgb(textLayer, 0.62f, 0.62f, 0.66f, 1f);
                if (titleLabel != null)
                {
                    titleLabel.color = new Color(0.72f, 0.72f, 0.76f, 1f);
                }

                if (subtitleLabel != null)
                {
                    subtitleLabel.color = new Color(0.62f, 0.62f, 0.66f, 0.9f);
                }

                SetAlpha(glow, 0f);
                return;
            }

            SetRgb(plate, 1f, 1f, 1f, 1f);
            SetRgb(baseLayer, 1f, 1f, 1f, 1f);
            SetRgb(gradient, 0.55f, 0.72f, 1f, Mathf.Lerp(0.05f, _hovered ? 0.12f : 0.08f, pulse));
            SetRgb(glass, 0.85f, 0.93f, 1f, Mathf.Lerp(0.04f, _hovered ? 0.1f : 0.06f, pulse));
            SetAlpha(shadow, _hovered ? 1f : 0.92f);
            SetAlpha(border, Mathf.Lerp(0.22f, _hovered ? 0.5f : 0.32f, pulse));
            SetAlpha(decoLeft, 1f);
            SetAlpha(decoRight, 1f);
            if (titleLabel != null)
            {
                titleLabel.color = Color.white;
            }

            if (subtitleLabel != null)
            {
                subtitleLabel.color = Color.white;
            }

            SetTextAlpha(titleLabel, 1f);
            SetTextAlpha(subtitleLabel, _hovered ? 1f : 0.88f);

            if (glow != null)
            {
                var min = _hovered || _pressed ? glowHoverMin : glowIdleMin;
                var max = _hovered || _pressed ? glowHoverMax : glowIdleMax;
                SetAlpha(glow, Mathf.Lerp(min, max, pulse));
            }
        }

        private void OrbitParticles()
        {
            if (_particleRoot == null)
            {
                return;
            }

            if (_locked)
            {
                for (var i = 0; i < ParticleCount; i += 1)
                {
                    SetAlpha(_bits[i], 0f);
                }

                return;
            }

            var orbitRect = _layoutTemplate != null ? _layoutTemplate.rect : ((RectTransform)transform).rect;
            if (orbitRect.width <= 1f || orbitRect.height <= 1f)
            {
                return;
            }

            var rx = orbitRect.width * 0.5f;
            var ry = orbitRect.height * 0.5f;
            for (var i = 0; i < ParticleCount; i += 1)
            {
                var bit = _bits[i];
                if (bit == null)
                {
                    continue;
                }

                var ang = _phase * particleOrbitSpeed + _bitPhase[i];
                var r = _bitRadius[i];
                bit.rectTransform.anchoredPosition = new Vector2(
                    Mathf.Cos(ang) * rx * r,
                    Mathf.Sin(ang * 1.15f) * ry * r);
                var a = 0.35f + 0.4f * (0.5f + 0.5f * Mathf.Sin(_phase * 2.1f + i));
                if (_hovered)
                {
                    a = Mathf.Min(1f, a + 0.2f);
                }

                SetAlpha(bit, a);
            }
        }

        public void ResolveRefs()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
            }

            shadow = shadow != null ? shadow : FindLayer("Layer_Shadow");
            baseLayer = baseLayer != null ? baseLayer : FindLayer("Layer_Base");
            gradient = gradient != null ? gradient : FindLayer("Layer_Gradient");
            glass = glass != null ? glass : FindLayer("Layer_Glass");
            border = border != null ? border : FindLayer("Layer_Border");
            decoLeft = decoLeft != null ? decoLeft : FindLayer("Layer_DecoLeft");
            decoRight = decoRight != null ? decoRight : FindLayer("Layer_DecoRight");
            textLayer = textLayer != null ? textLayer : FindLayer("Layer_Text");
            titleLabel = titleLabel != null ? titleLabel : FindText("TitleLabel");
            subtitleLabel = subtitleLabel != null ? subtitleLabel : FindText("SubtitleLabel");
            plate = plate != null ? plate : FindLayer("Layer_Plate");
            glow = glow != null ? glow : FindLayer("Layer_Glow");
            _layoutTemplate = ResolveLayoutTemplate();
        }

        private RectTransform ResolveLayoutTemplate()
        {
            if (plate != null)
            {
                return plate.rectTransform;
            }

            if (shadow != null)
            {
                return shadow.rectTransform;
            }

            return transform as RectTransform;
        }

        private void EnsurePack()
        {
            _layoutTemplate = ResolveLayoutTemplate();
            shadow = EnsureLayer("Layer_Shadow", shadow);
            plate = EnsureLayer("Layer_Plate", plate);
            glow = glow != null ? glow : FindLayer("Layer_Glow");
            if (plate != null)
            {
                plate.gameObject.SetActive(true);
                plate.preserveAspect = true;
            }

            EnsureTextRoot();
            EnsureTextLabels();
            HideUnusedOverlays();
            EnsureParticleBits();
        }

        private void HideUnusedOverlays()
        {
            HideLayer(FindLayer("Layer_Base"));
            HideLayer(FindLayer("Layer_Gradient"));
            HideLayer(FindLayer("Layer_Glass"));
            HideLayer(FindLayer("Layer_Border"));
            HideLayer(FindLayer("Layer_DecoLeft"));
            HideLayer(FindLayer("Layer_DecoRight"));
            HideLayer(glow != null ? glow : FindLayer("Layer_Glow"));
            HideLayer(textLayer != null ? textLayer : FindLayer("Layer_Text"));
            HideLayer(FindLayer("Layer_Wave"));
            HideLayer(FindLayer("Layer_Scanline"));
            HideLayer(FindLayer("Layer_Particles"));
        }

        private void EnsureTextRoot()
        {
            if (transform.Find("TextRoot") != null)
            {
                return;
            }

            var textRootGo = new GameObject("TextRoot", typeof(RectTransform));
            textRootGo.transform.SetParent(transform, false);
            var textRoot = textRootGo.GetComponent<RectTransform>();
            StretchFull(textRoot);
            textRoot.anchoredPosition = new Vector2(34.3f, -46.7f);
            SeedLabel(textRoot, "TitleLabel", "Resonance Dive", 60, new Vector2(0.38f, 0.42f), new Vector2(0.88f, 0.82f), new Vector2(-52f, -1f));
            SeedLabel(textRoot, "SubtitleLabel", "ENTER THE OTHER SIDE", 11, new Vector2(0.38f, 0.2f), new Vector2(0.88f, 0.42f), new Vector2(32f, 31f));
        }

        private void EnsureTextLabels()
        {
            if (titleLabel == null)
            {
                titleLabel = FindText("TitleLabel");
            }

            if (subtitleLabel == null)
            {
                subtitleLabel = FindText("SubtitleLabel");
            }
        }

        private void ApplyFonts()
        {
            if (titleLabel != null)
            {
                var titleSize = titleLabel.fontSize > 0 ? titleLabel.fontSize : -1;
                UiFontCatalog.Apply(titleLabel, UiFontRole.Display, titleSize);
                titleLabel.raycastTarget = false;
            }

            if (subtitleLabel != null)
            {
                var subtitleSize = subtitleLabel.fontSize > 0 ? subtitleLabel.fontSize : -1;
                UiFontCatalog.Apply(subtitleLabel, UiFontRole.Body, subtitleSize);
                subtitleLabel.raycastTarget = false;
            }
        }

        private void EnsureParticleBits()
        {
            var root = transform.Find("ParticleRoot") as RectTransform;
            if (root == null)
            {
                var visual = transform.Find("Visual");
                root = visual != null ? visual.Find("ParticleRoot") as RectTransform : null;
            }

            if (root == null)
            {
                var go = new GameObject("ParticleRoot", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                root = go.GetComponent<RectTransform>();
                if (_layoutTemplate != null)
                {
                    CopyLayout(_layoutTemplate, root);
                }
            }

            root.gameObject.SetActive(true);
            _particleRoot = root;
            var textRoot = transform.Find("TextRoot");
            if (textRoot != null)
            {
                root.SetSiblingIndex(textRoot.GetSiblingIndex());
            }
            for (var i = 0; i < ParticleCount; i += 1)
            {
                var name = "Bit_" + i;
                var child = root.Find(name);
                if (child == null)
                {
                    var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(root, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(ParticleBitSize, ParticleBitSize);
                    var img = go.GetComponent<Image>();
                    img.raycastTarget = false;
                    img.preserveAspect = true;
                    child = rt;
                }

                _bits[i] = child.GetComponent<Image>();
                var bitRt = child as RectTransform;
                if (bitRt != null
                    && Mathf.Abs(bitRt.sizeDelta.x - 100f) < 0.51f
                    && Mathf.Abs(bitRt.sizeDelta.y - 100f) < 0.51f)
                {
                    bitRt.sizeDelta = new Vector2(ParticleBitSize, ParticleBitSize);
                }

                if (_bits[i] != null)
                {
                    _bits[i].preserveAspect = true;
                    _bits[i].raycastTarget = false;
                }

                _bitPhase[i] = i * (Mathf.PI * 2f / ParticleCount);
                _bitRadius[i] = 0.68f + (i % 5) * 0.07f;
            }
        }

        private Image EnsureLayer(string name, Image current)
        {
            if (current != null)
            {
                return current;
            }

            var found = FindLayer(name);
            if (found != null)
            {
                return found;
            }

            return CreateLayer(transform, name);
        }

        private static void HideLayer(Image image)
        {
            if (image != null)
            {
                image.gameObject.SetActive(false);
            }
        }

        private Image FindLayer(string name)
        {
            var direct = transform.Find(name);
            if (direct != null)
            {
                return direct.GetComponent<Image>();
            }

            var visual = transform.Find("Visual");
            if (visual != null)
            {
                var nested = visual.Find(name);
                if (nested != null)
                {
                    return nested.GetComponent<Image>();
                }

                var mask = visual.Find("FaceMask");
                var underMask = mask != null ? mask.Find(name) : null;
                if (underMask != null)
                {
                    return underMask.GetComponent<Image>();
                }
            }

            var shadowRoot = transform.Find("ShadowRoot");
            var underShadow = shadowRoot != null ? shadowRoot.Find(name) : null;
            return underShadow != null ? underShadow.GetComponent<Image>() : null;
        }

        public void AssignMissingSprites()
        {
            ResolveRefs();
            EnsureParticleBits();
            ApplySprites();
            HideLayer(FindLayer("Layer_Particles"));
        }

        public void PreviewState(bool hovered, bool pressed)
        {
            _hovered = hovered;
            _pressed = pressed;
            ApplyVisual(0f);
        }

        public void EditorTick(float dt)
        {
            if (Application.isPlaying)
            {
                return;
            }

            _phase += dt;
            ApplyVisual((Mathf.Sin(_phase * pulseSpeed) + 1f) * 0.5f);
        }

        private void ApplySprites()
        {
            _stateNormal = LoadSprite("state_normal");
            _stateHover = LoadSprite("state_hover");
            _statePressed = LoadSprite("state_pressed");
            Assign(plate, ResolvePlateSprite());
            Assign(shadow, LoadSprite("12_Shadow"));
            Assign(baseLayer, LoadSprite("01_Base"));
            Assign(gradient, LoadSprite("04_Gradient"));
            Assign(glass, LoadSprite("03_Glass"));
            Assign(border, LoadSprite("02_Border"));
            Assign(decoLeft, LoadSprite("05_Deco_Left"));
            Assign(decoRight, LoadSprite("06_Deco_Right"));
            Assign(glow, LoadSprite("08_Glow"));
            for (var i = 0; i < ParticleCount; i += 1)
            {
                Assign(_bits[i], LoadSprite("09_p" + i));
            }
        }

        private Sprite ResolvePlateSprite()
        {
            if (_pressed && _statePressed != null)
            {
                return _statePressed;
            }

            if (_hovered && _stateHover != null)
            {
                return _stateHover;
            }

            return _stateNormal;
        }

        private static void SetRgb(Image image, float r, float g, float b, float a)
        {
            if (image == null)
            {
                return;
            }

            image.color = new Color(r, g, b, a);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            var c = image.color;
            c.a = alpha;
            image.color = c;
        }

        private static void SetTextAlpha(Text text, float alpha)
        {
            if (text == null)
            {
                return;
            }

            var c = text.color;
            c.a = alpha;
            text.color = c;
        }

        private Text FindText(string name)
        {
            var direct = transform.Find(name)?.GetComponent<Text>();
            if (direct != null)
            {
                return direct;
            }

            var root = transform.Find("TextRoot");
            return root != null ? FindTextUnder(root, name) : null;
        }

        private static Text FindTextUnder(Transform parent, string name)
        {
            var child = parent.Find(name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static void Assign(Image image, Sprite sprite)
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

            image.enabled = true;
            image.sprite = sprite;
        }

        private static Sprite LoadSprite(string fileNameNoExt)
        {
            var fromResources = Resources.Load<Sprite>($"{ResourceFolder}/{fileNameNoExt}");
            if (fromResources != null)
            {
                return fromResources;
            }

            var all = Resources.LoadAll<Sprite>($"{ResourceFolder}/{fileNameNoExt}");
            return all != null && all.Length > 0 ? all[0] : null;
        }

        private static ResonanceDiveButton Create(Transform parent, bool fillParent)
        {
            var go = new GameObject(ObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            if (fillParent)
            {
                StretchFull(rect);
                rect.anchoredPosition = new Vector2(0f, 68f);
            }

            var hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;

            // Layers must exist before AddComponent: Awake runs immediately on an active
            // parent and also seeds them. Seeding afterwards stacks a second, spriteless
            // plate that draws as a solid white rectangle over the button art.
            SeedDefaultLayers(rect);
            var fx = go.AddComponent<ResonanceDiveButton>();
            fx.button = button;
            fx._group = go.GetComponent<CanvasGroup>();
            return fx;
        }

        private static void SeedDefaultLayers(RectTransform root)
        {
            var shadow = SeedImageLayer(root, "Layer_Shadow");
            shadow.color = new Color(1f, 1f, 1f, 0.55f);
            var plate = SeedImageLayer(root, "Layer_Plate");
            plate.preserveAspect = true;
        }

        private static Image SeedImageLayer(RectTransform root, string name)
        {
            var existing = root.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Image>();
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(root, false);
            StretchFull(go.GetComponent<RectTransform>());
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            image.enabled = false;
            return image;
        }

        private static void SeedLabel(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = anchoredPosition;
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = name == "SubtitleLabel"
                ? new Color(0.2f, 0.75f, 1f, 0.88f)
                : Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            UiFontCatalog.Apply(text, name == "TitleLabel" ? UiFontRole.Display : UiFontRole.Body, fontSize);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Image CreateLayer(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            var template = _layoutTemplate != null ? _layoutTemplate : parent as RectTransform;
            if (template != null)
            {
                CopyLayout(template, rect);
            }

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            var templateImage = template != null ? template.GetComponent<Image>() : null;
            if (templateImage != null)
            {
                image.preserveAspect = templateImage.preserveAspect;
            }

            return image;
        }

        private static void CopyLayout(RectTransform from, RectTransform to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
            to.offsetMin = from.offsetMin;
            to.offsetMax = from.offsetMax;
            to.localRotation = from.localRotation;
            to.localScale = from.localScale;
        }
    }
}
