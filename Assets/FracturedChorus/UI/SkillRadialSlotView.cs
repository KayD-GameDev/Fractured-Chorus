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
        private const int LabelFontSize = 20;

        private static readonly Color IdleColor = new Color(0.16f, 0.16f, 0.22f, 0.96f);
        private static readonly Color HighlightColor = new Color(0.95f, 0.62f, 0.25f, 1f);
        private static readonly Color RingColor = new Color(0.75f, 0.8f, 0.95f, 1f);

        private RectTransform _rect;
        private Image _background;
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

            _label = transform.Find("Label")?.GetComponent<Text>();
            if (_label == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform));
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.SetParent(_rect, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(2f, 2f);
                labelRect.offsetMax = new Vector2(-2f, -2f);
                _label = labelGo.AddComponent<Text>();
                _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _label.alignment = TextAnchor.MiddleCenter;
                _label.horizontalOverflow = HorizontalWrapMode.Wrap;
                _label.verticalOverflow = VerticalWrapMode.Overflow;
                _label.raycastTarget = false;
            }

            ApplyCircleStyle();
            ApplyLabelStyle();

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

            if (_label != null)
            {
                _label.text = BuildLabel(skill, keyHint);
            }
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

        private static string BuildLabel(SkillDefinitionSO skill, string keyHint)
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
