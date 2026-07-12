#if UNITY_EDITOR
using FracturedChorus.Meta;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class VnRuntimeSmokeSetupEditor
    {
        private const string ScriptsFolder = "Assets/FracturedChorus/Narrative/Scripts";
        private const string SmokePath = ScriptsFolder + "/VnSmoke_Test.asset";

        [MenuItem("Fractured Chorus/Narrative/Create VN Smoke Test Script Asset")]
        public static void CreateSmokeScript()
        {
            EnsureFolder("Assets/FracturedChorus/Narrative");
            EnsureFolder(ScriptsFolder);

            var so = AssetDatabase.LoadAssetAtPath<VnScriptSO>(SmokePath);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<VnScriptSO>();
                AssetDatabase.CreateAsset(so, SmokePath);
            }

            so.id = "vn_smoke_test";
            so.nextScene = RunMapSceneCatalog.CampusHub;
            so.beats = new[]
            {
                new VnBeat
                {
                    kind = VnBeatKind.Narration,
                    text = "Smoke test — narration line."
                },
                new VnBeat
                {
                    kind = VnBeatKind.Line,
                    speakerId = VnSpeakerIds.Ren,
                    text = "Smoke test — Ren line."
                },
                new VnBeat
                {
                    kind = VnBeatKind.Cue,
                    sfxId = "missing_sfx_should_log_and_continue"
                },
                new VnBeat
                {
                    kind = VnBeatKind.End,
                    setFlags = new[] { StoryFlagIds.OpeningInvestigationDone }
                }
            };

            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
            Selection.activeObject = so;
            Debug.Log($"[Fractured Chorus] Created {SmokePath}. Assign to VnRuntimeController for Play Mode smoke.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
