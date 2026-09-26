using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Meta;
using FracturedChorus.RunMap;
using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    public static class CadenceIntroFlow
    {
        public static bool TryIntercept(VnBeat beat)
        {
            if (beat == null || beat.signalId != CadenceIntroScriptBuilder.LaunchTutorialSignal)
            {
                return false;
            }

            LaunchTutorial();
            return true;
        }

        public static bool TryTakeEscapeScript(out VnScriptSO script)
        {
            script = null;
            if (!GameMetaSession.HasSession)
            {
                return false;
            }

            var state = GameMetaSession.Current;
            if (!state.HasFlag(StoryFlagIds.CadenceTutorialPending)
                || state.HasFlag(StoryFlagIds.HimaEnrollmentDone)
                || !CombatEncounterHandoff.HasResult
                || !CombatEncounterHandoff.LastVictory)
            {
                return false;
            }

            CombatEncounterHandoff.ClearResultFlags();
            script = CadenceIntroScriptBuilder.CreateEscape();
            return script != null;
        }

        public static bool ShouldResumeTutorial()
        {
            if (!GameMetaSession.HasSession)
            {
                return false;
            }

            var state = GameMetaSession.Current;
            if (!state.HasFlag(StoryFlagIds.CadenceTutorialPending)
                || state.HasFlag(StoryFlagIds.HimaEnrollmentDone))
            {
                return false;
            }

            return !CombatEncounterHandoff.HasResult || !CombatEncounterHandoff.LastVictory;
        }

        public static void LaunchTutorial()
        {
            if (GameMetaSession.HasSession)
            {
                GameMetaSession.Current.SetFlag(StoryFlagIds.CadenceTutorialPending);
                GameMetaSession.Save();
            }

            CombatEncounterHandoff.ClearResultFlags();
            CombatEncounterHandoff.SetPending(
                EncounterCatalog.Tutorial,
                RunMapSceneCatalog.OpeningInvestigation);
            if (!RunMapSceneLoader.LoadByName(RunMapSceneCatalog.CombatTutorial))
            {
                Debug.LogError("[CadenceIntro] Failed to load CombatTutorial.");
            }
        }
    }
}
