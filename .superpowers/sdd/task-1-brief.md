### Task 1: LoadingProgress math

**Files:**
- Create: `Assets/FracturedChorus/UI/Loading/LoadingProgress.cs`
- Create: `Assets/FracturedChorus/Editor/LoadingProgressTests.cs`

**Interfaces:**
- Produces: `LoadingProgress.MapAsyncProgress(float)`, `LoadingProgress.CanActivate(float displayedFill, float holdElapsedSec)`, constants `UnityActivationCap`, `MinHoldSec`, `ActivateFill`, `FadeInSec`, `FadeOutSec`, `SmoothTime`, `PercentVisibleMin`.

- [ ] **Step 1: Write the failing tests**

```csharp
using FracturedChorus.UI.Loading;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class LoadingProgressTests
    {
        [Test]
        public void MapAsyncProgress_Zero_IsZero()
        {
            Assert.AreEqual(0f, LoadingProgress.MapAsyncProgress(0f), 0.0001f);
        }

        [Test]
        public void MapAsyncProgress_Cap_IsOne()
        {
            Assert.AreEqual(1f, LoadingProgress.MapAsyncProgress(0.9f), 0.0001f);
        }

        [Test]
        public void MapAsyncProgress_HalfCap_IsHalf()
        {
            Assert.AreEqual(0.5f, LoadingProgress.MapAsyncProgress(0.45f), 0.0001f);
        }

        [Test]
        public void MapAsyncProgress_AboveCap_ClampsToOne()
        {
            Assert.AreEqual(1f, LoadingProgress.MapAsyncProgress(1f), 0.0001f);
        }

        [Test]
        public void CanActivate_RequiresFillAndHold()
        {
            Assert.IsFalse(LoadingProgress.CanActivate(1f, 0.79f));
            Assert.IsFalse(LoadingProgress.CanActivate(0.98f, 1f));
            Assert.IsTrue(LoadingProgress.CanActivate(0.99f, 0.80f));
        }
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL (type missing)**

Unity: Window → General → Test Runner → EditMode → `LoadingProgressTests`.

Expected: fail compile / type not found `LoadingProgress`.

- [ ] **Step 3: Implement**

```csharp
using UnityEngine;

namespace FracturedChorus.UI.Loading
{
    public static class LoadingProgress
    {
        public const float UnityActivationCap = 0.9f;
        public const float FadeInSec = 0.20f;
        public const float FadeOutSec = 0.25f;
        public const float MinHoldSec = 0.80f;
        public const float SmoothTime = 0.12f;
        public const float ActivateFill = 0.99f;
        public const float PercentVisibleMin = 0.02f;

        public static float MapAsyncProgress(float unityProgress)
        {
            if (unityProgress <= 0f)
            {
                return 0f;
            }

            var mapped = unityProgress / UnityActivationCap;
            return Mathf.Clamp01(mapped);
        }

        public static bool CanActivate(float displayedFill, float holdElapsedSec)
        {
            return displayedFill >= ActivateFill && holdElapsedSec >= MinHoldSec;
        }
    }
}
```

- [ ] **Step 4: Re-run tests — expect PASS**

- [ ] **Step 5: Commit**

```
git add Assets/FracturedChorus/UI/Loading/LoadingProgress.cs Assets/FracturedChorus/UI/Loading/LoadingProgress.cs.meta Assets/FracturedChorus/Editor/LoadingProgressTests.cs Assets/FracturedChorus/Editor/LoadingProgressTests.cs.meta
git commit -m "Add loading progress mapping for async scene activation."
```

---

