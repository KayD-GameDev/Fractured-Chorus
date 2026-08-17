using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Data
{
    public static class SeededOfferPicker
    {
        public static T[] Pick<T>(IReadOnlyList<T> pool, int seed, int count) where T : class
        {
            if (pool == null || pool.Count == 0)
            {
                return System.Array.Empty<T>();
            }

            var valid = new List<T>(pool.Count);
            for (var i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null)
                {
                    valid.Add(pool[i]);
                }
            }

            if (valid.Count == 0)
            {
                return System.Array.Empty<T>();
            }

            var take = Mathf.Clamp(count, 1, valid.Count);
            var rng = new System.Random(seed);
            for (var i = valid.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (valid[i], valid[j]) = (valid[j], valid[i]);
            }

            var offers = new T[take];
            for (var i = 0; i < take; i++)
            {
                offers[i] = valid[i];
            }

            return offers;
        }
    }
}
