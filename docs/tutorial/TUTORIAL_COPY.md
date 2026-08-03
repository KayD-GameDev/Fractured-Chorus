# Tutorial Copy (VI runtime · EN docs)

Track ids: `hub` · `map` · `combat` · `combat_tutorial_linear` (replaces confirm-spam `cadence_intro` on CombatTutorial)

Runtime coach language: **VI**. Tables below list **VI** (runtime) and **EN** (docs/reference).

---

## Hub track (`hub`)

| stepId | VI | EN | requiresConfirm |
|--------|----|----|-----------------|
| `hub_menu` | Mở **MENU** (góc trên phải) để xem chỉ số đội, bond, lịch và slot save. | Open **MENU** (top-right) to view party stats, bonds, calendar, and save slots. | yes |
| `hub_town` | Bấm ghim bản đồ để dùng slot hoạt động. Quiz sáng và phase lịch khóa nội dung trong ngày. | Click map pins to spend activity slots. Morning quiz and calendar phases gate what you can do each day. | yes |
| `hub_done` | Cơ bản Hub xong. Khám phá campus, rồi vào Cadence run khi sẵn sàng. | Hub basics covered. Explore campus, then enter a Cadence run when ready. | yes |

---

## Map track (`map`)

| stepId | VI | EN | requiresConfirm |
|--------|----|----|-----------------|
| `map_nodes` | Chọn node tới được để tiến. Battle/Elite dẫn vào combat; cổng boss kết thúc sector. | Select reachable nodes to advance. Battle and Elite nodes lead to combat; the boss gate ends the sector. | yes |
| `map_camp` | Thua trận sẽ về camp gần nhất. HP giữ giữa các trận trong run. | After defeat you return to the nearest camp node. HP persists between fights on the run. | yes |
| `map_done` | Điều hướng map sẵn sàng. Mở đường tới boss khi đội hình ổn. | Map navigation ready. Clear the path to the boss when your party is set. | yes |

---

## Combat track (`combat`) — non-tutorial fights

| stepId | VI | EN | requiresConfirm |
|--------|----|----|-----------------|
| `combat_plan` | **Cửa sổ Planning:** vừa kéo unit sang cột FRONT / MID / BACK, vừa kéo skill lên beat timeline. FRONT ít dính sát thương; BACK đánh mạnh hơn. | **Planning window:** reposition units across the front / mid / back columns and drag skills onto the timeline at the same time. FRONT takes less damage; BACK deals more. | yes |
| `combat_standing` | Standing (chấm xám) để lộ trước telegraph boss. Đổi vị trí bất cứ lúc nào cửa sổ Planning còn mở. | Standing phases (grey dots) leave you exposed to boss telegraphs. Reposition any time the Planning window is open. | yes |
| `combat_execute` | Bấm **Execute** để chạy round — nhạc không dừng, scan bắt vào ô nhịp kế tiếp. Counter nốt boss đúng beat, rồi hạ cửa sổ skill. | Press **Execute** to resolve the round — the music keeps playing and the scan catches the next bar. Counter boss notes on beat, then finish with your skill windows. | yes |
| `combat_done` | Hướng dẫn combat ngắn xong. Giữ nhịp. | Combat tutorial complete. Good luck — keep the rhythm. | yes |

---

## Combat tutorial linear (`cadence_intro` / CombatTutorial scene)

**Runtime model (pass Formation):** slideshow Formation → practice kéo unit → slide chốt → chờ **Execute** → free play.  
Floating hint không chặn Execute/board drag. Timeline slides cũ tạm deferred (chờ feedback).

| Asset | Path |
|-------|------|
| Coach bust | `Art/Characters/Coda/Chibi/coda_cadence_chibi_bust_v1.png` |
| Step guide image | `Art/UI/Tutorial/Steps/{stepId}_v1.png` (thả PNG vào đây) |
| Hand (ghép vào ảnh step nếu cần) | `Art/UI/Tutorial/tutorial_point_hand_v1.png` |

**Luồng CombatTutorial (Formation)**
1. Slides: meet → Formation 6 ô → buff theo vị trí → FRONT / MID·BACK → đặt hợp lý  
2. Slide “Hãy di chuyển Ren và Coda…” + **Next**  
3. Next → tắt coach + badge hint · người chơi kéo unit tự do  
4. Đổi ô ≥1 lần → slide “Làm tốt lắm… nhấn Execute”  
5. Next → floating Execute · bấm Execute → flag `tutorial_cadence_intro_done` · free play  
6. Victory → RunMap  

- Runner: `TutorialDirector.StartCadenceIntroTrack()`  
- Chỉ còn **một** nút duy nhất: **Execute**. Deploy không còn là phase riêng — dời unit và gán skill dùng chung cửa sổ Planning.

### Step table (SoT — Formation pass)

Đặt PNG ghép vào `Art/UI/Tutorial/Steps/{stepId}_v1.png`. Thiếu file = chỉ hiện text + Coda.

| # | stepId | kind | VI (runtime) | EN | Gợi ý ảnh ghép |
|---|--------|------|--------------|----|----------------|
| 00 | `meet_danger` | Slide | Hiện tại ở đây rất nguy hiểm. Tôi là Coda — tôi sẽ hướng dẫn cậu thoát khỏi đây. Nghe kỹ từng bước, đừng nóng vội. | It's dangerous here. I'm Coda — I'll guide you out. Follow each step; don't rush. | Coda + Kiki / danger |
| 01 | `formation_grid` | Slide | Đầu tiên hãy đến với phần Formation — đội hình chia thành 6 ô tương ứng với BACK, MID và FRONT. | First, Formation — the board splits into 6 cells: BACK, MID, and FRONT. | Grid 2×3 labels |
| 02 | `formation_buff_intro` | Slide | Mỗi vị trí sẽ có buff khác nhau dựa vào tình huống nhất định. | Each lane buffs differently depending on the situation. | Lane icons |
| 03 | `formation_front` | Slide | FRONT giảm sát thương nhận vào. | FRONT reduces damage taken. | FRONT callout |
| 04 | `formation_mid_back` | Slide | MID tăng sát thương. BACK tăng khả năng buff và né. | MID boosts damage. BACK boosts buff power and evasion. | MID + BACK |
| 05 | `formation_situational` | Slide | Hãy dựa vào đội hình hiện tại và tình huống để đặt sao cho hợp lý. | Read the current formation and situation, then place units sensibly. | Party vs foe |
| 06 | `formation_practice` | PracticeFormation | Hãy di chuyển Ren và Coda giữa các ô. | Move Ren and Coda between cells. | Next → ẩn UI → chờ kéo |
| 07 | `formation_lock` | Slide | Làm tốt lắm. Khi bạn đã chốt xong vị trí, hãy nhấn Execute. | Nice work. When positions are set, press Execute. | Execute button |
| 08 | `formation_await_deploy` | AwaitDeploy | Nhấn Execute để bắt đầu round. | Press Execute to start the round. | — (floating hint) |

### Encounter wiring

- Id: `Encounter_Tutorial`
- **Scene:** `Assets/FracturedChorus/Scenes/CombatTutorial.unity`
- Party: Ren + Coda; skills = basic only
- Enemy: **Kiki Ueda** (Lv1 Elite visual)
- BG: `cadence_smoke_war_front_bg_v1`
- Entry (test): CampusHub **Tutorial Fight** → `CombatTutorial`
- Editor: **Fractured Chorus → Open / Prepare Combat Tutorial Scene** (Prepare cũng xóa legacy TutorialEditCanvas / Director layers)
