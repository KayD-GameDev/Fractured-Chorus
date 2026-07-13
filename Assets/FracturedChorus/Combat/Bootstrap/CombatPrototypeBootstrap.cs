using FracturedChorus.Audio;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
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
        [Header("Scene References — edit layout in Hierarchy")]
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
        [SerializeField] private CombatSfxController combatSfxController;
        [SerializeField] private CounterPresentationDriver counterPresentation;

        [Header("Grid layout")]
        [SerializeField] private float sideGap = HexBoardLayout.DefaultSideGap;

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
            EnsureCombatSfxController();
            EnsureCounterPresentation();
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

            counterPresentation?.Configure(combatSfxController, timelineView);
            timelineView?.SetCounterPresentation(counterPresentation);

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

            if (enemyStatusBarView == null)
            {
                enemyStatusBarView = FindAnyObjectByType<EnemyStatusBarUIView>();
            }

            if (enemyStatusBarView == null)
            {
                Debug.LogWarning(
                    "[Bootstrap] EnemyStatusBarUI not found in scene. " +
                    "Run Fractured Chorus → Add Enemy Status Bar (Hierarchy), save the scene, then Play again.");
                return;
            }

            enemyStatusBarView.WireReferences();

            if (enemyStatusBarView.CardTemplate == null)
            {
                var partyTemplate = partyStatusBarView.CardTemplate;
                if (partyTemplate != null)
                {
                    Debug.LogWarning(
                        "[Bootstrap] EnemyStatusBarUI is missing CardTemplate — temporarily borrowing party template. " +
                        "Run Setup Enemy Cards in Hierarchy to add a dedicated CardTemplate in the scene.");
                    enemyStatusBarView.SetCardTemplate(partyTemplate);
                }
                else
                {
                    Debug.LogWarning("[Bootstrap] No CardTemplate available for enemy cards.");
                    return;
                }
            }

            AlignEnemyBarToPartyY();
            enemyStatusBarView.BindFromSession(_session);
        }

        /// <summary>Canh trục Y của thanh thẻ quái trùng với thanh thẻ party (cùng đỉnh + chiều cao).</summary>
        private void AlignEnemyBarToPartyY()
        {
            if (partyStatusBarView == null || enemyStatusBarView == null)
            {
                return;
            }

            var partyRect = partyStatusBarView.transform as RectTransform;
            var enemyRect = enemyStatusBarView.transform as RectTransform;
            if (partyRect == null || enemyRect == null)
            {
                return;
            }

            // Giữ nguyên trục X (thẻ quái ở cạnh phải), chỉ đồng bộ trục Y theo thẻ players.
            var anchorMin = enemyRect.anchorMin;
            anchorMin.y = partyRect.anchorMin.y;
            enemyRect.anchorMin = anchorMin;

            var anchorMax = enemyRect.anchorMax;
            anchorMax.y = partyRect.anchorMax.y;
            enemyRect.anchorMax = anchorMax;

            var pivot = enemyRect.pivot;
            pivot.y = partyRect.pivot.y;
            enemyRect.pivot = pivot;

            var size = enemyRect.sizeDelta;
            size.y = partyRect.sizeDelta.y;
            enemyRect.sizeDelta = size;

            var pos = enemyRect.anchoredPosition;
            pos.y = partyRect.anchoredPosition.y;
            enemyRect.anchoredPosition = pos;
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

        private void EnsureCombatSfxController()
        {
            if (combatSfxController != null)
            {
                return;
            }

            combatSfxController = FindAnyObjectByType<CombatSfxController>();
            if (combatSfxController != null)
            {
                return;
            }

            if (musicController != null)
            {
                combatSfxController = musicController.gameObject.AddComponent<CombatSfxController>();
                return;
            }

            var sfxGo = new GameObject("CombatSfx");
            sfxGo.transform.SetParent(transform, false);
            combatSfxController = sfxGo.AddComponent<CombatSfxController>();
        }

        private void EnsureCounterPresentation()
        {
            if (counterPresentation == null)
            {
                counterPresentation = GetComponent<CounterPresentationDriver>();
            }

            if (counterPresentation == null)
            {
                counterPresentation = FindAnyObjectByType<CounterPresentationDriver>();
            }

            if (counterPresentation == null)
            {
                counterPresentation = gameObject.AddComponent<CounterPresentationDriver>();
            }

            counterPresentation.Configure(combatSfxController, timelineView);
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

            // Scene là nguồn chuẩn của layout (đã dựng 2×3 qua menu "Rebuild Hex Board Grid").
            // Runtime chỉ chuẩn bị visual/collider; ẩn an toàn ô ngoài phạm vi nếu còn sót.
            foreach (var marker in gridRoot.GetComponentsInChildren<GridCellMarker>(true))
            {
                if (!marker.Position.IsValid())
                {
                    marker.gameObject.SetActive(false);
                    continue;
                }

                marker.PrepareForPlay();
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
