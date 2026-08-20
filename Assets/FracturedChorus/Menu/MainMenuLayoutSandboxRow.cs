using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    public sealed class MainMenuLayoutSandboxRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private MainMenuLayoutSandboxMenu menu;
        [SerializeField] private int optionIndex;
        [SerializeField] private Image shard;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite highlightSprite;
        [SerializeField] private bool interactable = true;

        private Button _button;
        private RectTransform _rect;
        private Vector3 _baseScale;
        private bool _highlighted;
        private Coroutine _punch;

        public bool Interactable => interactable;

        public void Configure(
            MainMenuLayoutSandboxMenu boundMenu,
            int index,
            Image shardImage,
            Image iconImage,
            Text labelText,
            Sprite normal,
            Sprite highlight,
            bool canSelect)
        {
            menu = boundMenu;
            optionIndex = index;
            shard = shardImage;
            icon = iconImage;
            label = labelText;
            normalSprite = normal;
            highlightSprite = highlight;
            interactable = canSelect;
            _rect = transform as RectTransform;
            _baseScale = Vector3.one;
            _button = GetComponent<Button>();
            if (_button == null)
            {
                _button = gameObject.AddComponent<Button>();
            }
            _button.transition = Selectable.Transition.None;
            _button.interactable = canSelect;
            if (shard != null)
            {
                shard.raycastTarget = true;
                _button.targetGraphic = shard;
            }

            ApplyHighlight(false, instant: true);
        }

        public void ApplyHighlight(bool highlighted, bool instant)
        {
            _highlighted = highlighted && interactable;
            if (shard != null)
            {
                if (!interactable)
                {
                    shard.sprite = normalSprite != null ? normalSprite : shard.sprite;
                    shard.color = new Color(1f, 1f, 1f, 0.42f);
                }
                else if (_highlighted)
                {
                    shard.sprite = highlightSprite != null ? highlightSprite : shard.sprite;
                    shard.color = Color.white;
                }
                else
                {
                    shard.sprite = normalSprite != null ? normalSprite : shard.sprite;
                    shard.color = Color.white;
                }
            }

            if (icon != null)
            {
                icon.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }

            if (label != null)
            {
                if (!interactable)
                {
                    label.color = new Color(0.12f, 0.16f, 0.28f, 0.4f);
                }
                else
                {
                    label.color = _highlighted
                        ? new Color(0.22f, 0.08f, 0.42f, 1f)
                        : new Color(0.07f, 0.1f, 0.22f, 0.95f);
                }
            }

            if (_rect == null)
            {
                _rect = transform as RectTransform;
            }

            if (_rect == null)
            {
                return;
            }

            if (_punch != null && instant)
            {
                StopCoroutine(_punch);
                _punch = null;
            }

            if (_punch == null)
            {
                _rect.localScale = Vector3.one;
            }
        }

        public void PlayPress()
        {
            if (!interactable || _rect == null)
            {
                return;
            }

            if (_punch != null)
            {
                StopCoroutine(_punch);
            }

            _punch = StartCoroutine(Punch());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactable)
            {
                return;
            }

            menu?.SelectIndex(optionIndex, fromPointer: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable || eventData == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            menu?.ConfirmIndex(optionIndex);
        }

        private IEnumerator Punch()
        {
            var elapsed = 0f;
            while (elapsed < 0.06f)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / 0.06f);
                _rect.localScale = Vector3.Lerp(_baseScale, _baseScale * 0.96f, t);
                yield return null;
            }

            elapsed = 0f;
            var peak = _highlighted ? _baseScale * 1.05f : _baseScale * 1.02f;
            var rest = _highlighted ? _baseScale * 1.035f : _baseScale;
            while (elapsed < 0.12f)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / 0.12f);
                var e = 1f - ((1f - t) * (1f - t));
                _rect.localScale = Vector3.Lerp(peak, rest, e);
                yield return null;
            }

            _rect.localScale = rest;
            _punch = null;
        }
    }
}
