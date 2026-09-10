using FracturedChorus.Meta;
using UnityEngine;

namespace FracturedChorus.Narrative
{
    public static class RunProfile
    {
        private const string KeyPlayerName = "fc_player_name";
        private const string KeyContractSigned = "fc_contract_signed";

        public const string DefaultNameSuggestion = "Ren Takahashi";

        public static string PlayerName { get; private set; } = DefaultNameSuggestion;
        public static bool HasSignedContract { get; private set; }

        /// <summary>
        /// ESC / status menu chỉ mở sau khi đã ký contract trong Prologue.
        /// Save cũ không có flag này vẫn được tính là đã qua nếu đã tới Campus.
        /// </summary>
        public static bool CanOpenPauseMenu
        {
            get
            {
                if (HasSignedContract)
                {
                    return true;
                }

                if (!GameMetaSession.HasSession)
                {
                    return false;
                }

                var state = GameMetaSession.Current;
                return state.HasFlag(StoryFlagIds.ContractSigned)
                       || state.HasFlag(StoryFlagIds.OpeningInvestigationDone)
                       || state.HasFlag(StoryFlagIds.RenArrivedHima);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Load();
            GameMetaSession.SessionStateChanged -= OnSessionStateChanged;
            GameMetaSession.SessionStateChanged += OnSessionStateChanged;
        }

        private static void OnSessionStateChanged(GameMetaState state)
        {
            if (state == null)
            {
                return;
            }

            if (HasSignedContract)
            {
                state.SetFlag(StoryFlagIds.ContractSigned);
            }
        }

        public static void Load()
        {
            PlayerName = PlayerPrefs.GetString(KeyPlayerName, DefaultNameSuggestion);
            HasSignedContract = PlayerPrefs.GetInt(KeyContractSigned, 0) == 1;
        }

        public static void SetPlayerName(string value)
        {
            var trimmed = string.IsNullOrWhiteSpace(value) ? DefaultNameSuggestion : value.Trim();
            if (trimmed.Length > 24)
            {
                trimmed = trimmed.Substring(0, 24);
            }

            PlayerName = trimmed;
            PlayerPrefs.SetString(KeyPlayerName, PlayerName);
            PlayerPrefs.Save();
        }

        public static void MarkContractSigned()
        {
            HasSignedContract = true;
            PlayerPrefs.SetInt(KeyContractSigned, 1);
            PlayerPrefs.Save();

            if (GameMetaSession.HasSession)
            {
                GameMetaSession.Current.SetFlag(StoryFlagIds.ContractSigned);
            }
        }

        public static void ResetForNewRun()
        {
            HasSignedContract = false;
            PlayerPrefs.DeleteKey(KeyContractSigned);
            PlayerPrefs.Save();
        }
    }
}
