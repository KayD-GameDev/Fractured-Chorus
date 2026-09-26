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
                "Hiện tại ở đây rất nguy hiểm. Tôi là Coda — tôi sẽ hướng dẫn cậu thoát khỏi đây. Nghe kỹ từng bước, đừng nóng vội.",
                TutorialStepKind.Slide, true),
            new("formation_grid",
                "Đầu tiên hãy đến với phần Formation — đội hình chia thành 6 ô tương ứng với BACK, MID và FRONT.",
                TutorialStepKind.Slide, true),
            new("formation_buff_intro",
                "Mỗi vị trí sẽ có buff khác nhau dựa vào tình huống nhất định.",
                TutorialStepKind.Slide, true),
            new("formation_front", "FRONT giảm sát thương nhận vào.", TutorialStepKind.Slide, true),
            new("formation_mid_back",
                "MID tăng sát thương. BACK tăng khả năng buff và né.", TutorialStepKind.Slide, true),
            new("formation_situational",
                "Hãy dựa vào đội hình hiện tại và tình huống để đặt sao cho hợp lý.", TutorialStepKind.Slide, true),
            new("formation_practice", "Hãy di chuyển Coda sang vị trí của Ren.",
                TutorialStepKind.PracticeFormation, true),
            new("formation_good", "Tốt lắm.", TutorialStepKind.Slide, true),
            new("boss_note_total",
                "Số trên nốt là tổng số nốt — bạn sẽ phải counter đòn boss.", TutorialStepKind.Slide, true),
            new("boss_tap_intro", "Hãy bấm vào nhân vật.", TutorialStepKind.Slide, true),
            new("boss_tap_unit", string.Empty, TutorialStepKind.AwaitUnitSkillPanel),
            new("boss_tap_good", "Tốt lắm.", TutorialStepKind.Slide, true),
            new("boss_drag_intro",
                "Đây là skill của nhân vật. Hãy kéo xuống timeline để chặn nốt boss.",
                TutorialStepKind.Slide, true),
            new("boss_drag_skill", string.Empty, TutorialStepKind.AwaitSkillPlaced),
            new("boss_drag_good", "Tốt lắm.", TutorialStepKind.Slide, true),
            new("skill_big_note",
                "Mỗi kĩ năng có hai phần: nốt to là thời điểm nhân vật tung đòn. Đặt dưới nốt boss để counter và gây sát thương.",
                TutorialStepKind.Slide, true),
            new("skill_small_note",
                "Nốt nhỏ không tấn công — đó là thời gian chờ để bạn tung và kết thúc đòn.", TutorialStepKind.Slide, true),
            new("skill_small_overlap",
                "Các nốt nhỏ không thể chồng lên nhau, hãy tính toán đặt hợp lý.", TutorialStepKind.Slide, true),
            new("coda_place_prompt", "Bây giờ hãy đặt skill của tôi vào nữa nhé.", TutorialStepKind.Slide, true),
            new("coda_place_intro", "Kéo skill Coda xuống timeline.", TutorialStepKind.Slide, true),
            new("coda_place_skill", string.Empty, TutorialStepKind.AwaitSkillPlaced),
            new("coda_place_good", "Tốt lắm.", TutorialStepKind.Slide, true),
            new("coda_fill_timeline",
                "Bây giờ hãy đặt hết skill lên timeline beat nhé.", TutorialStepKind.Slide, true),
            new("coda_planning_sandbox", string.Empty, TutorialStepKind.BeginPlanningSandbox),
            new("coda_execute_run", string.Empty, TutorialStepKind.AwaitDeployUntilPlanning, false, null, 1),
            new("phase2_understood",
                "Tuyệt lắm, bạn đã hiểu cơ chế rồi đấy.", TutorialStepKind.Slide, true),
            new("phase2_qte_intro", "Bây giờ tới Quick Time Event.", TutorialStepKind.Slide, true),
            new("phase2_place_skills",
                "Hãy đặt skill vào các ô bên dưới timeline.", TutorialStepKind.Slide, true),
            new("phase2_skill_intro", "Kéo skill xuống timeline.", TutorialStepKind.Slide, true),
            new("phase2_skill_practice", string.Empty, TutorialStepKind.AwaitSkillPlaced),
            new("phase2_skill_good", "Tốt lắm.", TutorialStepKind.Slide, true),
            new("phase2_execute_confirm", "Bấm Execute.", TutorialStepKind.Slide, true),
            new("phase2_execute_qte", string.Empty, TutorialStepKind.AwaitDeployThenQte, false,
                "Đây là QTE — khi bạn bấm Space đúng lúc sẽ tăng thêm sát thương."),
            new("phase2_qte_praise", "Tuyệt lắm, bạn làm tốt lắm!", TutorialStepKind.Slide, true),
            new("cadence_repeat_notes",
                "Hãy làm tương tự với các nốt còn lại — tôi sẽ hỗ trợ.", TutorialStepKind.Slide, true)
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

        private static bool IsValidTrack(TutorialTrackSO track) =>
            track != null && track.steps != null && track.steps.Length > 0;
    }
}
