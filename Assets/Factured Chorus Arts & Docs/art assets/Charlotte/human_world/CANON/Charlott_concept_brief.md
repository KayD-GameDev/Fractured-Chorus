# Charlott — Human world v11

**Status:** LOCKED v11 · official HIMA crest · flood matte  
**Gen:** `Charlott_human_full_v11_gen.png` · `Charlott_human_chibi_v11_gen.png`  
**CANON:** `Charlott_human_CANON_full_alpha.png` · `Charlott_human_CANON_chibi_alpha.png`

## Pipeline

`scripts/charlotte_v8_finalize.py` — HIMA crest composite → flood matte → sync F:

## HIMA crest sync

| Form | Center | Size | Rule |
| ---- | ------ | ---- | ---- |
| Full | (545, 160) | 64 | Largest baked badge on viewer-right lapel |
| Chibi | (581, 292) | 57 | Same scale formula (`span × 2.25 + 8`, clamp 48–64) |

Logo source: `refs/HIMA_logo_source.png`

## QA v11

| Check | Full | Chibi |
| ----- | ---- | ----- |
| Transparent 1024×682 | ✓ | ✓ |
| shirt_holes = 0 | ✓ | ✓ |
| Official HIMA on lapel | ✓ | ✓ |
| fringe = 0 | ✓ | ✓ |
