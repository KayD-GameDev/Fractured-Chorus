using System;
using System.Collections;
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
        private bool _nameLocked;

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
            ApplyLayout();
            StyleFields();
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
            ApplyLayout();
            StyleFields();

            _onSigned = onSigned;
            _nameLocked = false;

            if (nameValueText != null)
            {
                nameValueText.text = string.Empty;
                nameValueText.gameObject.SetActive(false);
            }

            if (nameInput != null)
            {
                nameInput.gameObject.SetActive(true);
                nameInput.text = RunProfile.DefaultNameSuggestion;
                nameInput.interactable = true;
                if (nameInput.placeholder is Text placeholderText)
                {
                    placeholderText.text = RunProfile.DefaultNameSuggestion;
                }
            }

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

            signaturePad?.Clear();

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

            if (!_nameLocked)
            {
                if (nameValueText != null)
                {
                    nameValueText.text = entered;
                    nameValueText.gameObject.SetActive(true);
                }

                if (nameInput != null)
                {
                    nameInput.gameObject.SetActive(false);
                }

                _nameLocked = true;
                if (hintText != null)
                {
                    hintText.text = "Sign on the line below, then press Confirm again.";
                }

                return;
            }

            if (signaturePad != null && !signaturePad.HasStroke)
            {
                if (hintText != null)
                {
                    hintText.text = "Please sign on the line before confirming.";
                }

                return;
            }

            Finish(entered);
        }

        public void ApplyEditorPreview()
        {
            ResolveReferences();
            EnsureContractPaperSprite();
            ApplyLayout();
            StyleFields();
            _nameLocked = false;

            if (nameValueText != null)
            {
                nameValueText.gameObject.SetActive(false);
            }

            if (nameInput != null)
            {
                nameInput.gameObject.SetActive(true);
                nameInput.text = RunProfile.DefaultNameSuggestion;
                nameInput.interactable = true;
            }

            if (hintText != null)
            {
                hintText.text = $"Enter a name (suggested: {RunProfile.DefaultNameSuggestion})";
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(true);
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
            ApplyLayout();
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
        }

        private void EnsureContractPaperSprite()
        {
            if (contractPaper == null)
            {
                return;
            }

            var sprite = contractPaper.sprite;
            if (sprite != null && sprite.rect.width >= 800f)
            {
                return;
            }

            var resolved = PrologueContractSpriteUtility.LoadPrimarySprite();
            if (resolved != null)
            {
                contractPaper.sprite = resolved;
            }
        }

        private void ApplyLayout()
        {
            if (contractPaper == null)
            {
                return;
            }

            PrologueContractLayout.ApplyFieldRect(
                nameInput != null ? nameInput.GetComponent<RectTransform>() : null,
                layoutConfig,
                true);
            PrologueContractLayout.ApplyFieldRect(
                nameValueText != null ? nameValueText.rectTransform : null,
                layoutConfig,
                true);

            if (signaturePad != null)
            {
                PrologueContractLayout.ApplyFieldRect(
                    signaturePad.GetComponent<RectTransform>(),
                    layoutConfig,
                    false);
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
                nameValueText.alignment = TextAnchor.MiddleLeft;
                nameValueText.fontSize = 26;
                nameValueText.color = new Color(0.08f, 0.12f, 0.28f, 1f);
                nameValueText.raycastTarget = false;
            }

            if (nameInput != null && nameInput.textComponent is Text inputText)
            {
                inputText.alignment = TextAnchor.MiddleLeft;
                inputText.fontSize = 26;
                inputText.color = new Color(0.08f, 0.12f, 0.28f, 1f);
            }
        }
    }
}
