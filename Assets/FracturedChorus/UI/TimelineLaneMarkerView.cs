using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Marker skill của người chơi nằm trên dòng kẻ (lane) của unit trong beat timeline.
    /// Sinh ra kèm animation "bay vào lane" (scale + trượt lên nhẹ).
    /// </summary>
    public class TimelineLaneMarkerView : MonoBehaviour
    {
        private const float SpawnDuration = 0.18f;
        private const float SpawnSlideY = 18f;
        private const float SpawnStartScale = 0.4f;

        private RectTransform _rect;
        private Image _background;
        private Image _glow;
        private Text _label;

        private Vector2 _targetPos;
        private float _animT = 1f;
        private bool _animating;

        public void Build(RectTransform parent, float size)
        {
            _rect = gameObject.GetComponent<RectTransform>();
            if (_rect == null)
            {
                _rect = gameObject.AddComponent<RectTransform>();
            }

            _rect.SetParent(parent, false);
            _rect.anchorMin = new Vector2(0f, 0f);
            _rect.anchorMax = new Vector2(0f, 0f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(size, size);

            var glowGo = new GameObject("Glow", typeof(RectTransform));
            var glowRect = glowGo.GetComponent<RectTransform>();
            glowRect.SetParent(_rect, false);
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-4f, -4f);
            glowRect.offsetMax = new Vector2(4f, 4f);
            _glow = glowGo.AddComponent<Image>();
            _glow.sprite = UiCircleSpriteUtil.Circle;
            _glow.type = Image.Type.Simple;
            _glow.raycastTarget = false;

            _background = gameObject.AddComponent<Image>();
            _background.sprite = UiCircleSpriteUtil.Circle;
            _background.type = Image.Type.Simple;
            _background.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(_rect, false);
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -1f);
            labelRect.sizeDelta = new Vector2(64f, 14f);
            _label = labelGo.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 10;
            _label.alignment = TextAnchor.UpperCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.color = new Color(1f, 1f, 1f, 0.9f);
            _label.raycastTarget = false;
        }

        public void SetContent(CombatUnit unit, SkillDefinitionSO skill)
        {
            if (_background != null)
            {
                var tint = unit?.PlaceholderColor ?? Color.gray;
                _background.color = new Color(tint.r, tint.g, tint.b, 1f);
            }

            if (_glow != null)
            {
                _glow.color = GetGlowColor(skill != null ? skill.glowType : ActionGlowType.Rush);
            }

            if (_label != null)
            {
                _label.text = skill != null ? SkillUiNames.GetDisplayName(skill).ToUpperInvariant() : string.Empty;
            }
        }

        /// <summary>Đặt vị trí trong lane. animate=true → chạy hiệu ứng bay vào.</summary>
        public void SetLanePosition(Vector2 localPos, bool animate)
        {
            _targetPos = localPos;

            if (_rect == null)
            {
                return;
            }

            if (animate)
            {
                _animT = 0f;
                _animating = true;
                _rect.anchoredPosition = localPos + new Vector2(0f, SpawnSlideY);
                _rect.localScale = Vector3.one * SpawnStartScale;
            }
            else
            {
                _animating = false;
                _animT = 1f;
                _rect.anchoredPosition = localPos;
                _rect.localScale = Vector3.one;
            }
        }

        private void Update()
        {
            if (!_animating || _rect == null)
            {
                return;
            }

            _animT += SpawnDuration > 0f ? Time.unscaledDeltaTime / SpawnDuration : 1f;
            var t = Mathf.Clamp01(_animT);
            var ease = 1f - (1f - t) * (1f - t);
            _rect.anchoredPosition = Vector2.Lerp(_targetPos + new Vector2(0f, SpawnSlideY), _targetPos, ease);
            _rect.localScale = Vector3.one * Mathf.Lerp(SpawnStartScale, 1f, ease);

            if (t >= 1f)
            {
                _animating = false;
            }
        }

        public void SetGhost(bool ghost)
        {
            var alpha = ghost ? 0.45f : 1f;
            if (_background != null)
            {
                var c = _background.color;
                _background.color = new Color(c.r, c.g, c.b, alpha);
            }

            if (_label != null)
            {
                _label.enabled = !ghost;
            }
        }

        private static Color GetGlowColor(ActionGlowType glowType)
        {
            return glowType switch
            {
                ActionGlowType.Rush => new Color(0.2f, 0.5f, 1f, 0.7f),
                ActionGlowType.Support => new Color(0.2f, 0.9f, 0.4f, 0.7f),
                ActionGlowType.Guard => new Color(0.9f, 0.8f, 0.2f, 0.7f),
                _ => new Color(1f, 0.25f, 0.15f, 0.7f)
            };
        }
    }
}
