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
    /// Bảng skill dạng radial: 3 ô (Top=W, Left=A, Right=D) + token tròn ở tâm để kéo-thả.
    /// Bảng hiện ngay tại con trỏ chuột. Không còn ô Guard (guard nay dùng giữ Spacebar).
    /// </summary>
    public class SkillPanelUIView : MonoBehaviour
    {
        private const float PanelSize = 240f;
        private const float SlotSize = 70f;
        private const float TokenSize = 56f;
        private const float Radius = 78f;

        [SerializeField] private RectTransform panelRect;
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GameObject dismissBackdrop;

        private CombatSession _session;
        private CombatUnit _currentUnit;
        private UnitView _currentUnitView;
        private Func<CombatUnit, SkillDefinitionSO, bool> _onSkillSelected;
        private Func<CombatUnit, SkillDefinitionSO, Vector2, bool> _onSkillDroppedAtScreen;
        private Action<CombatUnit, Vector2> _onSkillDragPreview;
        private Action _onSkillDragEnd;
        private Coroutine _enableBackdropRoutine;
        private float _backdropDismissUnlockTime;

        private RectTransform _radialRoot;
        private readonly List<SkillRadialSlotView> _slots = new();
        private SkillCenterTokenView _token;
        private GameObject _dragGhost;

        public event Action<bool> VisibilityChanged;

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

            if (buttonContainer == null)
            {
                var buttons = transform.Find("Buttons");
                if (buttons != null)
                {
                    buttonContainer = buttons as RectTransform;
                }
            }

            if (titleLabel == null)
            {
                var title = transform.Find("Title");
                if (title != null)
                {
                    titleLabel = title.GetComponent<Text>();
                }
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        public void Bind(CombatSession session,
            Func<CombatUnit, SkillDefinitionSO, bool> onSkillSelected,
            Func<CombatUnit, SkillDefinitionSO, Vector2, bool> onSkillDroppedAtScreen = null,
            Action<CombatUnit, Vector2> onSkillDragPreview = null,
            Action onSkillDragEnd = null)
        {
            _session = session;
            _onSkillSelected = onSkillSelected;
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
            ApplyRadialPanelLayout();

            _currentUnit = unit;
            _currentUnitView = unitView;
            if (titleLabel != null)
            {
                titleLabel.text = unit.DisplayName;
            }

            BuildRadial();

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
            VisibilityChanged?.Invoke(true);
        }

        private IEnumerator EnableBackdropNextFrame()
        {
            yield return null;
            ShowDismissBackdrop();
            _enableBackdropRoutine = null;
        }

        private void ApplyRadialPanelLayout()
        {
            if (panelRect == null)
            {
                return;
            }

            // Radial là redesign cơ bản — luôn ép square + pivot tâm dù scene set preserveSceneLayout.
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelSize, PanelSize);
            panelRect.localScale = Vector3.one;

            var bg = panelRect.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = UiCircleSpriteUtil.Circle;
                bg.type = Image.Type.Simple;
                bg.color = new Color(0.06f, 0.06f, 0.1f, 0.82f);
            }

            ConfigureTitle();
        }

        private void ConfigureTitle()
        {
            if (titleLabel == null)
            {
                return;
            }

            var titleRect = titleLabel.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -6f);
            titleRect.sizeDelta = new Vector2(PanelSize - 12f, 24f);
            titleLabel.alignment = TextAnchor.MiddleCenter;
            titleLabel.raycastTarget = false;
        }

        private void BuildRadial()
        {
            EnsureRadialRoot();
            ClearRadial();

            if (buttonContainer != null)
            {
                buttonContainer.gameObject.SetActive(false);
            }

            var skills = CollectUsableSkills();

            CreateSlot(SkillRadialDirection.Top, TopPosition(), "W", skills, 0);
            CreateSlot(SkillRadialDirection.Left, LeftPosition(), "A", skills, 1);
            CreateSlot(SkillRadialDirection.Right, RightPosition(), "D", skills, 2);

            CreateToken();
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

        private void CreateSlot(SkillRadialDirection dir, Vector2 pos, string keyHint,
            List<SkillDefinitionSO> skills, int skillIndex)
        {
            var skill = skillIndex < skills.Count ? skills[skillIndex] : null;
            var slotGo = new GameObject($"SkillSlot_{dir}");
            var slot = slotGo.AddComponent<SkillRadialSlotView>();
            var capturedSkill = skill;
            slot.Build(_radialRoot, pos, SlotSize, dir, skill, keyHint, () => HandleSkillChosen(capturedSkill), this);
            _slots.Add(slot);
        }

        private void CreateToken()
        {
            var tokenGo = new GameObject("SkillCenterToken");
            _token = tokenGo.AddComponent<SkillCenterTokenView>();
            _token.Build(_radialRoot, TokenSize, this, GetRootCanvas(), GetUiCamera(), Vector2.zero);
            _token.transform.SetAsLastSibling();
        }

        private void HandleSkillChosen(SkillDefinitionSO skill)
        {
            if (skill == null)
            {
                return;
            }

            var armed = _onSkillSelected?.Invoke(_currentUnit, skill) ?? false;
            if (armed)
            {
                Hide();
            }
            else
            {
                FlashUnaffordable(skill);
            }
        }

        private void FlashUnaffordable(SkillDefinitionSO skill)
        {
            _token?.ResetToHome();
        }

        /// <summary>Token được thả tại điểm màn hình — chọn ô gần/chứa điểm đó. True nếu đã chọn.</summary>
        public bool TrySelectSlotAtScreenPoint(Vector2 screenPoint)
        {
            var camera = GetUiCamera();
            SkillRadialSlotView best = null;
            var bestDist = float.MaxValue;

            foreach (var slot in _slots)
            {
                if (slot == null || !slot.HasSkill || slot.Rect == null)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(slot.Rect, screenPoint, camera))
                {
                    best = slot;
                    break;
                }

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _radialRoot, screenPoint, camera, out var local))
                {
                    var dist = Vector2.Distance(local, slot.Rect.anchoredPosition);
                    if (dist < bestDist && dist <= SlotSize)
                    {
                        bestDist = dist;
                        best = slot;
                    }
                }
            }

            if (best == null)
            {
                return false;
            }

            best.SetHighlight(true);
            best.Select();
            return true;
        }

        // ---------------------------------------------------------------------
        // Kéo skill từ ô radial → thả lên timeline (lane của unit)
        // ---------------------------------------------------------------------

        public void BeginSkillDrag(SkillDefinitionSO skill)
        {
            EnsureDragGhost(skill);
        }

        public void UpdateSkillDrag(Vector2 screenPos)
        {
            MoveDragGhost(screenPos);
            _onSkillDragPreview?.Invoke(_currentUnit, screenPos);
        }

        public bool EndSkillDrag(SkillDefinitionSO skill, Vector2 screenPos)
        {
            DestroyDragGhost();
            _onSkillDragEnd?.Invoke();

            var consumed = _onSkillDroppedAtScreen?.Invoke(_currentUnit, skill, screenPos) ?? false;
            if (consumed)
            {
                Hide();
            }
            else
            {
                _token?.ResetToHome();
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
                rect.sizeDelta = new Vector2(TokenSize, TokenSize);

                var img = _dragGhost.AddComponent<Image>();
                img.sprite = UiCircleSpriteUtil.Circle;
                img.color = new Color(0.95f, 0.62f, 0.25f, 0.75f);
                img.raycastTarget = false;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.SetParent(rect, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(2f, 2f);
                labelRect.offsetMax = new Vector2(-2f, -2f);
                var label = labelGo.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 11;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.color = Color.white;
                label.raycastTarget = false;
            }

            var ghostLabel = _dragGhost.GetComponentInChildren<Text>();
            if (ghostLabel != null)
            {
                ghostLabel.text = skill != null ? SkillUiNames.GetDisplayName(skill) : string.Empty;
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
            if (!IsVisible || _slots.Count == 0)
            {
                return;
            }

            HandleKeyboardSelection();
        }

        private void HandleKeyboardSelection()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            {
                SelectDirection(SkillRadialDirection.Top);
            }
            else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
            {
                SelectDirection(SkillRadialDirection.Left);
            }
            else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
            {
                SelectDirection(SkillRadialDirection.Right);
            }
#else
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                SelectDirection(SkillRadialDirection.Top);
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SelectDirection(SkillRadialDirection.Left);
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                SelectDirection(SkillRadialDirection.Right);
            }
#endif
        }

        private void SelectDirection(SkillRadialDirection direction)
        {
            foreach (var slot in _slots)
            {
                if (slot != null && slot.Direction == direction && slot.HasSkill)
                {
                    slot.SetHighlight(true);
                    slot.Select();
                    return;
                }
            }
        }

        private static Vector2 TopPosition() => new Vector2(0f, Radius);
        private static Vector2 LeftPosition() => new Vector2(-Radius * 0.866f, -Radius * 0.5f);
        private static Vector2 RightPosition() => new Vector2(Radius * 0.866f, -Radius * 0.5f);

        private void EnsureRadialRoot()
        {
            if (_radialRoot != null)
            {
                return;
            }

            var rootGo = new GameObject("Radial", typeof(RectTransform));
            _radialRoot = rootGo.GetComponent<RectTransform>();
            _radialRoot.SetParent(panelRect, false);
            _radialRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _radialRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _radialRoot.pivot = new Vector2(0.5f, 0.5f);
            _radialRoot.anchoredPosition = new Vector2(0f, -10f);
            _radialRoot.sizeDelta = new Vector2(PanelSize, PanelSize);
        }

        private void ClearRadial()
        {
            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            _slots.Clear();

            if (_token != null)
            {
                Destroy(_token.gameObject);
                _token = null;
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

            // Pivot đáy-giữa → bảng nổi PHÍA TRÊN đầu nhân vật, cách 1 khoảng nhỏ.
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

        private Canvas GetRootCanvas()
        {
            var rect = GetRootCanvasRectTransform();
            return rect != null ? rect.GetComponent<Canvas>() : null;
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
            _onSkillDragEnd?.Invoke();

            if (panelRect != null && panelRect.gameObject.activeSelf)
            {
                panelRect.gameObject.SetActive(false);
                VisibilityChanged?.Invoke(false);
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
