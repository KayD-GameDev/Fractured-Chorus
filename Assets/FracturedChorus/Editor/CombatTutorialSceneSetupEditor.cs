#if UNITY_EDITOR
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
        private const string KikiIconPath = "Assets/FracturedChorus/Art/UI/Combat/Characters/kiki_ueda_character_icon_bars_elite_v1.png";
        private const string KikiControllerPath = "Assets/FracturedChorus/Art/Characters/KikiUeda/Unit_Kiki_Ueda.controller";
        private const string KikiPresetPath = "Assets/FracturedChorus/Resources/UnitPresets/UnitPreset_Kiki_Ueda.asset";

        [MenuItem("Fractured Chorus/Open Combat Tutorial Scene")]
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

        [MenuItem("Fractured Chorus/Prepare Combat Tutorial Scene (BG + party)")]
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
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EnsureInBuildSettings();
            Debug.Log(
                "[Fractured Chorus] CombatTutorial prepared — slideshow khung (ảnh step add sau). " +
                "BG + Ren/Coda/Kiki đã bật lại.");
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
                var isParty = view.Side == GridSide.Player
                              || key.Contains("ren")
                              || key.Contains("coda")
                              || key.Contains("mage")
                              || name.Contains("ren")
                              || name.Contains("mage");
                var hide = name.Contains("boss") || name.Contains("tank") || name.Contains("grunt");
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
                    || go.name == "TutorialHighlightOverlay")
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

        private static void ApplyEnemyCardIcon()
        {
            var icon = LoadFirstSprite(KikiIconPath);
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

            var cardArt = template.transform.Find("CardArt")?.GetComponent<Image>();
            if (cardArt == null)
            {
                return;
            }

            Undo.RecordObject(cardArt, "Set Kiki enemy card icon");
            cardArt.sprite = icon;
            cardArt.color = Color.white;
            EditorUtility.SetDirty(cardArt);

            var preset = AssetDatabase.LoadAssetAtPath<UnitPresetSO>(KikiPresetPath);
            if (preset != null && preset.combatCardSprite != icon)
            {
                Undo.RecordObject(preset, "Set Kiki combat card sprite");
                preset.combatCardSprite = icon;
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

                if (key.Contains("tank") || key.Contains("charlotte") || key.Contains("charlott")
                    || unitId.Contains("tank") || unitId.Contains("charlotte"))
                {
                    Undo.DestroyObjectImmediate(view.gameObject);
                }
            }
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

            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(view.gameObject);
        }

        private static void WirePartyAnimatorStates(UnitView view)
        {
            var key = view.DemoUnitKey?.ToLowerInvariant() ?? string.Empty;
            var so = new SerializedObject(view);

            if (key.Contains("ren"))
            {
                so.FindProperty("idleStateName").stringValue = "Ren Idle";
                so.FindProperty("counterStateName").stringValue = "Ren Counter";
                so.FindProperty("beCounteredStateName").stringValue = "Ren Hurt";
                so.FindProperty("movingStateName").stringValue = "Ren Moving";
            }
            else if (key.Contains("mage") || key.Contains("coda"))
            {
                so.FindProperty("idleStateName").stringValue = "Coda - Idle";
                so.FindProperty("counterStateName").stringValue = "Coda - Counter";
                so.FindProperty("beCounteredStateName").stringValue = "Coda - Hurt";
                so.FindProperty("movingStateName").stringValue = "Coda - Moving";
            }
            else
            {
                return;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
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
