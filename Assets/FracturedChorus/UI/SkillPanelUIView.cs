using System;
using System.Collections;
using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;
using FracturedChorus.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.UI
{
    /// <summary>
    /// Bảng skill dạng radial: 3 ô scene (Top=W, Left=A, Right=D).
    /// Click highlight; W/A/D gắn skill vào chuột; thả vào timeline để gán.
    /// </summary>
    public class SkillPanelUIView : MonoBehaviour
    {
        private const float FallbackPanelSize = 240f;

        [SerializeField] private RectTransform panelRect;
        [SerializeField] private RectTransform radialRoot;
        [SerializeField] private SkillRadialSlotView slotTop;
        [SerializeField] private SkillRadialSlotView slotLeft;
        [SerializeField] private SkillRadialSlotView slotRight;
        [Tooltip("Inactive chrome template under Radial. Empty → dùng SkillSlot_Top.")]
        [SerializeField] private RectTransform skillSlotTemplate;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GameObject dismissBackdrop;

        [Header("Layout")]
        [SerializeField] private bool preserveSceneLayout = true;
        [SerializeField] private float fallbackPanelSize = FallbackPanelSize;

        private CombatSession _session;
        private CombatUnit _currentUnit;
        private UnitView _currentUnitView;
        private Func<CombatUnit, SkillDefinitionSO, Vector2, bool> _onSkillDroppedAtScreen;
        private Action<CombatUnit, SkillDefinitionSO, Vector2> _onSkillDragPreview;
        private Action _onSkillDragEnd;
        private Func<bool> _isTimelinePlaybackActive;
        private Coroutine _enableBackdropRoutine;
        private float _backdropDismissUnlockTime;
        private SkillDefinitionSO _draggingSkill;
        private GameObject _dragGhost;
        private Image _dragGhostIcon;
        private Image _dragGhostFrame;
        private Text _dragGhostLabel;
        private bool _keyboardDragActive;

        private readonly List<SkillRadialSlotView> _slots = new();

        public bool ShouldIgnoreOutsideDismiss => Time.unscaledTime < _backdropDismissUnlockTime;

        public bool IsVisible => panelRect != null && panelRect.gameObject.activeSelf;

        private void Awake()
        {
            WireReferences();
            StripNestedCanvasIfAny();
        }

        public void WireReferences()
        {
            if (panelRect == null)
            {
                panelRect = transform as RectTransform;
            }

            if (radialRoot == null)
            {
                radialRoot = transform.Find("Radial") as RectTransform;
            }

            if (slotTop == null)
            {
                slotTop = radialRoot?.Find("SkillSlot_Top")?.GetComponent<SkillRadialSlotView>();
            }

            if (slotLeft == null)
            {
                slotLeft = radialRoot?.Find("SkillSlot_Left")?.GetComponent<SkillRadialSlotView>();
            }

            if (slotRight == null)
            {
                slotRight = radialRoot?.Find("SkillSlot_Right")?.GetComponent<SkillRadialSlotView>();
            }

            if (skillSlotTemplate == null)
            {
                skillSlotTemplate = SkillSlotChromeSync.ResolveTemplate(
                    radialRoot,
                    slotTop != null ? slotTop.transform as RectTransform : null);
            }

            if (titleLabel == null)
            {
                titleLabel = transform.Find("Title")?.GetComponent<Text>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            CacheSceneSlots();
        }

        private void CacheSceneSlots()
        {
            _slots.Clear();
            if (slotTop != null)
            {
                _slots.Add(slotTop);
            }

            if (slotLeft != null)
            {
                _slots.Add(slotLeft);
            }

            if (slotRight != null)
            {
                _slots.Add(slotRight);
            }
        }

        public void Bind(CombatSession session,
            Func<CombatUnit, SkillDefinitionSO, Vector2, bool> onSkillDroppedAtScreen = null,
            Action<CombatUnit, SkillDefinitionSO, Vector2> onSkillDragPreview = null,
            Action onSkillDragEnd = null,
            Func<bool> isTimelinePlaybackActive = null)
        {
            _session = session;
            _onSkillDroppedAtScreen = onSkillDroppedAtScreen;
            _onSkillDragPreview = onSkillDragPreview;
            _onSkillDragEnd = onSkillDragEnd;
            _isTimelinePlaybackActive = isTimelinePlaybackActive;
        }

        /// <summary>
        /// Deploy / không Planning / timeline đang playback (intro·execute) → không mở skill UI.
        /// Intro-pause (IsPlaybackActive=false) vẫn cho mở.
        /// </summary>
        public bool CanOpenSkillPanelNow()
        {
            if (_session == null || _session.Phase != CombatPhase.Planning || _session.AllowPlayerReposition)
            {
                return false;
            }

            if (_isTimelinePlaybackActive != null && _isTimelinePlaybackActive())
            {
                return false;
            }

            return true;
        }

        public void ToggleForUnit(CombatUnit unit, UnitView unitView)
        {
            if (unit == null || unitView == null)
            {
                return;
            }

            if (!CanOpenSkillPanelNow())
            {
                return;
            }

            if (_currentUnitView == unitView && IsVisible)
            {
                Hide();
                return;
            }

            ShowForUnit(unit, unitView);
        }

        public void ShowForUnit(CombatUnit unit, UnitView unitView)
        {
            if (unit == null || unitView == null || _session == null || panelRect == null)
            {
                return;
            }

            if (!CanOpenSkillPanelNow())
            {
                return;
            }

            WireReferences();
            StripNestedCanvasIfAny();
            ApplyPanelLayout();

            _currentUnit = unit;
            _currentUnitView = unitView;
            if (titleLabel != null)
            {
                titleLabel.text = unit.DisplayName;
            }

            BindRadialSlots();

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            panelRect.gameObject.SetActive(true);
            panelRect.SetAsLastSibling();
            PositionAboveUnit(unitView);

            _backdropDismissUnlockTime = Time.unscaledTime + 0.15f;
            if (_enableBackdropRoutine != null)
            {
                StopCoroutine(_enableBackdropRoutine);
            }

            _enableBackdropRoutine = StartCoroutine(EnableBackdropNextFrame());
        }

        private IEnumerator EnableBackdropNextFrame()
        {
            yield return null;
            ShowDismissBackdrop();
            _enableBackdropRoutine = null;
        }

        private void ApplyPanelLayout()
        {
            if (panelRect == null)
            {
                return;
            }

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            if (!preserveSceneLayout)
            {
                var size = RectSizeUtil.ResolveMinExtent(panelRect, fallbackPanelSize);
                panelRect.sizeDelta = new Vector2(size, size);
            }

            var bg = panelRect.GetComponent<Image>();
            if (bg != null)
            {
                if (bg.sprite == null || !preserveSceneLayout)
                {
                    bg.sprite = UiCircleSpriteUtil.Circle;
                    bg.type = Image.Type.Simple;
                }

                if (!preserveSceneLayout)
                {
                    bg.color = new Color(0.06f, 0.06f, 0.1f, 0.82f);
                }
            }

            if (titleLabel != null)
            {
                titleLabel.alignment = TextAnchor.MiddleCenter;
                titleLabel.raycastTarget = false;
            }
        }

        private void BindRadialSlots()
        {
            var skills = CollectUsableSkills();
            WireReferences();
            SyncSlotsFromTemplate();

            if (_slots.Count < 3)
            {
                Debug.LogWarning(
                    "[SkillPanelUI] Missing scene Radial slots — run Fractured Chorus → Setup Skill Panel in Hierarchy and save scene.");
                return;
            }

            BindSlot(slotTop, SkillRadialDirection.Top, "W", skills, 0);
            BindSlot(slotLeft, SkillRadialDirection.Left, "A", skills, 1);
            BindSlot(slotRight, SkillRadialDirection.Right, "D", skills, 2);
        }

        /// <summary>
        /// Áp chrome từ SkillSlot_Template lên 3 ô — mọi nhân vật (Ren/Coda/…) dùng cùng layout.
        /// </summary>
        private void SyncSlotsFromTemplate()
        {
            EnsureSkillSlotTemplateExists();

            skillSlotTemplate = SkillSlotChromeSync.ResolveTemplate(
                radialRoot,
                slotTop != null ? slotTop.transform as RectTransform : null);

            var template = skillSlotTemplate;
            if (template == null)
            {
                return;
            }

            // Chỉ tắt SkillSlot_Template riêng — không tắt Top khi đang fallback.
            if (template.name == SkillSlotChromeSync.TemplateName)
            {
                template.gameObject.SetActive(false);
            }

            ApplyTemplateToSlot(template, slotTop);
            ApplyTemplateToSlot(template, slotLeft);
            ApplyTemplateToSlot(template, slotRight);
        }

        /// <summary>Tạo inactive SkillSlot_Template từ Top nếu Hierarchy chưa có.</summary>
        private void EnsureSkillSlotTemplateExists()
        {
            if (radialRoot == null || slotTop == null)
            {
                return;
            }

            var existing = radialRoot.Find(SkillSlotChromeSync.TemplateName) as RectTransform;
            if (existing != null)
            {
                skillSlotTemplate = existing;
                existing.gameObject.SetActive(false);
                return;
            }

            var clone = Instantiate(slotTop.gameObject, radialRoot);
            clone.name = SkillSlotChromeSync.TemplateName;
            clone.SetActive(false);

            var slotView = clone.GetComponent<SkillRadialSlotView>();
            if (slotView != null)
            {
                Destroy(slotView);
            }

            var button = clone.GetComponent<Button>();
            if (button != null)
            {
                Destroy(button);
            }

            var templateRt = clone.GetComponent<RectTransform>();
            templateRt.anchoredPosition = Vector2.zero;
            SkillSlotChromeSync.ApplySiblingOrder(templateRt);
            clone.transform.SetAsFirstSibling();
            skillSlotTemplate = templateRt;
        }

        private static void ApplyTemplateToSlot(RectTransform template, SkillRadialSlotView slot)
        {
            if (slot == null)
            {
                return;
            }

            SkillSlotChromeSync.ApplyFromTemplate(template, slot.transform as RectTransform);
        }

        private void BindSlot(SkillRadialSlotView slot, SkillRadialDirection dir, string keyHint,
            List<SkillDefinitionSO> skills, int skillIndex)
        {
            if (slot == null)
            {
                return;
            }

            slot.WireFromScene(dir);
            var skill = skillIndex < skills.Count ? skills[skillIndex] : null;
            slot.Bind(skill, keyHint, () => HandleSkillHighlighted(slot, skill), this);
        }

        private List<SkillDefinitionSO> CollectUsableSkills()
        {
            var result = new List<SkillDefinitionSO>();
            if (_currentUnit?.Skills == null)
            {
                return result;
            }

            foreach (var skill in _currentUnit.Skills)
            {
                if (skill == null || skill.IsGuard)
                {
                    continue;
                }

                result.Add(skill);
                if (result.Count >= 3)
                {
                    break;
                }
            }

            return result;
        }

        private void HandleSkillHighlighted(SkillRadialSlotView slot, SkillDefinitionSO skill)
        {
            if (skill == null)
            {
                return;
            }

            foreach (var s in _slots)
            {
                s?.SetHighlight(s == slot);
            }
        }

        public void BeginSkillDrag(SkillDefinitionSO skill)
        {
            _draggingSkill = skill;
            SetDismissBackdropRaycast(false);
            EnsureDragGhost(skill);
        }

        public void UpdateSkillDrag(Vector2 screenPos)
        {
            MoveDragGhost(screenPos);
            if (_draggingSkill != null)
            {
                _onSkillDragPreview?.Invoke(_currentUnit, _draggingSkill, screenPos);
            }
        }

        public bool EndSkillDrag(SkillDefinitionSO skill, Vector2 screenPos)
        {
            DestroyDragGhost();
            _draggingSkill = null;
            _keyboardDragActive = false;
            SetDismissBackdropRaycast(true);
            _onSkillDragEnd?.Invoke();

            var consumed = _onSkillDroppedAtScreen?.Invoke(_currentUnit, skill, screenPos) ?? false;
            if (consumed)
            {
                Hide();
            }

            return consumed;
        }

        private void SetDismissBackdropRaycast(bool enabled)
        {
            if (dismissBackdrop == null)
            {
                return;
            }

            var image = dismissBackdrop.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = enabled;
            }
        }

        /// <summary>
        /// Click outside the radial while a skill is armed (W/A/D ghost) — attempt drop at screen point.
        /// Returns true when the armed skill was consumed (placed or cancelled from armed state).
        /// </summary>
        public bool TryConsumeArmedSkillDrop(Vector2 screenPos)
        {
            if (!_keyboardDragActive || _draggingSkill == null)
            {
                return false;
            }

            var skill = _draggingSkill;
            EndSkillDrag(skill, screenPos);
            return true;
        }

        public bool IsSkillArmed => _keyboardDragActive && _draggingSkill != null;

        private void EnsureDragGhost(SkillDefinitionSO skill)
        {
            var canvasRect = GetRootCanvasRectTransform();
            if (canvasRect == null)
            {
                return;
            }

            if (_dragGhost == null)
            {
                _dragGhost = new GameObject("SkillDragGhost", typeof(RectTransform));
                var rect = _dragGhost.GetComponent<RectTransform>();
                rect.SetParent(canvasRect, false);
                rect.sizeDelta = new Vector2(72f, 72f);

                var img = _dragGhost.AddComponent<Image>();
                img.sprite = UiCircleSpriteUtil.Circle;
                img.color = FcColorTokens.WithAlpha(FcColorTokens.Brand.CyanNeonBody, 0.75f);
                img.raycastTarget = false;

                var iconGo = new GameObject("Icon", typeof(RectTransform));
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.SetParent(rect, false);
                iconRect.anchorMin = new Vector2(0.08f, 0.08f);
                iconRect.anchorMax = new Vector2(0.92f, 0.92f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                var maskGraphic = iconGo.AddComponent<Image>();
                maskGraphic.sprite = UiCircleSpriteUtil.Circle;
                maskGraphic.type = Image.Type.Simple;
                maskGraphic.color = Color.white;
                maskGraphic.raycastTarget = false;
                var mask = iconGo.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                var artGo = new GameObject("Art", typeof(RectTransform));
                var artRect = artGo.GetComponent<RectTransform>();
                artRect.SetParent(iconRect, false);
                artRect.anchorMin = Vector2.zero;
                artRect.anchorMax = Vector2.one;
                artRect.offsetMin = Vector2.zero;
                artRect.offsetMax = Vector2.zero;
                _dragGhostIcon = artGo.AddComponent<Image>();
                _dragGhostIcon.raycastTarget = false;
                _dragGhostIcon.preserveAspect = true;

                var frameGo = new GameObject("Frame", typeof(RectTransform));
                var frameRect = frameGo.GetComponent<RectTransform>();
                frameRect.SetParent(rect, false);
                frameRect.anchorMin = Vector2.zero;
                frameRect.anchorMax = Vector2.one;
                frameRect.offsetMin = new Vector2(-6f, -6f);
                frameRect.offsetMax = new Vector2(6f, 6f);
                _dragGhostFrame = frameGo.AddComponent<Image>();
                _dragGhostFrame.sprite = UiCircleSpriteUtil.Circle;
                _dragGhostFrame.type = Image.Type.Simple;
                _dragGhostFrame.color = new Color(0.92f, 0.78f, 0.42f, 0.95f);
                _dragGhostFrame.raycastTarget = false;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.SetParent(rect, false);
                labelRect.anchorMin = new Vector2(1f, 1f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(1f, 1f);
                labelRect.anchoredPosition = new Vector2(-4f, -2f);
                labelRect.sizeDelta = new Vector2(28f, 18f);
                _dragGhostLabel = labelGo.AddComponent<Text>();
                _dragGhostLabel.font = UiFontCatalog.Body;
                _dragGhostLabel.fontSize = 12;
                _dragGhostLabel.alignment = TextAnchor.UpperRight;
                _dragGhostLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _dragGhostLabel.verticalOverflow = VerticalWrapMode.Overflow;
                _dragGhostLabel.color = Color.white;
                _dragGhostLabel.raycastTarget = false;

                // Icon → Frame → Label
                iconGo.transform.SetSiblingIndex(0);
                frameGo.transform.SetSiblingIndex(1);
                labelGo.transform.SetSiblingIndex(2);
            }

            if (_dragGhostIcon == null)
            {
                var iconRoot = _dragGhost.transform.Find("Icon");
                _dragGhostIcon = iconRoot?.Find("Art")?.GetComponent<Image>()
                    ?? iconRoot?.GetComponent<Image>();
            }

            if (_dragGhostFrame == null)
            {
                _dragGhostFrame = _dragGhost.transform.Find("Frame")?.GetComponent<Image>();
            }

            if (_dragGhostLabel == null)
            {
                _dragGhostLabel = _dragGhost.GetComponentInChildren<Text>();
            }

            var hasIcon = skill != null && skill.icon != null;
            var hasFrame = skill != null && skill.frame != null;
            var bg = _dragGhost.GetComponent<Image>();
            if (bg != null)
            {
                bg.enabled = !hasIcon;
            }

            if (_dragGhostIcon != null)
            {
                _dragGhostIcon.sprite = hasIcon ? skill.icon : null;
                _dragGhostIcon.enabled = hasIcon;
            }

            if (_dragGhostFrame != null)
            {
                if (hasFrame)
                {
                    _dragGhostFrame.sprite = skill.frame;
                    _dragGhostFrame.color = Color.white;
                    _dragGhostFrame.enabled = true;
                }
                else
                {
                    _dragGhostFrame.enabled = _dragGhostFrame.sprite != null;
                }

                var ghostRect = _dragGhost.transform as RectTransform;
                var kind = skill != null ? skill.slotKind : SkillSlotKind.BasicAttack;
                SkillRadialSlotView.FitFrameRectToKind(
                    _dragGhostFrame.rectTransform,
                    ghostRect,
                    kind);
                _dragGhostFrame.preserveAspect = false;
                _dragGhostFrame.raycastTarget = false;
            }

            if (_dragGhostLabel != null)
            {
                var labelRect = _dragGhostLabel.rectTransform;
                if (hasIcon)
                {
                    labelRect.anchorMin = new Vector2(1f, 1f);
                    labelRect.anchorMax = new Vector2(1f, 1f);
                    labelRect.pivot = new Vector2(1f, 1f);
                    labelRect.anchoredPosition = new Vector2(-4f, -2f);
                    labelRect.sizeDelta = new Vector2(28f, 18f);
                    _dragGhostLabel.fontSize = 12;
                    _dragGhostLabel.alignment = TextAnchor.UpperRight;
                    _dragGhostLabel.text = string.Empty;
                }
                else
                {
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(2f, 2f);
                    labelRect.offsetMax = new Vector2(-2f, -2f);
                    _dragGhostLabel.fontSize = 14;
                    _dragGhostLabel.alignment = TextAnchor.MiddleCenter;
                    _dragGhostLabel.text = skill != null ? SkillUiNames.GetDisplayName(skill) : string.Empty;
                }
            }

            _dragGhost.SetActive(true);
            _dragGhost.transform.SetAsLastSibling();
        }

        private void MoveDragGhost(Vector2 screenPos)
        {
            if (_dragGhost == null)
            {
                return;
            }

            var canvasRect = GetRootCanvasRectTransform();
            if (canvasRect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos, GetUiCamera(), out var local))
            {
                ((RectTransform)_dragGhost.transform).anchoredPosition = local;
            }
        }

        private void DestroyDragGhost()
        {
            if (_dragGhost != null)
            {
                Destroy(_dragGhost);
                _dragGhost = null;
            }
        }

        private void Update()
        {
            if (_keyboardDragActive)
            {
                HandleKeyboardDrag();
                return;
            }

            if (!IsVisible || _slots.Count == 0)
            {
                return;
            }

            HandleKeyboardSelection();
        }

        private void HandleKeyboardDrag()
        {
            if (_draggingSkill == null)
            {
                _keyboardDragActive = false;
                return;
            }

            if (TryGetDirectionKeyPressedThisFrame(out var direction))
            {
                SelectDirection(direction);
            }

            UpdateSkillDrag(GetMouseScreenPosition());

            if (WasMouseButtonReleasedThisFrame())
            {
                if (_draggingSkill != null)
                {
                    EndSkillDrag(_draggingSkill, GetMouseScreenPosition());
                }

                _keyboardDragActive = false;
            }
        }

        private static Vector2 GetMouseScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
#endif
            return Input.mousePosition;
        }

        private static bool WasMouseButtonReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.leftButton.wasReleasedThisFrame;
            }
#endif
            return Input.GetMouseButtonUp(0);
        }

        private void HandleKeyboardSelection()
        {
            if (TryGetDirectionKeyPressedThisFrame(out var direction))
            {
                SelectDirection(direction);
            }
        }

        private static bool TryGetDirectionKeyPressedThisFrame(out SkillRadialDirection direction)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null)
            {
                direction = default;
                return false;
            }

            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            {
                direction = SkillRadialDirection.Top;
                return true;
            }

            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
            {
                direction = SkillRadialDirection.Left;
                return true;
            }

            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
            {
                direction = SkillRadialDirection.Right;
                return true;
            }
#else
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                direction = SkillRadialDirection.Top;
                return true;
            }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                direction = SkillRadialDirection.Left;
                return true;
            }

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                direction = SkillRadialDirection.Right;
                return true;
            }
#endif
            direction = default;
            return false;
        }

        private void SelectDirection(SkillRadialDirection direction)
        {
            foreach (var slot in _slots)
            {
                if (slot == null || slot.Direction != direction || !slot.HasSkill)
                {
                    continue;
                }

                foreach (var s in _slots)
                {
                    s?.SetHighlight(s == slot);
                }

                BeginSkillDrag(slot.Skill);
                UpdateSkillDrag(GetMouseScreenPosition());
                _keyboardDragActive = true;
                return;
            }
        }

        private void PositionAboveUnit(UnitView unitView)
        {
            var canvasRect = GetRootCanvasRectTransform();
            if (canvasRect == null || panelRect == null || unitView == null || worldCamera == null)
            {
                return;
            }

            var anchorWorld = unitView.GetSkillPanelAboveAnchorWorld();
            var screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, anchorWorld);

            var camera = GetUiCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPoint, camera, out var localPoint))
            {
                return;
            }

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0f);

            localPoint.y += 14f;

            var halfCanvas = canvasRect.rect.size * 0.5f;
            var halfWidth = panelRect.rect.width * 0.5f;
            var height = panelRect.rect.height;
            localPoint.x = Mathf.Clamp(localPoint.x, -halfCanvas.x + halfWidth, halfCanvas.x - halfWidth);
            localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvas.y, halfCanvas.y - height);

            panelRect.anchoredPosition = localPoint;
        }

        private Camera GetUiCamera()
        {
            var rootCanvas = GetRootCanvas();
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : worldCamera;
            }

            return null;
        }

        private Canvas GetRootCanvas()
        {
            var rect = GetRootCanvasRectTransform();
            return rect != null ? rect.GetComponent<Canvas>() : null;
        }

        private void ShowDismissBackdrop()
        {
            EnsureDismissBackdrop();
            if (dismissBackdrop == null)
            {
                return;
            }

            dismissBackdrop.SetActive(true);
            dismissBackdrop.transform.SetAsLastSibling();
            panelRect.SetAsLastSibling();
        }

        private void HideDismissBackdrop()
        {
            if (dismissBackdrop != null)
            {
                dismissBackdrop.SetActive(false);
            }
        }

        private void EnsureDismissBackdrop()
        {
            if (dismissBackdrop != null)
            {
                return;
            }

            var canvasRect = GetRootCanvasRectTransform();
            if (canvasRect == null)
            {
                return;
            }

            dismissBackdrop = new GameObject("SkillPanelDismissBackdrop", typeof(RectTransform));
            dismissBackdrop.transform.SetParent(canvasRect, false);

            var rect = dismissBackdrop.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = dismissBackdrop.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            var dismiss = dismissBackdrop.AddComponent<SkillPanelDismissBackdrop>();
            dismiss.SetPanel(this);
            dismissBackdrop.SetActive(false);
        }

        private void StripNestedCanvasIfAny()
        {
            if (panelRect == null)
            {
                return;
            }

            var nestedCanvas = panelRect.GetComponent<Canvas>();
            if (nestedCanvas != null)
            {
                DestroyImmediate(nestedCanvas);
            }

            var nestedRaycaster = panelRect.GetComponent<GraphicRaycaster>();
            if (nestedRaycaster != null)
            {
                DestroyImmediate(nestedRaycaster);
            }
        }

        private RectTransform GetRootCanvasRectTransform()
        {
            if (panelRect == null)
            {
                return null;
            }

            var walk = panelRect.parent;
            while (walk != null)
            {
                if (walk.GetComponent<Canvas>() != null)
                {
                    return walk as RectTransform;
                }

                walk = walk.parent;
            }

            return null;
        }

        public void Hide()
        {
            if (_enableBackdropRoutine != null)
            {
                StopCoroutine(_enableBackdropRoutine);
                _enableBackdropRoutine = null;
            }

            // If a skill is still armed, clear ghost without treating as a successful place.
            if (_draggingSkill != null || _keyboardDragActive)
            {
                DestroyDragGhost();
                _draggingSkill = null;
                _keyboardDragActive = false;
                SetDismissBackdropRaycast(true);
                _onSkillDragEnd?.Invoke();
            }
            else
            {
                DestroyDragGhost();
            }

            SetDismissBackdropRaycast(true);
            HideDismissBackdrop();

            if (panelRect != null && panelRect.gameObject.activeSelf)
            {
                panelRect.gameObject.SetActive(false);
            }

            _currentUnit = null;
            _currentUnitView = null;
        }

        public bool IsShowingUnit(UnitView unitView)
        {
            return _currentUnitView == unitView && IsVisible;
        }
    }
}
