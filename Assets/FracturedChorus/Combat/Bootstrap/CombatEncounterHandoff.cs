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

        public static void SetPending(string encounterId, string returnScene = null, int sourceNodeId = -1)
        {
            EncounterId = encounterId;
            LastFoughtEncounterId = encounterId;
            ReturnSceneName = string.IsNullOrWhiteSpace(returnScene)
                ? RunMapSceneCatalog.RunMapPrototype
                : returnScene;
            SourceNodeId = sourceNodeId;
            HasResult = false;
            PendingReturnToNearestCamp = false;
            PendingRewardSummary = null;
        }

        public static void SetResult(bool victory)
        {
            LastVictory = victory;
            HasResult = true;
            PendingReturnToNearestCamp = !victory;
            PendingRewardSummary = victory
                ? BuildRewardStub(LastFoughtEncounterId)
                : null;
        }

        public static void ConsumePendingEncounter()
        {
            EncounterId = null;
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
        }

        private static string BuildRewardStub(string encounterId)
        {
            if (string.IsNullOrEmpty(encounterId))
            {
                return "REWARD (stub): Run progress updated · Loot TBD";
            }

            if (encounterId == EncounterCatalog.BossDespair ||
                encounterId.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "REWARD (stub): Boss cleared · Sector progress · Loot TBD";
            }

            if (encounterId == EncounterCatalog.EliteGrunts ||
                encounterId.IndexOf("Elite", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "REWARD (stub): Elite cleared · Bonus loot TBD";
            }

            return "REWARD (stub): Battle cleared · Loot TBD";
        }
    }
}
