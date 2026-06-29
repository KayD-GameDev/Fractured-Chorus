using FracturedChorus.Audio;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.UI;
using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
    public class CombatPrototypeBootstrap : MonoBehaviour
    {
        [Header("Scene References — chỉnh layout trong Hierarchy")]
        [SerializeField] private CombatController combatController;
        [SerializeField] private BeatTimelineUIView timelineView;
        [SerializeField] private SkillPanelUIView skillPanelView;
        [SerializeField] private PartyStatusBarUIView partyStatusBarView;
        [SerializeField] private EnemyStatusBarUIView enemyStatusBarView;
        [SerializeField] private CombatExecuteOverlayUIView executeOverlay;
        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Transform gridRoot;
        [SerializeField] private UnitView[] unitViews;
        [SerializeField] private Camera mainCamera;

        [Header("Encounter (optional if units already in scene)")]
        [SerializeField] private EncounterDefinitionSO encounterDefinition;

        [SerializeField] private CombatMusicController musicController;

        [Header("Grid layout")]
        [SerializeField] private float sideGap = HexBoardLayout.DefaultSideGap;
        [Tooltip("Giữ Transform/visual mọi object scene khi Play. Tắt chỉ khi cần snap lại lưới công thức.")]
        [SerializeField] private bool respectSceneAuthoring = true;

        private CombatSession _session;
        private DualGrid _grid;
        private BeatTimelineEngine _timeline;
        private Dictionary<GridPosition, Transform> _cellByPosition;
        private BoardDragController _boardDrag;

        private void Awake()
        {
            CombatInputSetup.Configure(mainCamera != null ? mainCamera : Camera.main);
            ResolveSceneReferences();
            EnsureMusicController();
            EnsureAudioListener();

            _grid = new DualGrid();
            _timeline = new BeatTimelineEngine();
            _session = new CombatSession();

            EnsureHoneycombGrid();
            CacheGridCellTransforms();

            if (HasSceneUnits())
            {
                RegisterSceneUnits();
            }
            else
            {
                var encounter = encounterDefinition != null
                    ? encounterDefinition
                    : EncounterRuntimeFactory.CreateDemoEncounter();
                SpawnUnitsFromEncounter(encounter);
            }

            _session.Initialize(_grid, _timeline);
            SetupBoardDrag();

            if (combatController == null)
            {
                combatController = GetComponent<CombatController>();
                if (combatController == null)
                {
                    combatController = gameObject.AddComponent<CombatController>();
                }
            }

            var executeOverlay = ResolveExecuteOverlay();

            combatController.Initialize(_session, _timeline, timelineView, skillPanelView, musicController,
                executeOverlay, _boardDrag);

            RefreshPartyStatusBar();
            EnsureEnemyStatusBar();

            if (skillPanelView != null && !skillPanelView.gameObject.activeSelf)
            {
                skillPanelView.Hide();
            }
        }

        private void EnsureEnemyStatusBar()
        {
            if (partyStatusBarView == null)
            {
                return;
            }

            var template = partyStatusBarView.CardTemplate;
            if (template == null)
            {
                Debug.LogWarning("[Bootstrap] No party card template — bỏ qua thanh thẻ quái.");
                return;
            }

            if (enemyStatusBarView == null)
            {
                enemyStatusBarView = FindAnyObjectByType<EnemyStatusBarUIView>();
            }

            if (enemyStatusBarView == null)
            {
                enemyStatusBarView = CreateEnemyStatusBar();
            }

            if (enemyStatusBarView == null)
            {
                return;
            }

            enemyStatusBarView.SetCardTemplate(template);
            enemyStatusBarView.BindFromSession(_session);
        }

        private EnemyStatusBarUIView CreateEnemyStatusBar()
        {
            var canvasRect = ResolveUiCanvasRect();
            if (canvasRect == null)
            {
                Debug.LogWarning("[Bootstrap] Không tìm thấy Canvas cho thanh thẻ quái.");
                return null;
            }

            var go = new GameObject("EnemyStatusBarUI", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(canvasRect, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -12f);
            rect.sizeDelta = new Vector2(420f, 128f);

            var view = go.AddComponent<EnemyStatusBarUIView>();
            view.EnsureRow();
            return view;
        }

        private RectTransform ResolveUiCanvasRect()
        {
            var walk = partyStatusBarView != null ? partyStatusBarView.transform.parent : null;
            while (walk != null)
            {
                if (walk.GetComponent<Canvas>() != null)
                {
                    return walk as RectTransform;
                }

                walk = walk.parent;
            }

            var anyCanvas = FindAnyObjectByType<Canvas>();
            return anyCanvas != null ? anyCanvas.transform as RectTransform : null;
        }

        private CombatExecuteOverlayUIView ResolveExecuteOverlay()
        {
            if (executeOverlay == null)
            {
                executeOverlay = FindAnyObjectByType<CombatExecuteOverlayUIView>();
            }

            executeOverlay?.WireReferences();
            return executeOverlay;
        }

        private void EnsureMusicController()
        {
            if (musicController != null)
            {
                return;
            }

            musicController = FindAnyObjectByType<CombatMusicController>();
            if (musicController != null)
            {
                return;
            }

            var audioGo = new GameObject("CombatMusic");
            audioGo.transform.SetParent(transform, false);
            musicController = audioGo.AddComponent<CombatMusicController>();
        }

        private void EnsureAudioListener()
        {
            if (FindAnyObjectByType<AudioListener>() != null)
            {
                return;
            }

            var cam = mainCamera != null ? mainCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Bootstrap] No AudioListener and no Main Camera found.");
                return;
            }

            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                Debug.Log("[Bootstrap] Added AudioListener to Main Camera.");
            }
        }

        private void ResolveSceneReferences()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (unitsRoot == null)
            {
                var found = transform.Find("World/Units");
                unitsRoot = found != null ? found : transform.Find("Units");
            }

            if (gridRoot == null)
            {
                var found = transform.Find("World/Grid");
                gridRoot = found != null ? found : transform.Find("Grid");
            }

            if (timelineView == null)
            {
                timelineView = FindAnyObjectByType<BeatTimelineUIView>();
            }

            if (skillPanelView == null)
            {
                skillPanelView = FindAnyObjectByType<SkillPanelUIView>();
            }

            if (partyStatusBarView == null)
            {
                partyStatusBarView = FindAnyObjectByType<PartyStatusBarUIView>();
            }

            if (enemyStatusBarView == null)
            {
                enemyStatusBarView = FindAnyObjectByType<EnemyStatusBarUIView>();
            }

            if (executeOverlay == null)
            {
                executeOverlay = FindAnyObjectByType<CombatExecuteOverlayUIView>();
            }

            if (unitViews == null || unitViews.Length == 0)
            {
                unitViews = unitsRoot != null
                    ? unitsRoot.GetComponentsInChildren<UnitView>(true)
                    : GetComponentsInChildren<UnitView>(true);
            }

            timelineView?.WireReferences();
            skillPanelView?.WireReferences();
            partyStatusBarView?.WireReferences();
            executeOverlay?.WireReferences();
        }

        private bool HasSceneUnits()
        {
            return unitViews != null && unitViews.Length > 0 &&
                   System.Array.Exists(unitViews, v => v != null && v.ResolvePreset() != null);
        }

        private void SetupBoardDrag()
        {
            _boardDrag = GetComponent<BoardDragController>();
            if (_boardDrag == null)
            {
                _boardDrag = gameObject.AddComponent<BoardDragController>();
            }

            GridCellMarker[] markers = gridRoot != null
                ? gridRoot.GetComponentsInChildren<GridCellMarker>(true)
                : System.Array.Empty<GridCellMarker>();
            _boardDrag.Initialize(_session, _grid, markers, mainCamera);
            _boardDrag.SetUnitClickHandler(HandleUnitSelected);
            _boardDrag.SetFormationChangedHandler(RefreshPartyStatusBar);

            foreach (var view in unitViews)
            {
                if (view?.Unit != null)
                {
                    view.Bind(view.Unit);
                }
            }
        }

        private void EnsureHoneycombGrid()
        {
            if (gridRoot == null)
            {
                return;
            }

            if (respectSceneAuthoring)
            {
                WireSceneGrid();
                return;
            }

            var floorSprite = HexSpriteUtil.ResolveHexagonFlatTop();
            foreach (var marker in gridRoot.GetComponentsInChildren<GridCellMarker>(true))
            {
                marker.SnapToLayoutPosition(sideGap);

                if (floorSprite != null && marker.transform.Find("Hexagon Flat Top") == null)
                {
                    marker.SetFloorSprite(floorSprite);
                }

                marker.PrepareForPlay();
            }
        }

        private void WireSceneGrid()
        {
            foreach (var marker in gridRoot.GetComponentsInChildren<GridCellMarker>(true))
            {
                marker.HideLegacyMeshOnly();
            }
        }

        private void RegisterSceneUnits()
        {
            foreach (var view in unitViews)
            {
                var unitPreset = view?.ResolvePreset();
                if (view == null || unitPreset == null)
                {
                    continue;
                }

                view.EnsureInteractionColliders();

                if (!TryResolveUnitGridPosition(view, out var pos))
                {
                    Debug.LogWarning($"[Bootstrap] Could not resolve grid cell for {view.name} ({view.DemoUnitKey})");
                    continue;
                }

                var unit = new CombatUnit(unitPreset, view.Side);
                if (!_grid.TryPlaceUnit(unit, pos))
                {
                    Debug.LogWarning($"[Bootstrap] Could not place {unitPreset.displayName} at {pos}");
                    continue;
                }

                view.PlaceOnGrid(pos);
                view.Bind(unit);

                if (!respectSceneAuthoring)
                {
                    SyncUnitTransformFromSceneOrCell(view);
                }
            }
        }

        private bool TryResolveUnitGridPosition(UnitView view, out GridPosition position)
        {
            if (view.IsPlacedOnGrid)
            {
                position = view.GridPosition;
                return true;
            }

            if (TryFindCellFromWorldPosition(view.FeetWorldPosition, view.Side, out position))
            {
                return true;
            }

            return DefaultPartyFormation.TryGetStartupCell(view.DemoUnitKey, view.Side, out position);
        }

        private bool TryFindCellFromWorldPosition(Vector3 world, GridSide side, out GridPosition position)
        {
            position = default;
            if (_cellByPosition == null || _cellByPosition.Count == 0)
            {
                return false;
            }

            GridCellMarker best = null;
            var bestDist = float.MaxValue;
            foreach (var pair in _cellByPosition)
            {
                if (pair.Key.Side != side || pair.Value == null)
                {
                    continue;
                }

                var cellPos = pair.Value.position;
                var dist = Vector2.Distance(new Vector2(world.x, world.y), new Vector2(cellPos.x, cellPos.y));
                if (dist >= 1.15f || dist >= bestDist)
                {
                    continue;
                }

                bestDist = dist;
                best = pair.Value.GetComponent<GridCellMarker>();
            }

            if (best == null)
            {
                return false;
            }

            position = best.Position;
            return true;
        }

        private void SyncUnitTransformFromSceneOrCell(UnitView view)
        {
            if (view == null)
            {
                return;
            }

            if (TryFindCellFromWorldPosition(view.FeetWorldPosition, view.Side, out var sceneCell)
                && sceneCell.Equals(view.GridPosition))
            {
                return;
            }

            AlignUnitViewToGridCell(view);
        }

        private void CacheGridCellTransforms()
        {
            _cellByPosition = new Dictionary<GridPosition, Transform>();
            if (gridRoot == null)
            {
                return;
            }

            foreach (var marker in gridRoot.GetComponentsInChildren<GridCellMarker>(true))
            {
                _cellByPosition[marker.Position] = marker.transform;
            }
        }

        private void AlignUnitViewToGridCell(UnitView view)
        {
            if (view == null)
            {
                return;
            }

            var gridPos = view.GridPosition;
            Vector3 worldPos;

            if (_cellByPosition != null && _cellByPosition.TryGetValue(gridPos, out var cellTransform))
            {
                worldPos = cellTransform.position;
            }
            else
            {
                worldPos = HexBoardLayout.GetWorldPosition(gridPos, sideGap);
            }

            var depth = gridPos.Row * 0.1f + gridPos.Column * 0.05f;
            view.SnapFeetTo(new Vector3(worldPos.x, worldPos.y, 0f), -0.05f + depth);
        }

        private void SpawnUnitsFromEncounter(EncounterDefinitionSO encounter)
        {
            if (unitsRoot == null)
            {
                var go = new GameObject("Units");
                go.transform.SetParent(transform, false);
                unitsRoot = go.transform;
            }

            foreach (var spawn in encounter.units)
            {
                if (spawn.preset == null)
                {
                    continue;
                }

                var unit = new CombatUnit(spawn.preset, spawn.side);
                var pos = new GridPosition(spawn.side, spawn.row, spawn.column);
                if (!_grid.TryPlaceUnit(unit, pos))
                {
                    Debug.LogWarning($"[Bootstrap] Could not place {spawn.preset.displayName} at {pos}");
                    continue;
                }

                var worldPos = HexBoardLayout.GetWorldPosition(pos, sideGap);
                var unitGo = new GameObject($"Unit_{unit.DisplayName}");
                unitGo.transform.SetParent(unitsRoot, false);
                unitGo.transform.position = worldPos;
                unitGo.transform.localScale = Vector3.one * 0.9f;

                var view = unitGo.AddComponent<UnitView>();
                view.ConfigureDemo(spawn.preset?.unitId ?? "grunt", spawn.side);
                view.PlaceOnGrid(pos);
                view.Bind(unit);
            }
        }

        private void HandleUnitSelected(CombatUnit unit, UnitView view)
        {
            if (_session != null && _session.AllowPlayerReposition)
            {
                return;
            }

            skillPanelView?.ToggleForUnit(unit, view);
        }

        private void RefreshPartyStatusBar()
        {
            if (partyStatusBarView == null)
            {
                return;
            }

            if (_session?.Grid != null)
            {
                partyStatusBarView.BindFromSession(_session);
                return;
            }

            var views = unitsRoot != null
                ? unitsRoot.GetComponentsInChildren<UnitView>(true)
                : GetComponentsInChildren<UnitView>(true);

            if (partyStatusBarView.BoundUnitCount > 0)
            {
                partyStatusBarView.RefreshFormationOrderFromUnitViews(views);
            }
            else
            {
                partyStatusBarView.BindFromUnitViews(views);
            }
        }
    }
}
