# Charlott — Human world v13

**Status:** LOCKED v13 · full crest synced from chibi scan  
**Gen:** `Charlott_human_full_v13_gen.png` · `Charlott_human_chibi_v13_gen.png`  
**CANON:** `Charlott_human_CANON_full_alpha.png` · `Charlott_human_CANON_chibi_alpha.png`

## Crest sync

Logo chibi (anchor + HIMA arc, navy circle) = master. Script quét patch từ chibi, scale ~78%, dán lên full lapel — thay badge sai trên gen full.

`scripts/charlotte_sync_crest_from_chibi.py`

## Pipeline

`scripts/charlotte_v8_finalize.py` — full: crest sync → flood matte; chibi: matte only

## F: drive

- `F:\Factured Chorus\art concept\Charlotte concept\CANON\` — PNG + `HIMA_crest_from_chibi.png`
- `F:\Factured Chorus\art concept\Charlotte concept\docs\` — LOCK + brief

## QA v13

| Check | Full | Chibi |
| ----- | ---- | ----- |
| Logo = chibi scan (anchor + HIMA) | ✓ | ✓ (source) |
| Transparent 1024×682 | ✓ | ✓ |
| shirt_holes = 0 | ✓ | ✓ |
