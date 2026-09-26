#if UNITY_EDITOR
using FracturedChorus.Meta;
using FracturedChorus.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class TutorialCadenceTrackSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/CombatTutorial.unity";
        private const string TrackPath = "Assets/FracturedChorus/Data/Tutorial/TutorialTrack_CadenceIntro.asset";
        private const string StepsFolder = "Assets/FracturedChorus/Data/Tutorial/CadenceIntro/Steps";
        private const string CodaPortraitPath =
            "Assets/FracturedChorus/Art/Characters/Coda/Chibi/coda_cadence_chibi_bust_v1.png";
        private const string StepImageFolder = "Assets/FracturedChorus/Art/UI/Tutorial/Steps";

        [MenuItem("Fractured Chorus/Tutorial/Sync Cadence Track Assets + Scene Director")]
        public static void SyncCadenceTrackAndScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Tutorial", "Thoát Play Mode trước.", "OK");
                return;
            }

            EnsureCadenceStepAssets();
            var track = EnsureCadenceTrackAsset();
            EnsureTutorialDirectorInOpenScene(track);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] Cadence tutorial track + scene director synced. Chỉnh step/track asset hoặc TutorialCoach trong scene.");
        }

        public static TutorialTrackSO EnsureCadenceTrackAsset()
        {
            EnsureCadenceStepAssets();
            var steps = LoadOrderedSteps();
            var track = AssetDatabase.LoadAssetAtPath<TutorialTrackSO>(TrackPath);
            if (track == null)
            {
                track = ScriptableObject.CreateInstance<TutorialTrackSO>();
                AssetDatabase.CreateAsset(track, TrackPath);
            }

            if (string.IsNullOrEmpty(track.trackId))
            {
                track.trackId = TutorialDirector.TrackCadenceIntro;
            }

            if (string.IsNullOrEmpty(track.completionFlag))
            {
                track.completionFlag = StoryFlagIds.TutorialCadenceIntroDone;
            }

            if (track.steps == null || track.steps.Length == 0)
            {
                track.steps = steps;
            }

            EditorUtility.SetDirty(track);
            return track;
        }

        public static void EnsureTutorialDirectorInOpenScene(TutorialTrackSO track)
        {
            var coach = Object.FindAnyObjectByType<TutorialCoachView>(FindObjectsInactive.Include);
            var director = Object.FindAnyObjectByType<TutorialDirector>(FindObjectsInactive.Include);
            if (director == null)
            {
                var combatRoot = GameObject.Find("CombatRoot");
                var go = new GameObject("TutorialDirector", typeof(TutorialDirector));
                if (combatRoot != null)
                {
                    go.transform.SetParent(combatRoot.transform, false);
                }

                director = go.GetComponent<TutorialDirector>();
                Undo.RegisterCreatedObjectUndo(go, "Create TutorialDirector");
            }

            var so = new SerializedObject(director);
            so.FindProperty("sceneBound").boolValue = true;
            so.FindProperty("coachView").objectReferenceValue = coach;
            so.FindProperty("cadenceIntroTrack").objectReferenceValue = track;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director);

            if (coach != null)
            {
                var coachSo = new SerializedObject(coach);
                var preserve = coachSo.FindProperty("preserveSceneLayout");
                if (preserve != null)
                {
                    preserve.boolValue = true;
                    coachSo.ApplyModifiedPropertiesWithoutUndo();
                }

                var coda = AssetDatabase.LoadAssetAtPath<Sprite>(CodaPortraitPath);
                var coachPortraitProp = coachSo.FindProperty("defaultCoachPortrait");
                if (coachPortraitProp != null && coachPortraitProp.objectReferenceValue == null && coda != null)
                {
                    coachPortraitProp.objectReferenceValue = coda;
                    coachSo.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorUtility.SetDirty(coach);
            }
        }

        public static void EnsureTutorialDirectorInCombatTutorialScene()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var track = EnsureCadenceTrackAsset();
            EnsureTutorialDirectorInOpenScene(track);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void EnsureCadenceStepAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Data/Tutorial"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Data", "Tutorial");
            }

            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Data/Tutorial/CadenceIntro"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Data/Tutorial", "CadenceIntro");
            }

            if (!AssetDatabase.IsValidFolder(StepsFolder))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Data/Tutorial/CadenceIntro", "Steps");
            }

            var coda = AssetDatabase.LoadAssetAtPath<Sprite>(CodaPortraitPath)
                       ?? TutorialCadenceTrackLibrary.LoadCodaPortrait();
            var stepDefs = BuildStepDefs();
            foreach (var def in stepDefs)
            {
                WriteStepAsset(def, coda);
            }
        }

        private static TutorialStepSO[] LoadOrderedSteps()
        {
            var stepDefs = BuildStepDefs();
            var list = new TutorialStepSO[stepDefs.Length];
            for (var i = 0; i < stepDefs.Length; i++)
            {
                list[i] = AssetDatabase.LoadAssetAtPath<TutorialStepSO>($"{StepsFolder}/{stepDefs[i].fileName}.asset");
            }

            return list;
        }

        private static void WriteStepAsset(StepDef def, Sprite codaPortrait)
        {
            var path = $"{StepsFolder}/{def.fileName}.asset";
            var step = AssetDatabase.LoadAssetAtPath<TutorialStepSO>(path);
            var isNew = step == null;
            if (isNew)
            {
                step = ScriptableObject.CreateInstance<TutorialStepSO>();
                AssetDatabase.CreateAsset(step, path);
            }

            step.stepId = def.stepId;
            step.trackId = TutorialDirector.TrackCadenceIntro;
            if (isNew || string.IsNullOrWhiteSpace(step.bodyCopy))
            {
                step.bodyCopy = def.body;
            }

            if (isNew)
            {
                step.kind = def.kind;
                step.requiresConfirm = def.kind is TutorialStepKind.Slide or TutorialStepKind.PracticeFormation;
                step.coachPortrait = def.useCodaPortrait ? codaPortrait : null;
                step.panelImage = LoadPanelImage(def.stepId);
                step.qteHintCopy = def.qteHint ?? string.Empty;
            }

            EditorUtility.SetDirty(step);
        }

        private static Sprite LoadPanelImage(string stepId)
        {
            var path = $"{StepImageFolder}/{stepId}_v1.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private readonly struct StepDef
        {
            public readonly string fileName;
            public readonly string stepId;
            public readonly string body;
            public readonly TutorialStepKind kind;
            public readonly bool useCodaPortrait;
            public readonly string qteHint;

            public StepDef(
                string fileName,
                string stepId,
                string body,
                TutorialStepKind kind,
                bool useCodaPortrait = false,
                string qteHint = null)
            {
                this.fileName = fileName;
                this.stepId = stepId;
                this.body = body;
                this.kind = kind;
                this.useCodaPortrait = useCodaPortrait;
                this.qteHint = qteHint;
            }
        }

        private static StepDef[] BuildStepDefs()
        {
            var seeds = TutorialCadenceTrackLibrary.CadenceSeeds;
            var defs = new StepDef[seeds.Length];
            for (var i = 0; i < seeds.Length; i++)
            {
                var seed = seeds[i];
                defs[i] = new StepDef(
                    $"{i + 1:00}_{seed.stepId}",
                    seed.stepId,
                    seed.body,
                    seed.kind,
                    seed.useCodaPortrait,
                    seed.qteHint);
            }

            return defs;
        }
    }
}
#endif
