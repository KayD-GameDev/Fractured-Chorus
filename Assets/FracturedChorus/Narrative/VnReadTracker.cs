using UnityEngine;

namespace FracturedChorus.Narrative
{
    public static class VnReadTracker
    {
        public static bool IsRead(string scope, int index)
        {
            return IsRead(BuildBeatKey(scope, index));
        }

        public static void MarkRead(string scope, int index)
        {
            MarkRead(BuildBeatKey(scope, index));
        }

        public static bool IsRead(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return PlayerPrefs.GetInt(BuildKey(key), 0) == 1;
        }

        public static void MarkRead(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            PlayerPrefs.SetInt(BuildKey(key), 1);
            PlayerPrefs.Save();
        }

        private static string BuildBeatKey(string scope, int index)
        {
            return $"{scope}_{index}";
        }

        private static string BuildKey(string key)
        {
            return $"fc_vn_read_{key}";
        }
    }
}
