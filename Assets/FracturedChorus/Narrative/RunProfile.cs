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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Load();
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
        }

        public static void ResetForNewRun()
        {
            HasSignedContract = false;
            PlayerPrefs.DeleteKey(KeyContractSigned);
            PlayerPrefs.Save();
        }
    }
}
