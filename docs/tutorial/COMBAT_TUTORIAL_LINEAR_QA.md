# Combat Tutorial — Play Mode QA (Formation pass)

Scene: `CombatTutorial.unity`  
Menu: **Fractured Chorus → Prepare Combat Tutorial Scene** rồi Play.

| # | Check | Pass |
|---|-------|------|
| 1 | Play → slide meet + Formation (bust Coda, Next/Back) | |
| 2 | Slide “Hãy di chuyển Ren và Coda…” có Next; badge hint vẫn hiện | |
| 3 | Next → coach + badge ẩn; Deploy hiện; kéo được unit | |
| 3b | Đổi ô ≥1 lần → mới hiện slide “Làm tốt lắm… Deploy” | |
| 4 | Next → floating Deploy; bấm Deploy → coach tắt, free play | |
| 5 | Flag `tutorial_cadence_intro_done` sau Deploy | |
| 6 | Victory → RunMap | |
| 7 | `CombatPrototype` không chạy cadence Formation track | |

Step list: `docs/tutorial/TUTORIAL_COPY.md` § Combat tutorial linear.
