using System;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub.CharacterBuild
{
    public enum CharacterBuildStatKind
    {
        Strength,
        Magic,
        Endurance,
        HeartBeat,
        Luck
    }

    /// <summary>One stat row — label, value, bar, optional [-] spent [+] controls.</summary>
    public sealed class CharacterBuildStatRowView : MonoBehaviour
    {
        [SerializeField] private CharacterBuildStatKind kind = CharacterBuildStatKind.Strength;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text subLabel;
        [SerializeField] private Image nameLabelArt;
        [SerializeField] private Text valueLabel;
        [SerializeField] private Image icon;
        [SerializeField] private Image barFill;
        [SerializeField] private Image barHandle;
        [SerializeField] private Button minusButton;
        [SerializeField] private Text spentLabel;
        [SerializeField] private Button plusButton;
        [SerializeField] private GameObject allocControlsRoot;

        public CharacterBuildStatKind Kind => kind;

        private void Awake()
        {
            LockBarInteraction();
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            if (icon != null)
            {
                icon.color = CharacterBuildStatTheme.IconImageColor;
            }

            if (barHandle != null)
            {
                barHandle.color = CharacterBuildStatTheme.HandleTint;
            }

            var meta = CharacterBuildStatTheme.ForKind(kind);
            if (nameLabel != null)
            {
                nameLabel.color = CharacterBuildStatTheme.LabelColor;
                nameLabel.text = meta.Title;
            }

            if (subLabel != null)
            {
                subLabel.color = CharacterBuildStatTheme.SubLabelColor;
                subLabel.text = meta.Subtitle;
            }

            if (valueLabel != null)
            {
                valueLabel.color = CharacterBuildStatTheme.LabelColor;
            }

            if (nameLabelArt != null)
            {
                nameLabelArt.enabled = false;
            }
        }

        public void WireCallbacks(Action onMinus, Action onPlus)
        {
            if (minusButton != null)
            {
                minusButton.onClick.RemoveAllListeners();
                minusButton.onClick.AddListener(() => onMinus?.Invoke());
            }

            if (plusButton != null)
            {
                plusButton.onClick.RemoveAllListeners();
                plusButton.onClick.AddListener(() => onPlus?.Invoke());
            }
        }

        public void Refresh(string displayName, float value, float barMax, int spentPoints, bool allocatable)
        {
            var meta = CharacterBuildStatTheme.ForKind(kind);
            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrEmpty(meta.Title) ? displayName : meta.Title;
                nameLabel.enabled = true;
            }

            if (subLabel != null)
            {
                subLabel.text = meta.Subtitle;
                subLabel.enabled = !string.IsNullOrEmpty(meta.Subtitle);
            }

            if (nameLabelArt != null)
            {
                nameLabelArt.enabled = false;
            }

            if (valueLabel != null)
            {
                valueLabel.text = Mathf.Approximately(value, Mathf.Round(value))
                    ? Mathf.RoundToInt(value).ToString()
                    : value.ToString("0.#");
            }

            if (barFill != null)
            {
                barFill.fillAmount = barMax > 0f ? Mathf.Clamp01(value / barMax) : 0f;
            }

            LockBarInteraction();
            SyncHandle();

            if (allocControlsRoot != null)
            {
                allocControlsRoot.SetActive(allocatable);
            }

            if (spentLabel != null)
            {
                spentLabel.text = allocatable ? spentPoints.ToString() : string.Empty;
            }

            if (minusButton != null)
            {
                minusButton.interactable = allocatable && spentPoints > 0;
            }

            if (plusButton != null)
            {
                plusButton.interactable = allocatable;
            }
        }

        public void SetPlusInteractable(bool interactable)
        {
            if (plusButton != null)
            {
                plusButton.interactable = interactable;
            }
        }

        private void LockBarInteraction()
        {
            if (barFill != null)
            {
                barFill.raycastTarget = false;
                var slider = barFill.GetComponentInParent<Slider>();
                if (slider != null)
                {
                    slider.interactable = false;
                }
            }

            if (barHandle != null)
            {
                barHandle.raycastTarget = false;
            }

            var track = barFill != null
                ? barFill.transform.parent
                : barHandle != null ? barHandle.transform.parent : null;
            if (track == null)
            {
                return;
            }

            var trackImage = track.GetComponent<Image>();
            if (trackImage != null)
            {
                trackImage.raycastTarget = false;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            SyncHandle();
        }

        private void SyncHandle()
        {
            if (barHandle == null || barFill == null)
            {
                return;
            }

            var handleRt = barHandle.rectTransform;
            var fillRt = barFill.rectTransform;
            var parent = handleRt.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var fillRect = fillRt.rect;
            var t = Mathf.Clamp01(barFill.fillAmount);
            var localOnFill = new Vector3(Mathf.Lerp(fillRect.xMin, fillRect.xMax, t), fillRect.center.y, 0f);
            var localInParent = (Vector2)parent.InverseTransformPoint(fillRt.TransformPoint(localOnFill));
            var parentRect = parent.rect;
            var anchorX = (handleRt.anchorMin.x + handleRt.anchorMax.x) * 0.5f;
            var anchorRefX = Mathf.Lerp(parentRect.xMin, parentRect.xMax, anchorX);
            handleRt.anchoredPosition = new Vector2(localInParent.x - anchorRefX, handleRt.anchoredPosition.y);
        }
    }
}
