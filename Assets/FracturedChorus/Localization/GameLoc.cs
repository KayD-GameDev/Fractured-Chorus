using System;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Localization
{
    public static class GameLoc
    {
        public const string PrefKey = "fc_language";

        public static event Action Changed;

        public static GameLanguage Language { get; private set; } = GameLanguage.English;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Language = (GameLanguage)PlayerPrefs.GetInt(PrefKey, (int)GameLanguage.English);
        }

        public static void SetLanguage(GameLanguage language)
        {
            if (Language == language)
            {
                return;
            }

            Language = language;
            PlayerPrefs.SetInt(PrefKey, (int)language);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static string Get(string key)
        {
            return GameLocPhrases.Get(key, Language);
        }

        public static string Tr(string english)
        {
            if (string.IsNullOrEmpty(english) || Language != GameLanguage.Vietnamese)
            {
                return english ?? string.Empty;
            }

            return GameLocPhrases.TryPhrase(english, out var vietnamese) ? vietnamese : english;
        }

        public static bool HasPhrase(string english)
        {
            return !string.IsNullOrEmpty(english) && GameLocPhrases.TryPhrase(english, out _);
        }

        public static string Pick(string english, string vietnamese)
        {
            if (Language == GameLanguage.Vietnamese && !string.IsNullOrEmpty(vietnamese))
            {
                return vietnamese;
            }

            return english ?? string.Empty;
        }

        public static void Apply(Text text, string english)
        {
            if (text == null)
            {
                return;
            }

            var binding = text.GetComponent<GameLocBinding>() ?? text.gameObject.AddComponent<GameLocBinding>();
            binding.source = english;
            binding.skip = false;
            ApplyBinding(text, binding);
        }

        public static void Skip(Text text)
        {
            if (text == null)
            {
                return;
            }

            var binding = text.GetComponent<GameLocBinding>() ?? text.gameObject.AddComponent<GameLocBinding>();
            binding.skip = true;
        }

        internal static void ApplyBinding(Text text, GameLocBinding binding)
        {
            if (text == null || binding == null || binding.skip || string.IsNullOrEmpty(binding.source))
            {
                return;
            }

            if (!HasPhrase(binding.source))
            {
                return;
            }

            text.text = Tr(binding.source);
        }
    }
}
