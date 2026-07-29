using System;
using UnityEngine;

namespace FracturedChorus.Menu
{
    public enum GameDifficulty
    {
        OnBeat = 0,
        Cadence = 1,
        OffBeat = 2
    }

    public static class MainMenuGameSettings
    {
        private const string KeyVolume = "fc_master_volume";
        private const string KeyBrightness = "fc_bg_brightness";
        private const string KeySkipUnreadText = "fc_skip_unread_text";
        private const string KeyDifficulty = "fc_difficulty";

        public static event Action SettingsChanged;

        public static float MasterVolume { get; private set; } = 0.85f;
        public static float BackgroundBrightness { get; private set; } = 1f;
        public static bool SkipUnreadText { get; private set; }
        public static GameDifficulty Difficulty { get; private set; } = GameDifficulty.Cadence;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Load();
        }

        public static void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(KeyVolume, 0.85f);
            BackgroundBrightness = PlayerPrefs.GetFloat(KeyBrightness, 1f);
            SkipUnreadText = PlayerPrefs.GetInt(KeySkipUnreadText, 0) == 1;
            Difficulty = (GameDifficulty)PlayerPrefs.GetInt(KeyDifficulty, (int)GameDifficulty.Cadence);
        }

        public static void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyVolume, MasterVolume);
            PlayerPrefs.Save();
            SettingsChanged?.Invoke();
        }

        public static void SetBackgroundBrightness(float value)
        {
            BackgroundBrightness = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyBrightness, BackgroundBrightness);
            PlayerPrefs.Save();
            SettingsChanged?.Invoke();
        }

        public static void SetSkipUnreadText(bool enabled)
        {
            SkipUnreadText = enabled;
            PlayerPrefs.SetInt(KeySkipUnreadText, SkipUnreadText ? 1 : 0);
            PlayerPrefs.Save();
            SettingsChanged?.Invoke();
        }

        public static void SetDifficulty(GameDifficulty value)
        {
            Difficulty = value;
            PlayerPrefs.SetInt(KeyDifficulty, (int)Difficulty);
            PlayerPrefs.Save();
            SettingsChanged?.Invoke();
        }

        public static void CycleDifficulty(int direction)
        {
            var count = Enum.GetValues(typeof(GameDifficulty)).Length;
            var next = ((int)Difficulty + direction + count) % count;
            SetDifficulty((GameDifficulty)next);
        }

        public static string GetDifficultyLabel(GameDifficulty value)
        {
            switch (value)
            {
                case GameDifficulty.OnBeat:
                    return "ON BEAT";
                case GameDifficulty.Cadence:
                    return "CADENCE";
                case GameDifficulty.OffBeat:
                    return "OFF-BEAT";
                default:
                    return value.ToString().ToUpperInvariant();
            }
        }

        public static string GetDifficultyDescription(GameDifficulty value)
        {
            switch (value)
            {
                case GameDifficulty.OnBeat:
                    return "Enemy HP/dmg ×0.85 · +1 planning beat · Notes ×1.1.";
                case GameDifficulty.Cadence:
                    return "Standard balance · intended pressure.";
                case GameDifficulty.OffBeat:
                    return "Enemy HP ×1.15 · dmg ×1.2 · stricter Early/Late blocks.";
                default:
                    return string.Empty;
            }
        }
    }
}
