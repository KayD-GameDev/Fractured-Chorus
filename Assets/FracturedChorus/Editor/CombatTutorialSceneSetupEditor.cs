#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EnsureInBuildSettings();
            Debug.Log("[Fractured Chorus] CombatTutorial prepared — edit BG under Background canvas, swap enemy UnitViews, Save.");
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
            view.transform.localScale = Vector3.one * 0.2f;

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

            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(view.gameObject);
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
