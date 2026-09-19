# Ren — Past Backstory (Bond Story Content)

**Date:** 2026-09-14  
**Status:** Design only — no Arc 4 / vault / implementation  
**Scope:** Quá khứ Ren trước HIMA · nội dung kể trong tuyến story/bond của Ren · không mở nhánh Index gameplay

---

## 1. Mục tiêu nội dung

Khi player đọc story Ren (bond episodes / VN beats sau này), **chỉ nghe quá khứ**:

- Ren từng viết nhạc indie, một mình, đêm khuya.
- **Touya Reeve** (alias **Toya**) — senpai DAW / collab — giữ file, mix, “đưa lên sàn”. Sprite: `Art/Characters/Touya/Past/touya_aoto_hs_fullbody_v1.png`. Past Ren: `Art/Characters/Ren/Past/ren_aoto_hs_fullbody_v1.png`.
- Người đó **ăn cắp chất xám**: đem demo của Ren lên con đường industry, đổi tên, leo danh.
- Ren bị **tố ngược**, account/page cũ chết, cộng đồng gọi anh là kẻ ghen.
- Ren lập **page ẩn danh** (không avatar, không bio) — chỉ stems + ngày upload. Đó là nguồn gốc thứ Charlotte sau này gọi *The Page He Keeps* (cùng page; Charlotte arc kể góc của cô, Ren arc kể góc của anh).
- Transfer HIMA = thoát comment loop, **không** = quên nhạc hay quên người kia.

**Không thuộc spec này:** mở Vault Index, combat CREDO, “giải cứu Touya”, flag sau vault 3, StellaWorks macro arc. Giữ trong `2026-09-14-ren-index-rescue-arc-design` (draft nội bộ) khi cần sau.

---

## 2. Khóa tính cách & giọng kể

| Khóa | Chi tiết |
|------|----------|
| Ren biết sự thật | Từ lúc hit ra đời — không twist “ồ ra là anh ta”. Story Ren = **cách anh sống với sự thật**, không phải trinh thám. |
| Im lặng | Không im vì xấu hổ. Im vì **nói ra chỉ làm người kia chìm sâu hơn** trong danh đã mất — và vì Ren không muốn biến nhạc thành tranh cãi comment. |
| Tone | Đen tối **vừa**: phản bội, cô độc, nhạc bị đánh cắp danh nghĩa — không torture porn, không copy Haruto (SyncPod hijack). |
| Ngầu | Lời ít, cảm xúc **kìm** — `annoyed` / `neutral` / một beat `startled` khi nghe lại melody. Không khóc kịch. |
| Theme cảm | *Music breaks the silence* — nhưng quá khứ dạy anh: im lặng đôi khi là cách duy nhất không phá vỡ ai. |

Canon hiện tại giữ: transfer HIMA, grey eyes, SyncPod xanh, waveform tie, indie playlist (Opening spec).

---

## 3. Nhân vật quá khứ

### Ren (trước HIMA)

- Học sinh / fresh transfer, **slim**, tóc đen rối, mắt xám lạnh.
- Viết trên laptop cũ + interface DAW; không có đội hình, không có label.
- Tự tin vào **nốt nhạc**, không vào **hệ thống credit**.

### Touya Reeve (tên khóa cho design doc)

| Field | Past-only framing |
|-------|-------------------|
| Vai trò | Senpai một năm · collab tin cậy · “anh mix giúp”, “anh upload giúp lần này” |
| Tính cách lúc còn bạn | Ấm, gọi Ren bằng tên, nhớ deadline của Ren hơn Ren nhớ. Ren **tôn** anh như người dạy anh nghe mix. |
| Bước phản bội | Không cướp file một đêm — **chuỗi**: giữ master → biến mất vài tuần → xuất hiện trên board industry dưới tên unit mới, melody từ demo `07` (working title). |
| Sau phản bội (chỉ nhắc trong flashback) | Không còn là “Touya mix cho Ren” — là face của unit **CREDO** (authentic indie branding). Ren **không** kể tên unit trong ep đầu; có thể nhắc muộn trong ep sau khi player đã thấy billboard ở main game. |

**Lưu ý:** Touya = head nhánh Index là **lore tương lai**; trong bond past-only chỉ cần “leo lên nhờ bài của Ren”, chưa cần giải thích Pentad ngón tay.

---

## 4. Timeline quá khứ (tháng tương đối)

```
T-18 tháng   Ren + Touya collab ổn định. Demo folder chung (Ren stems, Touya mix).
T-12 tháng   Demo "07" — melody Ren tự humm trước khi ghi. Touya: "Để anh giữ bản master."
T-8 tháng    Touya online ít. Ren vẫn gửi file — "backup thôi."
T-6 tuần     Touya im hẳn.
T-0 hit      MV/board: unit mới, hook = demo 07. Credit không có Ren.
T+3 ngày     Ren report → account khóa (false claim). Comment quay sang Ren.
T+2 tuần     Page ẩn danh lên — stem gốc + timestamp. Bio trống.
T+2 tháng    Quyết định transfer HIMA (thoát toxic feed, giữ page).
```

Thời điểm tuyệt đối (năm / thành phố cũ) **cố ý mơ** — chỉ cần “trước Lumina”.

---

## 5. Beats cảm xúc (dùng khi viết script)

1. **Tin** — Touya nghe bản thô, gật đầu không khen dài. Ren nghĩ đó là ngôn ngữ của pro.
2. **Giao master** — Một câu: “Anh giữ giúp. Lần này đừng để ai đụng vào.” Touya: “Ừ.”
3. **Im lặng dài** — Ren không đuổi hỏi sớm (xấu hổ vì “nghi ngờ bạn”).
4. **Nghe hit trên điện thoại** — Cùng progression, khác timbre production. Tay Ren dừng trên pavement. **Không** la hét.
5. **Report & fall** — Form ngắn, phản hồi template. Handle dead. Ren đọc comment một lần rồi tắt.
6. **Page** — Upload stem `07` raw + date. Không caption. Chỉ để **thời gian** nói trước industry.
7. **Im lặng hiện tại (HIMA)** — Party hỏi “sao không nói?” — Ren: một câu kiểu *“Nói thì ai được cứu?”* hoặc im + đổi chủ đề. **Past arc không giải cứu** — chỉ dừng ở “anh vẫn giữ file.”

---

## 6. Page ẩn danh (object story)

| Thuộc tính | Canon for Ren past |
|------------|-------------------|
| Tên hiển thị | Không tên · hoặc handle vô nghĩa một lần rồi bỏ |
| Nội dung | Stems, WIP, **không** MV, **không** lyric đầy đủ công khai |
| Mục đích | Bằng chứng thời gian cho **chính Ren** — không phải để viral |
| Liên Charlotte | Cùng URL/page trong lore; Charlotte tìm thấy **bản gốc**; Ren ep giải thích **vì sao tạo page**, không giải Charlotte arc |

---

## 7. Link episode titles (ship names)

Cùng pattern Charlotte: **Title Case**, ngắn, poetic · EN trên UI row + có thể bake vào promo JPG.

| # | **Episode title** | Beat (past) |
|---|-------------------|-------------|
| 1 | **The File He Gave Away** | Collab đêm · Touya giữ master · Ren tin |
| 2 | **Demo Seven** | Sáng tác demo `07` · melody gắn ký ức |
| 3 | **Dead Handle** | Hit ăn cắp · report · account chết · comment |
| 4 | **No Name, No Face** | Page ẩn danh · stem + timestamp |
| 5 | **Still Keeping the Files** | Folder + chat cũ · im lặng · chưa giải cứu |

**Asset paths (khi gen xong):**

```
Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep01_the_file_he_gave_away_v1.jpg
Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep02_demo_seven_v1.jpg
Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep03_dead_handle_v1.jpg
Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep04_no_name_no_face_v1.jpg
Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep05_still_keeping_the_files_v1.jpg
```

Gate gameplay **không** nằm trong doc này. Ep 5 **không** mở Index vault · **không** rescue mission.

---

## 7b. Promo art — shared style lock

Append **mọi** prompt (GPT Image 2 / tương đương):

```text
Fractured Chorus bond episode promo key art. Premium anime illustration, cinematic lighting, 16:9 landscape composition safe for UI crop (important subject center-left, negative space right for Bonds menu chrome). Cool palette: deep navy #0B1220, ice cyan #00D4FF accents, soft moonlight, subtle film grain. Music-indie mood — DAW waveforms, upload UI, phone screens OK as set dressing. Restrained emotion, not comedy, not horror gore. Original characters only — no Persona/Atlus logos, no real brand UI, no LUXE idol stage, no Eternal Spark title card. English episode title as small elegant typography in lower third (exact spelling from prompt). No other readable text, no watermark. JPG quality, ~1536×864 or 1920×1080.
```

**Ren (flashback / pre-HIMA):** slim late-teen boy, messy black hair, cool grey eyes, pale skin, plain dark hoodie or black tee — **no** HIMA blazer, **no** SyncPod unless ep notes present-day beat.

**Touya (original):** slightly older teen, neat dark hair, warm approachable face in ep1 only; later eps = silhouette, hands, or back only — tránh lock mặt lệch canon sau.

---

## 7c. Promo prompts (per episode)

### Ep 1 — The File He Gave Away

**Filename:** `ren_ep01_the_file_he_gave_away_v1.jpg`

```text
SUBJECT. Small bedroom studio at night, two boys at a desk: younger slim boy with messy black hair and grey eyes (Ren) watching the screen, older neat-haired senpai (Touya) adjusting a DAW mixer with confident smile. Warm desk lamp + cool monitor glow, exported waveforms on screen, a folder icon labeled only with abstract music symbols (no real app logos). Mood: trust before betrayal. Lower third title: "The File He Gave Away". Fractured Chorus bond episode promo key art… [append style lock]
```

### Ep 2 — Demo Seven

**Filename:** `ren_ep02_demo_seven_v1.jpg`

```text
SUBJECT. Ren alone at night with headphones, humming into a cheap microphone, piano-roll and track name "07" on a minimalist DAW UI (fictional). Melody visualized as faint cyan waveform ribbon in the air. Lonely but focused — the hook is being born. Window rain optional. Lower third title: "Demo Seven". Fractured Chorus bond episode promo key art… [append style lock]
```

### Ep 3 — Dead Handle

**Filename:** `ren_ep03_dead_handle_v1.jpg`

```text
SUBJECT. Ren standing on an empty night sidewalk, phone in hand showing a chart-ranking app with a generic idol unit silhouette at #1 and a gray "account suspended" banner on a second overlay (fictional UI). His expression cold and still — not screaming. Distant city billboards blurred, same melody shown as ghost waveform leaving the phone. Betrayal realized. Lower third title: "Dead Handle". Fractured Chorus bond episode promo key art… [append style lock]
```

### Ep 4 — No Name, No Face

**Filename:** `ren_ep04_no_name_no_face_v1.jpg`

```text
SUBJECT. Dark room lit only by monitor: anonymous music upload page with blank avatar circle, empty bio, single stem file uploading with visible timestamp, no profile name. Ren's hands on keyboard, face unseen or cropped above frame. Mood: deliberate invisibility, not shame. Cyan progress bar, minimal UI. Lower third title: "No Name, No Face". Fractured Chorus bond episode promo key art… [append style lock]
```

### Ep 5 — Still Keeping the Files

**Filename:** `ren_ep05_still_keeping_the_files_v1.jpg`

```text
SUBJECT. Close composition: external drive and labeled folder "07_masters" on desk beside a faded chat bubble on phone reading "I'll protect the files" (exact quote OK). Ren's grey eyes reflected in screen, neutral restrained pain — he has not deleted anything. No confrontation, no vault, no rescue action. Quiet aftermath. Lower third title: "Still Keeping the Files". Fractured Chorus bond episode promo key art… [append style lock]
```

**QA sau gen:** không HIMA uniform ở ep 1–4 · không Astra/LUXE · không chữ ngoài title · crop thử khung promo Bonds (frame behind image).

---

## 8. Dialogue seeds (EN — bond/VN sau)

**Touya (flashback):**  
“I’ll hold the master. You just write.”

**Ren (inner, flashback):**  
“I trusted the hands more than the contract.”

**Ren (present, từ chối kể):**  
“If I shout, the song becomes evidence. I don’t want that.”

**Ren (present, về page):**  
“It’s not a shrine. It’s a timestamp.”

**Charlotte (off-screen ref only):**  
Không viết full scene ở đây — chỉ note: cô thấy page trước khi Ren thừa nhận Touya.

---

## 9. Âm nhạc & visual cues (khi produce)

- Flashback DAW: waveform xanh **nhạt**, UI cũ, không SyncPod bắt buộc (pre-HIMA).
- Demo 07 motif: 4–8 bar piano/string hook — **reuse motif** khi CREDO xuất hiện ở main game (future).
- Không dùng *Eternal Spark* / LUXE mix trong flashback Ren past.

---

## 10. Out of scope (explicit)

- Arc 4 · Vault Index unlock · boss CREDO · flag `vault_3_cleared`
- Sửa `BondLinkEpisodeCatalog`, `BondState`, promo JPG
- Rescue climax · “Pull Him Back” gameplay
- Touya character lock art / sprite

---

## 11. Self-review

- Placeholder: demo `07` public title TBD when CREDO arc written.
- Không mâu thuẫn Opening: Ren indie playlist; không mind-control.
- Charlotte page: cùng object, khác góc kể — OK.
- User request: past only — ep 5 dừng ở giữ file, không rescue.

---

**Next (when user asks):** gen JPG → `BondPromoCatalog` + `BondLinkEpisodeCatalog` Ren rows · viết EN script từng ep · spec rescue arc tách file.
