using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnDialoguePortraitView : MonoBehaviour
    {
        [FormerlySerializedAs("root")]
        [SerializeField] private RectTransform leftRoot;
        [FormerlySerializedAs("shadowImage")]
        [SerializeField] private Image leftShadow;
        [FormerlySerializedAs("portraitImage")]
        [SerializeField] private Image leftPortrait;
        [SerializeField] private RectTransform rightRoot;
        [SerializeField] private Image rightShadow;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private bool hideWhenNoSpeaker = true;

        [Header("Layout (kéo slot trên Scene → Capture)")]
        [SerializeField] private Vector2 leftAnchoredPosition = new Vector2(28f, 420f);
        [SerializeField] private Vector2 rightAnchoredPosition = new Vector2(-28f, 420f);
        [SerializeField] private Vector2 slotSizeDelta = new Vector2(440f, 600f);

        [Header("Editor Preview")]
        [SerializeField] private bool editorPreview;
        [SerializeField] private VnSpeakerDefinitionSO previewLeftSpeaker;
        [SerializeField] private VnSpeakerDefinitionSO previewRightSpeaker;

        private VnSpeakerDefinitionSO _leftSpeaker;
        private VnSpeakerDefinitionSO _rightSpeaker;
        private string _activeSpeakerId;
        private string _leftExpression;
        private string _rightExpression;

        public RectTransform LeftRoot => leftRoot;
        public RectTransform RightRoot => rightRoot;

        public void Bind(
            RectTransform left,
            Image leftShadowImage,
            Image leftPortraitImage,
            RectTransform right,
            Image rightShadowImage,
            Image rightPortraitImage)
        {
            leftRoot = left;
            leftShadow = leftShadowImage;
            leftPortrait = leftPortraitImage;
            rightRoot = right;
            rightShadow = rightShadowImage;
            rightPortrait = rightPortraitImage;
            ApplyFixedSlotLayout();
            if (!editorPreview)
            {
                Hide();
            }
        }

        public void Bind(RectTransform portraitRoot, Image shadow, Image portrait)
        {
            leftRoot = portraitRoot;
            leftShadow = shadow;
            leftPortrait = portrait;
            ApplyFixedSlotLayout();
            if (!editorPreview)
            {
                Hide();
            }
        }

        public void Show(VnSpeakerDefinitionSO speaker, string expressionId = null)
        {
            editorPreview = false;
            if (speaker == null || speaker.ResolveBust(expressionId) == null)
            {
                if (hideWhenNoSpeaker)
                {
                    Hide();
                }

                return;
            }

            AssignSpeaker(speaker, expressionId);
            _activeSpeakerId = speaker.speakerId;
            RefreshSlots();
        }

        public void DimAll()
        {
            editorPreview = false;
            _activeSpeakerId = null;
            RefreshSlots();
        }

        public void Hide()
        {
            editorPreview = false;
            _leftSpeaker = null;
            _rightSpeaker = null;
            _activeSpeakerId = null;
            _leftExpression = null;
            _rightExpression = null;
            SetSlotActive(leftRoot, false);
            SetSlotActive(rightRoot, false);
        }

        public void ClearStage()
        {
            Hide();
        }

        public void ApplyStandardLayout()
        {
            leftAnchoredPosition = VnDialoguePortraitLayout.LeftAnchoredPosition;
            rightAnchoredPosition = VnDialoguePortraitLayout.RightAnchoredPosition;
            slotSizeDelta = VnDialoguePortraitLayout.SizeDelta;
            ApplyFixedSlotLayout();
        }

        public void CaptureLayoutFromSlots()
        {
            if (leftRoot != null)
            {
                leftAnchoredPosition = leftRoot.anchoredPosition;
                slotSizeDelta = leftRoot.sizeDelta;
            }

            if (rightRoot != null)
            {
                rightAnchoredPosition = rightRoot.anchoredPosition;
                if (leftRoot == null)
                {
                    slotSizeDelta = rightRoot.sizeDelta;
                }
            }
        }

        public void ApplySavedLayoutToSlots()
        {
            ApplyFixedSlotLayout();
        }

        public void ApplyEditorPreview(VnSpeakerDefinitionSO left, VnSpeakerDefinitionSO right)
        {
            previewLeftSpeaker = left;
            previewRightSpeaker = right;
            editorPreview = true;
            _leftSpeaker = left;
            _rightSpeaker = right;
            _leftExpression = null;
            _rightExpression = null;
            _activeSpeakerId = right != null ? right.speakerId : left != null ? left.speakerId : null;
            CaptureLayoutFromSlots();
            RefreshSlots();
        }

        public void RefreshEditorPreviewIfNeeded()
        {
            if (!Application.isPlaying && editorPreview)
            {
                ApplyEditorPreview(previewLeftSpeaker, previewRightSpeaker);
            }
        }

        private void AssignSpeaker(VnSpeakerDefinitionSO speaker, string expressionId)
        {
            if (_leftSpeaker != null && _leftSpeaker.speakerId == speaker.speakerId)
            {
                _leftSpeaker = speaker;
                _leftExpression = expressionId;
                return;
            }

            if (_rightSpeaker != null && _rightSpeaker.speakerId == speaker.speakerId)
            {
                _rightSpeaker = speaker;
                _rightExpression = expressionId;
                return;
            }

            if (speaker.IsProtagonist)
            {
                if (_rightSpeaker != null && !_rightSpeaker.IsProtagonist && _leftSpeaker == null)
                {
                    _leftSpeaker = _rightSpeaker;
                    _leftExpression = _rightExpression;
                }

                _rightSpeaker = speaker;
                _rightExpression = expressionId;
                return;
            }

            if (_leftSpeaker == null)
            {
                _leftSpeaker = speaker;
                _leftExpression = expressionId;
                return;
            }

            if (_rightSpeaker == null)
            {
                _rightSpeaker = speaker;
                _rightExpression = expressionId;
                return;
            }

            if (_activeSpeakerId == (_leftSpeaker != null ? _leftSpeaker.speakerId : null))
            {
                _rightSpeaker = speaker;
                _rightExpression = expressionId;
            }
            else
            {
                _leftSpeaker = speaker;
                _leftExpression = expressionId;
            }
        }

        private void RefreshSlots()
        {
            PaintSlot(true, _leftSpeaker, _leftExpression);
            PaintSlot(false, _rightSpeaker, _rightExpression);
        }

        private void PaintSlot(bool left, VnSpeakerDefinitionSO speaker, string expressionId)
        {
            var root = left ? leftRoot : rightRoot;
            var shadow = left ? leftShadow : rightShadow;
            var portrait = left ? leftPortrait : rightPortrait;

            if (speaker == null)
            {
                SetSlotActive(root, false);
                return;
            }

            var bust = speaker.ResolveBust(expressionId);
            if (bust == null)
            {
                SetSlotActive(root, false);
                return;
            }

            SetSlotActive(root, true);
            var active = !string.IsNullOrEmpty(_activeSpeakerId) && speaker.speakerId == _activeSpeakerId;
            if (editorPreview && string.IsNullOrEmpty(_activeSpeakerId))
            {
                active = true;
            }

            var flip = left == !speaker.facesRight;
            var tint = active ? Color.white : VnDialoguePortraitLayout.InactiveTint;
            var faceScale = flip ? new Vector3(-1f, 1f, 1f) : Vector3.one;
            var offset = speaker.shadowOffsetPixels.sqrMagnitude > 0.01f
                ? speaker.shadowOffsetPixels
                : VnDialoguePortraitLayout.DefaultShadowOffset;
            if (flip)
            {
                offset = new Vector2(-offset.x, offset.y);
            }

            if (portrait != null)
            {
                portrait.sprite = bust;
                portrait.preserveAspect = true;
                portrait.color = tint;
                portrait.enabled = true;
                portrait.rectTransform.localScale = faceScale;
            }

            if (shadow != null)
            {
                shadow.sprite = bust;
                shadow.preserveAspect = true;
                var shadowColor = speaker.shadowColor.a > 0f
                    ? speaker.shadowColor
                    : VnDialoguePortraitLayout.DefaultShadowColor;
                if (!active)
                {
                    shadowColor.a *= 0.55f;
                }

                shadow.color = shadowColor;
                shadow.rectTransform.anchoredPosition = offset;
                shadow.rectTransform.localScale = faceScale;
                shadow.enabled = true;
            }
        }

        private void ApplyFixedSlotLayout()
        {
            LayoutSlot(leftRoot, true);
            LayoutSlot(rightRoot, false);
        }

        private void LayoutSlot(RectTransform root, bool left)
        {
            if (root == null)
            {
                return;
            }

            if (left)
            {
                root.anchorMin = VnDialoguePortraitLayout.LeftAnchorMin;
                root.anchorMax = VnDialoguePortraitLayout.LeftAnchorMax;
                root.pivot = VnDialoguePortraitLayout.LeftPivot;
                root.anchoredPosition = leftAnchoredPosition;
            }
            else
            {
                root.anchorMin = VnDialoguePortraitLayout.RightAnchorMin;
                root.anchorMax = VnDialoguePortraitLayout.RightAnchorMax;
                root.pivot = VnDialoguePortraitLayout.RightPivot;
                root.anchoredPosition = rightAnchoredPosition;
            }

            root.sizeDelta = slotSizeDelta;
            root.localScale = Vector3.one;
        }

        private void SetSlotActive(RectTransform root, bool active)
        {
            if (root == null)
            {
                return;
            }

            if (root.gameObject == gameObject)
            {
                SetHostSlotVisualsActive(active);
                return;
            }

            if (root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        private void SetHostSlotVisualsActive(bool active)
        {
            if (leftShadow != null)
            {
                leftShadow.enabled = active && leftShadow.sprite != null;
            }

            if (leftPortrait != null)
            {
                leftPortrait.enabled = active && leftPortrait.sprite != null;
            }
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                editorPreview = false;
                Hide();
                return;
            }

            if (editorPreview)
            {
                ApplyEditorPreview(previewLeftSpeaker, previewRightSpeaker);
            }
        }
    }
}
