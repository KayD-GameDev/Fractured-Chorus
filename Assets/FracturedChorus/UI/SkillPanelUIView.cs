using System;
using System.Collections;
using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;
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
        private Coroutine _enableBackdropRoutine;
        private float _backdropDismissUnlockTime;
        private SkillDefinitionSO _draggingSkill;
        private GameObject _dragGhost;
        private Image _dragGhostIcon;
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
            Action onSkillDragEnd = null)
        {
            _session = session;
            _onSkillDroppedAtScreen = onSkillDroppedAtScreen;
            _onSkillDragPreview = onSkillDragPreview;
            _onSkillDragEnd = onSkillDragEnd;
        }

        public void ToggleForUnit(CombatUnit unit, UnitView unitView)
        {
            if (unit == null || unitView == null)
            {
                return;
            }

            if (_session != null && _session.AllowPlayerReposition)
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
            _onSkillDragEnd?.Invoke();

            var consumed = _onSkillDroppedAtScreen?.Invoke(_currentUnit, skill, screenPos) ?? false;
            if (consumed)
            {
                Hide();
            }

            return consumed;
        }

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
                img.color = new Color(0.95f, 0.62f, 0.25f, 0.75f);
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

                var labelGo = new GameObject("Label", typeof(RectTransform));
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.SetParent(rect, false);
                labelRect.anchorMin = new Vector2(1f, 1f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(1f, 1f);
                labelRect.anchoredPosition = new Vector2(-4f, -2f);
                labelRect.sizeDelta = new Vector2(28f, 18f);
                _dragGhostLabel = labelGo.AddComponent<Text>();
                _dragGhostLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _dragGhostLabel.fontSize = 12;
                _dragGhostLabel.alignment = TextAnchor.UpperRight;
                _dragGhostLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _dragGhostLabel.verticalOverflow = VerticalWrapMode.Overflow;
                _dragGhostLabel.color = Color.white;
                _dragGhostLabel.raycastTarget = false;
            }

            if (_dragGhostIcon == null)
            {
                var iconRoot = _dragGhost.transform.Find("Icon");
                _dragGhostIcon = iconRoot?.Find("Art")?.GetComponent<Image>()
                    ?? iconRoot?.GetComponent<Image>();
            }

            if (_dragGhostLabel == null)
            {
                _dragGhostLabel = _dragGhost.GetComponentInChildren<Text>();
            }

            var hasIcon = skill != null && skill.icon != null;
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
                EndSkillDrag(_draggingSkill, GetMouseScreenPosition());
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

            HideDismissBackdrop();
            DestroyDragGhost();
            _draggingSkill = null;
            _keyboardDragActive = false;
            _onSkillDragEnd?.Invoke();

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
