using FracturedChorus.Meta;
using FracturedChorus.RunMap;
using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    public static class CadenceIntroScriptBuilder
    {
        public const string LaunchTutorialSignal = "cadence_launch_tutorial";

        public static VnBeat[] BuildApproach()
        {
            return new[]
            {
                OpeningInvestigationScriptBuilder.Card("OPENING CEREMONY", 2.0f, VnBgIds.Black),
                OpeningInvestigationScriptBuilder.N(
                    "Noon. The hall fills.\nThe principal talks. The city listens.",
                    VnBgIds.HimaCeremonyHall,
                    VnAudioIds.EternalSpark,
                    dateHudDate: "02/09",
                    dateHudPhase: "Noon"),
                OpeningInvestigationScriptBuilder.N(
                    "Then the song in every ear skips — not to silence.\nTo a version that should not exist.",
                    VnBgIds.HimaCeremonyDesync),
                OpeningInvestigationScriptBuilder.L(VnSpeakerIds.Ren, "That's Eternal Spark. It isn't clean.", "startled"),
                OpeningInvestigationScriptBuilder.L(VnSpeakerIds.Charlotte, "You hear the undertone too.", "grim"),
                OpeningInvestigationScriptBuilder.N(
                    "The SyncPod on Ren's ear burns red.\nERROR. DESYNC DETECTED.",
                    VnBgIds.CgDesyncDetect),
                OpeningInvestigationScriptBuilder.N(
                    "The floor drops out of the hall.\nRen falls through a spiral of light, hand reaching for a sound that will not let go.",
                    VnBgIds.CgResonanceDive),
                OpeningInvestigationScriptBuilder.N(
                    "White stone. Broken arches. Banners hanging in a sky that is not a sky.\nThe same wrong note is still in the air.",
                    VnBgIds.CadenceFirstLook),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Ren,
                    "Where is this?\nThe sound here is exactly the error I heard before."),
                OpeningInvestigationScriptBuilder.N(
                    "The arches shudder.\nSomeone steps out of the light, already moving."),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Kiki,
                    "Why isn't this one under control?\nHe hasn't even changed. I'll hit him and find out."),
                OpeningInvestigationScriptBuilder.N(
                    "She lunges.\nA column of light cuts in front of Ren."),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Coda,
                    "Stay behind the light."),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Ren,
                    "Who are you?"),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Coda,
                    "Coda. I'll cover you in this fight."),
                Signal(LaunchTutorialSignal)
            };
        }

        public static VnScriptSO CreateEscape()
        {
            var script = ScriptableObject.CreateInstance<VnScriptSO>();
            script.id = "cadence_escape_en";
            script.nextScene = RunMapSceneCatalog.CampusHub;
            script.beats = new[]
            {
                OpeningInvestigationScriptBuilder.N(
                    "Mimi drops. The arches refuse to settle.\nFarther in, Charlotte is fighting something stitched from broken notes.",
                    VnBgIds.CadenceFirstLook),
                OpeningInvestigationScriptBuilder.N(
                    "Ren sees her. Coda is already running.\nThe two of them cut in and pull the thing off her.",
                    VnBgIds.CadenceFracturePull),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Ren,
                    "Charlotte!",
                    "startled"),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Coda,
                    "Stay with her. I'll hold this."),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Ren,
                    "Get behind us."),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Charlotte,
                    "Ren. You fell in too.",
                    "grim"),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Charlotte,
                    "I can still stand. That thing can't.",
                    "neutral"),
                OpeningInvestigationScriptBuilder.End(
                    StoryFlagIds.RenArrivedHima,
                    StoryFlagIds.HimaEnrollmentDone,
                    StoryFlagIds.OpeningCeremony,
                    StoryFlagIds.FirstResonanceDive,
                    StoryFlagIds.CodaMet,
                    StoryFlagIds.CodaRescue,
                    StoryFlagIds.CadenceBreach,
                    StoryFlagIds.MimiEncountered,
                    StoryFlagIds.CharlotteReunited,
                    StoryFlagIds.VaultQuestActive)
            };
            return script;
        }

        private static VnBeat Signal(string signalId) => new VnBeat
        {
            kind = VnBeatKind.Cue,
            signalId = signalId,
            bgId = VnBgIds.CadenceFirstLook
        };
    }
}
