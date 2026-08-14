using FracturedChorus.RunMap;

namespace FracturedChorus.Combat.Bootstrap
{
    public static class CombatEncounterHandoff
    {
        public static string EncounterId { get; private set; }
        public static string LastFoughtEncounterId { get; private set; }
        public static string ReturnSceneName { get; private set; } = RunMapSceneCatalog.RunMapPrototype;
        public static int SourceNodeId { get; private set; } = -1;
        public static bool LastVictory { get; private set; }
        public static bool HasResult { get; private set; }
        public static bool PendingReturnToNearestCamp { get; private set; }
        public static bool HasPendingEncounter => !string.IsNullOrEmpty(EncounterId);
        public static string PendingRewardSummary { get; private set; }
        public static CombatPoolRoll PendingPoolRoll { get; private set; }

        public static void SetPending(
            string encounterId,
            string returnScene = null,
            int sourceNodeId = -1,
            CombatPoolRoll poolRoll = null)
        {
            EncounterId = encounterId;
            LastFoughtEncounterId = encounterId;
            ReturnSceneName = string.IsNullOrWhiteSpace(returnScene)
                ? RunMapSceneCatalog.RunMapPrototype
                : returnScene;
            SourceNodeId = sourceNodeId;
            PendingPoolRoll = poolRoll;
            HasResult = false;
            PendingReturnToNearestCamp = false;
            PendingRewardSummary = null;
        }

        public static void SetResult(bool victory)
        {
            LastVictory = victory;
            HasResult = true;
            PendingReturnToNearestCamp = !victory;
            if (!victory)
            {
                PendingRewardSummary = null;
                return;
            }

            if (string.IsNullOrEmpty(PendingRewardSummary))
            {
                PendingRewardSummary = CombatRewardService.GrantVictoryNotes(LastFoughtEncounterId);
            }
        }

        public static void ConsumePendingEncounter()
        {
            EncounterId = null;
            PendingPoolRoll = null;
        }

        public static void ClearResultFlags()
        {
            HasResult = false;
            PendingReturnToNearestCamp = false;
            PendingRewardSummary = null;
        }

        public static void ClearAll()
        {
            EncounterId = null;
            LastFoughtEncounterId = null;
            ReturnSceneName = RunMapSceneCatalog.RunMapPrototype;
            SourceNodeId = -1;
            HasResult = false;
            PendingReturnToNearestCamp = false;
            LastVictory = false;
            PendingRewardSummary = null;
            PendingPoolRoll = null;
        }

    }
}
