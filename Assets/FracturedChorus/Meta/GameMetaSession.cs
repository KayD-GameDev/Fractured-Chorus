using System;

namespace FracturedChorus.Meta
{
    public static class GameMetaSession
    {
        /// <summary>Độ khó mặc định khi chưa có ai đăng ký provider (tương đương GameDifficulty.Cadence).</summary>
        public const int FallbackDifficulty = 1;

        private static GameMetaState s_state;
        private static int s_activeSlotIndex;

        /// <summary>
        /// Tầng UI đăng ký hàm này lúc bootstrap để Meta lấy được độ khó người chơi chọn
        /// mà không phải tham chiếu ngược lên namespace Menu.
        /// </summary>
        public static Func<int> DefaultDifficultyProvider { get; set; }

        /// <summary>
        /// Bắn mỗi khi state đang hoạt động bị thay bằng state khác (load slot, new game, replace).
        /// Các store runtime như PartyRunHpStore nghe sự kiện này để nạp lại từ save
        /// mà Meta không phải biết gì về tầng Combat.
        /// </summary>
        public static event Action<GameMetaState> SessionStateChanged;

        /// <summary>
        /// Bắn ngay trước khi ghi file. Run map / combat đăng ký để đổ node đã đi và HP đang đánh
        /// vào state, chứ Meta không được tham chiếu ngược lên các assembly đó.
        /// </summary>
        public static event Action Saving;

        private static bool s_flushing;

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
                    SetState(GameMetaSaveLoad.LoadOrNew());
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
            SetState(GameMetaState.CreateNew());
            s_state.Difficulty = ResolveDefaultDifficulty();
            GameMetaSaveLoad.TrySave(s_state, slot);
        }

        public static void BeginHubAfterPrologue()
        {
            BeginHubAfterOpening();
        }

        public static void BeginHubAfterOpening()
        {
            SetState(GameMetaState.CreateHubStart());
            s_state.SetFlag(StoryFlagIds.OpeningInvestigationDone);
            s_state.Difficulty = ResolveDefaultDifficulty();
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
            SetState(GameMetaSaveLoad.TryLoad(slot) ?? GameMetaState.CreateNew());
        }

        public static void Save()
        {
            SaveToSlot(s_activeSlotIndex);
        }

        public static void SaveToSlot(int slot)
        {
            if (s_state == null)
            {
                return;
            }

            FlushLiveState();
            s_activeSlotIndex = slot;
            GameMetaSaveLoad.TrySave(s_state, slot);
        }

        /// <summary>Đổ dữ liệu đang sống trên scene vào state trước khi ghi đĩa.</summary>
        private static void FlushLiveState()
        {
            if (s_flushing)
            {
                return;
            }

            s_flushing = true;
            try
            {
                Saving?.Invoke();
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError($"[Fractured Chorus] Saving listener lỗi: {error}");
            }
            finally
            {
                s_flushing = false;
            }
        }

        public static void ResetSession()
        {
            SetState(null);
            GameMetaSaveLoad.DeleteSave();
        }

        public static void Replace(GameMetaState state)
        {
            SetState(state);
        }

        private static void SetState(GameMetaState state)
        {
            s_state = state;
            if (state == null)
            {
                return;
            }

            try
            {
                SessionStateChanged?.Invoke(state);
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError($"[Fractured Chorus] SessionStateChanged listener lỗi: {error}");
            }
        }

        private static int ResolveDefaultDifficulty()
        {
            var provider = DefaultDifficultyProvider;
            if (provider == null)
            {
                return FallbackDifficulty;
            }

            try
            {
                return provider();
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError(
                    $"[Fractured Chorus] DefaultDifficultyProvider lỗi, dùng mặc định: {error}");
                return FallbackDifficulty;
            }
        }
    }
}
