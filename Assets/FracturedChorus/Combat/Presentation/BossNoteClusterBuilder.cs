using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Timeline;

namespace FracturedChorus.Combat.Presentation
{
    public enum BossNoteGlyphKind
    {
        Single,
        Beamed
    }

    public readonly struct BossNoteHead
    {
        public int BeatIndex { get; }
        public EnemyTelegraph Telegraph { get; }
        public int RemainingHits { get; }
        public BossNoteTier DisplayTier { get; }
        public int VariantIndex { get; }

        public BossNoteHead(
            int beatIndex,
            EnemyTelegraph telegraph,
            int remainingHits,
            BossNoteTier displayTier,
            int variantIndex)
        {
            BeatIndex = beatIndex;
            Telegraph = telegraph;
            RemainingHits = remainingHits;
            DisplayTier = displayTier;
            VariantIndex = variantIndex;
        }

        public bool IsCleared => RemainingHits <= 0;
    }

    public readonly struct BossNoteCluster
    {
        public BossNoteGlyphKind Kind { get; }
        public BossNoteHead Left { get; }
        public BossNoteHead Right { get; }

        public BossNoteCluster(BossNoteGlyphKind kind, BossNoteHead left, BossNoteHead right = default)
        {
            Kind = kind;
            Left = left;
            Right = right;
        }
    }

    /// <summary>Scene-authored boss note fallback when no telegraph exists at that beat.</summary>
    public readonly struct AuthoredBossNoteSpec
    {
        public int BeatIndex { get; }
        public int RemainingHits { get; }
        public BossNoteTier DisplayTier { get; }

        public AuthoredBossNoteSpec(int beatIndex, int remainingHits, BossNoteTier displayTier)
        {
            BeatIndex = beatIndex;
            RemainingHits = remainingHits;
            DisplayTier = displayTier;
        }
    }

    public static class BossNoteClusterBuilder
    {
        public const int SingleVariantCount = 5;

        public static List<BossNoteCluster> Build(
            BeatTimelineEngine timeline,
            IReadOnlyList<AuthoredBossNoteSpec> authoredFallback = null)
        {
            var result = new List<BossNoteCluster>();
            if (timeline == null)
            {
                AppendAuthoredSingles(result, authoredFallback, covered: null);
                return result;
            }

            var beats = CollectImpactBeats(timeline);
            var covered = new HashSet<int>();
            var i = 0;
            while (i < beats.Count)
            {
                var beatA = beats[i];
                var teleA = GetPrimaryImpact(timeline, beatA);
                if (teleA == null)
                {
                    i++;
                    continue;
                }

                var remA = CombatCounterResolver.GetRemainingHits(teleA, timeline);
                var canPair = IsSpawnOneHit(teleA);

                if (canPair &&
                    i + 1 < beats.Count &&
                    beats[i + 1] == beatA + 1)
                {
                    var beatB = beats[i + 1];
                    var teleB = GetPrimaryImpact(timeline, beatB);
                    if (teleB != null && IsSpawnOneHit(teleB))
                    {
                        var remB = CombatCounterResolver.GetRemainingHits(teleB, timeline);
                        if (remA > 0 || remB > 0)
                        {
                            result.Add(new BossNoteCluster(
                                BossNoteGlyphKind.Beamed,
                                MakeHead(beatA, teleA, remA),
                                MakeHead(beatB, teleB, remB)));
                            covered.Add(beatA);
                            covered.Add(beatB);
                            i += 2;
                            continue;
                        }
                    }
                }

                result.Add(new BossNoteCluster(
                    BossNoteGlyphKind.Single,
                    MakeHead(beatA, teleA, remA)));
                covered.Add(beatA);
                i++;
            }

            AppendAuthoredSingles(result, authoredFallback, covered);
            return result;
        }

        private static void AppendAuthoredSingles(
            List<BossNoteCluster> result,
            IReadOnlyList<AuthoredBossNoteSpec> authoredFallback,
            HashSet<int> covered)
        {
            if (authoredFallback == null || authoredFallback.Count == 0)
            {
                return;
            }

            foreach (var spec in authoredFallback)
            {
                if (spec.BeatIndex < 0 || spec.RemainingHits < 0)
                {
                    continue;
                }

                if (covered != null && covered.Contains(spec.BeatIndex))
                {
                    continue;
                }

                var tier = spec.DisplayTier;
                if (spec.RemainingHits > 0 &&
                    CombatCounterResolver.TryGetDisplayTier(spec.RemainingHits, out var resolved))
                {
                    tier = resolved;
                }

                result.Add(new BossNoteCluster(
                    BossNoteGlyphKind.Single,
                    new BossNoteHead(
                        spec.BeatIndex,
                        telegraph: null,
                        spec.RemainingHits,
                        tier,
                        VariantForBeat(spec.BeatIndex))));
                covered?.Add(spec.BeatIndex);
            }
        }

        private static BossNoteHead MakeHead(int beat, EnemyTelegraph tele, int remaining)
        {
            CombatCounterResolver.TryGetDisplayTier(remaining, out var tier);
            if (remaining <= 0)
            {
                tier = BossNoteTier.Red;
            }

            return new BossNoteHead(beat, tele, remaining, tier, VariantForBeat(beat));
        }

        private static bool IsSpawnOneHit(EnemyTelegraph tele) =>
            tele != null && (tele.HitsRequired > 0 ? tele.HitsRequired : (int)tele.NoteTier) == 1;

        private static List<int> CollectImpactBeats(BeatTimelineEngine timeline)
        {
            var set = new SortedSet<int>();
            foreach (var tele in timeline.Telegraphs)
            {
                if (tele == null || tele.IsWindupOnly || tele.BeatIndex < 0)
                {
                    continue;
                }

                set.Add(tele.BeatIndex);
            }

            return new List<int>(set);
        }

        private static EnemyTelegraph GetPrimaryImpact(BeatTimelineEngine timeline, int beatIndex)
        {
            var list = timeline.GetImpactTelegraphsAtBeat(beatIndex);
            if (list == null || list.Count == 0)
            {
                return null;
            }

            EnemyTelegraph best = null;
            var bestHits = -1;
            foreach (var tele in list)
            {
                if (tele == null)
                {
                    continue;
                }

                var hits = tele.HitsRequired > 0 ? tele.HitsRequired : (int)tele.NoteTier;
                if (hits > bestHits)
                {
                    bestHits = hits;
                    best = tele;
                }
            }

            return best;
        }

        public static int VariantForBeat(int beatIndex) =>
            ((beatIndex % SingleVariantCount) + SingleVariantCount) % SingleVariantCount;
    }
}
