using System;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class SkillPanelUIView : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float screenPaddingPx = 1.5f;

        private CombatSession _session;
        private CombatUnit _currentUnit;
        private UnitView _currentUnitView;
        private Func<CombatUnit, SkillDefinitionSO, bool> _onSkillSelected;

        public event Action<bool> VisibilityChanged;

        public bool IsVisible => panelRect != null && panelRect.gameObject.activeSelf;

        private void Awake()
        {
            screenPaddingPx = 1.5f;
            WireReferences();
            StripNestedCanvasIfAny();
            if (panelRect != null)
            {
                panelRect.pivot = new Vector2(0f, 0.5f);
            }
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

        public void Bind(CombatSession session, Func<CombatUnit, SkillDefinitionSO, bool> onSkillSelected)
        {
            _session = session;
            _onSkillSelected = onSkillSelected;
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
            ApplyOverlayLayout();

            _currentUnit = unit;
            _currentUnitView = unitView;
            if (titleLabel != null)
            {
                titleLabel.text = unit.DisplayName;
            }

            RebuildButtons();
            PositionBesideUnit(unitView);
            panelRect.gameObject.SetActive(true);
            panelRect.SetAsLastSibling();
            VisibilityChanged?.Invoke(true);
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

        private void ApplyOverlayLayout()
        {
            if (panelRect == null)
            {
                return;
            }

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.localScale = Vector3.one;
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

        private void RebuildButtons()
        {
            if (buttonContainer == null)
            {
                return;
            }

            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            if (_currentUnit?.Skills == null)
            {
                return;
            }

            foreach (var skill in _currentUnit.Skills)
            {
                if (skill == null)
                {
                    continue;
                }

                var btnGo = new GameObject($"Skill_{skill.skillId}");
                var btnView = btnGo.AddComponent<SkillButtonView>();
                var capturedSkill = skill;
                var capturedUnit = _currentUnit;
                btnView.Build(buttonContainer, skill, () =>
                {
                    var armed = _onSkillSelected?.Invoke(capturedUnit, capturedSkill) ?? false;
                    if (armed)
                    {
                        Hide();
                    }
                });
            }
        }

        private void PositionBesideUnit(UnitView unitView)
        {
            if (worldCamera == null || panelRect == null || unitView == null)
            {
                return;
            }

            var canvasRect = GetRootCanvasRectTransform();
            if (canvasRect == null)
            {
                return;
            }

            var spriteRenderer = unitView.GetComponent<SpriteRenderer>();
            Vector3 anchorWorld;
            if (spriteRenderer != null)
            {
                anchorWorld = new Vector3(spriteRenderer.bounds.max.x, spriteRenderer.bounds.center.y, 0f);
            }
            else
            {
                anchorWorld = unitView.transform.position + Vector3.right * 0.5f;
            }

            var screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, anchorWorld);
            screenPoint.x += screenPaddingPx;

            var rootCanvas = canvasRect.GetComponent<Canvas>();
            var uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    uiCamera,
                    out var localPoint))
            {
                panelRect.anchoredPosition = localPoint;
            }
        }

        private void LateUpdate()
        {
            if (_currentUnitView != null && IsVisible)
            {
                PositionBesideUnit(_currentUnitView);
            }
        }
    }
}
