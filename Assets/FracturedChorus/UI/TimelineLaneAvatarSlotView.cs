using System;
using FracturedChorus.Combat.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class TimelineLaneAvatarSlotView : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color SelectionTint = new Color(1f, 0.55f, 1f, 1f);

        private Image _avatar;
        private Image _frameRing;
        private Image _selectionRing;
        private Sprite _ringSprite;
        private CombatUnit _unit;
        private Action<CombatUnit> _onClicked;

        public CombatUnit Unit => _unit;

        private void Awake()
        {
            EnsureBuilt();
        }

        public void SetRingSprite(Sprite ringSprite)
        {
            _ringSprite = ringSprite;
            EnsureBuilt();
            ApplyRingSprites();
        }

        public void Bind(CombatUnit unit, Action<CombatUnit> onClicked)
        {
            _unit = unit;
            _onClicked = onClicked;
            EnsureBuilt();
            RefreshVisual();
        }

        public void SetSelected(bool selected)
        {
            EnsureBuilt();
            if (_selectionRing != null)
            {
                _selectionRing.enabled = selected;
            }

            if (_frameRing != null && _ringSprite != null)
            {
                _frameRing.color = selected
                    ? new Color(0.55f, 1f, 1f, 1f)
                    : Color.white;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_unit != null)
            {
                _onClicked?.Invoke(_unit);
            }
        }

        private void EnsureBuilt()
        {
            if (_avatar != null)
            {
                ApplyRingSprites();
                return;
            }

            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            _avatar = gameObject.GetComponent<Image>();
            if (_avatar == null)
            {
                _avatar = gameObject.AddComponent<Image>();
            }

            _avatar.sprite = UiCircleSpriteUtil.Circle;
            _avatar.type = Image.Type.Simple;
            _avatar.raycastTarget = true;

            var frameGo = new GameObject("FrameRing", typeof(RectTransform));
            var frameRect = frameGo.GetComponent<RectTransform>();
            frameRect.SetParent(rect, false);
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            _frameRing = frameGo.AddComponent<Image>();
            _frameRing.type = Image.Type.Simple;
            _frameRing.preserveAspect = true;
            _frameRing.raycastTarget = false;
            _frameRing.color = Color.white;

            var ringGo = new GameObject("SelectionRing", typeof(RectTransform));
            var ringRect = ringGo.GetComponent<RectTransform>();
            ringRect.SetParent(rect, false);
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = new Vector2(-4f, -4f);
            ringRect.offsetMax = new Vector2(4f, 4f);
            _selectionRing = ringGo.AddComponent<Image>();
            _selectionRing.type = Image.Type.Simple;
            _selectionRing.preserveAspect = true;
            _selectionRing.color = SelectionTint;
            _selectionRing.raycastTarget = false;
            _selectionRing.enabled = false;

            ApplyRingSprites();
        }

        private void ApplyRingSprites()
        {
            if (_frameRing != null)
            {
                _frameRing.sprite = _ringSprite != null ? _ringSprite : UiCircleSpriteUtil.Circle;
                _frameRing.enabled = true;
            }

            if (_selectionRing != null)
            {
                _selectionRing.sprite = _ringSprite != null ? _ringSprite : UiCircleSpriteUtil.Circle;
            }
        }

        private void RefreshVisual()
        {
            if (_avatar == null || _unit == null)
            {
                return;
            }

            var aliveAlpha = _unit.IsAlive ? 1f : 0.35f;
            if (_unit.TimelineAvatarSprite != null)
            {
                _avatar.sprite = _unit.TimelineAvatarSprite;
                _avatar.preserveAspect = true;
                _avatar.color = new Color(1f, 1f, 1f, aliveAlpha);
            }
            else
            {
                _avatar.sprite = UiCircleSpriteUtil.Circle;
                _avatar.preserveAspect = false;
                var tint = _unit.PlaceholderColor;
                _avatar.color = new Color(tint.r, tint.g, tint.b, aliveAlpha);
            }

            if (_frameRing != null)
            {
                var c = _frameRing.color;
                _frameRing.color = new Color(c.r, c.g, c.b, aliveAlpha);
            }
        }
    }
}
