using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Meta;
using FracturedChorus.RunMap;
using FracturedChorus.UI.Loading;
using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    public static class CadenceIntroFlow
    {
        private static bool s_playEscapeOnNextOpening;

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
            if (s_playEscapeOnNextOpening)
            {
                s_playEscapeOnNextOpening = false;
                CombatEncounterHandoff.ClearResultFlags();
                script = CadenceIntroScriptBuilder.CreateEscape();
                return script != null;
            }

            if (!GameMetaSession.HasSession)
            {
                return false;
            }

            var state = GameMetaSession.Current;
            if (!state.HasFlag(StoryFlagIds.CadenceTutorialPending)
                || state.HasFlag(StoryFlagIds.CharlotteReunited)
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

            if (s_playEscapeOnNextOpening)
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

        public static bool EscapeAlreadyPlayed()
        {
            return GameMetaSession.HasSession
                && GameMetaSession.Current.HasFlag(StoryFlagIds.CharlotteReunited);
        }

        public static void ArmEscapeReturn()
        {
            s_playEscapeOnNextOpening = true;
            var state = GameMetaSession.Current;
            if (!state.HasFlag(StoryFlagIds.CadenceTutorialPending))
            {
                state.SetFlag(StoryFlagIds.CadenceTutorialPending);
                GameMetaSession.Save();
            }

            CombatEncounterHandoff.SetReturnScene(RunMapSceneCatalog.OpeningInvestigation);
            CombatEncounterHandoff.SetResult(true);
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
            if (!LoadingScreenController.Ensure().BeginLoadOrChain(RunMapSceneCatalog.CombatTutorial))
            {
                Debug.LogError("[CadenceIntro] Failed to load CombatTutorial.");
            }
        }
    }
}
