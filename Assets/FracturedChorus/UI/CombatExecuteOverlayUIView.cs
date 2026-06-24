using System;
using FracturedChorus.Combat.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// EXECUTE button wired from Hierarchy — layout and styling are edited in the scene, not created at runtime.
    /// </summary>
    public class CombatExecuteOverlayUIView : MonoBehaviour
    {
        [SerializeField] private Button executeButton;
        [SerializeField] private Text labelText;
        [SerializeField] private CombatController combatController;

        private Action _onExecuteClicked;
        private bool _warnedMissingButton;

        public void WireReferences()
        {
            executeButton = ResolveExecuteButton();

            if (labelText == null && executeButton != null)
            {
                labelText = executeButton.GetComponentInChildren<Text>(true);
            }

            if (combatController == null)
            {
                combatController = FindAnyObjectByType<CombatController>();
            }
        }

        private Button ResolveExecuteButton()
        {
            var buttonTransform = transform.Find("ExecuteButton");
            if (buttonTransform != null)
            {
                var button = buttonTransform.GetComponent<Button>();
                if (button != null)
                {
                    return button;
                }

                var image = buttonTransform.GetComponent<Image>();
                if (image == null)
                {
                    image = buttonTransform.gameObject.AddComponent<Image>();
                    image.color = new Color(0.35f, 0.15f, 0.55f, 0.95f);
                    image.raycastTarget = true;
                }

                button = buttonTransform.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.interactable = true;
                return button;
            }

            if (executeButton != null)
            {
                return executeButton;
            }

            return GetComponentInChildren<Button>(true);
        }

        private void Awake()
        {
            WireReferences();
        }

        private void Start()
        {
            WireReferences();
            if (combatController != null)
            {
                Bind(combatController.StartRound);
            }
        }

        public void Bind(Action onExecuteClicked)
        {
            WireReferences();
            _onExecuteClicked = onExecuteClicked;
            executeButton = ResolveExecuteButton();

            if (executeButton == null)
            {
                if (!_warnedMissingButton)
                {
                    _warnedMissingButton = true;
                    Debug.LogWarning(
                        "[ExecuteOverlay] Không tìm thấy ExecuteButton. Thêm con tên ExecuteButton có Image + Button.");
                }

                return;
            }

            executeButton.onClick.RemoveListener(HandleClick);
            executeButton.onClick.AddListener(HandleClick);
        }

        /// <summary>Gán vào Button → On Click () trong Inspector nếu cần.</summary>
        public void OnExecutePressed()
        {
            HandleClick();
        }

        public void SetVisible(bool visible)
        {
            WireReferences();
            if (executeButton != null)
            {
                executeButton.gameObject.SetActive(visible);
                return;
            }

            gameObject.SetActive(visible);
        }

        private void HandleClick()
        {
            if (_onExecuteClicked != null)
            {
                _onExecuteClicked.Invoke();
                return;
            }

            if (combatController == null)
            {
                combatController = FindAnyObjectByType<CombatController>();
            }

            combatController?.StartRound();
        }
    }
}
