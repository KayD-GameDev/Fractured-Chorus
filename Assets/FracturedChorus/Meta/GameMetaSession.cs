namespace FracturedChorus.Meta
{
    public static class GameMetaSession
    {
        private static GameMetaState s_state;

        public static bool HasSession => s_state != null;

        public static GameMetaState Current
        {
            get
            {
                if (s_state == null)
                {
                    s_state = GameMetaSaveLoad.LoadOrNew();
                }

                return s_state;
            }
        }

        public static void BeginNewGame()
        {
            s_state = GameMetaState.CreateNew();
            GameMetaSaveLoad.TrySave(s_state);
        }

        public static void BeginHubAfterPrologue()
        {
            s_state = GameMetaState.CreateHubStart();
            GameMetaSaveLoad.TrySave(s_state);
        }

        public static void Load()
        {
            s_state = GameMetaSaveLoad.LoadOrNew();
        }

        public static void Save()
        {
            if (s_state == null)
            {
                return;
            }

            GameMetaSaveLoad.TrySave(s_state);
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
