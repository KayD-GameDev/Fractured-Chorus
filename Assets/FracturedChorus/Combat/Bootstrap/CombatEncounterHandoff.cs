using FracturedChorus.RunMap;
using FracturedChorus.RunMap.Core;

namespace FracturedChorus.Combat.Bootstrap
{
    public static class CombatEncounterHandoff
    {
        public static string EncounterId { get; private set; }
        public static string LastFoughtEncounterId { get; private set; }
        public static string ReturnSceneName { get; private set; } = RunMapSceneCatalog.RunMapPrototype;
        public static int SourceNodeId { get; private set; } = -1;
        public static int SourceFloor { get; private set; }

        /// <summary>
        /// Node tầng 1–3 (đoạn thẳng dưới bản đồ). Party vào trận ở Lv1, skill và ult khóa.
        /// </summary>
        public static bool IsLowerFloorStart =>
            SourceNodeId >= 0
            && SourceFloor >= 1
            && SourceFloor <= MapLayoutConstants.ExclusivePrefixFloors;
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
            CombatPoolRoll poolRoll = null,
            int sourceFloor = 0)
        {
            EncounterId = encounterId;
            LastFoughtEncounterId = encounterId;
            ReturnSceneName = string.IsNullOrWhiteSpace(returnScene)
                ? RunMapSceneCatalog.RunMapPrototype
                : returnScene;
            SourceNodeId = sourceNodeId;
            SourceFloor = sourceFloor;
            PendingPoolRoll = poolRoll;
            HasResult = false;
            PendingReturnToNearestCamp = false;
            PendingRewardSummary = null;
        }

        public static void SetReturnScene(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                ReturnSceneName = sceneName;
            }
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
            SourceFloor = 0;
            HasResult = false;
            PendingReturnToNearestCamp = false;
            LastVictory = false;
            PendingRewardSummary = null;
            PendingPoolRoll = null;
        }

    }
}
