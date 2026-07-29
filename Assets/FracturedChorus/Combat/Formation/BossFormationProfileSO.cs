using FracturedChorus.Combat.Bootstrap;
using UnityEngine;

namespace FracturedChorus.Combat.Formation
{
    public enum FormationDisruptKind
    {
        None = 0,
        ForceSwapAdjacent = 1,
        PinColumn = 2
    }

    [CreateAssetMenu(fileName = "BossFormationProfile", menuName = "Fractured Chorus/Boss Formation Profile")]
    public sealed class BossFormationProfileSO : ScriptableObject
    {
        public float frontTargetWeight = 1f;
        [Range(0f, 1f)] public float backPierceChance;
        public int columnSlamColumn = -1;
        public FormationDisruptKind formationDisrupt = FormationDisruptKind.None;
        public string pressureSummary = string.Empty;

        public static BossFormationProfileSO GetDefaultForEncounter(string encounterId)
        {
            if (string.IsNullOrWhiteSpace(encounterId))
            {
                return CreateRuntime(Neutral());
            }

            if (EncounterCatalog.IsTutorial(encounterId))
            {
                return CreateRuntime(Tutorial());
            }

            if (encounterId == EncounterCatalog.BossDespair
                || encounterId.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0
                || encounterId.IndexOf("Pulse", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CreateRuntime(BossDespair());
            }

            if (encounterId == EncounterCatalog.EliteGrunts
                || encounterId.IndexOf("Elite", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CreateRuntime(Elite());
            }

            return CreateRuntime(Neutral());
        }

        private static BossFormationProfileSO CreateRuntime(BossFormationProfileSO template)
        {
            var instance = CreateInstance<BossFormationProfileSO>();
            instance.frontTargetWeight = template.frontTargetWeight;
            instance.backPierceChance = template.backPierceChance;
            instance.columnSlamColumn = template.columnSlamColumn;
            instance.formationDisrupt = template.formationDisrupt;
            instance.pressureSummary = template.pressureSummary;
            return instance;
        }

        private static BossFormationProfileSO Neutral()
        {
            var profile = CreateInstance<BossFormationProfileSO>();
            profile.frontTargetWeight = 1f;
            profile.backPierceChance = 0.08f;
            profile.columnSlamColumn = -1;
            profile.formationDisrupt = FormationDisruptKind.None;
            profile.pressureSummary = string.Empty;
            return profile;
        }

        private static BossFormationProfileSO Tutorial()
        {
            var profile = CreateInstance<BossFormationProfileSO>();
            profile.frontTargetWeight = 1f;
            profile.backPierceChance = 0.08f;
            profile.columnSlamColumn = -1;
            profile.formationDisrupt = FormationDisruptKind.None;
            profile.pressureSummary = "Kiki holds the center — park Ren mid, let Coda cover from back.";
            return profile;
        }

        private static BossFormationProfileSO Elite()
        {
            var profile = CreateInstance<BossFormationProfileSO>();
            profile.frontTargetWeight = 1.35f;
            profile.backPierceChance = 0.12f;
            profile.columnSlamColumn = -1;
            profile.formationDisrupt = FormationDisruptKind.None;
            profile.pressureSummary = "Elite squad favors the front row — spread before Execute.";
            return profile;
        }

        private static BossFormationProfileSO BossDespair()
        {
            var profile = CreateInstance<BossFormationProfileSO>();
            profile.frontTargetWeight = 2.4f;
            profile.backPierceChance = 0.28f;
            profile.columnSlamColumn = 1;
            profile.formationDisrupt = FormationDisruptKind.ForceSwapAdjacent;
            profile.pressureSummary = "The Pulse anchors the front — mid-column slams punish clustered lines.";
            return profile;
        }
    }
}
