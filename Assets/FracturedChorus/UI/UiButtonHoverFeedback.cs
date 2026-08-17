using FracturedChorus.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    [DisallowMultipleComponent]
    public sealed class UiButtonHoverFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private Selectable selectable;
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private float hoverScale = 1.04f;
        [SerializeField] private bool scaleOnHover = true;
        [SerializeField] private Color hoverTint;
        [SerializeField] private Color disabledTint = new Color(1f, 1f, 1f, 0.45f);

        private Vector3 _baseScale = Vector3.one;
        private Color _baseColor = Color.white;
        private bool _baseCaptured;

        private void Awake()
        {
            if (hoverTint == default)
            {
                hoverTint = FcColorTokens.Brand.CyanHover;
            }

            CaptureBase();
            ApplyHoverColorsToSelectable();
            ApplyVisual(false);
        }

        private void OnEnable()
        {
            CaptureBase();
            ResetHoverState();
        }

        private void OnDisable()
        {
            ResetHoverState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            ApplyVisual(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ApplyVisual(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            FindAnyObjectByType<CombatSfxController>()?.PlayUiClick();
            ResetHoverState();
        }

        public void ResetHoverState()
        {
            CaptureBase();
            RestoreBase();

            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                var pointerData = new PointerEventData(eventSystem)
                {
                    pointerEnter = gameObject
                };
                ExecuteEvents.Execute(gameObject, pointerData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(gameObject, pointerData, ExecuteEvents.pointerExitHandler);

                if (eventSystem.currentSelectedGameObject == gameObject)
                {
                    eventSystem.SetSelectedGameObject(null);
                }
            }

            if (targetGraphic != null)
            {
                targetGraphic.color = _baseColor;
                targetGraphic.CrossFadeColor(_baseColor, 0f, true, true);
            }
        }

        public void RecaptureBaseFromGraphic()
        {
            _baseCaptured = false;
            CaptureBase();
            RestoreBase();
        }

        public void SetHovered(bool hovered)
        {
            ApplyVisual(hovered);
        }

        public static UiButtonHoverFeedback Ensure(GameObject host)
        {
            if (host == null)
            {
                return null;
            }

            var feedback = host.GetComponent<UiButtonHoverFeedback>();
            if (feedback == null)
            {
                feedback = host.AddComponent<UiButtonHoverFeedback>();
            }

            feedback.CaptureBase();
            feedback.ApplyHoverColorsToSelectable();
            return feedback;
        }

        private void CaptureBase()
        {
            if (selectable == null)
            {
                selectable = GetComponent<Selectable>();
            }

            if (targetGraphic == null)
            {
                targetGraphic = selectable != null ? selectable.targetGraphic : GetComponent<Graphic>();
            }

            if (!_baseCaptured)
            {
                _baseScale = transform.localScale;
                if (_baseScale == Vector3.zero)
                {
                    _baseScale = Vector3.one;
                }

                if (targetGraphic != null)
                {
                    var c = targetGraphic.color;
                    // Scene/authored dim alpha must not lock hover base forever.
                    if (c.a < 0.9f)
                    {
                        c = new Color(1f, 1f, 1f, 1f);
                        targetGraphic.color = c;
                    }

                    _baseColor = c;
                }

                _baseCaptured = true;
            }
        }

        private void ApplyHoverColorsToSelectable()
        {
            if (selectable == null)
            {
                return;
            }

            if (selectable.transition == Selectable.Transition.None)
            {
                selectable.transition = Selectable.Transition.ColorTint;
            }

            if (selectable.transition != Selectable.Transition.ColorTint)
            {
                return;
            }

            var block = selectable.colors;
            block.normalColor = Color.white;
            block.highlightedColor = hoverTint;
            block.pressedColor = Color.Lerp(hoverTint, Color.white, 0.25f);
            block.selectedColor = hoverTint;
            block.disabledColor = disabledTint;
            block.colorMultiplier = 1f;
            block.fadeDuration = 0.08f;
            selectable.colors = block;
        }

        private bool IsInteractable() => selectable == null || selectable.IsInteractable();

        private void ApplyVisual(bool hovered)
        {
            CaptureBase();

            if (!IsInteractable())
            {
                RestoreBase();
                if (targetGraphic != null)
                {
                    targetGraphic.color = disabledTint;
                }

                return;
            }

            if (scaleOnHover)
            {
                transform.localScale = hovered ? _baseScale * hoverScale : _baseScale;
            }

            if (targetGraphic != null && selectable != null && selectable.transition == Selectable.Transition.None)
            {
                targetGraphic.color = hovered ? hoverTint : _baseColor;
            }
        }

        private void RestoreBase()
        {
            if (_baseCaptured)
            {
                transform.localScale = _baseScale;
            }

            if (targetGraphic != null)
            {
                targetGraphic.color = _baseColor;
            }
        }
    }
}
