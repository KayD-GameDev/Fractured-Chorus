### Task 1: Pure gesture helper + EditMode tests

**Files:**
- Create: `Assets/FracturedChorus/UI/BoardPointerGesture.cs`
- Create: `Assets/FracturedChorus/UI/BoardPointerGesture.cs.meta` (Unity generates on import if missing)
- Create: `Assets/FracturedChorus/Tests/EditMode/BoardPointerGestureTests.cs`
- Create: `Assets/FracturedChorus/Tests/EditMode/FracturedChorus.EditModeTests.asmdef` (if no EditMode asmdef exists yet)

**Interfaces:**
- Consumes: none
- Produces:
  - `BoardPointerGesture.ShouldCommitDrag(Vector2 pointerDownScreen, Vector2 currentScreen, float thresholdPx) -> bool`
  - `BoardPointerGesture.IsClick(Vector2 pointerDownScreen, Vector2 releaseScreen, float thresholdPx) -> bool` (= `!ShouldCommitDrag`)

- [ ] **Step 1: Check for existing EditMode asmdef**

Run:
```powershell
Get-ChildItem -Recurse Assets -Filter *.asmdef | Select-Object FullName
```

If none under `Assets/FracturedChorus/Tests`, create:

`Assets/FracturedChorus/Tests/EditMode/FracturedChorus.EditModeTests.asmdef`
```json
{
  "name": "FracturedChorus.EditModeTests",
  "rootNamespace": "FracturedChorus.Tests",
  "references": [
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner",
    "GUID:PLACEHOLDER_ASSEMBLY_CSHARP"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "optionalUnityReferences": ["TestAssemblies"]
}
```

Resolve `Assembly-CSharp` reference: open `Assets/FracturedChorus` scripts â€” they compile into `Assembly-CSharp` by default (no runtime asmdef). Prefer referencing the assembly by name `"Assembly-CSharp"` in the asmdef `references` array (Unity accepts it for default assemblies). If Unity rejects it, put the test file under `Assets/FracturedChorus/Editor/` as a simple Editor test without a separate asmdef, using `NUnit.Framework` already available to Editor scripts â€” **prefer that fallback** to avoid asmdef fights:

Fallback path if asmdef fails:
- Create: `Assets/FracturedChorus/Editor/BoardPointerGestureTests.cs` with `[Test]` and menu/Test Runner discovery under EditMode.

- [ ] **Step 2: Write failing tests first**

`Assets/FracturedChorus/Tests/EditMode/BoardPointerGestureTests.cs` (or Editor fallback):
```csharp
using FracturedChorus.UI;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class BoardPointerGestureTests
    {
        [Test]
        public void ShouldCommitDrag_False_WhenDistanceAtOrBelowThreshold()
        {
            var down = new Vector2(100f, 100f);
            Assert.IsFalse(BoardPointerGesture.ShouldCommitDrag(down, down + new Vector2(8f, 0f), 8f));
            Assert.IsFalse(BoardPointerGesture.ShouldCommitDrag(down, down, 8f));
        }

        [Test]
        public void ShouldCommitDrag_True_WhenDistanceAboveThreshold()
        {
            var down = new Vector2(100f, 100f);
            Assert.IsTrue(BoardPointerGesture.ShouldCommitDrag(down, down + new Vector2(8.1f, 0f), 8f));
        }

        [Test]
        public void IsClick_True_OnlyWhenNotCommitted()
        {
            var down = new Vector2(50f, 50f);
            Assert.IsTrue(BoardPointerGesture.IsClick(down, down + new Vector2(3f, 4f), 8f));
            Assert.IsFalse(BoardPointerGesture.IsClick(down, down + new Vector2(10f, 0f), 8f));
        }
    }
}
```

- [ ] **Step 3: Run tests â€” expect FAIL (type missing)**

Unity Test Runner â†’ EditMode â†’ run `BoardPointerGestureTests`.  
Or CLI if available:
```powershell
# Only if project has batchmode test script; otherwise use Test Runner window
```
Expected: compile error / missing `BoardPointerGesture`.

- [ ] **Step 4: Implement helper**

`Assets/FracturedChorus/UI/BoardPointerGesture.cs`:
```csharp
using UnityEngine;

namespace FracturedChorus.UI
{
    public static class BoardPointerGesture
    {
        public static bool ShouldCommitDrag(Vector2 pointerDownScreen, Vector2 currentScreen, float thresholdPx)
        {
            return Vector2.Distance(pointerDownScreen, currentScreen) > thresholdPx;
        }

        public static bool IsClick(Vector2 pointerDownScreen, Vector2 releaseScreen, float thresholdPx)
        {
            return !ShouldCommitDrag(pointerDownScreen, releaseScreen, thresholdPx);
        }
    }
}
```

- [ ] **Step 5: Re-run tests â€” expect PASS**

- [ ] **Step 6: Commit**

```powershell
git add Assets/FracturedChorus/UI/BoardPointerGesture.cs Assets/FracturedChorus/UI/BoardPointerGesture.cs.meta Assets/FracturedChorus/Tests Assets/FracturedChorus/Editor/BoardPointerGestureTests.cs
git commit -m @"
Add click-vs-drag gesture helper with EditMode tests.

Keeps planning click/drag rules testable before BoardDragController wiring.
"@
```

---
