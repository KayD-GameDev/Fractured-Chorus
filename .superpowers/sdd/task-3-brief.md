### Task 3: Extend `TimelineLayoutLock` + wire ScanBar / TrackLine

**Files:**
- Modify: `Assets/FracturedChorus/UI/TimelineLayoutLock.cs`
- Modify: `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` (`ApplyTrackLineLayout`, `AlignScanBar`, `GetScanLineX`)

**Interfaces:**
- Consumes: existing `SlotWidth`, `ScanBarWidth`, `ScanBarVerticalInset`
- Produces: `TrackLineY`, `TrackLineHeight` constants

- [ ] **Step 1: Add TrackLine constants**

In `TimelineLayoutLock.cs`, after `ScanBarVerticalInset`:
```csharp
public const float TrackLineY = 6f;
public const float TrackLineHeight = 2f;
```

- [ ] **Step 2: Wire `ApplyTrackLineLayout`**

```csharp
trackLine.anchorMin = new Vector2(0f, 0f);
trackLine.anchorMax = new Vector2(1f, 0f);
trackLine.pivot = new Vector2(0.5f, 0f);
trackLine.anchoredPosition = new Vector2(0f, TimelineLayoutLock.TrackLineY);
trackLine.sizeDelta = new Vector2(0f, TimelineLayoutLock.TrackLineHeight);
```

- [ ] **Step 3: Wire `AlignScanBar` + idle ScanBar X**

`AlignScanBar` already uses `TimelineLayoutLock.ScanBarWidth` / `ScanBarVerticalInset` â€” verify; if magic numbers remain, replace them.

`GetScanLineX`:
```csharp
private float GetScanLineX()
{
    return TimelineLayoutLock.ClampSlotWidth(slotWidth) * 0.5f;
}
```

- [ ] **Step 4: Verify scene locks (no YAML rewrite unless drifted)**

```powershell
Select-String -Path Assets/FracturedChorus/Scenes/CombatTutorial.unity,Assets/FracturedChorus/Scenes/CombatPrototype.unity -Pattern "slotWidth:|m_PreferredWidth: 73|m_SizeDelta: \{x: 73.85"
```
Expected: both scenes show `slotWidth: 73.85` and Beat_0 width/preferredWidth 73.85.

- [ ] **Step 5: Compile + commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/check-compile.ps1
git add Assets/FracturedChorus/UI/TimelineLayoutLock.cs Assets/FracturedChorus/UI/BeatTimelineUIView.cs
git commit -m @"
Lock TrackLine and ScanBar sizes to CombatTutorial constants.

Prevents runtime layout from drifting off the authored beat strip.
"@
```

---
