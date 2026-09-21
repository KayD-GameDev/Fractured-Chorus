using FracturedChorus.Meta;
using UnityEngine;

namespace FracturedChorus.Hub
{
    internal static class BondPromoSprites
    {
        private static Sprite _defaultPromo;
        private static Sprite[] _charlotteByEpisode;
        private static Sprite[] _renByEpisode;

        public static Sprite DefaultPromo => LoadPath(ref _defaultPromo, BondPromoCatalog.DefaultPromoPath);

        public static Sprite Episode(string npcId, int episodeRowIndex)
        {
            var cache = CacheFor(npcId);
            if (cache == null || episodeRowIndex < 0 || episodeRowIndex >= cache.Length)
            {
                return null;
            }

            return cache[episodeRowIndex];
        }

        public static Sprite CharlotteEpisode(int episodeRowIndex) =>
            Episode(BondNpcIds.Charlotte, episodeRowIndex);

        private static Sprite[] CacheFor(string npcId)
        {
            if (npcId == BondNpcIds.Ren)
            {
                EnsureCache(ref _renByEpisode, BondPromoCatalog.RenEpisodePromoPaths);
                return _renByEpisode;
            }

            if (npcId == BondNpcIds.Charlotte)
            {
                EnsureCache(ref _charlotteByEpisode, BondPromoCatalog.CharlotteEpisodePromoPaths);
                return _charlotteByEpisode;
            }

            return null;
        }

        private static void EnsureCache(ref Sprite[] cache, string[] paths)
        {
            if (cache != null)
            {
                return;
            }

            cache = new Sprite[paths.Length];
            for (var i = 0; i < paths.Length; i++)
            {
                var slot = cache[i];
                cache[i] = LoadPath(ref slot, paths[i]);
            }
        }

        private static Sprite LoadPath(ref Sprite cache, string assetPath)
        {
            if (cache != null)
            {
                return cache;
            }

#if UNITY_EDITOR
            cache = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif
            return cache;
        }
    }
}
