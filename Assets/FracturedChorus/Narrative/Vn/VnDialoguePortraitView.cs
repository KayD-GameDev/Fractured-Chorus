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
            EnsureDualFromLegacy(portraitRoot, shadow, portrait);
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

            EnsureDualSlotsExist();
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

        public void ApplyEditorPreview(VnSpeakerDefinitionSO left, VnSpeakerDefinitionSO right)
        {
            EnsureDualSlotsExist();
            previewLeftSpeaker = left;
            previewRightSpeaker = right;
            editorPreview = true;
            _leftSpeaker = left;
            _rightSpeaker = right;
            _leftExpression = null;
            _rightExpression = null;
            _activeSpeakerId = right != null ? right.speakerId : left != null ? left.speakerId : null;
            ApplyFixedSlotLayout();
            RefreshSlots();
        }

        public void RefreshEditorPreviewIfNeeded()
        {
            if (!Application.isPlaying && editorPreview)
            {
                ApplyEditorPreview(previewLeftSpeaker, previewRightSpeaker);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || !editorPreview)
            {
                return;
            }

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || Application.isPlaying || !editorPreview)
                {
                    return;
                }

                EnsureDualSlotsExist();
                ApplyFixedSlotLayout();
                _leftSpeaker = previewLeftSpeaker;
                _rightSpeaker = previewRightSpeaker;
                _activeSpeakerId = previewRightSpeaker != null
                    ? previewRightSpeaker.speakerId
                    : previewLeftSpeaker != null ? previewLeftSpeaker.speakerId : null;
                RefreshSlots();
            };
        }
#endif

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

        private static void SetSlotActive(RectTransform root, bool active)
        {
            if (root != null)
            {
                root.gameObject.SetActive(active);
            }
        }

        private void EnsureDualSlotsExist()
        {
            if (leftRoot != null && rightRoot != null)
            {
                return;
            }

            if (leftRoot != null)
            {
                EnsureDualFromLegacy(leftRoot, leftShadow, leftPortrait);
            }
        }

        private void EnsureDualFromLegacy(RectTransform portraitRoot, Image shadow, Image portrait)
        {
            if (portraitRoot == null)
            {
                return;
            }

            leftRoot = portraitRoot;
            leftShadow = shadow;
            leftPortrait = portrait;
            leftRoot.name = "DialoguePortrait_Left";

            if (rightRoot != null)
            {
                return;
            }

            var parent = portraitRoot.parent;
            var rightGo = Instantiate(portraitRoot.gameObject, parent, false);
            rightGo.name = "DialoguePortrait_Right";
            rightRoot = rightGo.GetComponent<RectTransform>();
            var images = rightGo.GetComponentsInChildren<Image>(true);
            rightShadow = null;
            rightPortrait = null;
            for (var i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject.name == "Shadow")
                {
                    rightShadow = images[i];
                }
                else if (images[i].gameObject.name == "Portrait")
                {
                    rightPortrait = images[i];
                }
            }

            var nested = rightGo.GetComponent<VnDialoguePortraitView>();
            if (nested != null && nested != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(nested);
                }
                else
                {
                    DestroyImmediate(nested);
                }
            }

            LayoutSlot(leftRoot, true);
            LayoutSlot(rightRoot, false);
            SetSlotActive(rightRoot, false);
        }

        private void Awake()
        {
            if (leftRoot != null && rightRoot == null)
            {
                EnsureDualFromLegacy(leftRoot, leftShadow, leftPortrait);
            }

            ApplyFixedSlotLayout();

            if (!Application.isPlaying && editorPreview)
            {
                ApplyEditorPreview(previewLeftSpeaker, previewRightSpeaker);
            }
            else if (Application.isPlaying && !editorPreview)
            {
                Hide();
            }
        }
    }
}
