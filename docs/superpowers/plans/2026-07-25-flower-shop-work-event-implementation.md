# Flower Shop Work Event — Implementation Plan

> Mirror of approved plan; tracking implementation status.

**Goal:** VN flower-shop work event with intro, random quiz, Resonance bonus.

## Tasks

- [x] Meta bridge: `StoryFlagIds.FlowerJobIntroDone`, `HubPendingActivity`, `HubPhaseDriver` load `FlowerShopWork`
- [x] VN Choice: `VnBeat.choices` / `choiceNextBeatIndex`, `VnChoiceView`, runtime dispatch
- [x] Scenario pool (5) + `FlowerWorkScriptBuilder`
- [x] `FlowerWorkEventController` + Editor scene setup menu / auto-flag
- [x] Wed/Sat availability on `flower_shop`
- [x] Design + plan docs

## Manual step (Editor)

1. Open Unity project (or run menu **Fractured Chorus / Hub / Create FlowerShopWork Scene**).  
2. Confirm `Assets/FracturedChorus/Scenes/FlowerShopWork.unity` exists and is in Build Settings.  
3. Playtest from CampusHub on a Wednesday/Saturday Day phase.
