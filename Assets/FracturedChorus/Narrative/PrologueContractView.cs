using System;
using System.Collections;
using FracturedChorus.Narrative.Vn;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Narrative
{
    public class PrologueContractView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Image contractPaper;
        [SerializeField] private Text nameValueText;
        [SerializeField] private InputField nameInput;
        [SerializeField] private Text hintText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private PrologueSignaturePad signaturePad;
        [SerializeField] private PrologueVNLayoutConfig layoutConfig;

        private Action<string> _onSigned;
        private bool _nameInputListenersBound;

        private static readonly Color NameValueFilledColor = new Color(0.05f, 0.05f, 0.08f, 1f);
        private static readonly Color NameValueSuggestionColor = new Color(0.08f, 0.12f, 0.28f, 0.55f);
        private static readonly Color NameInputCaretColor = new Color(0.05f, 0.05f, 0.08f, 0f);

        public void Bind(PrologueAudioController audio)
        {
            if (signaturePad != null)
            {
                signaturePad.Bind(audio);
            }
        }

        private void Awake()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirm);
            }

            ResolveReferences();
            EnsureContractPaperSprite();
            StyleFields();
            EnsureConfirmButtonHover();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.alpha = 0f;
                root.interactable = false;
                root.blocksRaycasts = false;
                root.gameObject.SetActive(false);
            }
        }

        public void Show(Action<string> onSigned)
        {
            PrepareShow(onSigned);
            if (root != null)
            {
                root.alpha = 1f;
                root.interactable = true;
                root.blocksRaycasts = true;
            }

            ActivateNameInput();
        }

        public void PrepareShow(Action<string> onSigned)
        {
            ResolveReferences();
            EnsureContractPaperSprite();
            StyleFields();
            EnsureConfirmButtonHover();

            _onSigned = onSigned;

            EnsureNameInputBindings();

            if (nameInput != null)
            {
                nameInput.gameObject.SetActive(true);
                nameInput.interactable = true;
                nameInput.text = string.Empty;
                if (nameInput.placeholder is Text placeholderText)
                {
                    placeholderText.text = RunProfile.DefaultNameSuggestion;
                    placeholderText.enabled = true;
                }
            }

            RefreshNameDisplay(nameInput != null ? nameInput.text : string.Empty);

            if (hintText != null)
            {
                hintText.gameObject.SetActive(true);
                hintText.text = $"Enter a name (suggested: {RunProfile.DefaultNameSuggestion})";
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(true);
                confirmButton.interactable = true;
            }

            if (signaturePad != null)
            {
                signaturePad.Clear();
                signaturePad.gameObject.SetActive(false);
            }

            if (root != null)
            {
                BringRootToFront();

                root.gameObject.SetActive(true);
                root.alpha = 0f;
                root.interactable = false;
                root.blocksRaycasts = false;
            }
        }

        public IEnumerator FadeIn(float duration)
        {
            if (root == null)
            {
                ActivateNameInput();
                yield break;
            }

            duration = Mathf.Max(0.01f, duration);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                root.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            root.alpha = 1f;
            root.interactable = true;
            root.blocksRaycasts = true;

            if (confirmButton != null && !confirmButton.gameObject.activeSelf)
            {
                confirmButton.gameObject.SetActive(true);
            }

            ActivateNameInput();
        }

        private void ActivateNameInput()
        {
            if (nameInput == null || !nameInput.gameObject.activeInHierarchy)
            {
                return;
            }

            nameInput.ActivateInputField();
            nameInput.Select();
        }

        private void BringRootToFront()
        {
            if (root == null)
            {
                return;
            }

            var fadeOverlay = root.transform.parent != null
                ? root.transform.parent.Find("FadeOverlay")
                : null;
            if (fadeOverlay != null)
            {
                root.transform.SetSiblingIndex(fadeOverlay.GetSiblingIndex());
            }
            else
            {
                root.transform.SetAsLastSibling();
            }
        }

        private void HandleConfirm()
        {
            var entered = nameInput != null ? nameInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(entered))
            {
                entered = RunProfile.DefaultNameSuggestion;
            }

            entered = entered.Trim();

            if (nameValueText != null)
            {
                nameValueText.text = entered;
                nameValueText.enabled = true;
                nameValueText.raycastTarget = false;
                nameValueText.gameObject.SetActive(true);
            }

            if (nameInput != null)
            {
                nameInput.gameObject.SetActive(false);
            }

            Finish(entered);
        }

        public void ApplyEditorPreview()
        {
            ResolveReferences();
            EnsureContractPaperSprite();
            StyleFields();
            EnsureConfirmButtonHover();

            EnsureNameInputBindings();

            if (nameInput != null)
            {
                nameInput.gameObject.SetActive(true);
                nameInput.interactable = true;
                nameInput.text = string.Empty;
            }

            RefreshNameDisplay(string.Empty);

            if (hintText != null)
            {
                hintText.text = $"Enter a name (suggested: {RunProfile.DefaultNameSuggestion})";
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(true);
            }

            if (signaturePad != null)
            {
                signaturePad.gameObject.SetActive(false);
            }

            if (root != null)
            {
                root.gameObject.SetActive(true);
                root.alpha = 1f;
                root.interactable = false;
                root.blocksRaycasts = false;
            }
        }

        public bool CaptureLayoutToConfig(PrologueVNLayoutConfig config)
        {
            if (config == null)
            {
                return false;
            }

            ResolveReferences();
            layoutConfig = config;

            if (contractPaper == null)
            {
                return false;
            }

            var paperRect = contractPaper.rectTransform;
            var nameRect = nameInput != null
                ? nameInput.GetComponent<RectTransform>()
                : nameValueText != null
                    ? nameValueText.rectTransform
                    : null;

            config.CaptureFrom(
                paperRect,
                nameRect,
                signaturePad != null ? signaturePad.GetComponent<RectTransform>() : null);
            return true;
        }

        public void ApplyLayoutConfig(PrologueVNLayoutConfig config)
        {
            layoutConfig = config;
            StyleFields();
        }

        public void SetLayoutConfig(PrologueVNLayoutConfig config)
        {
            layoutConfig = config;
        }

        private void Finish(string playerName)
        {
            Hide();
            _onSigned?.Invoke(playerName);
        }

        private void EnsureNameInputBindings()
        {
            if (nameInput == null || _nameInputListenersBound)
            {
                return;
            }

            nameInput.onValueChanged.AddListener(OnNameInputChanged);
            _nameInputListenersBound = true;
        }

        private void OnNameInputChanged(string value)
        {
            RefreshNameDisplay(value);
        }

        private void RefreshNameDisplay(string raw)
        {
            var suggestion = RunProfile.DefaultNameSuggestion;
            var isEmpty = string.IsNullOrEmpty(raw);
            var display = isEmpty ? suggestion : raw;

            if (nameValueText == null)
            {
                return;
            }

            nameValueText.gameObject.SetActive(true);
            nameValueText.enabled = true;
            nameValueText.raycastTarget = false;
            nameValueText.text = display;
            nameValueText.fontStyle = isEmpty ? FontStyle.Italic : FontStyle.Normal;
            nameValueText.color = isEmpty ? NameValueSuggestionColor : NameValueFilledColor;
        }

        private void ResolveReferences()
        {
            if (contractPaper == null)
            {
                contractPaper = transform.Find("ContractPaper")?.GetComponent<Image>();
            }

            if (confirmButton == null)
            {
                confirmButton = transform.Find("ConfirmButton")?.GetComponent<Button>();
            }

            if (hintText == null)
            {
                hintText = transform.Find("HintText")?.GetComponent<Text>();
            }

            if (nameValueText == null)
            {
                var nameSlot = transform.Find("Name") ?? transform.Find("NameValue");
                if (nameSlot != null)
                {
                    nameValueText = nameSlot.GetComponent<Text>();
                }
            }

            if (nameInput == null)
            {
                var nameRoot = transform.Find("Name");
                nameInput = nameRoot != null
                    ? nameRoot.GetComponentInChildren<InputField>(true)
                    : GetComponentInChildren<InputField>(true);
            }
        }

        private void EnsureContractPaperSprite()
        {
            if (contractPaper == null)
            {
                return;
            }

            var resolved = PrologueContractSpriteUtility.LoadPrimarySprite();
            if (resolved != null)
            {
                contractPaper.sprite = resolved;
            }
        }

        private void StyleFields()
        {
            if (nameInput != null && nameInput.TryGetComponent<Image>(out var inputBackground))
            {
                inputBackground.color = Color.clear;
                inputBackground.raycastTarget = true;
            }

            if (signaturePad != null && signaturePad.TryGetComponent<Image>(out var signatureBackground))
            {
                signatureBackground.color = Color.clear;
                signatureBackground.raycastTarget = false;
            }

            if (nameValueText != null)
            {
                VnUiFont.Apply(nameValueText, 26, FontStyle.Normal);
                nameValueText.alignment = TextAnchor.MiddleLeft;
                nameValueText.horizontalOverflow = HorizontalWrapMode.Overflow;
                nameValueText.raycastTarget = false;
            }

            if (nameInput != null)
            {
                if (nameInput.textComponent is Text inputText)
                {
                    VnUiFont.Apply(inputText, 26, FontStyle.Normal);
                    inputText.alignment = TextAnchor.MiddleLeft;
                    inputText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    inputText.color = NameInputCaretColor;
                    inputText.raycastTarget = false;
                }

                if (nameInput.placeholder is Text placeholderText)
                {
                    VnUiFont.Apply(placeholderText, 26, FontStyle.Italic);
                    placeholderText.alignment = TextAnchor.MiddleLeft;
                    placeholderText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    placeholderText.enabled = true;
                    placeholderText.raycastTarget = false;
                    placeholderText.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureConfirmButtonHover()
        {
            if (confirmButton == null)
            {
                return;
            }

            confirmButton.transition = Selectable.Transition.None;
            var image = confirmButton.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var hoverView = confirmButton.GetComponent<PrologueContractConfirmButtonView>();
            if (hoverView == null)
            {
                hoverView = confirmButton.gameObject.AddComponent<PrologueContractConfirmButtonView>();
            }

            hoverView.Configure(image);
        }
    }
}
