using System;
using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    [Serializable]
    public sealed class VnCueEntry
    {
        public string id;
        public Sprite sprite;
        public AudioClip clip;
    }

    public sealed class VnCueResolver : MonoBehaviour
    {
        [SerializeField] private VnCueEntry[] entries;

        public bool TryGetSprite(string id, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            var entry = Find(id);
            if (entry == null || entry.sprite == null)
            {
                Debug.LogError($"[VnCueResolver] Missing sprite cue id '{id}'.");
                return false;
            }

            sprite = entry.sprite;
            return true;
        }

        public bool TryGetClip(string id, out AudioClip clip)
        {
            clip = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            var entry = Find(id);
            if (entry == null || entry.clip == null)
            {
                Debug.LogError($"[VnCueResolver] Missing audio cue id '{id}'.");
                return false;
            }

            clip = entry.clip;
            return true;
        }

        private VnCueEntry Find(string id)
        {
            if (entries == null)
            {
                return null;
            }

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.id == id)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
