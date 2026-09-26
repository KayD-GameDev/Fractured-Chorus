using FracturedChorus.RunMap;
using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    public static class HimaEnrollmentScriptBuilder
    {
        private const string EnrollmentDate = "02/09";
        private const string EnrollmentMorning = "Morning";

        public static VnScriptSO CreateRuntimeInstance()
        {
            var script = ScriptableObject.CreateInstance<VnScriptSO>();
            script.id = "hima_enrollment_en";
            script.nextScene = RunMapSceneCatalog.CampusHub;
            script.beats = BuildBeats();
            return script;
        }

        public static VnBeat[] BuildMeeting()
        {
            return new[]
            {
                OpeningInvestigationScriptBuilder.N(
                    "HIMA Music Academy.\nThe banners already know his name as a category: newcomer.",
                    VnBgIds.HimaCampusDay,
                    sfxId: VnAudioIds.StopSfx,
                    dateHudDate: EnrollmentDate,
                    dateHudPhase: EnrollmentMorning),
                OpeningInvestigationScriptBuilder.N(
                    "Glass corridor. Class 3-2.\nHe turns the corner too fast.",
                    VnBgIds.HimaHallwayDay),
                OpeningInvestigationScriptBuilder.N("Impact.", VnBgIds.CgHallwayBump, sfxId: VnAudioIds.Bump),
                OpeningInvestigationScriptBuilder.L(VnSpeakerIds.Ren, "—!", "startled"),
                OpeningInvestigationScriptBuilder.L(VnSpeakerIds.Charlotte, "Hey— watch it!", "startled"),
                OpeningInvestigationScriptBuilder.N(
                    "Two SyncPods hit the stone. Blue LEDs still pulsing.",
                    VnBgIds.CgSyncpodsFloor),
                OpeningInvestigationScriptBuilder.N(
                    "Charlotte picks his up first.\nThe display is still playing.",
                    VnBgIds.CgIndiePlayerBreath),
                OpeningInvestigationScriptBuilder.L(
                    VnSpeakerIds.Charlotte,
                    "Breath of the World…?\nThat isn't on the indie boards.",
                    "curious"),
                OpeningInvestigationScriptBuilder.L(VnSpeakerIds.Ren, "It's mine.", "neutral"),
                OpeningInvestigationScriptBuilder.L(VnSpeakerIds.Charlotte, "…Yours.", "curious"),
                OpeningInvestigationScriptBuilder.N(
                    "They don't have time to finish it.\nHomeroom is already filling.",
                    VnBgIds.HimaClassroomDay),
                OpeningInvestigationScriptBuilder.L(VnSpeakerIds.Ren, "You're in this class too.", "curious"),
                OpeningInvestigationScriptBuilder.L(VnSpeakerIds.Charlotte, "Don't make it weird.", "neutral")
            };
        }

        public static VnBeat[] BuildBeats()
        {
            var meet = BuildMeeting();
            var approach = CadenceIntroScriptBuilder.BuildApproach();
            var beats = new VnBeat[meet.Length + approach.Length];
            meet.CopyTo(beats, 0);
            approach.CopyTo(beats, meet.Length);
            return beats;
        }
    }
}
