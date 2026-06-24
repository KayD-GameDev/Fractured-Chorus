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
        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Transform gridRoot;
        [SerializeField] private UnitView[] unitViews;
        [SerializeField] private Camera mainCamera;

        [Header("Encounter (optional if units already in scene)")]
        [SerializeField] private EncounterDefinitionSO encounterDefinition;

        [SerializeField] private CombatMusicController musicController;
        [SerializeField] private bool playBossMusicOnStart = true;

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
            EnsureAudioListener();

            _grid = new DualGrid();
            _timeline = new BeatTimelineEngine();
            _session = new CombatSession();

            EnsureHoneycombGrid();

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

            if (playBossMusicOnStart)
            {
                musicController?.PlayBossMusic();
            }

            combatController.Initialize(_session, _timeline, timelineView, skillPanelView, musicController);

            skillPanelView?.Hide();
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

            if (unitViews == null || unitViews.Length == 0)
            {
                unitViews = unitsRoot != null
                    ? unitsRoot.GetComponentsInChildren<UnitView>(true)
                    : GetComponentsInChildren<UnitView>(true);
            }

            timelineView?.WireReferences();
            skillPanelView?.WireReferences();
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

            foreach (var view in unitViews)
            {
                if (view?.Unit != null)
                {
                    view.Bind(view.Unit, HandleUnitSelected, _boardDrag);
                }
            }
        }

        private void EnsureHoneycombGrid()
        {
            if (gridRoot == null)
            {
                return;
            }

            var floorSprite = HexSpriteUtil.ResolveHexagonFlatTop();
            foreach (var marker in gridRoot.GetComponentsInChildren<GridCellMarker>(true))
            {
                marker.SnapToLayoutPosition(sideGap);
                marker.SetFloorSprite(floorSprite);
                marker.RebuildVisualsForPlay();
            }
        }

        private void RegisterSceneUnits()
        {
            CacheGridCellTransforms();

            foreach (var view in unitViews)
            {
                var unitPreset = view?.ResolvePreset();
                if (view == null || unitPreset == null)
                {
                    continue;
                }

                view.ConfigureDemo(view.DemoUnitKey, view.Side);

                if (!DefaultPartyFormation.TryGetStartupCell(view.DemoUnitKey, view.Side, out var pos))
                {
                    Debug.LogWarning($"[Bootstrap] No startup cell for {view.DemoUnitKey} ({view.Side})");
                    continue;
                }

                var unit = new CombatUnit(unitPreset, view.Side);
                if (!_grid.TryPlaceUnit(unit, pos))
                {
                    Debug.LogWarning($"[Bootstrap] Could not place {unitPreset.displayName} at {pos}");
                    continue;
                }

                view.PlaceOnGrid(pos);
                view.Bind(unit, HandleUnitSelected);
                AlignUnitViewToGridCell(view);
            }
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
            view.transform.position = new Vector3(worldPos.x, worldPos.y, -0.05f + depth);
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
                view.Bind(unit, HandleUnitSelected);
            }
        }

        private void HandleUnitSelected(CombatUnit unit, UnitView view)
        {
            skillPanelView?.ToggleForUnit(unit, view);
        }
    }
}
