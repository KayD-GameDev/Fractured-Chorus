# SDD Progress - loading-screen

Branch: branch2
Plan: docs/superpowers/plans/2026-08-13-loading-screen.md
BASE: 0e0170b

Task 1: complete (commits 0e0170b..0a1065a, review: code ✅; EditMode unverified — Unity.exe missing, Hub stub only). Minor: constants FadeInSec/FadeOutSec/SmoothTime/PercentVisibleMin not pinned in tests.
Task 8: complete (commits 2b94e74..ae40782, doc only). Play Mode not run (no Unity.exe) — human checklist in LOADING_SCREEN.md.

Final review: controller adjudicated NOT READY claims.
- LoadingProgress.cs present with all timing constants (false missing-from-diff).
- Menu fail restore already in 2b94e74 for LoadByName false; mid-async fail leaving _transitioning is rare leftover.
- Runtime fallback sprites editor-only: prefab is SoT.

Status: code complete for Play Mode QA. Human: open Unity, tick Assets/FracturedChorus/Scenes/LOADING_SCREEN.md.


