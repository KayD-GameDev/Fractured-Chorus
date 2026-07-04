using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FracturedChorus.Narrative
{
    public class PrologueChoiceView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Text promptText;
        [SerializeField] private Text agreeLabel;
        [SerializeField] private Text disagreeLabel;
        [SerializeField] private Image agreeHighlight;
        [SerializeField] private Image disagreeHighlight;
        [FormerlySerializedAs("agreeSelectedColor")]
        [SerializeField] private Color selectedColor = new Color(0.35f, 0.72f, 1f, 0.92f);
        [FormerlySerializedAs("agreeIdleColor")]
        [SerializeField] private Color idleColor = new Color(0.04f, 0.04f, 0.06f, 0.94f);

        private int _selectedIndex;
        private bool _active;
        private Action<bool> _onChosen;
        private PrologueAudioController _audio;

        private void Awake()
        {
            NormalizeLegacyColors();
            EnsureChoiceUi();
        }

        public void Bind(PrologueAudioController audio)
        {
            _audio = audio;
        }

        public void Hide()
        {
            _active = false;
            _onChosen = null;
            if (root != null)
            {
                root.alpha = 0f;
                root.interactable = false;
                root.blocksRaycasts = false;
                root.gameObject.SetActive(false);
            }
        }

        public void ShowOptions(string agreeText, string disagreeText, Action<bool> onChosen)
        {
            Show(null, agreeText, disagreeText, onChosen);
        }

        public void Show(string prompt, string agreeText, string disagreeText, Action<bool> onChosen)
        {
            NormalizeLegacyColors();
            EnsureChoiceUi();
            _onChosen = onChosen;
            _selectedIndex = 0;
            _active = true;

            if (promptText != null)
            {
                if (string.IsNullOrEmpty(prompt))
                {
                    promptText.gameObject.SetActive(false);
                }
                else
                {
                    promptText.gameObject.SetActive(true);
                    promptText.text = prompt;
                }
            }

            if (agreeLabel != null)
            {
                agreeLabel.text = agreeText;
            }

            if (disagreeLabel != null)
            {
                disagreeLabel.text = disagreeText;
            }

            RefreshSelectionVisuals();

            if (root != null)
            {
                root.gameObject.SetActive(true);
                root.alpha = 1f;
                root.interactable = true;
                root.blocksRaycasts = true;
            }
        }

        public void HoverOption(int optionIndex)
        {
            if (!_active)
            {
                return;
            }

            var nextIndex = Mathf.Clamp(optionIndex, 0, 1);
            if (nextIndex == _selectedIndex)
            {
                return;
            }

            _selectedIndex = nextIndex;
            _audio?.PlayButtonPress();
            RefreshSelectionVisuals();
        }

        public void ClickOption(int optionIndex)
        {
            if (!_active)
            {
                return;
            }

            _selectedIndex = Mathf.Clamp(optionIndex, 0, 1);
            ConfirmSelection();
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (PrologueInput.WasUpPressedThisFrame())
            {
                HoverOption(_selectedIndex == 0 ? 1 : 0);
            }
            else if (PrologueInput.WasDownPressedThisFrame())
            {
                HoverOption(_selectedIndex == 0 ? 1 : 0);
            }
            else if (PrologueInput.WasAdvancePressedThisFrame())
            {
                ConfirmSelection();
            }
        }

        private void ConfirmSelection()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            var agreed = _selectedIndex == 0;
            if (!agreed)
            {
                _audio?.PlayButtonPress();
            }

            var callback = _onChosen;
            Hide();
            callback?.Invoke(agreed);
        }

        public void ApplyEditorPreview(bool showPrompt = true)
        {
            NormalizeLegacyColors();
            EnsureChoiceUi();

            if (promptText != null)
            {
                if (showPrompt)
                {
                    promptText.gameObject.SetActive(true);
                    promptText.text = "Only those who have agreed to the above\nhave the privilege of partaking in this game.";
                }
                else
                {
                    promptText.gameObject.SetActive(false);
                }
            }

            if (agreeLabel != null)
            {
                agreeLabel.text = "I agree.";
            }

            if (disagreeLabel != null)
            {
                disagreeLabel.text = "I do not agree.";
            }

            _selectedIndex = 0;
            RefreshSelectionVisuals();

            if (root != null)
            {
                root.gameObject.SetActive(true);
                root.alpha = 1f;
                root.interactable = false;
                root.blocksRaycasts = false;
            }
        }

        private void RefreshSelectionVisuals()
        {
            ApplyHighlight(agreeHighlight, _selectedIndex == 0);
            ApplyHighlight(disagreeHighlight, _selectedIndex == 1);

            if (agreeLabel != null)
            {
                agreeLabel.fontStyle = _selectedIndex == 0 ? FontStyle.Bold : FontStyle.Normal;
                agreeLabel.color = Color.white;
            }

            if (disagreeLabel != null)
            {
                disagreeLabel.fontStyle = _selectedIndex == 1 ? FontStyle.Bold : FontStyle.Normal;
                disagreeLabel.color = Color.white;
            }
        }

        private void ApplyHighlight(Image highlight, bool selected)
        {
            if (highlight == null)
            {
                return;
            }

            highlight.color = selected ? selectedColor : idleColor;
            highlight.gameObject.SetActive(true);
        }

        private void EnsureChoiceUi()
        {
            EnsureDisagreeRow();

            if (agreeLabel != null)
            {
                agreeLabel.raycastTarget = false;
            }

            if (disagreeLabel != null)
            {
                disagreeLabel.raycastTarget = false;
            }

            if (agreeHighlight == null && agreeLabel != null)
            {
                agreeHighlight = CreateHighlight("AgreeHighlight", GetRowTransform(agreeLabel.transform), -4f);
            }

            if (disagreeHighlight == null && disagreeLabel != null)
            {
                disagreeHighlight = CreateHighlight("DisagreeHighlight", GetRowTransform(disagreeLabel.transform), 4f);
            }

            if (agreeLabel != null)
            {
                EnsureHitbox(GetRowTransform(agreeLabel.transform), 0, this);
            }

            if (disagreeLabel != null)
            {
                EnsureHitbox(GetRowTransform(disagreeLabel.transform), 1, this);
            }
        }

        private void EnsureDisagreeRow()
        {
            if (disagreeLabel == null)
            {
                return;
            }

            var labelTransform = disagreeLabel.transform;
            var parent = labelTransform.parent;
            if (parent != null && parent.name == "DisagreeRow")
            {
                return;
            }

            if (parent == null)
            {
                return;
            }

            var labelRect = labelTransform as RectTransform;
            if (labelRect == null)
            {
                return;
            }

            var rowGo = new GameObject("DisagreeRow", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            rowGo.transform.SetSiblingIndex(labelTransform.GetSiblingIndex());

            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = labelRect.anchorMin;
            rowRect.anchorMax = labelRect.anchorMax;
            rowRect.anchoredPosition = labelRect.anchoredPosition;
            rowRect.sizeDelta = labelRect.sizeDelta;
            rowRect.pivot = labelRect.pivot;
            rowRect.localRotation = labelRect.localRotation;
            rowRect.offsetMin = labelRect.offsetMin;
            rowRect.offsetMax = labelRect.offsetMax;

            labelTransform.SetParent(rowGo.transform, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.localRotation = Quaternion.identity;

            var legacyHighlight = labelTransform.Find("DisagreeHighlight");
            if (legacyHighlight != null)
            {
                legacyHighlight.SetParent(rowGo.transform, false);
                legacyHighlight.SetAsFirstSibling();
                if (legacyHighlight.TryGetComponent<Image>(out var legacyImage))
                {
                    disagreeHighlight = legacyImage;
                }
            }
        }

        private static Transform GetRowTransform(Transform labelTransform)
        {
            if (labelTransform == null)
            {
                return null;
            }

            var parent = labelTransform.parent;
            if (parent != null &&
                parent.GetComponent<PrologueChoiceView>() == null &&
                parent.GetComponent<CanvasGroup>() == null &&
                parent.name != "ChoicePanel")
            {
                return parent;
            }

            return labelTransform;
        }

        private static Image CreateHighlight(string name, Transform row, float zRotation)
        {
            if (row == null)
            {
                return null;
            }

            var existing = row.Find(name);
            if (existing != null && existing.TryGetComponent<Image>(out var existingImage))
            {
                return existingImage;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(row, false);
            go.transform.SetAsFirstSibling();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, zRotation);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void EnsureHitbox(Transform row, int optionIndex, PrologueChoiceView view)
        {
            if (row == null || view == null)
            {
                return;
            }

            var hitAreaTransform = row.Find("HitArea");
            Image image = null;
            if (hitAreaTransform != null)
            {
                hitAreaTransform.TryGetComponent(out image);
            }

            if (image == null)
            {
                if (hitAreaTransform != null)
                {
                    DestroyHitArea(hitAreaTransform.gameObject);
                }

                var hitGo = new GameObject("HitArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                hitGo.transform.SetParent(row, false);
                var rect = hitGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                hitAreaTransform = hitGo.transform;
                image = hitGo.GetComponent<Image>();
            }

            if (image == null)
            {
                Debug.LogError($"PrologueChoiceView: failed to create HitArea on {row.name}.");
                return;
            }

            image.color = Color.clear;
            image.raycastTarget = true;
            hitAreaTransform.SetAsLastSibling();

            var hitbox = hitAreaTransform.GetComponent<PrologueChoiceOptionHitbox>();
            if (hitbox == null)
            {
                hitbox = hitAreaTransform.gameObject.AddComponent<PrologueChoiceOptionHitbox>();
            }

            hitbox.Initialize(view, optionIndex);
        }

        private void NormalizeLegacyColors()
        {
            if (idleColor.a < 0.5f)
            {
                idleColor = new Color(0.04f, 0.04f, 0.06f, 0.94f);
            }
        }

        private static void DestroyHitArea(GameObject hitArea)
        {
            if (hitArea == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(hitArea);
            }
            else
            {
                DestroyImmediate(hitArea);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            UnityEditor.EditorApplication.delayCall -= DelayedEditorRefresh;
            UnityEditor.EditorApplication.delayCall += DelayedEditorRefresh;
        }

        private void DelayedEditorRefresh()
        {
            UnityEditor.EditorApplication.delayCall -= DelayedEditorRefresh;
            if (this == null)
            {
                return;
            }

            NormalizeLegacyColors();
            EnsureChoiceUi();
        }
#endif
    }
}
