# PrologueVN — Scene setup

**Scene:** `Assets/FracturedChorus/Scenes/PrologueVN.unity`
**Flow:** Main Menu NEW GAME → PrologueVN → OpeningInvestigation → CampusHub

## Tạo / rebuild scene

Unity Editor (đóng batch nếu đang chạy):

1. Menu **Fractured Chorus → Create PrologueVN Scene** (lần đầu)
   - Hoặc **Setup PrologueVN Scene Hierarchy** (rebuild trên scene đang mở)
2. Save scene · Build Settings tự thêm index 1

Batch (Unity **đóng**):

```powershell
Unity -batchmode -projectPath "d:\Fractured-Chorus1" -executeMethod FracturedChorus.Editor.PrologueVNSceneSetupEditor.BatchCreatePrologueVNScene -logFile Logs/PrologueVNSceneSetup.log
```

## Playtest flow

| Bước | Hành vi |
|------|---------|
| 1 | Disclaimer 3 dòng + typing SFX · Enter/Space advance · Enter skip typewriter |
| 2 | BGM Velvet Reverie + butterfly wings loop · 11 dòng triết lý + dialogue box |
| 3 | Choice Persona-style · **I agree.** / **I do not agree.** (↑↓ Enter) |
| 4a | Disagree → fade → MainMenuStartGame |
| 4b | Agree → Contract · nhập tên (gợi ý Ren) · ký · Confirm ×2 |
| 5 | Cảm ơn + tên → fade → RunMapPrototype |

## Audio

| Clip | Path |
|------|------|
| BGM | `Audio/Music/Velvet_Reverie_BGM.mp3` |
| Typing | `Audio/SFX/Prologue_Typing.mp3` |
| Butterfly | `Audio/SFX/Prologue_ButterflyWings.mp3` |
| Pen sign | `Audio/SFX/Prologue_PenSign.mp3` |

## Scripts

| Script | Role |
|--------|------|
| `Narrative/PrologueVNController.cs` | State machine |
| `Narrative/PrologueTypewriterView.cs` | Typewriter + typing SFX |
| `Narrative/PrologueChoiceView.cs` | Yes/No UI |
| `Narrative/PrologueContractView.cs` | Name + sign |
| `Narrative/PrologueSignaturePad.cs` | Vẽ chữ ký |
| `Narrative/PrologueAudioController.cs` | BGM / SFX |
| `Narrative/RunProfile.cs` | PlayerPrefs tên |

## Build order

0 MainMenuStartGame · 1 PrologueVN · 2 RunMapPrototype · 3 CombatPrototype
