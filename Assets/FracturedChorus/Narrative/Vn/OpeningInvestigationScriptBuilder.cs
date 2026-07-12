using FracturedChorus.Meta;
using FracturedChorus.RunMap;

namespace FracturedChorus.Narrative.Vn
{
    public static class OpeningInvestigationScriptBuilder
    {
        public const string ScriptAssetPath =
            "Assets/FracturedChorus/Narrative/Scripts/OpeningInvestigation_EN.asset";

        private const float EarPainPitch = 0.38f;

        public static void ApplyTo(VnScriptSO script)
        {
            if (script == null)
            {
                return;
            }

            script.id = "opening_investigation_en";
            script.nextScene = RunMapSceneCatalog.CampusHub;
            script.beats = BuildBeats();
        }

        public static VnBeat[] BuildBeats()
        {
            return new[]
            {
                N("Late night in Lumina.\nA city that never sleeps.", VnBgIds.LuminaStreetNight),
                N("On the giant screens, Prime Unit MVs loop again and again — no one remembers how many times.\nPeople pass by humming along, almost unconscious."),
                L(VnSpeakerIds.Haruto, "…Another late shift. I’m wiped.", "neutral"),
                N("SyncPod on his right ear. Blue LED. Slow pulse.\nPersonal playlist — track three: Bring Me Home.",
                    bgmId: VnAudioIds.BringMeHome),
                L(VnSpeakerIds.Haruto, "Bring Me Home… Yeah. That fits.", "neutral"),
                N("A line of the hit bleeds in through his left ear.\nHaruto shakes his head — his SyncPod stays on his own track."),
                L(VnSpeakerIds.Haruto, "Not now.", "neutral"),

                L(VnSpeakerIds.Haruto, "—!", "startled", bgmId: VnAudioIds.EternalSpark),
                N("The LED on his ear flickers. Once. Twice."),
                L(VnSpeakerIds.Haruto, "What the…? Why did it change tracks on its own?", "startled"),
                L(VnSpeakerIds.Haruto, "It hurts. What is this?", "pain", bgmPitch: EarPainPitch),
                N("Not the song he was listening to.\nNot the song on the billboard.\nSomething else — like a thousand needles driven into the heart."),
                L(VnSpeakerIds.Haruto, "Stop…! I have to shut it off!", "fear"),
                N("The SyncPod doesn’t answer. His feet keep moving.\nNot toward home."),
                L(VnSpeakerIds.Haruto, "No… don’t…", "fear"),
                N("Vision blurs. His body feels heavy — pulled forward on an invisible wire.\nHe slips through the crowd. No one notices."),

                N("Lotus Service Lane — no billboards here.\nOnly emergency lights and the damp breath of shop ACs.", VnBgIds.LuminaAlleyNight),
                L(VnSpeakerIds.Haruto, "…Someone… help me…", "desperate"),
                N("The SyncPod burns red. The waveform spins — not music.\nLike suction."),
                L(VnSpeakerIds.Haruto, "ARRGHH…!", "agony"),
                N("The neon on the wall glitches for one beat — then settles.\nHaruto drops to his knees. One hand clawing at the SyncPod. It won’t come off."),
                L(VnSpeakerIds.Haruto, "Help… —", "desperate"),
                N("The whine cuts dead.\nSilence.", bgmId: VnAudioIds.StopBgm),

                Card("Four hours later", 1.6f),
                Fade(0.8f),

                N("Yellow tape across the mouth of the lane.\nA patrol car. Two people.", VnBgIds.LuminaAlleyNight),
                L(VnSpeakerIds.Ryo, "Inspector Lin… how many is this now?", "nervous"),
                L(VnSpeakerIds.MeiLin, "Don’t count. Counting just makes it worse.", "weary"),
                N("Male victim. Mid-to-late twenties.\nFace-down. One hand reaching toward his ear.",
                    VnBgIds.LuminaAlleyHarutoBody),
                L(VnSpeakerIds.Ryo, "His skin…", "startled"),
                L(VnSpeakerIds.MeiLin, "Like a husk. Like something drank him dry from the inside.", "stern"),
                L(VnSpeakerIds.Ryo, "Overdose? Or the same as the others?", "uneasy"),
                L(VnSpeakerIds.MeiLin, "…", "concerned"),
                N("SyncPod SP-01. Red LED — a crack down the glass."),
                L(VnSpeakerIds.Ryo, "What was he listening to before he died?", "concerned",
                    bgId: VnBgIds.LuminaAlleyNight),
                L(VnSpeakerIds.MeiLin, "Chorus Board logged the device online at 23:51.\nPersonal playlist: indie.\nBut the last entry…", "stern"),
                N("Mei Lin opens her tablet. The screen stays angled away from the player."),
                L(VnSpeakerIds.MeiLin, "…a track that isn’t in the public catalog.\nID: SW-ES-040", "warning"),
                L(VnSpeakerIds.Ryo, "SW is…", "nervous"),
                L(VnSpeakerIds.MeiLin, "StellaWorks. Guesswork only. Don’t put it in the report.", "warning"),
                L(VnSpeakerIds.Ryo, "Why not, Inspector?", "uneasy"),
                L(VnSpeakerIds.MeiLin, "Because a report with that name vanishes before it can ever be filed.\nSame as the three before this.", "weary"),
                L(VnSpeakerIds.Ryo, "Damn it… so what do we do?", "nervous"),
                L(VnSpeakerIds.MeiLin, "….", "concerned"),
                N("From the mouth of the lane, the Eternal Spark hook still carries — thin, far, innocent."),
                L(VnSpeakerIds.Ryo, "Inspector…", "concerned"),
                L(VnSpeakerIds.MeiLin, "Do your job.\nAnd hope this time it doesn’t happen to you.", "stern"),
                L(VnSpeakerIds.MeiLin, "…If you ever hear something strange in your ears…\nSave yourself first.", "warning"),

                Fade(1.0f),

                N("At the same hour — across the city.\nThe station. The crowd. Neon still singing.", VnBgIds.LuminaStreetNight),
                N("A SyncPod on Ren’s ear. Blue LED.\nOne pulse — then the track cuts out."),
                N("Mandatory broadcast window.\nCurrent Chorus Board #1: Eternal Spark — LUXE.",
                    bgmId: VnAudioIds.EternalSpark),
                L(VnSpeakerIds.Ren, "…This one again."),
                N("He doesn’t switch it off. Doesn’t flinch.\nJust tightens his backpack strap and steps into the flow."),
                L(VnSpeakerIds.Ren, "If it’s number one, you listen. That’s how this city works."),
                N("Around him, mouths hum the hook without looking at the billboards.\nRen hears it with them — clear, clean, on the beat."),
                N("No pain. No tug on his feet.\nJust a hit song… and a newcomer to Lumina."),
                N("Bag on his shoulder. Early-enrollment papers for HIMA.\nLight rain still falling."),

                Card("September 1 — Ren Takahashi arrives in Lumina.", 2.0f),

                End(
                    StoryFlagIds.LuminaCaseOpen,
                    StoryFlagIds.OpeningInvestigationDone,
                    StoryFlagIds.RenArrivedHima)
            };
        }

        private static VnBeat N(
            string text,
            string bgId = null,
            string bgmId = null,
            float bgmPitch = 0f) => new VnBeat
        {
            kind = VnBeatKind.Narration,
            text = text,
            bgId = bgId,
            bgmId = bgmId,
            bgmPitch = bgmPitch
        };

        private static VnBeat L(
            string speakerId,
            string text,
            string expression = null,
            string bgId = null,
            string bgmId = null,
            float bgmPitch = 0f) => new VnBeat
        {
            kind = VnBeatKind.Line,
            speakerId = speakerId,
            text = text,
            expression = expression,
            bgId = bgId,
            bgmId = bgmId,
            bgmPitch = bgmPitch
        };

        private static VnBeat Card(string text, float duration) => new VnBeat
        {
            kind = VnBeatKind.TextCard,
            text = text,
            duration = duration
        };

        private static VnBeat Fade(float duration) => new VnBeat
        {
            kind = VnBeatKind.Fade,
            duration = duration
        };

        private static VnBeat End(params string[] flags) => new VnBeat
        {
            kind = VnBeatKind.End,
            setFlags = flags
        };
    }
}
