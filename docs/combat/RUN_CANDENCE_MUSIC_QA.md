# QA — Run Candence Music

> **Ngày:** 2026-08-12 · **Spec:** [`2026-08-12-run-candence-music-design.md`](../superpowers/specs/2026-08-12-run-candence-music-design.md)

Chạy trên `RunMapPrototype.unity` + `CombatPrototype.unity`. Đánh dấu ✅ / ❌.

| # | Kiểm tra | Kỳ vọng |
|---|----------|---------|
| 1 | Chọn Pinky Vault | Candence play ngay, beat 0 |
| 2 | Đi node map ~30s | Vol ~40%, nhịp đều |
| 3 | Battle node | Vol 100%, timeline sync ngay, **không intro scan** |
| 4 | Planning window | Duck 70% + trầm |
| 5 | Execute | Full volume |
| 6 | Thắng → map | Vol 40%, beat liên tục |
| 7 | Boss node | BossRemix + intro 12 beat; Candence im |
| 8 | Thắng boss → map | Candence resume đúng beat pre-boss |
| 9 | Escape → Hub | Candence stop; `The_Locked_Vault` chạy |
| 10 | Run >272s | Loop mượt, beat không nhảy lùi |
| 11 | Boss scene playtest riêng | Không regression 677 beat / intro |
