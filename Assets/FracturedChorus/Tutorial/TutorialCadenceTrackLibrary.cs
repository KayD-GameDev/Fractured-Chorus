using System.Collections.Generic;
using FracturedChorus.Meta;
using UnityEngine;

namespace FracturedChorus.Tutorial
{
    public static class TutorialCadenceTrackLibrary
    {
        private const string ResourcesTrackPath = "Tutorial/TutorialTrack_CadenceIntro";
        private const string CodaChibiResource = "Characters/Coda/coda_cadence_chibi_bust_v1";
        private const string CodaBustResource = "VN/Portraits/coda_cadence_bust_neutral_v1";

        public readonly struct StepSeed
        {
            public readonly string stepId;
            public readonly string body;
            public readonly TutorialStepKind kind;
            public readonly bool useCodaPortrait;
            public readonly string qteHint;
            public readonly int planningSegmentToContinue;

            public StepSeed(
                string stepId,
                string body,
                TutorialStepKind kind,
                bool useCodaPortrait = false,
                string qteHint = null,
                int planningSegmentToContinue = 1)
            {
                this.stepId = stepId;
                this.body = body;
                this.kind = kind;
                this.useCodaPortrait = useCodaPortrait;
                this.qteHint = qteHint;
                this.planningSegmentToContinue = planningSegmentToContinue;
            }
        }

        public static readonly StepSeed[] CadenceSeeds =
        {
            new("meet_danger",
                "It's dangerous here. I'm Coda. I'll walk you out. Listen to each step, and don't rush.",
                TutorialStepKind.Slide, true),
            new("formation_grid",
                "First, Formation. The party sits in six cells: BACK, MID, and FRONT.",
                TutorialStepKind.Slide, true),
            new("formation_buff_intro",
                "Each spot gives a different buff, depending on the fight.",
                TutorialStepKind.Slide, true),
            new("formation_front", "FRONT cuts the damage you take.", TutorialStepKind.Slide, true),
            new("formation_mid_back",
                "MID raises damage. BACK raises buffs and dodge.", TutorialStepKind.Slide, true),
            new("formation_situational",
                "Look at the party and the fight, then place people where they fit.", TutorialStepKind.Slide, true),
            new("formation_practice", "Move Ren into the FRONT cell on the same row.",
                TutorialStepKind.PracticeFormation, true),
            new("formation_good", "Nice.", TutorialStepKind.Slide, true),
            new("boss_note_total",
                "The number on a note is the note total. You have to counter the boss hit.", TutorialStepKind.Slide, true),
            new("boss_tap_intro", "Tap the character.", TutorialStepKind.Slide, true),
            new("boss_tap_unit", string.Empty, TutorialStepKind.AwaitUnitSkillPanel),
            new("boss_tap_good", "Nice.", TutorialStepKind.Slide, true),
            new("boss_drag_intro",
                "This is the character's skill. Drag it onto the timeline to block the boss note.",
                TutorialStepKind.Slide, true),
            new("boss_drag_skill", string.Empty, TutorialStepKind.AwaitSkillPlaced),
            new("boss_counter_explain",
                "Look at the first note. The number just dropped. That is a counter: your skill ate one hit, so the boss has less left on that beat. A note nobody covers still comes down. You will feel that one.",
                TutorialStepKind.Slide, true),
            new("boss_drag_good", "Nice.", TutorialStepKind.Slide, true),
            new("skill_big_note",
                "Each skill has two parts. The big note is when they strike. Put it under the boss note to counter and deal damage.",
                TutorialStepKind.Slide, true),
            new("skill_small_note",
                "The small note doesn't attack. It's the wait before the strike starts and ends.", TutorialStepKind.Slide, true),
            new("skill_small_overlap",
                "Small notes can't overlap. Leave room when you place them.", TutorialStepKind.Slide, true),
            new("coda_place_prompt", "Now drop my skill in too.", TutorialStepKind.Slide, true),
            new("coda_place_intro", "Drag Coda's skill onto the timeline.", TutorialStepKind.Slide, true),
            new("coda_place_skill", string.Empty, TutorialStepKind.AwaitSkillPlaced),
            new("coda_place_good", "Nice.", TutorialStepKind.Slide, true),
            new("coda_fill_timeline",
                "One skill from Ren, one from me, on two notes. Then you can press Execute. Leave the third note. That one still hits.",
                TutorialStepKind.Slide, true),
            new("coda_planning_sandbox", string.Empty, TutorialStepKind.BeginPlanningSandbox),
            new("coda_execute_run", string.Empty, TutorialStepKind.AwaitDeployUntilPlanning, false, null, 1),
            new("phase2_understood",
                "You've got the idea.", TutorialStepKind.Slide, true),
            new("phase2_qte_intro", "Next is the Quick Time Event.", TutorialStepKind.Slide, true),
            new("phase2_place_skills",
                "Place skills in the cells under the timeline.", TutorialStepKind.Slide, true),
            new("phase2_skill_intro", "Drag a skill onto the timeline.", TutorialStepKind.Slide, true),
            new("phase2_skill_practice", string.Empty, TutorialStepKind.AwaitSkillPlaced),
            new("phase2_skill_good", "Nice.", TutorialStepKind.Slide, true),
            new("phase2_execute_confirm", "Press Execute.", TutorialStepKind.Slide, true),
            new("phase2_execute_qte", string.Empty, TutorialStepKind.AwaitDeployThenQte, false,
                "This is a QTE. Hit Space on time and you deal extra damage."),
            new("phase2_qte_praise", "Nice work.", TutorialStepKind.Slide, true),
            new("cadence_repeat_notes",
                "Do the same for the notes left. I'll back you up.", TutorialStepKind.Slide, true)
        };

        public static TutorialTrackSO ResolveCadenceIntroTrack(TutorialTrackSO sceneTrack)
        {
            if (IsValidTrack(sceneTrack))
            {
                return sceneTrack;
            }

            var resources = Resources.Load<TutorialTrackSO>(ResourcesTrackPath);
            if (IsValidTrack(resources))
            {
                return resources;
            }

            return BuildRuntimeTrack();
        }

        public static TutorialStepSO[] BuildRuntimeSteps()
        {
            var coda = LoadCodaPortrait();
            var steps = new TutorialStepSO[CadenceSeeds.Length];
            for (var i = 0; i < CadenceSeeds.Length; i++)
            {
                steps[i] = CreateRuntimeStep(CadenceSeeds[i], coda);
            }

            return steps;
        }

        public static TutorialTrackSO BuildRuntimeTrack()
        {
            var track = ScriptableObject.CreateInstance<TutorialTrackSO>();
            track.trackId = TutorialDirector.TrackCadenceIntro;
            track.completionFlag = StoryFlagIds.TutorialCadenceIntroDone;
            track.slideshow = true;
            track.steps = BuildRuntimeSteps();
            return track;
        }

        public static TutorialStepSO CreateRuntimeStep(StepSeed seed, Sprite codaPortrait)
        {
            var step = ScriptableObject.CreateInstance<TutorialStepSO>();
            step.stepId = seed.stepId;
            step.trackId = TutorialDirector.TrackCadenceIntro;
            step.bodyCopy = seed.body;
            step.kind = seed.kind;
            step.requiresConfirm = seed.kind is TutorialStepKind.Slide or TutorialStepKind.PracticeFormation;
            step.coachPortrait = seed.useCodaPortrait ? codaPortrait ?? LoadCodaPortrait() : null;
            step.panelImage = LoadPanelImage(seed.stepId);
            step.qteHintCopy = seed.qteHint ?? string.Empty;
            step.planningSegmentToContinue = seed.planningSegmentToContinue;
            return step;
        }

        public static Sprite LoadCodaPortrait()
        {
            return Resources.Load<Sprite>(CodaChibiResource)
                   ?? Resources.Load<Sprite>(CodaBustResource)
                   ?? Resources.Load<Sprite>("Characters/Coda/coda_chibi_fullbody_v1");
        }

        public static Sprite LoadPanelImage(string stepId)
        {
            if (string.IsNullOrEmpty(stepId))
            {
                return null;
            }

            return Resources.Load<Sprite>($"UI/Tutorial/Steps/{stepId}_v1");
        }

        public static UnityEngine.Video.VideoClip LoadPanelClip(string stepId)
        {
            if (string.IsNullOrEmpty(stepId))
            {
                return null;
            }

            return Resources.Load<UnityEngine.Video.VideoClip>($"UI/Tutorial/Clips/{stepId}");
        }

        private static bool IsValidTrack(TutorialTrackSO track) =>
            track != null && track.steps != null && track.steps.Length > 0;
    }
}
