using System;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FracturedChorus.Narrative
{
    public class PrologueChoiceView : MonoBehaviour
    {
        private static readonly Color LabelOnLightButton = new Color(0.10f, 0.14f, 0.28f, 1f);
        private static readonly Color LabelOnDarkButton = new Color(0.96f, 0.97f, 1f, 1f);

        [SerializeField] private CanvasGroup root;
        [SerializeField] private Text promptText;
        [SerializeField] private Text agreeLabel;
        [SerializeField] private Text disagreeLabel;
        [SerializeField] private Image agreeHighlight;
        [SerializeField] private Image disagreeHighlight;
        [SerializeField] private Sprite agreeNormalSprite;
        [SerializeField] private Sprite agreeSelectedSprite;
        [SerializeField] private Sprite disagreeNormalSprite;
        [SerializeField] private Sprite disagreeSelectedSprite;
        [FormerlySerializedAs("agreeSelectedColor")]
        [SerializeField] private Color selectedColor;
        [FormerlySerializedAs("agreeIdleColor")]
        [SerializeField] private Color idleColor;
        [SerializeField] private Color hoverColor;

        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private bool _active;
        private int _ignoreInputUntilFrame = -1;
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
            _ignoreInputUntilFrame = -1;
            _onChosen = null;
            _hoverIndex = -1;
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
            _selectedIndex = -1;
            _hoverIndex = -1;
            _ignoreInputUntilFrame = Time.frameCount + 1;
            _active = true;

            if (promptText != null)
            {
                promptText.raycastTarget = false;
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
                agreeLabel.gameObject.SetActive(true);
                agreeLabel.text = agreeText;
            }

            if (disagreeLabel != null)
            {
                disagreeLabel.gameObject.SetActive(true);
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

        private bool CanAcceptInput()
        {
            return Time.frameCount > _ignoreInputUntilFrame;
        }

        public void HoverOption(int optionIndex)
        {
            if (!_active)
            {
                return;
            }

            var nextIndex = Mathf.Clamp(optionIndex, 0, 1);
            if (nextIndex == _hoverIndex)
            {
                return;
            }

            _hoverIndex = nextIndex;
            _audio?.PlayButtonPress();
            RefreshSelectionVisuals();
        }

        public void HoverExitOption(int optionIndex)
        {
            if (!_active || _hoverIndex != optionIndex)
            {
                return;
            }

            _hoverIndex = -1;
            RefreshSelectionVisuals();
        }

        public void ClickOption(int optionIndex)
        {
            if (!_active || !CanAcceptInput())
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

            if (root != null && !root.interactable && CanAcceptInput())
            {
                root.interactable = true;
                root.blocksRaycasts = true;
            }

            if (!CanAcceptInput())
            {
                return;
            }

            if (PrologueInput.WasUpPressedThisFrame())
            {
                MoveKeyboardSelection(-1);
            }
            else if (PrologueInput.WasDownPressedThisFrame())
            {
                MoveKeyboardSelection(1);
            }
            else if (PrologueInput.WasAdvancePressedThisFrame() && _selectedIndex >= 0)
            {
                ConfirmSelection();
            }
        }

        private void MoveKeyboardSelection(int delta)
        {
            var next = _selectedIndex < 0 ? (delta > 0 ? 0 : 1) : (_selectedIndex + delta + 2) % 2;
            if (next == _selectedIndex)
            {
                return;
            }

            _selectedIndex = next;
            _hoverIndex = -1;
            _audio?.PlayButtonPress();
            RefreshSelectionVisuals();
        }

        private void ConfirmSelection()
        {
            if (!_active || _selectedIndex < 0)
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
                agreeLabel.gameObject.SetActive(true);
                agreeLabel.text = "I agree";
            }

            if (disagreeLabel != null)
            {
                disagreeLabel.gameObject.SetActive(true);
                disagreeLabel.text = "I do not agree";
            }

            _selectedIndex = -1;
            _hoverIndex = -1;
            _active = true;
            RefreshSelectionVisuals();

            if (promptText != null)
            {
                promptText.raycastTarget = false;
            }

            if (root != null)
            {
                root.gameObject.SetActive(true);
                root.alpha = 1f;
                root.interactable = true;
                root.blocksRaycasts = true;
            }
        }

        private void RefreshSelectionVisuals()
        {
            ApplyHighlight(agreeHighlight, ResolveHighlightState(0), agreeNormalSprite, agreeSelectedSprite);
            ApplyHighlight(disagreeHighlight, ResolveHighlightState(1), disagreeNormalSprite, disagreeSelectedSprite);

            if (agreeLabel != null)
            {
                UiFontCatalog.Apply(agreeLabel, UiFontRole.Display);
                agreeLabel.raycastTarget = false;
                agreeLabel.color = ContrastLabelColor(agreeHighlight, true);
            }

            if (disagreeLabel != null)
            {
                UiFontCatalog.Apply(disagreeLabel, UiFontRole.Display);
                disagreeLabel.raycastTarget = false;
                disagreeLabel.color = ContrastLabelColor(disagreeHighlight, false);
            }
        }

        private HighlightState ResolveHighlightState(int optionIndex)
        {
            if (_selectedIndex == optionIndex)
            {
                return HighlightState.Selected;
            }

            if (_hoverIndex == optionIndex)
            {
                return HighlightState.Hover;
            }

            return HighlightState.Idle;
        }

        private static bool HasButtonArt(Sprite normal, Sprite selected)
        {
            return normal != null || selected != null;
        }

        private static Color ContrastLabelColor(Image highlight, bool lightButtonFallback)
        {
            var sprite = highlight != null ? highlight.sprite : null;
            if (TryGetSpriteLuminance(sprite, out var luminance))
            {
                return luminance >= 0.5f ? LabelOnLightButton : LabelOnDarkButton;
            }

            return lightButtonFallback ? LabelOnLightButton : LabelOnDarkButton;
        }

        private static bool TryGetSpriteLuminance(Sprite sprite, out float luminance)
        {
            luminance = 0f;
            if (sprite == null)
            {
                return false;
            }

            var texture = sprite.texture;
            if (texture == null || !texture.isReadable)
            {
                return false;
            }

            var rect = sprite.textureRect;
            var x = Mathf.Clamp(Mathf.RoundToInt(rect.x + rect.width * 0.5f), 0, texture.width - 1);
            var y = Mathf.Clamp(Mathf.RoundToInt(rect.y + rect.height * 0.5f), 0, texture.height - 1);
            var color = texture.GetPixel(x, y);
            luminance = (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
            return true;
        }

        private void ApplyHighlight(Image highlight, HighlightState state, Sprite normal, Sprite selected)
        {
            if (highlight == null)
            {
                return;
            }

            var lit = state != HighlightState.Idle;
            if (HasButtonArt(normal, selected))
            {
                highlight.sprite = lit ? (selected != null ? selected : normal) : (normal != null ? normal : selected);
                highlight.color = Color.white;
                highlight.preserveAspect = true;
                highlight.raycastTarget = false;
                highlight.gameObject.SetActive(true);
                return;
            }

            highlight.color = state switch
            {
                HighlightState.Selected => selectedColor,
                HighlightState.Hover => hoverColor,
                _ => idleColor
            };
            highlight.gameObject.SetActive(true);
        }

        private void EnsureChoiceUi()
        {
            EnsureOptionRow(agreeLabel, "AgreeRow");
            EnsureOptionRow(disagreeLabel, "DisagreeRow");
            RebindRowHighlights();

            if (agreeLabel != null)
            {
                agreeLabel.raycastTarget = false;
                agreeLabel.gameObject.SetActive(true);
            }

            if (disagreeLabel != null)
            {
                disagreeLabel.raycastTarget = false;
                disagreeLabel.gameObject.SetActive(true);
            }

            if (agreeHighlight == null && agreeLabel != null)
            {
                agreeHighlight = CreateHighlight("AgreeHighlight", GetRowTransform(agreeLabel.transform));
            }

            if (disagreeHighlight == null && disagreeLabel != null)
            {
                disagreeHighlight = CreateHighlight("DisagreeHighlight", GetRowTransform(disagreeLabel.transform));
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

        private void RebindRowHighlights()
        {
            if (agreeHighlight == null && agreeLabel != null)
            {
                var row = GetRowTransform(agreeLabel.transform);
                agreeHighlight = row?.Find("AgreeHighlight")?.GetComponent<Image>();
            }

            if (disagreeHighlight == null && disagreeLabel != null)
            {
                var row = GetRowTransform(disagreeLabel.transform);
                disagreeHighlight = row?.Find("DisagreeHighlight")?.GetComponent<Image>();
            }
        }

        private static void EnsureOptionRow(Text label, string rowName)
        {
            if (label == null)
            {
                return;
            }

            var labelTransform = label.transform;
            var parent = labelTransform.parent;
            if (parent != null && parent.name == rowName)
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

            var rowGo = new GameObject(rowName, typeof(RectTransform));
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

            var legacyHighlight = labelTransform.Find($"{rowName.Replace("Row", "Highlight")}");
            if (legacyHighlight == null)
            {
                legacyHighlight = labelTransform.Find(rowName == "AgreeRow" ? "AgreeHighlight" : "DisagreeHighlight");
            }

            if (legacyHighlight != null)
            {
                legacyHighlight.SetParent(rowGo.transform, false);
                legacyHighlight.SetAsFirstSibling();
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

        private static Image CreateHighlight(string name, Transform row)
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
            rect.offsetMin = new Vector2(8f, 6f);
            rect.offsetMax = new Vector2(-8f, -6f);
            rect.localRotation = Quaternion.identity;
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
            selectedColor = FcColorTokens.Selection.VnChoiceHighlight;
            hoverColor = FcColorTokens.WithAlpha(FcColorTokens.Brand.CyanHover, 0.88f);
            idleColor = FcColorTokens.WithAlpha(FcColorTokens.Surface.Panel, 0f);
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

        private enum HighlightState
        {
            Idle,
            Hover,
            Selected
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
            RefreshSelectionVisuals();
        }
#endif
    }
}
