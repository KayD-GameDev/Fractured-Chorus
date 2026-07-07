using System;
using FracturedChorus.Combat.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class TimelineLaneAvatarSlotView : MonoBehaviour, IPointerClickHandler
    {
        private Image _avatar;
        private Image _selectionRing;
        private CombatUnit _unit;
        private Action<CombatUnit> _onClicked;

        public CombatUnit Unit => _unit;

        private void Awake()
        {
            EnsureBuilt();
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
                return;
            }

            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            var ringGo = new GameObject("SelectionRing", typeof(RectTransform));
            var ringRect = ringGo.GetComponent<RectTransform>();
            ringRect.SetParent(rect, false);
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = new Vector2(-3f, -3f);
            ringRect.offsetMax = new Vector2(3f, 3f);
            _selectionRing = ringGo.AddComponent<Image>();
            _selectionRing.sprite = UiCircleSpriteUtil.Circle;
            _selectionRing.type = Image.Type.Simple;
            _selectionRing.color = new Color(1f, 1f, 1f, 0.95f);
            _selectionRing.raycastTarget = false;
            _selectionRing.enabled = false;

            _avatar = gameObject.GetComponent<Image>();
            if (_avatar == null)
            {
                _avatar = gameObject.AddComponent<Image>();
            }

            _avatar.sprite = UiCircleSpriteUtil.Circle;
            _avatar.type = Image.Type.Simple;
            _avatar.raycastTarget = true;
        }

        private void RefreshVisual()
        {
            if (_avatar == null || _unit == null)
            {
                return;
            }

            var tint = _unit.PlaceholderColor;
            _avatar.color = new Color(tint.r, tint.g, tint.b, _unit.IsAlive ? 1f : 0.35f);
        }
    }
}
