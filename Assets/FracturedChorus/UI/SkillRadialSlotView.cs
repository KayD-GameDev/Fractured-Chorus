using System;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>Hướng radial của một ô kỹ năng — map sang phím điều hướng.</summary>
    public enum SkillRadialDirection
    {
        Top,    // W
        Left,   // A
        Right   // D
    }

    /// <summary>
    /// Một ô kỹ năng tròn trên bảng radial. Click highlight; W/A/D hoặc kéo vào timeline để gán.
    /// </summary>
    public class SkillRadialSlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const int LabelFontSize = 12;
        private const int FallbackLabelFontSize = 20;

        /// <summary>
        /// Pixel design khung skill (W×H):
        /// Basic 315×328 · Skill 322×325 · Ultimate 329×359.
        /// </summary>
        public static readonly Vector2 FrameDesignBasic = new Vector2(315f, 328f);
        public static readonly Vector2 FrameDesignSkill = new Vector2(322f, 325f);
        public static readonly Vector2 FrameDesignUltimate = new Vector2(329f, 359f);

        /// <summary>Hộp design chuẩn để scale đồng nhất (lấy max W/H của 3 khung).</summary>
        private static readonly Vector2 FrameDesignRef = new Vector2(329f, 359f);

        private static readonly Color IdleColor = new Color(0.16f, 0.16f, 0.22f, 0.96f);
        private static readonly Color HighlightColor = new Color(0.95f, 0.62f, 0.25f, 1f);
        private static readonly Color RingColor = new Color(0.75f, 0.8f, 0.95f, 1f);

        private RectTransform _rect;
        private Image _background;
        private Image _icon;
        private Image _frame;
        private Text _label;
        private Action _onSelect;
        private SkillPanelUIView _panel;
        private bool _dragging;

        public SkillRadialDirection Direction { get; private set; }
        public SkillDefinitionSO Skill { get; private set; }
        public RectTransform Rect => _rect;
        public bool HasSkill => Skill != null;

        public void WireFromScene(SkillRadialDirection direction)
        {
            Direction = direction;
            _rect = transform as RectTransform;
            _background = GetComponent<Image>();
            if (_background == null)
            {
                _background = gameObject.AddComponent<Image>();
            }

            _icon = EnsureCircularIconImage();

            _label = transform.Find("Label")?.GetComponent<Text>();
            if (_label == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform));
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.SetParent(_rect, false);
                labelRect.anchorMin = new Vector2(1f, 1f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(1f, 1f);
                labelRect.anchoredPosition = new Vector2(-4f, -2f);
                labelRect.sizeDelta = new Vector2(28f, 18f);
                _label = labelGo.AddComponent<Text>();
                _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _label.alignment = TextAnchor.UpperRight;
                _label.horizontalOverflow = HorizontalWrapMode.Overflow;
                _label.verticalOverflow = VerticalWrapMode.Overflow;
                _label.raycastTarget = false;
            }

            ApplyCircleStyle();
            ApplyLabelStyle();
            EnsureFrameVisual();

            var button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(Select);
            }
        }

        public void Bind(SkillDefinitionSO skill, string keyHint, Action onSelect, SkillPanelUIView panel)
        {
            Skill = skill;
            _onSelect = onSelect;
            _panel = panel;
            SetHighlight(false);
            ApplySkillPresentation(skill, keyHint);
        }

        /// <summary>Runtime fallback when scene slots are missing.</summary>
        public void Build(RectTransform parent, Vector2 anchoredPos, float size,
            SkillRadialDirection direction, SkillDefinitionSO skill, string keyHint, Action onSelect,
            SkillPanelUIView panel = null)
        {
            Direction = direction;
            Skill = skill;
            _onSelect = onSelect;
            _panel = panel;

            _rect = gameObject.GetComponent<RectTransform>();
            if (_rect == null)
            {
                _rect = gameObject.AddComponent<RectTransform>();
            }

            _rect.SetParent(parent, false);
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(size, size);
            _rect.anchoredPosition = anchoredPos;

            if (transform.Find("Frame") == null)
            {
                var frame = new GameObject("Frame", typeof(RectTransform));
                var frameRect = frame.GetComponent<RectTransform>();
                frameRect.SetParent(_rect, false);
                frameRect.anchorMin = Vector2.zero;
                frameRect.anchorMax = Vector2.one;
                frameRect.offsetMin = new Vector2(-6f, -6f);
                frameRect.offsetMax = new Vector2(6f, 6f);
                var frameImage = frame.AddComponent<Image>();
                frameImage.sprite = UiCircleSpriteUtil.Circle;
                frameImage.type = Image.Type.Simple;
                frameImage.color = new Color(0.92f, 0.78f, 0.42f, 0.95f);
                frameImage.raycastTarget = false;
            }

            if (transform.Find("Ring") == null)
            {
                var ring = new GameObject("Ring", typeof(RectTransform));
                var ringRect = ring.GetComponent<RectTransform>();
                ringRect.SetParent(_rect, false);
                ringRect.anchorMin = Vector2.zero;
                ringRect.anchorMax = Vector2.one;
                ringRect.offsetMin = new Vector2(-3f, -3f);
                ringRect.offsetMax = new Vector2(3f, 3f);
                var ringImage = ring.AddComponent<Image>();
                ringImage.sprite = UiCircleSpriteUtil.Circle;
                ringImage.type = Image.Type.Simple;
                ringImage.color = RingColor;
                ringImage.raycastTarget = false;
            }

            _background = gameObject.GetComponent<Image>();
            if (_background == null)
            {
                _background = gameObject.AddComponent<Image>();
            }

            _background.sprite = UiCircleSpriteUtil.Circle;
            _background.type = Image.Type.Simple;
            _background.color = IdleColor;
            _background.raycastTarget = true;

            var button = gameObject.GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(Select);
            }

            WireFromScene(direction);
            Bind(skill, keyHint, onSelect, panel);
        }

        private void ApplySkillPresentation(SkillDefinitionSO skill, string keyHint)
        {
            EnsureCircularIconImage();
            EnsureFrameVisual();
            ApplySkillFrame(skill);
            var hasIcon = skill != null && skill.icon != null;

            if (_icon != null)
            {
                if (hasIcon)
                {
                    _icon.sprite = skill.icon;
                    _icon.enabled = true;
                    _icon.color = Color.white;
                    FitArtInsideIcon(_icon.sprite);
                }
                else
                {
                    // Keep authored/migrated sprite if skill.icon failed to resolve.
                    _icon.enabled = _icon.sprite != null;
                    if (_icon.enabled)
                    {
                        FitArtInsideIcon(_icon.sprite);
                    }
                }
            }

            var iconRoot = transform.Find("Icon");
            if (iconRoot != null)
            {
                iconRoot.gameObject.SetActive(skill != null && (_icon != null && _icon.enabled));
            }

            EnsureSlotRaycastTargets();

            if (_label == null)
            {
                return;
            }

            if (_icon != null && _icon.enabled && _icon.sprite != null)
            {
                _label.fontSize = LabelFontSize;
                _label.alignment = TextAnchor.UpperRight;
                _label.text = skill != null ? $"[{keyHint}]" : $"[{keyHint}]\n—";
            }
            else
            {
                _label.fontSize = FallbackLabelFontSize;
                _label.alignment = TextAnchor.MiddleCenter;
                _label.text = BuildFallbackLabel(skill, keyHint);
            }
        }

        /// <summary>
        /// Art giữ tỷ lệ gốc của sprite, fit-inside vùng Icon (theo Frame content).
        /// </summary>
        private void FitArtInsideIcon(Sprite sprite)
        {
            if (_icon == null || sprite == null)
            {
                return;
            }

            var artRect = _icon.rectTransform;
            var iconRoot = transform.Find("Icon") as RectTransform;
            if (artRect == null || iconRoot == null)
            {
                return;
            }

            artRect.anchorMin = new Vector2(0.5f, 0.5f);
            artRect.anchorMax = new Vector2(0.5f, 0.5f);
            artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.anchoredPosition = Vector2.zero;
            artRect.localScale = Vector3.one;

            Canvas.ForceUpdateCanvases();
            var parentSize = iconRoot.rect.size;
            if (parentSize.x < 1f || parentSize.y < 1f)
            {
                // Layout chưa kịp — ước lượng từ slot × inset Icon mặc định.
                var slot = _rect != null ? _rect.rect.size : new Vector2(96f, 96f);
                if (slot.x < 1f || slot.y < 1f)
                {
                    slot = _rect != null ? _rect.sizeDelta : new Vector2(96f, 96f);
                }

                parentSize = slot * 0.84f;
            }

            var spriteSize = sprite.rect.size;
            if (spriteSize.x < 0.01f || spriteSize.y < 0.01f)
            {
                return;
            }

            var aspect = spriteSize.x / spriteSize.y;
            float width;
            float height;
            if (parentSize.x / parentSize.y > aspect)
            {
                height = parentSize.y;
                width = height * aspect;
            }
            else
            {
                width = parentSize.x;
                height = width / aspect;
            }

            artRect.sizeDelta = new Vector2(width, height);
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
        }

        /// <summary>Chỉ root slot nhận drag; Frame/Icon/Art/Label không chắn raycast.</summary>
        private void EnsureSlotRaycastTargets()
        {
            if (_background != null)
            {
                _background.raycastTarget = true;
            }

            if (_frame != null)
            {
                _frame.raycastTarget = false;
            }

            if (_icon != null)
            {
                _icon.raycastTarget = false;
            }

            var iconRoot = transform.Find("Icon")?.GetComponent<Image>();
            if (iconRoot != null)
            {
                iconRoot.raycastTarget = false;
            }

            var ring = transform.Find("Ring")?.GetComponent<Image>();
            if (ring != null)
            {
                ring.raycastTarget = false;
            }

            if (_label != null)
            {
                _label.raycastTarget = false;
            }
        }

        private static string BuildFallbackLabel(SkillDefinitionSO skill, string keyHint)
        {
            if (skill == null)
            {
                return $"[{keyHint}]\n—";
            }

            return $"[{keyHint}]\n{SkillUiNames.GetDisplayName(skill)}";
        }

        public void SetHighlight(bool on)
        {
            if (_background != null)
            {
                _background.color = on ? HighlightColor : IdleColor;
            }
        }

        public void Select()
        {
            if (Skill == null)
            {
                return;
            }

            _onSelect?.Invoke();
        }

        /// <summary>
        /// Ensure Frame exists trên art. Không đè Rect/sprite/color đã author (template/scene).
        /// </summary>
        private void EnsureFrameVisual()
        {
            if (_rect == null)
            {
                _rect = transform as RectTransform;
            }

            if (_rect == null)
            {
                return;
            }

            var frameTransform = transform.Find("Frame") as RectTransform;
            var created = false;
            if (frameTransform == null)
            {
                var frame = new GameObject("Frame", typeof(RectTransform));
                frameTransform = frame.GetComponent<RectTransform>();
                frameTransform.SetParent(_rect, false);
                created = true;
            }

            var frameImage = frameTransform.GetComponent<Image>();
            if (frameImage == null)
            {
                frameImage = frameTransform.gameObject.AddComponent<Image>();
                created = true;
            }

            // Chỉ apply default khi mới tạo — giữ Hierarchy/template đã chỉnh.
            if (created || frameImage.sprite == null)
            {
                if (created)
                {
                    frameTransform.anchorMin = Vector2.zero;
                    frameTransform.anchorMax = Vector2.one;
                    frameTransform.pivot = new Vector2(0.5f, 0.5f);
                    frameTransform.anchoredPosition = Vector2.zero;
                    frameTransform.sizeDelta = Vector2.zero;
                    frameTransform.offsetMin = new Vector2(-6f, -6f);
                    frameTransform.offsetMax = new Vector2(6f, 6f);
                    frameImage.color = new Color(0.92f, 0.78f, 0.42f, 0.95f);
                }

                if (frameImage.sprite == null)
                {
                    frameImage.sprite = UiCircleSpriteUtil.Circle;
                    frameImage.type = Image.Type.Simple;
                }
            }

            frameImage.raycastTarget = false;
            _frame = frameImage;
            SkillSlotChromeSync.ApplySiblingOrder(_rect);
        }

        /// <summary>
        /// Gán khung riêng từ <see cref="SkillDefinitionSO.frame"/> (trên art).
        /// Null → giữ sprite Frame từ SkillSlot_Template / Hierarchy.
        /// Size Frame theo tỷ lệ design: Basic 315×328 · Skill 322×325 · Ult 329×359.
        /// </summary>
        private void ApplySkillFrame(SkillDefinitionSO skill)
        {
            if (_frame == null)
            {
                _frame = transform.Find("Frame")?.GetComponent<Image>();
            }

            if (_frame == null)
            {
                return;
            }

            if (skill != null && skill.frame != null)
            {
                _frame.sprite = skill.frame;
                _frame.type = Image.Type.Simple;
                _frame.color = Color.white;
                _frame.enabled = true;
                _frame.gameObject.SetActive(true);
            }

            FitFrameToSlotKind(skill != null ? skill.slotKind : SkillSlotKind.BasicAttack);
            _frame.raycastTarget = false;
            SkillSlotChromeSync.ApplySiblingOrder(_rect != null ? _rect : transform as RectTransform);
        }

        /// <summary>Design pixel size của khung theo <see cref="SkillSlotKind"/>.</summary>
        public static Vector2 GetFrameDesignSize(SkillSlotKind kind)
        {
            return kind switch
            {
                SkillSlotKind.Skill => FrameDesignSkill,
                SkillSlotKind.Ultimate => FrameDesignUltimate,
                _ => FrameDesignBasic
            };
        }

        /// <summary>
        /// Fit Frame vào slot theo pixel design của <paramref name="kind"/>.
        /// Scale đồng nhất so với Ultimate (329×359) để Basic/Skill/Ult giữ đúng tỷ lệ tương đối.
        /// </summary>
        public static void FitFrameRectToKind(RectTransform frameRect, RectTransform slotRect, SkillSlotKind kind)
        {
            if (frameRect == null)
            {
                return;
            }

            var design = GetFrameDesignSize(kind);

            Canvas.ForceUpdateCanvases();
            var slotSize = slotRect != null ? slotRect.rect.size : Vector2.zero;
            if (slotSize.x < 1f || slotSize.y < 1f)
            {
                slotSize = slotRect != null ? slotRect.sizeDelta : new Vector2(96f, 96f);
            }

            if (slotSize.x < 1f || slotSize.y < 1f)
            {
                slotSize = new Vector2(96f, 96f);
            }

            // Bleed nhẹ quanh slot (~6px mỗi cạnh); scale theo hộp design max (Ult).
            var fitBox = slotSize + new Vector2(12f, 12f);
            var scale = Mathf.Min(
                fitBox.x / Mathf.Max(0.01f, FrameDesignRef.x),
                fitBox.y / Mathf.Max(0.01f, FrameDesignRef.y));
            var width = design.x * scale;
            var height = design.y * scale;

            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = Vector2.zero;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            frameRect.sizeDelta = new Vector2(width, height);
            frameRect.localScale = Vector3.one;
        }

        private void FitFrameToSlotKind(SkillSlotKind kind)
        {
            if (_frame == null)
            {
                return;
            }

            FitFrameRectToKind(_frame.rectTransform, _rect != null ? _rect : transform as RectTransform, kind);
            // Fill đúng hộp design (315×328 / 322×325 / 329×359) — không letterbox theo sprite.
            _frame.preserveAspect = false;
            _frame.type = Image.Type.Simple;
        }

        private void ApplyCircleStyle()
        {
            if (_background != null && _background.sprite == null)
            {
                _background.sprite = UiCircleSpriteUtil.Circle;
                _background.type = Image.Type.Simple;
            }

            var ring = transform.Find("Ring")?.GetComponent<Image>();
            if (ring != null && ring.sprite == null)
            {
                ring.sprite = UiCircleSpriteUtil.Circle;
                ring.type = Image.Type.Simple;
            }

            var frame = transform.Find("Frame")?.GetComponent<Image>();
            if (frame != null && frame.sprite == null)
            {
                frame.sprite = UiCircleSpriteUtil.Circle;
                frame.type = Image.Type.Simple;
            }

            EnsureCircularIconImage();
        }

        /// <summary>
        /// Icon clipped to a circle: Icon (Mask + circle) / Art (skill sprite).
        /// Không đè stretch đã author từ SkillSlot_Template / scene.
        /// </summary>
        private Image EnsureCircularIconImage()
        {
            if (_rect == null)
            {
                _rect = transform as RectTransform;
            }

            var maskRoot = transform.Find("Icon") as RectTransform;
            var createdIcon = false;
            if (maskRoot == null)
            {
                var maskGo = new GameObject("Icon", typeof(RectTransform));
                maskRoot = maskGo.GetComponent<RectTransform>();
                maskRoot.SetParent(_rect, false);
                createdIcon = true;
            }

            if (createdIcon)
            {
                maskRoot.anchorMin = new Vector2(0.08f, 0.08f);
                maskRoot.anchorMax = new Vector2(0.92f, 0.92f);
                maskRoot.offsetMin = Vector2.zero;
                maskRoot.offsetMax = Vector2.zero;
                maskRoot.pivot = new Vector2(0.5f, 0.5f);
            }

            var maskGraphic = maskRoot.GetComponent<Image>();
            if (maskGraphic == null)
            {
                maskGraphic = maskRoot.gameObject.AddComponent<Image>();
            }

            var artRoot = maskRoot.Find("Art") as RectTransform;
            Image artImage = artRoot != null ? artRoot.GetComponent<Image>() : null;

            // Migrate legacy: skill sprite lived on Icon Image itself (before Art child existed).
            Sprite legacySprite = null;
            if (artRoot == null && maskGraphic.sprite != null && maskGraphic.sprite != UiCircleSpriteUtil.Circle)
            {
                legacySprite = maskGraphic.sprite;
            }

            if (maskGraphic.sprite == null || maskGraphic.sprite == legacySprite)
            {
                maskGraphic.sprite = UiCircleSpriteUtil.Circle;
                maskGraphic.type = Image.Type.Simple;
            }

            if (maskGraphic.color.a <= 0.01f)
            {
                maskGraphic.color = Color.white;
            }

            maskGraphic.raycastTarget = false;

            var mask = maskRoot.GetComponent<Mask>();
            if (mask == null)
            {
                mask = maskRoot.gameObject.AddComponent<Mask>();
            }

            mask.showMaskGraphic = false;

            if (artRoot == null)
            {
                var artGo = new GameObject("Art", typeof(RectTransform));
                artRoot = artGo.GetComponent<RectTransform>();
                artRoot.SetParent(maskRoot, false);
                artRoot.anchorMin = new Vector2(0.5f, 0.5f);
                artRoot.anchorMax = new Vector2(0.5f, 0.5f);
                artRoot.pivot = new Vector2(0.5f, 0.5f);
                artRoot.anchoredPosition = Vector2.zero;
                artRoot.sizeDelta = maskRoot.rect.size;
                artImage = artGo.AddComponent<Image>();
                if (legacySprite != null)
                {
                    artImage.sprite = legacySprite;
                }
            }

            if (artImage == null)
            {
                artImage = artRoot.gameObject.AddComponent<Image>();
            }

            artImage.preserveAspect = true;
            artImage.raycastTarget = false;
            _icon = artImage;
            return _icon;
        }

        private void ApplyLabelStyle()
        {
            if (_label == null)
            {
                return;
            }

            if (_label.font == null)
            {
                _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            _label.fontSize = LabelFontSize;
            _label.color = Color.black;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!HasSkill || _panel == null)
            {
                return;
            }

            _dragging = true;
            _panel.BeginSkillDrag(Skill);
            _panel.UpdateSkillDrag(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _panel == null)
            {
                return;
            }

            _panel.UpdateSkillDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging || _panel == null)
            {
                return;
            }

            _dragging = false;
            _panel.EndSkillDrag(Skill, eventData.position);
        }
    }
}
