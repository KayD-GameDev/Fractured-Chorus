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

        /// <summary>
        /// Keep scene FrameRing Image sprite when authored; only apply fallback if missing.
        /// </summary>
        public void ApplyFrameSpriteIfMissing(Sprite fallback)
        {
            EnsureBuilt();
            if (_frameRing == null)
            {
                return;
            }

            if (_frameRing.sprite != null)
            {
                _ringSprite = _frameRing.sprite;
                return;
            }

            if (fallback == null)
            {
                return;
            }

            _ringSprite = fallback;
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
            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            if (_avatar == null)
            {
                _avatar = gameObject.GetComponent<Image>();
                if (_avatar == null)
                {
                    _avatar = gameObject.AddComponent<Image>();
                }

                _avatar.type = Image.Type.Simple;
                _avatar.raycastTarget = true;
                // Keep scene/preset sprite; only fill missing Image so hierarchy art wins.
                if (_avatar.sprite == null)
                {
                    _avatar.sprite = UiCircleSpriteUtil.Circle;
                }
            }

            if (_frameRing == null)
            {
                _frameRing = FindFirstChildImage("FrameRing");
            }

            if (_selectionRing == null)
            {
                _selectionRing = FindFirstChildImage("SelectionRing");
            }

            // Scene may already have FrameRing/SelectionRing; never create a second copy.
            PurgeDuplicateNamedChildren("FrameRing", _frameRing);
            PurgeDuplicateNamedChildren("SelectionRing", _selectionRing);

            if (_frameRing == null)
            {
                _frameRing = CreateRingChild(
                    "FrameRing",
                    rect,
                    Vector2.zero,
                    Vector2.zero,
                    Color.white,
                    enabled: true);
            }

            if (_selectionRing == null)
            {
                _selectionRing = CreateRingChild(
                    "SelectionRing",
                    rect,
                    new Vector2(-4f, -4f),
                    new Vector2(4f, 4f),
                    SelectionTint,
                    enabled: false);
            }

            ApplyRingSprites();
        }

        private Image FindFirstChildImage(string childName)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child == null || child.name != childName)
                {
                    continue;
                }

                var image = child.GetComponent<Image>();
                if (image != null)
                {
                    return image;
                }
            }

            return null;
        }

        private void PurgeDuplicateNamedChildren(string childName, Image keep)
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child == null || child.name != childName)
                {
                    continue;
                }

                if (keep != null && child.gameObject == keep.gameObject)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static Image CreateRingChild(
            string name,
            RectTransform parent,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color,
            bool enabled)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var ringRect = go.GetComponent<RectTransform>();
            ringRect.SetParent(parent, false);
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = offsetMin;
            ringRect.offsetMax = offsetMax;
            var image = go.AddComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = color;
            image.enabled = enabled;
            return image;
        }

        private void ApplyRingSprites()
        {
            if (_frameRing != null)
            {
                // Assign caller sprite onto the hierarchy FrameRing Image; do not overwrite
                // with a hardcoded circle when no sprite was provided (keep scene art).
                if (_ringSprite != null)
                {
                    _frameRing.sprite = _ringSprite;
                }

                _frameRing.enabled = true;
                _frameRing.preserveAspect = true;
            }

            if (_selectionRing != null && _ringSprite != null)
            {
                _selectionRing.sprite = _ringSprite;
                _selectionRing.preserveAspect = true;
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
                var tint = _unit.TimelineLaneColor;
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
