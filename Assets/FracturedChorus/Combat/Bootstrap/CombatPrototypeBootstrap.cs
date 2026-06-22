using FracturedChorus.Combat.Bootstrap;
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

        [Header("Grid layout (used when spawning from encounter only)")]
        [SerializeField] private float cellWidth = 1.4f;
        [SerializeField] private float cellHeight = 1.2f;
        [SerializeField] private float sideGap = 3.5f;

        private CombatSession _session;
        private DualGrid _grid;
        private BeatTimelineEngine _timeline;
        private Dictionary<GridPosition, Transform> _cellByPosition;

        private void Awake()
        {
            CombatInputSetup.Configure(mainCamera != null ? mainCamera : Camera.main);
            ResolveSceneReferences();

            _grid = new DualGrid();
            _timeline = new BeatTimelineEngine();
            _session = new CombatSession();

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

            if (combatController == null)
            {
                combatController = GetComponent<CombatController>();
                if (combatController == null)
                {
                    combatController = gameObject.AddComponent<CombatController>();
                }
            }

            combatController.Initialize(_session, _timeline, timelineView, skillPanelView);
            skillPanelView?.Hide();
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

                var unit = new CombatUnit(unitPreset, view.Side);
                if (!_grid.TryPlaceUnit(unit, view.GridPosition))
                {
                    Debug.LogWarning($"[Bootstrap] Could not place {unitPreset.displayName} at {view.GridPosition}");
                    continue;
                }

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
                worldPos = _grid.GetWorldPosition(gridPos, cellWidth, cellHeight, sideGap);
            }

            var depth = gridPos.Row * 0.1f + gridPos.Column * 0.05f;
            view.transform.position = new Vector3(worldPos.x, worldPos.y, depth);
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

                var worldPos = _grid.GetWorldPosition(pos, cellWidth, cellHeight, sideGap);
                var unitGo = new GameObject($"Unit_{unit.DisplayName}");
                unitGo.transform.SetParent(unitsRoot, false);
                unitGo.transform.position = worldPos;
                unitGo.transform.localScale = Vector3.one * 0.9f;

                var view = unitGo.AddComponent<UnitView>();
                view.ConfigureDemo(spawn.preset?.unitId ?? "grunt", spawn.side, spawn.row, spawn.column);
                view.Bind(unit, HandleUnitSelected);
            }
        }

        private void HandleUnitSelected(CombatUnit unit, UnitView view)
        {
            skillPanelView?.ToggleForUnit(unit, view);
        }
    }
}
