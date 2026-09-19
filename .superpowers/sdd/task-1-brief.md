### Task 1: Presentation + episode catalog (TDD)

**Files:**
- Create: `Assets/FracturedChorus/Hub/BondPresentation.cs`
- Create: `Assets/FracturedChorus/Hub/BondLinkEpisodeCatalog.cs`
- Create: `Assets/FracturedChorus/Editor/BondPresentationTests.cs`

**Interfaces:**
- Produces:
  - `BondPresentation.RosterOrder` → `string[6]` = `ren, charlotte, coda, astra, ryo, mei_lin`
  - `BondPresentation.VisibleChipCount` → `7`
  - `BondPresentation.GetDisplayName(string npcId)` → `string`
  - `BondPresentation.GetRoleLabel(string npcId)` → `"Player"` for `ren`, else `""`
  - `BondPresentation.GetBio(string npcId)` / `GetQuote(string npcId)` → `string`
  - `BondPresentation.IsPortraitUnlocked(string npcId)` → `true` for ren/charlotte/coda/astra
  - `BondLinkEpisodeCatalog.Episodes` → `BondLinkEpisode[5]`
  - `BondLinkEpisodeCatalog.IsUnlocked(int bondRank, int requiredRank)` → `bool`

- [ ] **Step 1: Write the failing tests**

```csharp
using FracturedChorus.Hub;
using FracturedChorus.Meta;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class BondPresentationTests
    {
        [Test]
        public void RosterOrder_MatchesHubBondList()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    BondNpcIds.Ren,
                    BondNpcIds.Charlotte,
                    BondNpcIds.Coda,
                    BondNpcIds.Astra,
                    BondNpcIds.Ryo,
                    BondNpcIds.MeiLin
                },
                BondPresentation.RosterOrder);
        }

        [Test]
        public void VisibleChipCount_IsSixNpcsPlusReserved()
        {
            Assert.AreEqual(7, BondPresentation.VisibleChipCount);
            Assert.AreEqual(6, BondPresentation.RosterOrder.Length);
        }

        [Test]
        public void DisplayNames_MatchLock()
        {
            Assert.AreEqual("Ren", BondPresentation.GetDisplayName(BondNpcIds.Ren));
            Assert.AreEqual("Charlotte", BondPresentation.GetDisplayName(BondNpcIds.Charlotte));
            Assert.AreEqual("Coda", BondPresentation.GetDisplayName(BondNpcIds.Coda));
            Assert.AreEqual("Astra", BondPresentation.GetDisplayName(BondNpcIds.Astra));
            Assert.AreEqual("Ryo", BondPresentation.GetDisplayName(BondNpcIds.Ryo));
            Assert.AreEqual("Mei Lin", BondPresentation.GetDisplayName(BondNpcIds.MeiLin));
            Assert.AreEqual("???", BondPresentation.GetDisplayName("reserved"));
        }

        [Test]
        public void RoleLabel_OnlyRenIsPlayer()
        {
            Assert.AreEqual("Player", BondPresentation.GetRoleLabel(BondNpcIds.Ren));
            Assert.AreEqual(string.Empty, BondPresentation.GetRoleLabel(BondNpcIds.Charlotte));
        }

        [Test]
        public void PortraitUnlock_Arc1VisibleFour()
        {
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Ren));
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Charlotte));
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Coda));
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Astra));
            Assert.IsFalse(BondPresentation.IsPortraitUnlocked(BondNpcIds.Ryo));
            Assert.IsFalse(BondPresentation.IsPortraitUnlocked(BondNpcIds.MeiLin));
            Assert.IsFalse(BondPresentation.IsPortraitUnlocked(null));
        }

        [Test]
        public void CharlotteCopy_MatchesMock()
        {
            StringAssert.Contains("quiet yet passionate", BondPresentation.GetBio(BondNpcIds.Charlotte));
            StringAssert.Contains("little kinder", BondPresentation.GetQuote(BondNpcIds.Charlotte));
        }

        [Test]
        public void LinkEpisodes_FiveTitles_RankGates()
        {
            Assert.AreEqual(5, BondLinkEpisodeCatalog.Episodes.Length);
            Assert.AreEqual("A Usual Day", BondLinkEpisodeCatalog.Episodes[0].Title);
            Assert.AreEqual(1, BondLinkEpisodeCatalog.Episodes[0].RequiredRank);
            Assert.AreEqual("After Class", BondLinkEpisodeCatalog.Episodes[1].Title);
            Assert.AreEqual(2, BondLinkEpisodeCatalog.Episodes[1].RequiredRank);
            Assert.AreEqual("A Different Melody", BondLinkEpisodeCatalog.Episodes[2].Title);
            Assert.AreEqual("Unspoken Words", BondLinkEpisodeCatalog.Episodes[3].Title);
            Assert.AreEqual("Toward Tomorrow", BondLinkEpisodeCatalog.Episodes[4].Title);
            Assert.AreEqual(5, BondLinkEpisodeCatalog.Episodes[4].RequiredRank);
        }

        [Test]
        public void EpisodeUnlock_UsesBondRank()
        {
            Assert.IsTrue(BondLinkEpisodeCatalog.IsUnlocked(1, 1));
            Assert.IsFalse(BondLinkEpisodeCatalog.IsUnlocked(1, 2));
            Assert.IsTrue(BondLinkEpisodeCatalog.IsUnlocked(5, 5));
            Assert.IsFalse(BondLinkEpisodeCatalog.IsUnlocked(0, 1));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Unity → Window → General → Test Runner → **EditMode** → `BondPresentationTests`.

Expected: FAIL — `BondPresentation` / `BondLinkEpisodeCatalog` type not found.

- [ ] **Step 3: Write minimal implementation**

`Assets/FracturedChorus/Hub/BondPresentation.cs`:

```csharp
using FracturedChorus.Meta;

namespace FracturedChorus.Hub
{
    public static class BondPresentation
    {
        public const string Title = "BONDS";
        public const string TitleJp = "絆";
        public const string SocialStatsTitle = "SOCIAL STATS";
        public const string SocialStatsJp = "共鳴ステータス";
        public const string LinkEpisodesTitle = "LINK EPISODES";
        public const string LinkEpisodesJp = "リンクエピソード";
        public const string Location = "HIMA CITY";
        public const string LocationTagline = "Music Lives in You";
        public const string TaglineRight = "PEOPLE MAKE MUSIC.";
        public const string Wordmark = "FRACTURE CHORUS";
        public const string WordmarkSub = "MUSIC PEOPLE CONNECT THE WORLD";
        public const string NextRankLabel = "NEXT RANK";
        public const string NextRankHint = "A small step,\na closer heart.";
        public const string EpisodeLockHint = "Reach higher rank to unlock new episodes.";
        public const string PromoCaption = "Music People Connect The World";
        public const int VisibleChipCount = 7;

        public static readonly string[] RosterOrder =
        {
            BondNpcIds.Ren,
            BondNpcIds.Charlotte,
            BondNpcIds.Coda,
            BondNpcIds.Astra,
            BondNpcIds.Ryo,
            BondNpcIds.MeiLin
        };

        public static string GetDisplayName(string npcId) => npcId switch
        {
            BondNpcIds.Ren => "Ren",
            BondNpcIds.Charlotte => "Charlotte",
            BondNpcIds.Coda => "Coda",
            BondNpcIds.Astra => "Astra",
            BondNpcIds.Ryo => "Ryo",
            BondNpcIds.MeiLin => "Mei Lin",
            _ => "???"
        };

        public static string GetRoleLabel(string npcId) =>
            npcId == BondNpcIds.Ren ? "Player" : string.Empty;

        public static bool IsPortraitUnlocked(string npcId) =>
            npcId == BondNpcIds.Ren
            || npcId == BondNpcIds.Charlotte
            || npcId == BondNpcIds.Coda
            || npcId == BondNpcIds.Astra;

        public static string GetBio(string npcId) => npcId switch
        {
            BondNpcIds.Charlotte =>
                "A quiet yet passionate girl who always stays close to music. Her melodies feel like sunlight—gentle, but strong enough to change someone's day.",
            BondNpcIds.Ren =>
                "A HIMA newcomer still learning the city's rhythm. He listens first, then steps in when the chorus starts to fracture.",
            BondNpcIds.Coda =>
                "A Harmony mage who treats every silence like a measure rest—soft-spoken, precise, and stubborn about keeping people together.",
            BondNpcIds.Astra =>
                "Campus guide by day, LUXE pulse on stage. She makes a room feel like a spotlight even when she is only pointing you down a hallway.",
            BondNpcIds.Ryo =>
                "Locked. Story progress required.",
            BondNpcIds.MeiLin =>
                "Locked. Story progress required.",
            _ => "An echo waiting to be named."
        };

        public static string GetQuote(string npcId) => npcId switch
        {
            BondNpcIds.Charlotte => "Maybe... music can make the world a little kinder, right?",
            BondNpcIds.Ren => "If the city is out of tune, someone has to count the beats.",
            BondNpcIds.Coda => "Hold the note. The rest of us will find you.",
            BondNpcIds.Astra => "Keep your chin up. The chorus sounds better when you look at it.",
            _ => string.Empty
        };
    }
}
```

`Assets/FracturedChorus/Hub/BondLinkEpisodeCatalog.cs`:

```csharp
namespace FracturedChorus.Hub
{
    public readonly struct BondLinkEpisode
    {
        public BondLinkEpisode(int index, string title, int requiredRank)
        {
            Index = index;
            Title = title;
            RequiredRank = requiredRank;
        }

        public int Index { get; }
        public string Title { get; }
        public int RequiredRank { get; }
    }

    public static class BondLinkEpisodeCatalog
    {
        public static readonly BondLinkEpisode[] Episodes =
        {
            new BondLinkEpisode(1, "A Usual Day", 1),
            new BondLinkEpisode(2, "After Class", 2),
            new BondLinkEpisode(3, "A Different Melody", 3),
            new BondLinkEpisode(4, "Unspoken Words", 4),
            new BondLinkEpisode(5, "Toward Tomorrow", 5)
        };

        public static bool IsUnlocked(int bondRank, int requiredRank) =>
            bondRank >= requiredRank && requiredRank >= 1;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Test Runner → EditMode → `BondPresentationTests`. Expected: **PASS** (8 tests).

- [ ] **Step 5: Commit**

```bash
git add Assets/FracturedChorus/Hub/BondPresentation.cs Assets/FracturedChorus/Hub/BondPresentation.cs.meta Assets/FracturedChorus/Hub/BondLinkEpisodeCatalog.cs Assets/FracturedChorus/Hub/BondLinkEpisodeCatalog.cs.meta Assets/FracturedChorus/Editor/BondPresentationTests.cs Assets/FracturedChorus/Editor/BondPresentationTests.cs.meta
git commit -m "$(cat <<'EOF'
Add Bonds presentation constants for sandbox HUD copy.

EOF
)"
```

---

