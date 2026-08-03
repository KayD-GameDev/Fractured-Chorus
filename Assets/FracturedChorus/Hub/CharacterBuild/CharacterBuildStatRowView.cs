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
        [SerializeField] private Text valueLabel;
        [SerializeField] private Image barFill;
        [SerializeField] private Button minusButton;
        [SerializeField] private Text spentLabel;
        [SerializeField] private Button plusButton;
        [SerializeField] private GameObject allocControlsRoot;

        public CharacterBuildStatKind Kind => kind;

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
            if (nameLabel != null)
            {
                nameLabel.text = displayName;
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
    }
}
