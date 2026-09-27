#if UNITY_EDITOR
using System.Collections.Generic;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.Tutorial;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class CombatTutorialSceneSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/CombatTutorial.unity";
        private const string SourceScenePath = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";
        private const string TutorialBgPath = "Assets/FracturedChorus/Art/Backgrounds/cadence_smoke_war_front_bg_v1.png";
        private const string KikiIdlePath = "Assets/FracturedChorus/Art/Characters/KikiUeda/kiki_ueda_idle_v1.png";
        private const string MimiIconPath = "Assets/FracturedChorus/Art/UI/Combat/Characters/mimi_character_icon_bars_elite_v1.png";
        private const string MimiAvatarPath = "Assets/FracturedChorus/Art/UI/Combat/Characters/Avatars/mimi_enemy_avatar_v1.png";
        private const string KikiControllerPath = "Assets/FracturedChorus/Art/Characters/KikiUeda/Unit_Kiki_Ueda.controller";
        private const string KikiPresetPath = "Assets/FracturedChorus/Resources/UnitPresets/UnitPreset_Kiki_Ueda.asset";

        public static void OpenCombatTutorialScene()
        {
            EnsureSceneExists();
            if (!System.IO.File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Fractured Chorus", "CombatTutorial.unity missing.", "OK");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        public static void PrepareCombatTutorialScene()
        {
            EnsureSceneExists();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath);
            ApplyTutorialAuthoringDefaults();
            StripLegacyTutorialLayers();
            RestoreCombatTutorialVisuals();
            SyncCombatTutorialUi();
            CombatQteOverlaySetupEditor.SetupQteOverlay();
            TutorialCadenceTrackSetupEditor.EnsureTutorialDirectorInCombatTutorialScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EnsureInBuildSettings();
            Debug.Log(
                "[Fractured Chorus] CombatTutorial prepared — slideshow khung (ảnh step add sau). " +
                "BG + Ren/Coda/Kiki đã bật lại.");
        }

        /// <summary>Batch entry: no dialogs. Unity -executeMethod FracturedChorus.Editor.CombatTutorialSceneSetupEditor.SyncCombatTutorialUiBatch</summary>
        public static void SyncCombatTutorialUiBatch()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SyncCombatTutorialUi();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Fractured Chorus] CombatTutorial UI batch sync saved.");
        }

        [MenuItem("Fractured Chorus/Tutorial/Sync CombatTutorial UI With CombatPrototype")]
        public static void SyncCombatTutorialUiMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Combat Tutorial", "Thoát Play Mode trước.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SyncCombatTutorialUi();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            SceneView.RepaintAll();
            Debug.Log(
                "[Fractured Chorus] CombatTutorial UI copied from CombatPrototype objects: " +
                "timeline rects, boss note rail, grid cells, card chrome. Boss TV left behind.");
            EditorUtility.DisplayDialog(
                "Combat Tutorial",
                "Đã copy layout từ CombatPrototype & Save.\n\n" +
                "• Timeline + line note quái + lane/avatar lấy từ object prototype\n" +
                "• Ô đứng copy theo tên Cell_*\n" +
                "• Thẻ party/quái: avatar + ống HP/gauge, không TV boss\n\n" +
                "Mở tab Game rồi Play để kiểm tra.",
                "OK");
        }

        /// <summary>
        /// Đưa TLB, ô đứng và thẻ của CombatTutorial về object đã author ở CombatPrototype.
        /// Không copy TV boss (BuffAstraTv, BuffReduceS2, AstraStageTv).
        /// </summary>
        public static void SyncCombatTutorialUi()
        {
            var tutorialScene = SceneManager.GetActiveScene();
            if (!string.Equals(tutorialScene.path, ScenePath, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[Fractured Chorus] Sync expects the active scene to be CombatTutorial.");
                return;
            }

            var prototypeScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            try
            {
                CopyTimelineLayoutFromPrototype(prototypeScene, tutorialScene);
                CopyGridCellsFromPrototype(prototypeScene, tutorialScene);
                CopyStatusCardsFromPrototype(prototypeScene, tutorialScene);
                StripBossOnlyTv(tutorialScene);
            }
            finally
            {
                if (prototypeScene.IsValid())
                {
                    EditorSceneManager.CloseScene(prototypeScene, true);
                }
            }

            StripTutorialHotkeys();
            ApplyEnemyCardIcon();
        }

        [MenuItem("Fractured Chorus/Tutorial/Strip Tutorial Fight Hotkey In All Scenes")]
        public static void StripTutorialHotkeyInAllScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Combat Tutorial", "Thoát Play Mode trước.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var activePath = SceneManager.GetActiveScene().path;
            var stripped = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/FracturedChorus/Scenes" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var before = CountTutorialHotkeys();
                if (before == 0)
                {
                    continue;
                }

                StripTutorialHotkeys();
                stripped += before;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(activePath))
            {
                EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
            }

            Debug.Log($"[Fractured Chorus] Stripped {stripped} TutorialCombatHotkey object(s) across scenes.");
        }

        private static int CountTutorialHotkeys()
        {
            var count = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go != null && go.name == "TutorialCombatHotkey")
                {
                    count++;
                }
            }

            return count;
        }

        [MenuItem("Fractured Chorus/Tutorial/Restore CombatTutorial Visuals (BG + units)")]
        public static void RestoreVisualsMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Combat Tutorial", "Thoát Play Mode trước.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RestoreCombatTutorialVisuals();
            SyncCombatTutorialUi();
            CombatQteOverlaySetupEditor.SetupQteOverlay();
            TutorialCadenceTrackSetupEditor.EnsureTutorialDirectorInCombatTutorialScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            SceneView.RepaintAll();
            Debug.Log(
                "[Fractured Chorus] Restored + saved CombatTutorial: " +
                "CombatCanvas=Camera, TutorialCoach→CombatCanvas, BG/World/Units on, dimmer light.");
            EditorUtility.DisplayDialog(
                "Combat Tutorial",
                "Đã reload scene, restore & Save.\n\n" +
                "• CombatCanvas → Screen Space Camera\n" +
                "• TutorialCoach ra khỏi ResultOverlay\n" +
                "• BG + Ren/Coda/Kiki bật\n\n" +
                "Mở tab Game rồi Play để kiểm tra.",
                "OK");
        }

        private static void EnsureSceneExists()
        {
            if (System.IO.File.Exists(ScenePath))
            {
                EnsureInBuildSettings();
                return;
            }

            if (!System.IO.File.Exists(SourceScenePath))
            {
                Debug.LogError("[Fractured Chorus] CombatPrototype.unity not found — cannot clone CombatTutorial.");
                return;
            }

            AssetDatabase.CopyAsset(SourceScenePath, ScenePath);
            EnsureInBuildSettings();
            AssetDatabase.Refresh();
        }

        private static void RestoreCombatTutorialVisuals()
        {
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = GameObject.Find("Main Camera");
                if (camGo != null)
                {
                    mainCam = camGo.GetComponent<Camera>();
                }
            }

            var combatCanvas = GameObject.Find("CombatCanvas")?.GetComponent<Canvas>();
            if (combatCanvas != null)
            {
                Undo.RecordObject(combatCanvas, "Restore CombatCanvas Camera Mode");
                combatCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                if (mainCam != null)
                {
                    combatCanvas.worldCamera = mainCam;
                }

                combatCanvas.planeDistance = 100f;
                EditorUtility.SetDirty(combatCanvas);
            }

            var bgRoot = GameObject.Find("Background canvas");
            if (bgRoot != null)
            {
                Undo.RecordObject(bgRoot, "Restore Tutorial BG");
                bgRoot.SetActive(true);
                var bgCanvas = bgRoot.GetComponent<Canvas>();
                if (bgCanvas != null)
                {
                    Undo.RecordObject(bgCanvas, "Restore BG Canvas");
                    bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    if (mainCam != null)
                    {
                        bgCanvas.worldCamera = mainCam;
                    }

                    bgCanvas.planeDistance = 100f;
                    bgCanvas.sortingOrder = -1;
                    EditorUtility.SetDirty(bgCanvas);
                }

                var bgImage = bgRoot.GetComponentInChildren<Image>(true);
                if (bgImage != null)
                {
                    Undo.RecordObject(bgImage, "Restore Tutorial BG Image");
                    bgImage.gameObject.SetActive(true);
                    bgImage.enabled = true;
                    bgImage.color = Color.white;
                    var sprite = LoadFirstSprite(TutorialBgPath);
                    if (sprite != null)
                    {
                        bgImage.sprite = sprite;
                    }

                    EditorUtility.SetDirty(bgImage);
                }

                EditorUtility.SetDirty(bgRoot);
            }

            var world = GameObject.Find("World");
            if (world != null)
            {
                Undo.RecordObject(world, "Restore World");
                world.SetActive(true);
                EditorUtility.SetDirty(world);
            }

            var units = GameObject.Find("Units") ?? GameObject.Find("World/Units");
            if (units != null)
            {
                Undo.RecordObject(units, "Restore Units Root");
                units.SetActive(true);
                EditorUtility.SetDirty(units);
            }

            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include))
            {
                if (view == null)
                {
                    continue;
                }

                var key = view.DemoUnitKey?.ToLowerInvariant() ?? string.Empty;
                var name = view.gameObject.name.ToLowerInvariant();
                var isKiki = name.Contains("kiki") || key.Contains("kiki");
                var isCharlotte = key.Contains("charl") || name.Contains("charlott") || name.Contains("charlotte");
                var isParty = !isCharlotte
                              && (view.Side == GridSide.Player
                                  || key.Contains("ren")
                                  || key.Contains("coda")
                                  || key.Contains("mage")
                                  || name.Contains("ren")
                                  || name.Contains("mage"));
                var hide = name.Contains("boss") || name.Contains("grunt") || name.Contains("tank") || isCharlotte;
                var keep = (isParty || isKiki) && !hide;
                if (isKiki)
                {
                    keep = true;
                }

                Undo.RecordObject(view.gameObject, "Restore Tutorial Units");
                view.gameObject.SetActive(keep);
                if (keep)
                {
                    view.SetVisualDimFactor(1f);
                    foreach (var sr in view.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (sr == null)
                        {
                            continue;
                        }

                        Undo.RecordObject(sr, "Restore Tutorial Sprite");
                        sr.enabled = true;
                        if (sr.sprite == null && view.Preset != null && view.Preset.battleSprite != null)
                        {
                            sr.sprite = view.Preset.battleSprite;
                        }

                        sr.color = Color.white;
                        EditorUtility.SetDirty(sr);
                    }
                }

                EditorUtility.SetDirty(view.gameObject);
            }

            foreach (var dimmer in Object.FindObjectsByType<CombatFocusDimmer>(FindObjectsInactive.Include))
            {
                dimmer.ReleaseImmediate();
                EditorUtility.SetDirty(dimmer);
            }

            var resultOverlay = GameObject.Find("CombatResultOverlay");
            if (resultOverlay != null)
            {
                Undo.RecordObject(resultOverlay, "Keep Result Overlay Hidden");
                resultOverlay.SetActive(false);
                EditorUtility.SetDirty(resultOverlay);
            }

            var coach = Object.FindAnyObjectByType<TutorialCoachView>(FindObjectsInactive.Include);
            if (coach != null)
            {
                if (combatCanvas != null && coach.transform.parent != combatCanvas.transform)
                {
                    Undo.SetTransformParent(coach.transform, combatCanvas.transform, "Move TutorialCoach under CombatCanvas");
                    var rt = coach.transform as RectTransform;
                    if (rt != null)
                    {
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                        rt.localScale = Vector3.one;
                    }
                }

                Undo.RecordObject(coach.gameObject, "Hide Tutorial Coach in Edit");
                coach.gameObject.SetActive(false);
                var dimmerTf = coach.transform.Find("Dimmer");
                if (dimmerTf != null)
                {
                    var dimmerImg = dimmerTf.GetComponent<Image>();
                    if (dimmerImg != null)
                    {
                        Undo.RecordObject(dimmerImg, "Lighten coach dimmer");
                        dimmerImg.color = new Color(0f, 0f, 0f, 0.12f);
                        dimmerImg.raycastTarget = false;
                        EditorUtility.SetDirty(dimmerImg);
                    }
                }

                var so = new SerializedObject(coach);
                var dimmerProp = so.FindProperty("dimmer");
                if (dimmerProp != null && dimmerTf != null)
                {
                    dimmerProp.objectReferenceValue = dimmerTf.GetComponent<Image>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var alphaProp = so.FindProperty("slideshowDimmerAlpha");
                if (alphaProp != null)
                {
                    alphaProp.floatValue = 0.12f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorUtility.SetDirty(coach.gameObject);
            }

            RefreshBootstrapUnitViews();
        }

        private static void StripLegacyTutorialLayers()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go == null)
                {
                    continue;
                }

                if (go.name == "TutorialEditCanvas"
                    || go.name == "CombatTutorialDirector"
                    || go.name == "TutorialSteps"
                    || go.name == "TutorialHighlightOverlay"
                    || go.name == "TutorialCombatHotkey")
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }

            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb == null)
                {
                    continue;
                }

                var typeName = mb.GetType().Name;
                if (typeName is "TutorialCombatBridge" or "CombatTutorialDirector" or "CombatTutorialStepAuthoring")
                {
                    Undo.DestroyObjectImmediate(mb);
                }
            }
        }

        /// <summary>
        /// CombatPrototype scene là source of truth. Copy RectTransform + field đã author,
        /// không ghi số cứng. preserveSceneLayout=1 nên Play không tự sửa.
        /// </summary>
        private static void CopyTimelineLayoutFromPrototype(Scene prototypeScene, Scene tutorialScene)
        {
            var prototype = FindInScene<BeatTimelineUIView>(prototypeScene);
            var tutorial = FindInScene<BeatTimelineUIView>(tutorialScene);
            if (prototype == null || tutorial == null)
            {
                Debug.LogWarning("[Fractured Chorus] BeatTimelineUIView missing in prototype or tutorial.");
                return;
            }

            var protoRoot = prototype.transform as RectTransform;
            var tutorialRoot = tutorial.transform as RectTransform;
            CopyRectTransform(protoRoot, tutorialRoot);
            CopyImageColor(prototype.GetComponent<Image>(), tutorial.GetComponent<Image>());

            var protoViewport = protoRoot != null ? protoRoot.Find("Viewport") : null;
            var tutorialViewport = tutorialRoot != null ? tutorialRoot.Find("Viewport") : null;
            CopyRectTransform(protoViewport as RectTransform, tutorialViewport as RectTransform);

            foreach (var childName in new[] { "ScrollContent", "LaneFootprint", "LaneMarkers", "LaneLines" })
            {
                SyncChild(protoViewport, tutorialViewport, childName);
            }

            SyncChild(protoRoot, tutorialRoot, "LaneAvatarGutter");

            var protoLanes = protoViewport != null ? protoViewport.Find("LaneLines") : null;
            var tutorialLanes = tutorialViewport != null ? tutorialViewport.Find("LaneLines") : null;
            for (var i = 0; i < 4; i++)
            {
                SyncChild(protoLanes, tutorialLanes, $"Lane_{i}");
            }

            var protoGutter = protoRoot != null ? protoRoot.Find("LaneAvatarGutter") : null;
            var tutorialGutter = tutorialRoot != null ? tutorialRoot.Find("LaneAvatarGutter") : null;
            for (var i = 0; i < 4; i++)
            {
                SyncChild(protoGutter, tutorialGutter, $"LaneAvatar_{i}");
            }

            SyncChild(protoViewport, tutorialViewport, "BossTrackFrame");
            var protoFrame = protoViewport != null ? protoViewport.Find("BossTrackFrame") : null;
            var tutorialFrame = tutorialViewport != null ? tutorialViewport.Find("BossTrackFrame") : null;
            SyncChild(protoFrame, tutorialFrame, "BorderTop");

            SyncChild(protoViewport, tutorialViewport, "BossNoteClusterLayer");
            var protoCluster = protoViewport != null ? protoViewport.Find("BossNoteClusterLayer") : null;
            var cluster = tutorialViewport != null ? tutorialViewport.Find("BossNoteClusterLayer") : null;
            SyncChild(protoCluster, cluster, BossNoteSimulator.ObjectName);

            SyncChild(protoRoot, tutorialRoot, "BrowseLeftButton");
            SyncChild(protoRoot, tutorialRoot, "BrowseRightButton");

            var scroll = tutorialViewport != null ? tutorialViewport.Find("ScrollContent") : null;
            DestroyNamedChild(scroll, "Beat_1");

            CopyTimelineFields(prototype, tutorial, tutorialFrame as RectTransform);
            tutorial.WireReferences();
            EditorUtility.SetDirty(tutorial);
        }

        private static void CopyTimelineFields(
            BeatTimelineUIView prototype,
            BeatTimelineUIView tutorial,
            RectTransform tutorialFrame)
        {
            var src = new SerializedObject(prototype);
            var dst = new SerializedObject(tutorial);
            var noteVisuals = src.FindProperty("noteVisuals");
            if (noteVisuals != null)
            {
                dst.CopyFromSerializedProperty(noteVisuals);
            }

            var rail = src.FindProperty("bossNoteRailAnchoredY");
            var dstRail = dst.FindProperty("bossNoteRailAnchoredY");
            if (rail != null && dstRail != null)
            {
                dstRail.floatValue = rail.floatValue;
            }

            var srcSlot = src.FindProperty("leftRailLayout")?.FindPropertyRelative("avatarSlotSize");
            var dstSlot = dst.FindProperty("leftRailLayout")?.FindPropertyRelative("avatarSlotSize");
            if (srcSlot != null && dstSlot != null)
            {
                dstSlot.floatValue = srcSlot.floatValue;
            }

            var frameProp = dst.FindProperty("bossTrackFrame");
            if (frameProp != null && tutorialFrame != null)
            {
                frameProp.objectReferenceValue = tutorialFrame;
            }

            dst.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SyncChild(Transform sourceParent, Transform destParent, string childName)
        {
            if (sourceParent == null || destParent == null || string.IsNullOrEmpty(childName))
            {
                return;
            }

            var source = sourceParent.Find(childName);
            if (source == null)
            {
                return;
            }

            var dest = destParent.Find(childName);
            if (dest == null)
            {
                var clone = Object.Instantiate(source.gameObject, destParent);
                clone.name = childName;
                Undo.RegisterCreatedObjectUndo(clone, "Copy " + childName);
                return;
            }

            CopyRectTransform(source as RectTransform, dest as RectTransform);
        }

        private static void DestroyNamedChild(Transform parent, string childName)
        {
            var child = parent != null ? parent.Find(childName) : null;
            if (child != null)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static void CopyRectTransform(RectTransform source, RectTransform dest)
        {
            if (source == null || dest == null)
            {
                return;
            }

            Undo.RecordObject(dest, "Copy rect from CombatPrototype");
            dest.anchorMin = source.anchorMin;
            dest.anchorMax = source.anchorMax;
            dest.pivot = source.pivot;
            dest.anchoredPosition = source.anchoredPosition;
            dest.sizeDelta = source.sizeDelta;
            dest.localRotation = source.localRotation;
            dest.localScale = source.localScale;
            EditorUtility.SetDirty(dest);
        }

        private static void CopyImageColor(Image source, Image dest)
        {
            if (source == null || dest == null)
            {
                return;
            }

            Undo.RecordObject(dest, "Copy image color from CombatPrototype");
            dest.color = source.color;
            EditorUtility.SetDirty(dest);
        }

        private static void CopyGridCellsFromPrototype(Scene prototypeScene, Scene tutorialScene)
        {
            var prototypeGrid = FindNamed(prototypeScene, "Grid");
            var tutorialGrid = FindNamed(tutorialScene, "Grid");
            if (prototypeGrid == null || tutorialGrid == null)
            {
                Debug.LogWarning("[Fractured Chorus] Grid missing in prototype or tutorial.");
                return;
            }

            var prototypeCells = new List<Transform>();
            CollectByPrefix(prototypeGrid, "Cell_", prototypeCells);
            foreach (var source in prototypeCells)
            {
                var dest = FindDeep(tutorialGrid, source.name);
                if (dest == null)
                {
                    continue;
                }

                Undo.RecordObject(dest, "Copy grid cell from CombatPrototype");
                dest.localPosition = source.localPosition;
                EditorUtility.SetDirty(dest);
            }
        }

        private static void ApplyTutorialAuthoringDefaults()
        {
            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null)
            {
                var so = new SerializedObject(bootstrap);
                so.FindProperty("tutorialSceneMode").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bootstrap);
            }

            CombatDataAssetGenerator.CreateKikiUedaAssets();
            RemoveBossDuplicateEnemies();

            var kikiPreset = AssetDatabase.LoadAssetAtPath<UnitPresetSO>(KikiPresetPath);
            var kikiSprite = LoadFirstSprite(KikiIdlePath);
            var kikiController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KikiControllerPath);
            var wiredKiki = false;

            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include))
            {
                if (view == null)
                {
                    continue;
                }

                var key = view.DemoUnitKey?.ToLowerInvariant() ?? string.Empty;
                if (!wiredKiki && (view.Side == GridSide.Enemy || key.Contains("kiki")))
                {
                    WireKikiView(view, kikiPreset, kikiSprite, kikiController);
                    wiredKiki = true;
                    continue;
                }

                if (view.Side == GridSide.Player)
                {
                    WirePartyAnimatorStates(view);
                }
            }

            if (!wiredKiki)
            {
                foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include))
                {
                    if (view == null || view.Side != GridSide.Enemy)
                    {
                        continue;
                    }

                    view.gameObject.SetActive(true);
                    WireKikiView(view, kikiPreset, kikiSprite, kikiController);
                    break;
                }
            }

            RefreshBootstrapUnitViews();
            ApplyBackgroundSprite();
            ApplyEnemyCardIcon();
            EnsureStrikeChoreographer();
        }

        private static void EnsureStrikeChoreographer()
        {
            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap == null)
            {
                return;
            }

            var host = bootstrap.gameObject;
            if (host.GetComponent<EnemyStrikeChoreographer>() == null)
            {
                Undo.AddComponent<EnemyStrikeChoreographer>(host);
            }

            if (host.GetComponent<CombatFocusDimmer>() == null)
            {
                Undo.AddComponent<CombatFocusDimmer>(host);
            }

            EditorUtility.SetDirty(host);
        }

        private static void CopyStatusCardsFromPrototype(Scene prototypeScene, Scene tutorialScene)
        {
            CopyStatusBar(
                FindInScene<PartyStatusBarUIView>(prototypeScene),
                FindInScene<PartyStatusBarUIView>(tutorialScene));
            CopyStatusBar(
                FindInScene<EnemyStatusBarUIView>(prototypeScene),
                FindInScene<EnemyStatusBarUIView>(tutorialScene));
        }

        /// <summary>
        /// Copy khung thẻ (avatar, ống HP/gauge) từ prototype. Không mang BuffAstraTv / BuffReduceS2.
        /// </summary>
        private static void CopyStatusBar(MonoBehaviour prototype, MonoBehaviour tutorial)
        {
            if (prototype == null || tutorial == null)
            {
                return;
            }

            CopyRectTransform(prototype.transform as RectTransform, tutorial.transform as RectTransform);

            var protoSo = new SerializedObject(prototype);
            var tutorialSo = new SerializedObject(tutorial);
            var protoSpacing = protoSo.FindProperty("cardSpacing");
            var tutorialSpacing = tutorialSo.FindProperty("cardSpacing");
            if (protoSpacing != null && tutorialSpacing != null)
            {
                tutorialSpacing.floatValue = protoSpacing.floatValue;
            }

            CopyRectTransform(
                protoSo.FindProperty("cardsRow")?.objectReferenceValue as RectTransform,
                tutorialSo.FindProperty("cardsRow")?.objectReferenceValue as RectTransform);

            var protoTemplate = protoSo.FindProperty("cardTemplate")?.objectReferenceValue as PartyMemberCardView;
            if (protoTemplate == null)
            {
                tutorialSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tutorial);
                return;
            }

            var clone = Object.Instantiate(protoTemplate.gameObject, tutorial.transform);
            clone.name = "CardTemplate";
            Undo.RegisterCreatedObjectUndo(clone, "Copy CardTemplate");
            DestroyNamedDeep(clone.transform, "BuffAstraTv");
            DestroyNamedDeep(clone.transform, "BuffReduceS2");
            var avatar = clone.transform.Find("Avatar");
            EnsureAvatarMask(avatar);
            var barStack = clone.transform.Find("BarStack");
            if (avatar != null && barStack != null)
            {
                barStack.SetSiblingIndex(avatar.GetSiblingIndex() + 1);
            }

            var oldTemplate = tutorialSo.FindProperty("cardTemplate")?.objectReferenceValue as PartyMemberCardView;
            if (oldTemplate != null)
            {
                Undo.DestroyObjectImmediate(oldTemplate.gameObject);
            }

            var templateProp = tutorialSo.FindProperty("cardTemplate");
            if (templateProp != null)
            {
                templateProp.objectReferenceValue = clone.GetComponent<PartyMemberCardView>();
            }

            tutorialSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tutorial);
        }

        private static void EnsureAvatarMask(Transform avatar)
        {
            if (avatar == null || avatar.GetComponent<RectMask2D>() != null)
            {
                return;
            }

            Undo.AddComponent<RectMask2D>(avatar.gameObject);
        }

        private static void StripBossOnlyTv(Scene scene)
        {
            foreach (var name in new[] { "BuffAstraTv", "BuffReduceS2", "AstraStageTv" })
            {
                var matches = new List<GameObject>();
                foreach (var root in scene.GetRootGameObjects())
                {
                    CollectByName(root.transform, name, matches);
                }

                foreach (var match in matches)
                {
                    if (match != null)
                    {
                        Undo.DestroyObjectImmediate(match);
                    }
                }
            }
        }

        private static void DestroyNamedDeep(Transform root, string objectName)
        {
            var matches = new List<GameObject>();
            CollectByName(root, objectName, matches);
            foreach (var match in matches)
            {
                if (match != null)
                {
                    Undo.DestroyObjectImmediate(match);
                }
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var component in Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            {
                if (component != null && component.gameObject.scene == scene)
                {
                    return component;
                }
            }

            return null;
        }

        private static Transform FindNamed(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindDeep(root.transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void CollectByPrefix(Transform root, string prefix, List<Transform> into)
        {
            if (root == null)
            {
                return;
            }

            if (root.name.StartsWith(prefix, System.StringComparison.Ordinal))
            {
                into.Add(root);
            }

            for (var i = 0; i < root.childCount; i++)
            {
                CollectByPrefix(root.GetChild(i), prefix, into);
            }
        }

        private static void CollectByName(Transform root, string objectName, List<GameObject> into)
        {
            if (root == null)
            {
                return;
            }

            if (root.name == objectName)
            {
                into.Add(root.gameObject);
            }

            for (var i = 0; i < root.childCount; i++)
            {
                CollectByName(root.GetChild(i), objectName, into);
            }
        }

        /// <summary>Hotkey "Tutorial Fight" đã bỏ khỏi CampusHubController — quét sạch bản còn sót trong scene.</summary>
        private static void StripTutorialHotkeys()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go != null && go.name == "TutorialCombatHotkey")
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }
        }

        private static void ApplyEnemyCardIcon()
        {
            var icon = LoadFirstSprite(MimiAvatarPath) ?? LoadFirstSprite(MimiIconPath);
            if (icon == null)
            {
                return;
            }

            var enemyBar = Object.FindAnyObjectByType<EnemyStatusBarUIView>();
            var template = enemyBar != null ? enemyBar.CardTemplate : null;
            if (template == null)
            {
                var templateTransform = GameObject.Find("EnemyStatusBarUI/CardTemplate");
                if (templateTransform != null)
                {
                    template = templateTransform.GetComponent<PartyMemberCardView>();
                }
            }

            if (template == null)
            {
                return;
            }

            var avatar = template.transform.Find("Avatar");
            var art = avatar != null ? avatar.GetComponent<Image>() : null;
            if (art == null)
            {
                return;
            }

            EnsureAvatarMask(avatar);
            Undo.RecordObject(art, "Set Mimi enemy card icon");
            art.sprite = icon;
            art.color = Color.white;
            art.type = Image.Type.Simple;
            art.preserveAspect = true;
            EditorUtility.SetDirty(art);

            var preset = AssetDatabase.LoadAssetAtPath<UnitPresetSO>(KikiPresetPath);
            if (preset != null)
            {
                Undo.RecordObject(preset, "Set Mimi combat card");
                if (preset.combatCardSprite != icon)
                {
                    preset.combatCardSprite = icon;
                }

                if (preset.displayName != "Mimi")
                {
                    preset.displayName = "Mimi";
                }

                EditorUtility.SetDirty(preset);
            }
        }

        private static void RemoveBossDuplicateEnemies()
        {
            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include))
            {
                if (view == null)
                {
                    continue;
                }

                var key = view.DemoUnitKey?.ToLowerInvariant() ?? string.Empty;
                var preset = view.ResolvePreset();
                var unitId = preset?.unitId?.ToLowerInvariant() ?? string.Empty;

                if (key.Contains("kiki") || unitId.Contains("kiki"))
                {
                    continue;
                }

                if (view.Side == GridSide.Enemy)
                {
                    Undo.DestroyObjectImmediate(view.gameObject);
                    continue;
                }

                if (IsCharlotteUnitView(view)
                    || key.Contains("tank") || key.Contains("charlotte") || key.Contains("charlott")
                    || unitId.Contains("tank") || unitId.Contains("charlotte"))
                {
                    Undo.DestroyObjectImmediate(view.gameObject);
                }
            }
        }

        private static bool IsCharlotteUnitView(UnitView view)
        {
            if (view == null)
            {
                return false;
            }

            var key = view.DemoUnitKey?.ToLowerInvariant() ?? string.Empty;
            var name = view.gameObject.name.ToLowerInvariant();
            return key.Contains("charl") || name.Contains("charlott") || name.Contains("charlotte");
        }

        private static void RefreshBootstrapUnitViews()
        {
            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap == null)
            {
                return;
            }

            var views = Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include);
            var so = new SerializedObject(bootstrap);
            var prop = so.FindProperty("unitViews");
            prop.arraySize = views.Length;
            for (var i = 0; i < views.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);
        }

        private static void WireKikiView(
            UnitView view,
            UnitPresetSO preset,
            Sprite sprite,
            RuntimeAnimatorController controller)
        {
            var so = new SerializedObject(view);
            so.FindProperty("demoUnitKey").stringValue = "kiki_ueda";
            so.FindProperty("preset").objectReferenceValue = preset;
            so.FindProperty("side").enumValueIndex = (int)GridSide.Enemy;
            so.FindProperty("row").intValue = 1;
            so.FindProperty("column").intValue = 1;
            so.FindProperty("idleStateName").stringValue = "Kiki-Idle";
            so.FindProperty("counterStateName").stringValue = "Kiki-Counter";
            so.FindProperty("beCounteredStateName").stringValue = "Kiki-Hurt";
            so.FindProperty("movingStateName").stringValue = "Kiki-Moving";
            so.FindProperty("deathStateName").stringValue = "Kiki-Death";
            so.ApplyModifiedPropertiesWithoutUndo();
            view.gameObject.name = "Unit_Kiki_Ueda";
            view.gameObject.SetActive(true);

            var sr = view.GetComponent<SpriteRenderer>();
            if (sr != null && sprite != null)
            {
                Undo.RecordObject(sr, "Set Kiki Sprite");
                sr.sprite = sprite;
                EditorUtility.SetDirty(sr);
            }

            var animator = view.GetComponent<Animator>();
            if (animator != null && controller != null)
            {
                Undo.RecordObject(animator, "Set Kiki Animator");
                animator.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(animator);
            }

            var sim = UnitSpriteSimulator.EnsureOn(view);
            sim?.AuthorCurrentAsState(UnitCombatVisualState.Idle);
            SkillVfxSimulator.EnsureOn(view);

            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(view.gameObject);
        }

        private static void WirePartyAnimatorStates(UnitView view)
        {
            PartyCombatVisualSetupEditor.WireParty(view);
        }

        private static void ApplyBackgroundSprite()
        {
            var bgRoot = GameObject.Find(CombatUiHierarchy.BackgroundCanvasName);
            if (bgRoot == null)
            {
                return;
            }

            var image = bgRoot.GetComponentInChildren<Image>(true);
            if (image == null)
            {
                return;
            }

            var sprite = LoadFirstSprite(TutorialBgPath);
            if (sprite == null)
            {
                return;
            }

            Undo.RecordObject(image, "Set Tutorial BG");
            image.sprite = sprite;
            image.color = Color.white;
            EditorUtility.SetDirty(image);
        }

        private static Sprite LoadFirstSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Sprite s)
                {
                    return s;
                }
            }

            return null;
        }

        private static void EnsureInBuildSettings()
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var existing in list)
            {
                if (existing != null && existing.path == ScenePath)
                {
                    return;
                }
            }

            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
