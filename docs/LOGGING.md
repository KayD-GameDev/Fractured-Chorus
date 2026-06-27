# Cách ghi log — Fractured Chorus (2 dev)

**Cập nhật:** 2026-06-23 · Làm chung với **Thiên** (story) + team trên **GitHub / Notion / Linear**.

---

## Một nguồn sự thật mỗi loại

| Loại nội dung | Canonical | Ai ghi | Mirror / bằng chứng |
|---------------|-----------|--------|---------------------|
| **Task hàng ngày** | [Linear FAC](https://linear.app/factured-chorus-taskboard/team/FAC) | Người làm task | PR GitHub, commit |
| **Nhật ký + quyết định** | [Notion Work Log](https://app.notion.com/p/37441bb3f2a281768901eb58a16bc252) | **Khoa** maintain; mọi người gửi bullet khi Done | `docs/PROJECT_LOG.md` (GitHub) |
| **Story canon** | [Google Doc kit](https://docs.google.com/document/d/18JEBaFeZ3HPVz2HhtkHlEyk1g2o7IRxjhO9eNb6CjD8/edit?usp=sharing) | **Thiên** | `docs/design/STORY_SUMMARY.md` (tóm tắt, không thay Doc) |
| **Design / diagram** | Google Doc + `docs/diagrams/*.drawio` | Khoa, Thiên (story flow) | Notion wiki |
| **Art canon + pipeline** | `assets/characters/*/`*LOCK*, brief | **Khoa** (script + maintain) | `docs/ASSET_INVENTORY.md` |
| **Code gameplay** | `F:\Unity_Project\Fractured Chorus` | Dev code (hiện Khoa) | Issue Linear → PR (khi Unity vào Git) |
| **Snapshot tiến độ** | `docs/PROJECT_STATUS.md` | Khoa / scribe sau audit lớn | — |

**Không** ghi task chỉ Messenger. Chat báo nhanh; việc chính thức → Linear.

---

## Luồng hàng ngày (mọi thành viên, kể cả Thiên)

1. Linear → **In Progress** task của mình.
2. Làm việc (Doc / art / draw.io / Unity / script repo).
3. Cập nhật file canonical (Google Doc tab Story nếu là Thiên, art folder / repo nếu Khoa, v.v.).
4. Linear → **Done**.
5. Gửi **Khoa** 2–5 bullet (Done / Decision / Blocker) **hoặc** tự thêm entry Notion nếu đã có quyền.
6. Khoa (hoặc người làm PR) **prepend** tóm tắt vào `docs/PROJECT_LOG.md` khi có milestone / quyết định quan trọng.

---

## Template entry (`docs/PROJECT_LOG.md`)

Newest first. Copy block này:

```markdown
## YYYY-MM-DD — [tiêu đề ngắn]

**Focus:** art | code | design | audio | production

**Owner:** Thien | Khoa | team

**Done**
- …

**Decisions**
- …

**Blockers**
- …

**Next**
- …

**Refs:** Linear FAC-xx · PR #n · Google Doc section · `path/in/repo`
```

---

## Sync Notion ↔ GitHub

| Khi nào | Việc |
|---------|------|
| Cuối tuần / sau họp | Entry đầy đủ trên **Notion** (database Work Log) |
| Quyết định scope / canon | Thêm **Decision Log** Notion; 1 dòng tóm tắt trong `PROJECT_LOG.md` |
| Merge PR có thay đổi design/code | Checklist PR: tick **PROJECT_LOG updated** nếu cần |
| Thiên cập nhật story lớn | Thiên Done Linear P1-1; Khoa sync `STORY_SUMMARY.md` nếu canon đổi (PR nhỏ) |

---

## Unity vs repo GitHub

| | GitHub `fractured-chorus` | Unity local |
|--|---------------------------|-------------|
| Hiện tại | Docs, art meta, scripts Python, diagrams | **48** script C# combat + audio prototype |
| Log combat session | Tóm tắt trong `PROJECT_LOG.md` | Không commit log riêng |
| Tương lai | Submodule hoặc repo Unity riêng + PR | Push theo Linear issue |

Chi tiết code: [`docs/setup/UNITY_WORKFLOW.md`](setup/UNITY_WORKFLOW.md).

---

## Việc Thiên (story) — ghi log thế nào

- **Không** ghi story canon chỉ trong chat.
- Tab **Story** trên Google Doc = nguồn sự thật.
- Khi đóng task Linear (FAC story): bullet cho Khoa: *đã thêm/sửa arc X, nhân vật Y, Vault Z*.
- Thay đổi ảnh hưởng gameplay → nhắn Khoa để cập nhật draw.io / GDD link trên Notion wiki.

---

## Checklist audit (2026-06-23)

- [x] Canonical log trỏ về **GitHub** (bỏ path `F:\Factured Chorus` — không còn dùng).
- [ ] **First push** GitHub + invite **Thiên** collaborator.
- [ ] Điền URL GitHub vào `docs/LINKS.md` sau khi push.
- [ ] Thiên xác nhận tab Story trên Google Doc đồng bộ milestone P1-1.
