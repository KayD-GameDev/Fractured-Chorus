using FracturedChorus.Menu;

namespace FracturedChorus.Meta
{
    public static class GameMetaSession
    {
        private static GameMetaState s_state;
        private static int s_activeSlotIndex;

        public static bool HasSession => s_state != null;

        public static int ActiveSlotIndex => s_activeSlotIndex;

        public static GameMetaState Current
        {
            get
            {
                if (s_state == null)
                {
                    GameMetaSaveLoad.MigrateLegacySaveOnce();
                    s_activeSlotIndex = GameMetaSaveLoad.ActiveSlot;
                    s_state = GameMetaSaveLoad.LoadOrNew();
                }

                return s_state;
            }
        }

        public static void BeginNewGame()
        {
            BeginNewGame(s_activeSlotIndex);
        }

        public static void BeginNewGame(int slot)
        {
            s_activeSlotIndex = slot;
            GameMetaSaveLoad.ActiveSlot = slot;
            s_state = GameMetaState.CreateNew();
            s_state.Difficulty = (int)MainMenuGameSettings.Difficulty;
            GameMetaSaveLoad.TrySave(s_state, slot);
        }

        public static void BeginHubAfterPrologue()
        {
            BeginHubAfterOpening();
        }

        public static void BeginHubAfterOpening()
        {
            s_state = GameMetaState.CreateHubStart();
            s_state.SetFlag(StoryFlagIds.OpeningInvestigationDone);
            s_state.Difficulty = (int)MainMenuGameSettings.Difficulty;
            GameMetaSaveLoad.TrySave(s_state, s_activeSlotIndex);
        }

        public static void Load()
        {
            LoadSlot(s_activeSlotIndex);
        }

        public static void LoadSlot(int slot)
        {
            GameMetaSaveLoad.MigrateLegacySaveOnce();
            s_activeSlotIndex = slot;
            GameMetaSaveLoad.ActiveSlot = slot;
            s_state = GameMetaSaveLoad.TryLoad(slot) ?? GameMetaState.CreateNew();
        }

        public static void Save()
        {
            if (s_state == null)
            {
                return;
            }

            GameMetaSaveLoad.TrySave(s_state, s_activeSlotIndex);
        }

        public static void SaveToSlot(int slot)
        {
            if (s_state == null)
            {
                return;
            }

            s_activeSlotIndex = slot;
            GameMetaSaveLoad.TrySave(s_state, slot);
        }

        public static void ResetSession()
        {
            s_state = null;
            GameMetaSaveLoad.DeleteSave();
        }

        public static void Replace(GameMetaState state)
        {
            s_state = state;
        }
    }
}
