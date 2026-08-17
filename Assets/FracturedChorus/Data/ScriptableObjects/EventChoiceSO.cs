using UnityEngine;

namespace FracturedChorus.Data
{
    public enum EventChoiceKind
    {
        NextBattleDamage = 0,
        HealOverflowShield = 1,
        NextBattleDefense = 2,
        FirstNoteReduceS2 = 3,
        PrepBonus = 4,
        StartShieldPercent = 5,
        NextBattleCrit = 6,
        Notes = 7
    }

    [CreateAssetMenu(fileName = "EventChoice", menuName = "Fractured Chorus/Event Choice")]
    public sealed class EventChoiceSO : ScriptableObject
    {
        public const string OverdriveSoloId = "overdrive_solo";
        public const string RhythmRestorationId = "rhythm_restoration";
        public const string FrequencyWallId = "frequency_wall";
        public const string FastTempoId = "fast_tempo";
        public const string EncoreSparkId = "encore_spark";
        public const string SubwooferShieldId = "subwoofer_shield";
        public const string NeonCritId = "neon_crit";
        public const string NoiseIsFreedomId = "noise_is_freedom";

        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private string description;
        [SerializeField] private EventChoiceKind kind;
        [SerializeField] private float magnitude;
        [SerializeField] private Sprite icon;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public EventChoiceKind Kind => kind;
        public float Magnitude => magnitude;
        public Sprite Icon => icon;

        public string KindLabel => kind switch
        {
            EventChoiceKind.NextBattleDamage => "DMG +%",
            EventChoiceKind.HealOverflowShield => "HEAL",
            EventChoiceKind.NextBattleDefense => "DEF",
            EventChoiceKind.FirstNoteReduceS2 => "S2",
            EventChoiceKind.PrepBonus => "PREP",
            EventChoiceKind.StartShieldPercent => "SHIELD",
            EventChoiceKind.NextBattleCrit => "CRIT",
            EventChoiceKind.Notes => "NOTES",
            _ => kind.ToString()
        };

        public static EventChoiceSO CreateRuntime(
            string choiceId,
            string choiceTitle,
            string choiceDescription,
            EventChoiceKind choiceKind,
            float choiceMagnitude)
        {
            var asset = CreateInstance<EventChoiceSO>();
            asset.EditorAssign(choiceId, choiceTitle, choiceDescription, choiceKind, choiceMagnitude);
            return asset;
        }

        public static EventChoiceSO[] CreateDefaultCatalog()
        {
            return new[]
            {
                CreateRuntime(
                    OverdriveSoloId,
                    "Overdrive Solo",
                    "Trận tiếp theo: damage party +5%.",
                    EventChoiceKind.NextBattleDamage,
                    0.05f),
                CreateRuntime(
                    RhythmRestorationId,
                    "Rhythm Restoration",
                    "Lúc vào trận: hồi 30% Max HP. Phần dư thành shield.",
                    EventChoiceKind.HealOverflowShield,
                    0.30f),
                CreateRuntime(
                    FrequencyWallId,
                    "Frequency Wall",
                    "Trận tiếp theo: nhận damage −10%.",
                    EventChoiceKind.NextBattleDefense,
                    0.10f),
                CreateRuntime(
                    FastTempoId,
                    "Fast Tempo",
                    "Nốt đầu tiên đặt lên board: −1 S2.",
                    EventChoiceKind.FirstNoteReduceS2,
                    1f),
                CreateRuntime(
                    EncoreSparkId,
                    "Encore Spark",
                    "Lúc vào trận: +1 Prep toàn party.",
                    EventChoiceKind.PrepBonus,
                    1f),
                CreateRuntime(
                    SubwooferShieldId,
                    "Subwoofer Shield",
                    "Lúc vào trận: shield = 20% Max HP.",
                    EventChoiceKind.StartShieldPercent,
                    0.20f),
                CreateRuntime(
                    NeonCritId,
                    "Neon Crit",
                    "Trận tiếp theo: +8% crit.",
                    EventChoiceKind.NextBattleCrit,
                    8f),
                CreateRuntime(
                    NoiseIsFreedomId,
                    "Noise Is Freedom",
                    "+40 Notes ngay.",
                    EventChoiceKind.Notes,
                    40f)
            };
        }

        public void EditorAssign(
            string choiceId,
            string choiceTitle,
            string choiceDescription,
            EventChoiceKind choiceKind,
            float choiceMagnitude,
            Sprite choiceIcon = null)
        {
            id = choiceId;
            title = choiceTitle;
            description = choiceDescription;
            kind = choiceKind;
            magnitude = choiceMagnitude;
            icon = choiceIcon;
        }
    }
}
