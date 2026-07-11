using FracturedChorus.Meta;

namespace FracturedChorus.Hub
{
    public static class CampusHubStoryBeats
    {
        public static bool TryGetMorningMessage(GameMetaState state, out string message)
        {
            message = null;
            var date = state.Calendar.CurrentDate;

            if (date.Month == 9 && date.Day == 1)
            {
                message = "Ren đến HIMA. Ký túc xá, cánh cổng campus — tuần nhập học sớm bắt đầu.";
                return true;
            }

            if (date.Month == 9 && date.Day == 2)
            {
                message = "Nhập học sớm: Astra đón Ren và dẫn đi tham quan HIMA.";
                return true;
            }

            if (state.Calendar.MorningQuizDone)
            {
                return false;
            }

            if (date.Month == 9 && date.Day >= 3)
            {
                message = "Buổi sáng — lớp học. (Quiz sẽ có ở Phase 3)";
                return true;
            }

            message = "Buổi sáng mới tại HIMA.";
            return true;
        }

        public static void ApplyMorningFlags(GameMetaState state)
        {
            var date = state.Calendar.CurrentDate;

            if (date.Month == 9 && date.Day == 2)
            {
                state.SetFlag(StoryFlagIds.AstraMet);
                state.SetFlag(StoryFlagIds.HimaTourDone);
            }
        }

        public static bool IsDungeonActivityAvailable(GameMetaState state, HubActivityOption option)
        {
            if (option.Id != "dungeon_run")
            {
                return true;
            }

            return state.Flags.Has(StoryFlagIds.VaultQuestActive);
        }
    }
}
