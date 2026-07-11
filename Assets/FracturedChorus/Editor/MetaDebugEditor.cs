#if UNITY_EDITOR
using FracturedChorus.Meta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class MetaDebugEditor
    {
        [MenuItem("Fractured Chorus/Meta/Open CampusHub Scene")]
        public static void OpenCampusHubScene()
        {
            const string scenePath = "Assets/FracturedChorus/Scenes/CampusHub.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                CampusHubSceneSetupEditor.CreateCampusHubScene();
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        [MenuItem("Fractured Chorus/Meta/Test Save Round-Trip")]
        public static void TestSaveRoundTrip()
        {
            var original = GameMetaState.CreateHubStart();
            original.CompleteMorningQuiz();
            original.AddStatExp(SocialStatType.Cadence, 20);
            original.GetBond(BondNpcIds.Astra).AddExp(12);
            original.SetFlag(StoryFlagIds.AstraMet, true);
            original.ConsumeActivitySlot();

            var json = GameMetaSaveLoad.Serialize(original);
            var loaded = GameMetaSaveLoad.Deserialize(json);

            Assert(original.Calendar.CurrentDate.Equals(loaded.Calendar.CurrentDate),
                $"Date mismatch: {original.Calendar.CurrentDate.ToDisplayString()} vs {loaded.Calendar.CurrentDate.ToDisplayString()}");
            Assert(original.Calendar.CurrentPhase == loaded.Calendar.CurrentPhase,
                $"Phase mismatch: {original.Calendar.CurrentPhase} vs {loaded.Calendar.CurrentPhase}");
            Assert(original.Calendar.SlotsUsedToday == loaded.Calendar.SlotsUsedToday,
                $"Slots mismatch: {original.Calendar.SlotsUsedToday} vs {loaded.Calendar.SlotsUsedToday}");
            Assert(original.SocialStats.GetRank(SocialStatType.Cadence) == loaded.SocialStats.GetRank(SocialStatType.Cadence),
                "Cadence rank mismatch after round-trip.");
            Assert(loaded.Flags.Has(StoryFlagIds.AstraMet), "Flag astra_met missing after round-trip.");
            Assert(loaded.GetBond(BondNpcIds.Astra).Exp == original.GetBond(BondNpcIds.Astra).Exp,
                "Astra bond exp mismatch.");

            Debug.Log("[Fractured Chorus] Meta save round-trip OK.");
        }

        [MenuItem("Fractured Chorus/Meta/Advance One Day")]
        public static void AdvanceOneDay()
        {
            var state = GameMetaSession.Current;
            state.AdvanceDay();
            GameMetaSession.Save();
            Debug.Log($"[Fractured Chorus] Advanced to {state.Calendar.CurrentDate.ToDisplayString()} · {state.Calendar.CurrentPhase}");
        }

        [MenuItem("Fractured Chorus/Meta/Jump To Date (09/05)")]
        public static void JumpTo0905()
        {
            JumpTo(9, 5);
        }

        [MenuItem("Fractured Chorus/Meta/Jump To Date (09/20)")]
        public static void JumpTo0920()
        {
            JumpTo(9, 20);
        }

        [MenuItem("Fractured Chorus/Meta/Reset Meta Save")]
        public static void ResetMetaSave()
        {
            GameMetaSession.ResetSession();
            Debug.Log("[Fractured Chorus] Meta save deleted.");
        }

        private static void JumpTo(int month, int day)
        {
            var state = GameMetaSession.Current;
            state.Calendar.ResetForNewDay(new GameDate(month, day));
            GameMetaSession.Save();
            Debug.Log($"[Fractured Chorus] Jumped to {state.Calendar.CurrentDate.ToDisplayString()}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new UnityException($"[Fractured Chorus] Meta test failed: {message}");
            }
        }
    }
}
#endif
