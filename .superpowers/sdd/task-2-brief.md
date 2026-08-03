### Task 2: Deferred drag in `BoardDragController`

**Files:**
- Modify: `Assets/FracturedChorus/UI/BoardDragController.cs`

**Interfaces:**
- Consumes: `BoardPointerGesture.ShouldCommitDrag`, `BoardPointerGesture.IsClick`
- Produces: same public API (`CanDragUnit`, `BeginDrag`, `EndDrag`, `CancelActiveDrag`, click handler)

- [ ] **Step 1: Update class summary + remove eager BeginDrag on pointer down**

Replace the class XML summary with:
```csharp
/// <summary>
/// Planning window: short click opens skill panel; drag past threshold repositions unit.
/// Uses Physics2D pick â€” reliable with Screen Space Overlay UI + Input System.
/// </summary>
```

Replace `HandlePointerDown` body so it **never** calls `BeginDrag`:
```csharp
private void HandlePointerDown(Vector2 screenPos)
{
    _pointerDownUnit = null;
    _dragPointerActive = false;
    _draggingUnit = null;

    if (IsScreenPointBlockedByUi(screenPos))
    {
        return;
    }

    var view = PickUnitAtScreen(screenPos);
    if (view == null)
    {
        return;
    }

    _pointerDownUnit = view;
    _pointerDownScreen = screenPos;
    _dragPointerActive = true;
}
```

- [ ] **Step 2: Commit drag only after threshold in `Update`**

Replace the held-pointer block in `Update` with:
```csharp
if (IsPointerHeld() && _dragPointerActive && _pointerDownUnit != null)
{
    if (_draggingUnit == null
        && CanDragUnit(_pointerDownUnit)
        && BoardPointerGesture.ShouldCommitDrag(_pointerDownScreen, screenPos, clickDragThresholdPx))
    {
        BeginDrag(_pointerDownUnit);
    }

    if (_draggingUnit != null)
    {
        UpdateDragAtScreen(screenPos);
    }
}
```

- [ ] **Step 3: Fix `HandlePointerUp` for deferred drag**

```csharp
private void HandlePointerUp(Vector2 screenPos)
{
    if (_draggingUnit != null)
    {
        EndDrag(_draggingUnit);
    }
    else if (_pointerDownUnit != null
             && BoardPointerGesture.IsClick(_pointerDownScreen, screenPos, clickDragThresholdPx)
             && CanOpenSkillPanelFor(_pointerDownUnit))
    {
        _onUnitClicked?.Invoke(_pointerDownUnit.Unit, _pointerDownUnit);
    }

    _pointerDownUnit = null;
    _dragPointerActive = false;
}
```

- [ ] **Step 4: Clear pointer state in `CancelActiveDrag`**

Ensure:
```csharp
public void CancelActiveDrag()
{
    if (_draggingUnit != null)
    {
        CancelDrag(_draggingUnit);
    }

    _dragPointerActive = false;
    _pointerDownUnit = null;
    _draggingUnit = null;
}
```

- [ ] **Step 5: Compile check**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/check-compile.ps1
```
Expected: `COMPILE OK`

- [ ] **Step 6: Commit**

```powershell
git add Assets/FracturedChorus/UI/BoardDragController.cs
git commit -m @"
Defer unit drag so short clicks open the skill panel.

Fixes Planning-window regression after Deploy merged with skill assign.
"@
```

---
