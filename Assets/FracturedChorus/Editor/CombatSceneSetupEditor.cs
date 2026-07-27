#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    [InitializeOnLoad]
    public static class CombatSceneSetupEditor
    {
        static CombatSceneSetupEditor()
        {
            EditorSceneManager.sceneOpened += (_, _) =>
            {
                RefreshGridCellVisualsInScene();
                EditorApplication.delayCall += () =>
                {
                    if (!Application.isPlaying)
                    {
                        EnsureUnitInteractionCollidersInScene(silent: true);
                        FixCombatSceneErrors(silent: true);
                    }
                };
            };
            EditorApplication.delayCall += () =>
            {
                if (!Application.isPlaying)
                {
                    RefreshGridCellVisualsInScene();
                }
            };
        }

        private static void RefreshGridCellVisualsInScene()
        {
            foreach (var marker in Object.FindObjectsByType<GridCellMarker>(FindObjectsInactive.Include))
            {
                // Scene-first: never wipe hex colors/active state on editor load.
                marker.PrepareForPlay();
            }
        }

        private static void EnsureUnitInteractionCollidersInScene(bool silent)
        {
            var views = Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include);
            if (views.Length == 0)
            {
                return;
            }

            var changed = false;
            foreach (var view in views)
            {
                view.EnsureInteractionColliders();
                EditorUtility.SetDirty(view);
                changed = true;
            }

            CombatInputSetup.EnsureCameraRaycaster(Camera.main, destroyImmediate: true);

            if (changed)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                if (!silent)
                {
                    Debug.Log(
                        $"[Fractured Chorus] Ensured BoxCollider2D + FeetAnchor on {views.Length} unit(s). Save scene (Ctrl+S).");
                }
            }
        }

        private const float SideGap = HexBoardLayout.DefaultSideGap;

        [MenuItem("Fractured Chorus/Fix Input System (EventSystem)")]
        public static void FixInputSystemInScene()
        {
            CombatInputSetup.EnsureEventSystem();
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                CombatInputSetup.ApplyInputModule(eventSystem.gameObject, destroyImmediate: true);
            }

            CombatInputSetup.EnsureCameraRaycaster(Camera.main, destroyImmediate: true);
            EditorSceneManager.MarkSceneDirty(eventSystem != null ? eventSystem.gameObject.scene : UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] EventSystem + Physics2DRaycaster on Main Camera. Save scene.");
        }

        [MenuItem("Fractured Chorus/Apply All Play-Ready Updates")]
        public static void ApplyAllPlayReadyUpdates()
        {
            FixInputSystemInScene();
            MigrateUnitCollidersTo2D();
            RestoreUnitSpritesFromPresets();
            WireCombatMusicInScene();

            foreach (var marker in Object.FindObjectsByType<GridCellMarker>(FindObjectsInactive.Include))
            {
                SetSerializedField(marker, "preserveSceneVisuals", true);
                marker.PrepareForPlay();
                EditorUtility.SetDirty(marker);
            }

            foreach (var timeline in Object.FindObjectsByType<BeatTimelineUIView>(FindObjectsInactive.Include))
            {
                timeline.WireReferences();
                TimelineHierarchyBuilder.EnsureBeatTemplateVisuals(timeline);
                timeline.ForceRefitViewportSlots();
                EditorUtility.SetDirty(timeline);
            }

            CombatUiHierarchy.EnsurePartyCardsInHierarchy();
            CombatUiHierarchy.EnsureSkillPanelInHierarchy();
            if (Object.FindAnyObjectByType<EnemyStatusBarUIView>(FindObjectsInactive.Include) == null)
            {
                CombatUiHierarchy.AddEnemyStatusBarToScene();
            }
            else
            {
                CombatUiHierarchy.EnsureEnemyCardsInHierarchy();
            }
            ElementBadgeIconSetup.ApplyToStatBlocks();

            CleanOrphanedUiLeftovers();
            ApplyDeployLabelToExecuteOverlay();
            PruneNullUnitViewsInScene();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[Fractured Chorus] Applied play-ready updates (input, colliders, sprites, grid, timeline, orphan cleanup, Deploy label) and saved scene.");
        }

        /// <summary>
        /// Xoá các UI bị "rò" ra root scene (RectTransform không nằm dưới Canvas) — ví dụ HealthBarFill/Avatar/Border
        /// bị tách khỏi thẻ khi chỉnh sửa. UI hợp lệ luôn phải nằm dưới một Canvas.
        /// </summary>
        private static void CleanOrphanedUiLeftovers()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            var removed = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                // RectTransform ở root mà không phải Canvas ⇒ UI mồ côi (leaked khỏi thẻ).
                if (root.GetComponent<RectTransform>() != null && root.GetComponent<Canvas>() == null)
                {
                    Debug.LogWarning($"[Fractured Chorus] Removed orphaned UI at root: '{root.name}'.");
                    Object.DestroyImmediate(root);
                    removed++;
                }
            }

            if (removed > 0)
            {
                Debug.Log($"[Fractured Chorus] Removed {removed} orphaned UI object(s) from scene root.");
            }
        }

        /// <summary>Đổi nhãn nút Execute → "Deploy" cho pha dàn trận (đồng bộ với runtime).</summary>
        private static void ApplyDeployLabelToExecuteOverlay()
        {
            foreach (var overlay in Object.FindObjectsByType<CombatExecuteOverlayUIView>(FindObjectsInactive.Include))
            {
                overlay.WireReferences();
                var label = overlay.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = "Deploy";
                    EditorUtility.SetDirty(label);
                }

                EditorUtility.SetDirty(overlay);
            }
        }

        /// <summary>Gán beat map + CSV lên CombatMusicController trong scene hiện tại.</summary>
        private static void WireCombatMusicInScene()
        {
            CombatMusicSceneSetup.WireCurrentScene();
        }

        /// <summary>Loại phần tử null khỏi unitViews trên bootstrap (tránh MissingReference).</summary>
        private static void PruneNullUnitViewsInScene()
        {
            foreach (var bootstrap in Object.FindObjectsByType<CombatPrototypeBootstrap>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(bootstrap);
                var prop = so.FindProperty("unitViews");
                if (prop == null || !prop.isArray)
                {
                    continue;
                }

                var removed = 0;
                for (var i = prop.arraySize - 1; i >= 0; i--)
                {
                    if (prop.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        prop.DeleteArrayElementAtIndex(i);
                        removed++;
                    }
                }

                if (removed > 0)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(bootstrap);
                    Debug.Log($"[Fractured Chorus] Pruned {removed} null unitViews entry on {bootstrap.name}.");
                }
            }
        }

        [MenuItem("Fractured Chorus/Restore Scene/From SceneBackup (BU — honeycomb layout)")]
        public static void RestoreSceneFromBackupBu()
        {
            var buPath = "Assets/SceneBackup/CombatPrototypeBU.unity";
            if (!System.IO.File.Exists(buPath))
            {
                EditorUtility.DisplayDialog("Fractured Chorus", "CombatPrototypeBU.unity not found.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Restore Scene",
                    "Replace CombatPrototype.unity with the SceneBackup (BU) copy?\nHoneycomb layout (23/6) — differs from the current scene.",
                    "Restore",
                    "Cancel"))
            {
                return;
            }

            RestoreSceneFromAsset(buPath);
        }

        [MenuItem("Fractured Chorus/Restore Scene/From HEAD backup (2026-06-25)")]
        public static void RestoreSceneFromHeadBackup()
        {
            RestoreSceneFromHeadBackupInternal(applyPlayReadyUpdates: false);
        }

        [MenuItem("Fractured Chorus/Restore Scene/From HEAD backup + Apply All Updates")]
        public static void RestoreSceneFromHeadBackupAndApplyAll()
        {
            RestoreSceneFromHeadBackupInternal(applyPlayReadyUpdates: true);
        }

        private static void RestoreSceneFromHeadBackupInternal(bool applyPlayReadyUpdates)
        {
            var path = "Assets/SceneBackup/CombatPrototype_HEAD_20260625.unity";
            if (!System.IO.File.Exists(path))
            {
                EditorUtility.DisplayDialog("Fractured Chorus", "CombatPrototype_HEAD_20260625.unity not found.", "OK");
                return;
            }

            var message = applyPlayReadyUpdates
                ? "Restore scene from HEAD backup, then run Apply All (2D collider, sprite, timeline)?\nRen layout (-3.87, 0)."
                : "Restore scene from HEAD backup (before YAML edits)?\nThis is the latest git commit — Ren layout (-3.87, 0).";

            if (!EditorUtility.DisplayDialog("Restore Scene", message, "Restore", "Cancel"))
            {
                return;
            }

            RestoreSceneFromAsset(path, applyPlayReadyUpdates);
        }

        private static void RestoreSceneFromAsset(string sourceAssetPath, bool applyPlayReadyUpdates = false)
        {
            var targetPath = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";
            if (!AssetDatabase.CopyAsset(sourceAssetPath, targetPath))
            {
                Debug.LogError($"[Fractured Chorus] CopyAsset failed: {sourceAssetPath} → {targetPath}");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
            FixCombatSceneErrors(silent: true);

            if (applyPlayReadyUpdates)
            {
                ApplyAllPlayReadyUpdates();
                FixCombatSceneErrors(silent: true);
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log(
                applyPlayReadyUpdates
                    ? $"[Fractured Chorus] Restored + applied play-ready updates from {sourceAssetPath}."
                    : $"[Fractured Chorus] Restored scene from {sourceAssetPath}. Save scene (Ctrl+S).");
        }

        [MenuItem("Fractured Chorus/Fix Combat Scene Errors (Missing Scripts + Timeline Clones)")]
        public static void FixCombatSceneErrorsMenu()
        {
            FixCombatSceneErrors(silent: false);
        }

        private static void FixCombatSceneErrors(bool silent)
        {
            var removedMissing = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                removedMissing += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }

            var removedClones = CleanTimelineRuntimeClonesInScene();
            if (removedMissing > 0 || removedClones > 0)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                if (!silent)
                {
                    Debug.Log(
                        $"[Fractured Chorus] Removed {removedMissing} missing script(s) and {removedClones} BeatSlot clone(s). Save scene.");
                }
            }
            else if (!silent)
            {
                Debug.Log("[Fractured Chorus] No missing scripts or BeatSlot clones found.");
            }
        }

        private static int CleanTimelineRuntimeClonesInScene()
        {
            var scroll = GameObject.Find("CombatCanvas/BeatTimelineUI/Viewport/ScrollContent")
                ?? GameObject.Find("BeatTimelineUI/Viewport/ScrollContent");
            if (scroll == null)
            {
                return 0;
            }

            var removed = 0;
            for (var i = scroll.transform.childCount - 1; i >= 0; i--)
            {
                var child = scroll.transform.GetChild(i);
                if (child.name.StartsWith("BeatSlot_")
                    || (child.name.StartsWith("Beat_") && child.name != "Beat_0"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    removed++;
                }
            }

            return removed;
        }

        [MenuItem("Fractured Chorus/Migrate Unit Colliders (2D + Feet)")]
        public static void MigrateUnitCollidersTo2D()
        {
            var views = Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include);
            foreach (var view in views)
            {
                view.EnsureInteractionColliders();
                EditorUtility.SetDirty(view);
            }

            CombatInputSetup.EnsureCameraRaycaster(Camera.main, destroyImmediate: true);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log(
                $"[Fractured Chorus] Migrated {views.Length} UnitView(s) to BoxCollider2D + FeetAnchor (keeps scene collider when Preserve Scene Collider is enabled). Save scene.");
        }

        [MenuItem("Fractured Chorus/Fit Unit Colliders To Sprite (override scene)")]
        public static void FitUnitCollidersToSprite()
        {
            var views = Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include);
            foreach (var view in views)
            {
                view.RefitBodyColliderToSprite();
                EditorUtility.SetDirty(view);
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[Fractured Chorus] Refit BoxCollider2D to sprite on {views.Length} unit(s). Save scene (Ctrl+S).");
        }

        [MenuItem("Fractured Chorus/Restore Unit Sprites from Presets")]
        public static void RestoreUnitSpritesFromPresets()
        {
            var views = Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include);
            var restored = 0;
            foreach (var view in views)
            {
                var preset = view.ResolvePreset();
                if (preset?.battleSprite == null)
                {
                    continue;
                }

                var sr = view.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    continue;
                }

                var current = sr.sprite;
                if (current != null && (current.rect.width > 1f || current.rect.height > 1f))
                {
                    continue;
                }

                sr.sprite = preset.battleSprite;
                EditorUtility.SetDirty(sr);
                EditorUtility.SetDirty(view);
                restored++;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[Fractured Chorus] Restored battleSprite on {restored} unit(s). Save scene.");
        }

        [MenuItem("Fractured Chorus/Setup Combat Scene Hierarchy")]
        public static void SetupCombatSceneHierarchy()
        {
            var existing = GameObject.Find("CombatRoot");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Setup Combat Scene",
                        "CombatRoot already exists. Delete and recreate hierarchy?",
                        "Recreate",
                        "Cancel"))
                {
                    return;
                }

                Undo.DestroyObjectImmediate(existing);
            }

            var root = new GameObject("CombatRoot");
            Undo.RegisterCreatedObjectUndo(root, "Create CombatRoot");

            var encounter = EncounterRuntimeFactory.CreateDemoEncounter();
            var bootstrap = root.GetComponent<CombatPrototypeBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = Undo.AddComponent<CombatPrototypeBootstrap>(root);
            }

            var controller = root.GetComponent<CombatController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<CombatController>(root);
            }

            EnsureCamera();
            EnsureEventSystem();
            CombatInputSetup.EnsureCameraRaycaster(Camera.main);

            var canvas = CreateCanvas(root.transform);
            var world = CreateWorldRoot(root.transform);
            var gridRoot = CreateGrid(world, encounter);
            var unitsRoot = CreateUnits(world, encounter);
            var timelineUi = TimelineHierarchyBuilder.BuildTimeline(canvas.transform);
            var skillPanel = TimelineHierarchyBuilder.BuildSkillPanel(canvas.transform);
            var partyBar = TimelineHierarchyBuilder.BuildPartyStatusBar(canvas.transform);
            var executeOverlay = TimelineHierarchyBuilder.BuildExecuteOverlay(canvas.transform);

            WireBootstrap(bootstrap, controller, timelineUi, skillPanel, partyBar, executeOverlay, unitsRoot, gridRoot);
            WireController(controller, timelineUi, skillPanel, executeOverlay);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            Debug.Log("[Fractured Chorus] Combat hierarchy created. Save scene, then drag objects in Hierarchy to adjust layout.");
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var camGo = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGo, "Create Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
        }

        private static void EnsureEventSystem()
        {
            CombatInputSetup.EnsureEventSystem();

            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                CombatInputSetup.ApplyInputModule(eventSystem.gameObject, destroyImmediate: true);
            }
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            var existingTransform = parent.Find(CombatUiHierarchy.CombatCanvasName);
            if (existingTransform != null &&
                existingTransform.TryGetComponent<Canvas>(out var existingCanvas))
            {
                return existingCanvas;
            }

            var canvasGo = new GameObject(CombatUiHierarchy.CombatCanvasName);
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create CombatCanvas");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Transform CreateWorldRoot(Transform parent)
        {
            var world = new GameObject("World");
            Undo.RegisterCreatedObjectUndo(world, "Create World");
            world.transform.SetParent(parent, false);
            return world.transform;
        }

        private static Transform CreateGrid(Transform world, EncounterDefinitionSO encounter)
        {
            var grid = new GameObject("Grid");
            Undo.RegisterCreatedObjectUndo(grid, "Create Grid");
            grid.transform.SetParent(world, false);

            var playerGrid = new GameObject("PlayerGrid");
            playerGrid.transform.SetParent(grid.transform, false);
            var enemyGrid = new GameObject("EnemyGrid");
            enemyGrid.transform.SetParent(grid.transform, false);

            for (var side = 0; side < 2; side++)
            {
                var gridSide = side == 0 ? GridSide.Player : GridSide.Enemy;
                var parent = gridSide == GridSide.Player ? playerGrid.transform : enemyGrid.transform;

                for (var row = 0; row < DualGrid.Rows; row++)
                {
                    for (var col = 0; col < DualGrid.Columns; col++)
                    {
                        CreateGridCell(parent, gridSide, row, col);
                    }
                }
            }

            return grid.transform;
        }

        private static void CreateGridCell(Transform parent, GridSide side, int row, int col)
        {
            var pos = new GridPosition(side, row, col);
            var world = HexBoardLayout.GetWorldPosition(pos);

            var cellGo = new GameObject($"Cell_{side}_R{row}_C{col}");
            Undo.RegisterCreatedObjectUndo(cellGo, "Create Grid Cell");
            cellGo.transform.SetParent(parent, false);
            cellGo.transform.position = new Vector3(world.x, world.y, 0f);

            var marker = Undo.AddComponent<GridCellMarker>(cellGo);
            marker.Configure(side, row, col);
            marker.SetFloorSprite(HexSpriteUtil.ResolveHexagonFlatTop());
            marker.EnsureVisuals();
        }

        private static Transform CreateUnits(Transform world, EncounterDefinitionSO encounter)
        {
            var units = new GameObject("Units");
            Undo.RegisterCreatedObjectUndo(units, "Create Units");
            units.transform.SetParent(world, false);

            var unitViews = new System.Collections.Generic.List<UnitView>();
            foreach (var spawn in encounter.units)
            {
                if (spawn.preset == null)
                {
                    continue;
                }

                var pos = new GridPosition(spawn.side, spawn.row, spawn.column);
                var worldPos = GetWorldPosition(pos);

                var unitGo = new GameObject($"Unit_{spawn.preset.displayName}");
                Undo.RegisterCreatedObjectUndo(unitGo, "Create Unit");
                unitGo.transform.SetParent(units.transform, false);
                unitGo.transform.position = worldPos;
                unitGo.transform.localScale = Vector3.one * 0.9f;

                var sr = unitGo.AddComponent<SpriteRenderer>();
                sr.sprite = CreatePlaceholderSprite();
                sr.color = spawn.preset.placeholderColor;
                sr.sortingOrder = 10 + spawn.row;

                var view = Undo.AddComponent<UnitView>(unitGo);
                view.ConfigureDemo(GetDemoKey(spawn.preset), spawn.side);
                view.PlaceOnGrid(pos);
                view.EnsureInteractionColliders();
                unitViews.Add(view);
            }

            return units.transform;
        }

        private static string GetDemoKey(UnitPresetSO preset)
        {
            if (preset == null || string.IsNullOrEmpty(preset.unitId))
            {
                return "grunt";
            }

            return preset.unitId;
        }

        private static BeatTimelineUIView CreateTimelineUi(Canvas canvas)
        {
            var timelineGo = new GameObject("BeatTimelineUI");
            Undo.RegisterCreatedObjectUndo(timelineGo, "Create BeatTimelineUI");
            timelineGo.transform.SetParent(canvas.transform, false);

            var rootRect = timelineGo.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.05f, 0.02f);
            rootRect.anchorMax = new Vector2(0.95f, 0.18f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var barBg = timelineGo.AddComponent<Image>();
            barBg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            CreateTimelineHeader(timelineGo.transform);
            var segments = CreateTimelineSegments(timelineGo.transform);
            CreateConfirmButton(timelineGo.transform); // must run before WireReferences

            var confirmButton = timelineGo.transform.Find("ConfirmButton")?.GetComponent<Button>();
            var phaseLabel = timelineGo.transform.Find("ConfirmButton/PhaseLabel")?.GetComponent<Text>();
            var budgetLabel = timelineGo.transform.Find("Header/Budget/BudgetText")?.GetComponent<Text>();

            var ui = Undo.AddComponent<BeatTimelineUIView>(timelineGo);
            SetSerializedField(ui, "confirmButton", confirmButton);
            SetSerializedField(ui, "phaseLabel", phaseLabel);
            SetSerializedField(ui, "budgetLabel", budgetLabel);
            ui.WireReferences();
            return ui;
        }

        private static void CreateTimelineHeader(Transform parent)
        {
            var headerGo = CreateUiObject("Header", parent);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 0.5f);
            headerRect.sizeDelta = new Vector2(110f, 0f);

            var clefGo = CreateUiObject("Clef", headerGo.transform);
            var clefRect = clefGo.GetComponent<RectTransform>();
            clefRect.anchorMin = new Vector2(0f, 0.5f);
            clefRect.anchorMax = new Vector2(0f, 0.5f);
            clefRect.anchoredPosition = new Vector2(12f, 0f);
            clefRect.sizeDelta = new Vector2(24f, 48f);
            var clefImage = clefGo.AddComponent<Image>();
            var clefSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/FracturedChorus/Resources/UI/clef_g_v1.png");
            if (clefSprite != null)
            {
                clefImage.sprite = clefSprite;
            }

            clefImage.type = Image.Type.Simple;
            clefImage.preserveAspect = true;
            clefImage.color = Color.white;
            clefImage.raycastTarget = false;

            var budgetGo = CreateUiObject("Budget", headerGo.transform);
            var budgetRect = budgetGo.GetComponent<RectTransform>();
            budgetRect.anchorMin = new Vector2(0f, 0.5f);
            budgetRect.anchorMax = new Vector2(0f, 0.5f);
            budgetRect.anchoredPosition = new Vector2(70f, 0f);
            budgetRect.sizeDelta = new Vector2(36f, 36f);
            budgetGo.AddComponent<Image>().color = new Color(0.8f, 0.2f, 0.6f, 0.8f);

            var budgetTextGo = CreateUiObject("BudgetText", budgetGo.transform);
            StretchFull(budgetTextGo.GetComponent<RectTransform>());
            var budgetText = budgetTextGo.AddComponent<Text>();
            ApplyTextDefaults(budgetText);
            budgetText.text = "3/3";
        }

        private static BeatSegmentView[] CreateTimelineSegments(Transform parent)
        {
            var segmentsGo = CreateUiObject("Segments", parent);
            var segmentsRect = segmentsGo.GetComponent<RectTransform>();
            segmentsRect.anchorMin = Vector2.zero;
            segmentsRect.anchorMax = Vector2.one;
            segmentsRect.offsetMin = new Vector2(120f, 8f);
            segmentsRect.offsetMax = new Vector2(-120f, -8f);

            var layout = segmentsGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var segments = new BeatSegmentView[BeatTimelineEngine.BeatCount];
            for (var i = 0; i < BeatTimelineEngine.BeatCount; i++)
            {
                segments[i] = CreateBeatSegment(segmentsGo.transform, i);
            }

            return segments;
        }

        private static BeatSegmentView CreateBeatSegment(Transform parent, int index)
        {
            var segGo = CreateUiObject($"Beat_{index}", parent);
            segGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.85f);

            var glowGo = CreateUiObject("Glow", segGo.transform);
            StretchWithPadding(glowGo.GetComponent<RectTransform>(), 0.05f, 0.1f, 0.95f, 0.9f);
            glowGo.AddComponent<Image>().color = new Color(1f, 0.2f, 0.2f, 0.15f);

            var portraitGo = CreateUiObject("Portrait", segGo.transform);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0.5f);
            portraitRect.anchorMax = new Vector2(0f, 0.5f);
            portraitRect.pivot = new Vector2(0f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(4f, 0f);
            portraitRect.sizeDelta = new Vector2(28f, 28f);
            portraitGo.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f, 1f);

            var labelGo = CreateUiObject("ActionLabel", segGo.transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(36f, 4f);
            labelRect.offsetMax = new Vector2(-4f, -4f);
            var label = labelGo.AddComponent<Text>();
            ApplyTextDefaults(label);
            label.fontStyle = FontStyle.Italic;
            label.fontSize = 12;
            label.alignment = TextAnchor.MiddleLeft;

            var segment = segGo.AddComponent<BeatSegmentView>();
            segment.WireReferences();
            return segment;
        }

        private static Button CreateConfirmButton(Transform parent)
        {
            var btnGo = CreateUiObject("ConfirmButton", parent);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1f, 0.5f);
            btnRect.anchorMax = new Vector2(1f, 0.5f);
            btnRect.pivot = new Vector2(1f, 0.5f);
            btnRect.anchoredPosition = new Vector2(-8f, 0f);
            btnRect.sizeDelta = new Vector2(100f, 40f);
            btnGo.AddComponent<Image>().color = new Color(0.35f, 0.15f, 0.55f, 0.95f);
            var button = btnGo.AddComponent<Button>();

            var labelGo = CreateUiObject("Label", btnGo.transform);
            StretchFull(labelGo.GetComponent<RectTransform>());
            var label = labelGo.AddComponent<Text>();
            ApplyTextDefaults(label);
            label.text = "Deploy";

            var phaseGo = CreateUiObject("PhaseLabel", btnGo.transform);
            var phaseRect = phaseGo.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0.5f, 1f);
            phaseRect.anchorMax = new Vector2(0.5f, 1f);
            phaseRect.anchoredPosition = new Vector2(0f, 18f);
            phaseRect.sizeDelta = new Vector2(120f, 20f);
            var phase = phaseGo.AddComponent<Text>();
            ApplyTextDefaults(phase);
            phase.fontSize = 12;
            phase.text = "PLANNING";

            return button;
        }

        private static void WireBootstrap(
            CombatPrototypeBootstrap bootstrap,
            CombatController controller,
            BeatTimelineUIView timeline,
            SkillPanelUIView skillPanel,
            PartyStatusBarUIView partyBar,
            CombatExecuteOverlayUIView executeOverlay,
            Transform unitsRoot,
            Transform gridRoot)
        {
            SetSerializedField(bootstrap, "combatController", controller);
            SetSerializedField(bootstrap, "timelineView", timeline);
            SetSerializedField(bootstrap, "skillPanelView", skillPanel);
            SetSerializedField(bootstrap, "partyStatusBarView", partyBar);
            SetSerializedField(bootstrap, "executeOverlay", executeOverlay);
            SetSerializedField(bootstrap, "unitsRoot", unitsRoot);
            SetSerializedField(bootstrap, "gridRoot", gridRoot);
            SetSerializedField(bootstrap, "unitViews", unitsRoot.GetComponentsInChildren<UnitView>(true));
            SetSerializedField(bootstrap, "mainCamera", Camera.main);

            if (bootstrap.GetComponent<BoardDragController>() == null)
            {
                Undo.AddComponent<BoardDragController>(bootstrap.gameObject);
            }
        }

        private static void WireController(
            CombatController controller,
            BeatTimelineUIView timeline,
            SkillPanelUIView skillPanel,
            CombatExecuteOverlayUIView executeOverlay = null)
        {
            SetSerializedField(controller, "timelineView", timeline);
            SetSerializedField(controller, "skillPanelView", skillPanel);
            SetSerializedField(controller, "executeOverlay", executeOverlay);
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchWithPadding(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyTextDefaults(Text text)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        private static Vector3 GetWorldPosition(GridPosition position)
        {
            return HexBoardLayout.GetWorldPosition(position, SideGap);
        }

        [MenuItem("Fractured Chorus/Rebuild Hex Board Grid (scene)")]
        public static void RebuildHexBoardInScene()
        {
            var gridRoot = GameObject.Find("CombatRoot/World/Grid") ?? GameObject.Find("World/Grid") ?? GameObject.Find("Grid");
            if (gridRoot == null)
            {
                Debug.LogWarning("[Fractured Chorus] No Grid root found.");
                return;
            }

            var floorSprite = HexSpriteUtil.ResolveHexagonFlatTop();
            var allMarkers = gridRoot.GetComponentsInChildren<GridCellMarker>(true);

            var deleted = 0;
            foreach (var side in new[] { GridSide.Player, GridSide.Enemy })
            {
                // Nhóm ô theo hàng dựa trên Y hiện tại, sắp từ TRÊN xuống DƯỚI.
                var rows = allMarkers
                    .Where(m => m != null && m.Side == side)
                    .GroupBy(m => Mathf.Round(m.transform.position.y * 4f) / 4f)
                    .OrderByDescending(g => g.Key)
                    .ToList();

                for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    // Chỉ giữ DualGrid.Rows hàng trên cùng; hàng dưới cùng bị xoá khỏi scene.
                    if (rowIndex >= DualGrid.Rows)
                    {
                        foreach (var marker in rows[rowIndex])
                        {
                            Undo.DestroyObjectImmediate(marker.gameObject);
                            deleted++;
                        }

                        continue;
                    }

                    // Re-index hàng còn lại về 0-based (row 0 = trên) để khớp code lưới.
                    foreach (var marker in rows[rowIndex])
                    {
                        Undo.RecordObject(marker.transform, "Rebuild Hex Board");
                        Undo.RecordObject(marker, "Rebuild Hex Board");
                        marker.gameObject.SetActive(true);
                        marker.Configure(side, rowIndex, marker.Column);
                        marker.SnapToLayoutPosition(SideGap);
                        marker.SetFloorSprite(floorSprite);
                        marker.RebuildVisuals();
                        EditorUtility.SetDirty(marker.gameObject);
                    }
                }
            }

            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include))
            {
                if (DefaultPartyFormation.TryGetStartupCell(view.DemoUnitKey, view.Side, out var pos))
                {
                    view.PlaceOnGrid(pos);
                }

                if (!view.IsPlacedOnGrid)
                {
                    continue;
                }

                var world = HexBoardLayout.GetWorldPosition(view.GridPosition, SideGap);
                view.transform.position = new Vector3(world.x, world.y, -0.05f);
                EditorUtility.SetDirty(view.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(gridRoot.scene);
            Debug.Log($"[Fractured Chorus] Hex board rebuilt 2×3. Removed {deleted} bottom-row cell(s), kept top 2 rows (units + top), re-indexed to R0(top)/R1(units) + snapped saved coordinates. Save scene.");
        }

        [MenuItem("Fractured Chorus/Add Knight of Despair (Boss) to Scene")]
        public static void AddKnightOfDespairToScene()
        {
            RunBossSceneSetup(saveScene: true, log: true);
        }

        public static void RunBossSceneSetupBatch()
        {
            EditorSceneManager.OpenScene("Assets/FracturedChorus/Scenes/CombatPrototype.unity");
            RunBossSceneSetup(saveScene: true, log: true);
        }

        private static void RunBossSceneSetup(bool saveScene, bool log)
        {
            CombatDataAssetGenerator.CreateBossDespairAssets();
            AssetDatabase.SaveAssets();

            var preset = AssetDatabase.LoadAssetAtPath<UnitPresetSO>(
                "Assets/FracturedChorus/Resources/UnitPresets/UnitPreset_Boss_Despair.asset");
            if (preset == null)
            {
                Debug.LogError("[Fractured Chorus] UnitPreset_Boss_Despair missing after asset generation.");
                return;
            }

            var unitsRoot = GameObject.Find("CombatRoot/World/Units")?.transform
                ?? GameObject.Find("World/Units")?.transform;
            if (unitsRoot == null)
            {
                Debug.LogError("[Fractured Chorus] Units root not found — open CombatPrototype scene first.");
                return;
            }

            var bossCell = new GridPosition(GridSide.Enemy, 1, 1);
            var bossWorld = HexBoardLayout.GetWorldPosition(bossCell, SideGap);

            var existing = unitsRoot.Find("Unit_Knight of Despair");
            if (existing != null)
            {
                var existingView = existing.GetComponent<UnitView>();
                if (existingView != null)
                {
                    existingView.PlaceOnGrid(bossCell);
                    existing.transform.position = new Vector3(bossWorld.x, bossWorld.y, -0.05f);
                    EditorUtility.SetDirty(existingView);
                }

                EditorUtility.SetDirty(existing);
                RegisterBossWithBootstrap(existingView);
                EditorSceneManager.MarkSceneDirty(unitsRoot.gameObject.scene);

                if (log)
                {
                    Debug.Log("[Fractured Chorus] Unit_Knight of Despair repositioned to Enemy R1 C1.");
                }

                if (saveScene)
                {
                    EditorSceneManager.SaveOpenScenes();
                }

                return;
            }

            var pos = bossCell;
            var worldPos = bossWorld;

            var unitGo = new GameObject("Unit_Knight of Despair");
            Undo.RegisterCreatedObjectUndo(unitGo, "Add Knight of Despair");
            unitGo.transform.SetParent(unitsRoot, false);
            unitGo.transform.position = new Vector3(worldPos.x, worldPos.y, -0.05f);
            unitGo.transform.localScale = Vector3.one * 0.2f;

            var sr = unitGo.AddComponent<SpriteRenderer>();
            sr.sprite = preset.battleSprite;
            sr.sortingOrder = 10 + pos.Row;
            sr.color = preset.battleSprite != null ? Color.white : preset.placeholderColor;

            var view = Undo.AddComponent<UnitView>(unitGo);
            SetSerializedField(view, "preset", preset);
            SetSerializedField(view, "demoUnitKey", "boss_despair");
            SetSerializedField(view, "side", (int)GridSide.Enemy);
            view.PlaceOnGrid(pos);
            view.EnsureInteractionColliders();
            view.RefitBodyColliderToSprite();
            EditorUtility.SetDirty(view);

            RegisterBossWithBootstrap(view);
            EditorSceneManager.MarkSceneDirty(unitsRoot.gameObject.scene);

            if (saveScene)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            if (log)
            {
                Debug.Log(
                    "[Fractured Chorus] Added Knight of Despair at Enemy R1 C1. HP 1680 · STR 58 · Pulse 130.");
            }
        }

        private static void RegisterBossWithBootstrap(UnitView bossView)
        {
            if (bossView == null)
            {
                return;
            }

            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap == null)
            {
                return;
            }

            var so = new SerializedObject(bootstrap);
            var prop = so.FindProperty("unitViews");
            if (prop == null || !prop.isArray)
            {
                return;
            }

            for (var i = 0; i < prop.arraySize; i++)
            {
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == bossView)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            prop.arraySize += 1;
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = bossView;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);
        }

        private static Sprite CreatePlaceholderSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private static void SetSerializedField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedField<T>(Object target, string fieldName, T value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            switch (value)
            {
                case bool boolValue:
                    prop.boolValue = boolValue;
                    break;
                case float floatValue:
                    prop.floatValue = floatValue;
                    break;
                case int intValue:
                    prop.intValue = intValue;
                    break;
                case string stringValue:
                    prop.stringValue = stringValue;
                    break;
                default:
                    if (typeof(T).IsArray && value != null)
                    {
                        prop.arraySize = ((System.Array)(object)value).Length;
                        var array = (System.Array)(object)value;
                        for (var i = 0; i < array.Length; i++)
                        {
                            prop.GetArrayElementAtIndex(i).objectReferenceValue = array.GetValue(i) as Object;
                        }
                    }

                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
