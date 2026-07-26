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
        private Image _hitTarget;
        private TimelineLaneSkillDragHandle _dragHandle;
        private Image _glow;
        private Image _outline;
        private Text _label;
        private bool _gapAnchorMode;

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

            var outlineGo = new GameObject("Outline", typeof(RectTransform));
            var outlineRect = outlineGo.GetComponent<RectTransform>();
            outlineRect.SetParent(_rect, false);
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = new Vector2(-3f, -3f);
            outlineRect.offsetMax = new Vector2(3f, 3f);
            outlineRect.SetAsFirstSibling();
            _outline = outlineGo.AddComponent<Image>();
            _outline.sprite = UiCircleSpriteUtil.Circle;
            _outline.type = Image.Type.Simple;
            _outline.color = new Color(1f, 1f, 1f, 0.92f);
            _outline.raycastTarget = false;

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

        public void SetGapAnchorMode(bool gapAnchor)
        {
            _gapAnchorMode = gapAnchor;
            ApplyCircleVisibility();
        }

        public void SetContent(CombatUnit unit, SkillDefinitionSO skill)
        {
            if (_background != null && !_gapAnchorMode)
            {
                var tint = unit?.PlaceholderColor ?? Color.gray;
                _background.color = new Color(tint.r, tint.g, tint.b, 0.2f);
            }

            if (_glow != null && !_gapAnchorMode)
            {
                _glow.color = GetGlowColor(skill != null ? skill.glowType : ActionGlowType.Rush);
            }

            if (_label != null)
            {
                var skillName = skill != null ? SkillUiNames.GetDisplayName(skill).ToUpperInvariant() : string.Empty;
                _label.text = string.IsNullOrEmpty(skillName) ? string.Empty : $"▸ {skillName}";
            }

            ApplyCircleVisibility();
        }

        private void ApplyCircleVisibility()
        {
            if (_gapAnchorMode)
            {
                if (_background != null)
                {
                    _background.color = new Color(0f, 0f, 0f, 0f);
                }

                if (_glow != null)
                {
                    _glow.enabled = false;
                }

                if (_outline != null)
                {
                    _outline.enabled = false;
                }

                return;
            }

            if (_glow != null)
            {
                _glow.enabled = true;
            }

            if (_outline != null)
            {
                _outline.enabled = true;
            }
        }

        public void SetPlanningInteractionEnabled(bool enabled)
        {
            EnsureHitTarget();

            if (_hitTarget != null)
            {
                _hitTarget.raycastTarget = enabled;
            }

            if (_dragHandle != null)
            {
                _dragHandle.SetInteractionEnabled(enabled);
            }

            if (_background != null)
            {
                _background.raycastTarget = false;
            }
        }

        public void WireSkillDrag(BeatTimelineUIView timeline, CombatUnit unit, int placementBeat)
        {
            EnsureHitTarget();
            if (_dragHandle == null)
            {
                return;
            }

            var canRelocate = timeline != null && timeline.CanRelocateLaneMarker();
            _dragHandle.Configure(timeline, unit, placementBeat);
            _dragHandle.SetInteractionEnabled(canRelocate);
            if (_hitTarget != null)
            {
                _hitTarget.raycastTarget = canRelocate;
                _hitTarget.enabled = true;
                // Luôn trên cùng trong marker để nhận drag.
                _hitTarget.transform.SetAsLastSibling();
            }

            // Marker layer phải nhận event — background không chắn.
            if (_background != null)
            {
                _background.raycastTarget = false;
            }
        }

        private void EnsureHitTarget()
        {
            if (_hitTarget != null || _rect == null)
            {
                return;
            }

            var hitGo = new GameObject("HitTarget", typeof(RectTransform));
            var hitRect = hitGo.GetComponent<RectTransform>();
            hitRect.SetParent(_rect, false);
            hitRect.anchorMin = new Vector2(0.5f, 0.5f);
            hitRect.anchorMax = new Vector2(0.5f, 0.5f);
            hitRect.pivot = new Vector2(0.5f, 0.5f);
            hitRect.anchoredPosition = Vector2.zero;
            var size = _rect.sizeDelta;
            hitRect.sizeDelta = new Vector2(Mathf.Max(size.x * 2f, 48f), Mathf.Max(size.y * 2f, 48f));
            _hitTarget = hitGo.AddComponent<Image>();
            _hitTarget.sprite = UiCircleSpriteUtil.Circle;
            _hitTarget.type = Image.Type.Simple;
            _hitTarget.color = new Color(1f, 1f, 1f, 0.01f);
            _hitTarget.raycastTarget = false;
            _dragHandle = hitGo.AddComponent<TimelineLaneSkillDragHandle>();
            hitGo.transform.SetAsLastSibling();
        }

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

        public void SetRelocateVisualHidden(bool hidden)
        {
            if (_glow != null)
            {
                _glow.enabled = !hidden && !_gapAnchorMode;
            }

            if (_outline != null)
            {
                _outline.enabled = !hidden && !_gapAnchorMode;
            }

            if (_background != null)
            {
                _background.enabled = !hidden;
            }

            if (_label != null)
            {
                _label.enabled = !hidden;
            }

            if (_hitTarget != null)
            {
                _hitTarget.raycastTarget = !hidden;
            }

            _dragHandle?.SetInteractionEnabled(!hidden);
        }

        public void SetGhost(bool ghost)
        {
            if (_label != null)
            {
                _label.enabled = !ghost;
            }

            if (_gapAnchorMode || _background == null)
            {
                return;
            }

            var alpha = ghost ? 0.45f : 1f;
            var c = _background.color;
            _background.color = new Color(c.r, c.g, c.b, alpha);
        }

        public void SetInvalidPreview(bool invalid)
        {
            if (_background == null || _gapAnchorMode)
            {
                return;
            }

            if (invalid)
            {
                _background.color = new Color(0.85f, 0.2f, 0.2f, 0.55f);
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
