#!/usr/bin/env python3
"""Add or repair EnemyStatusBarUI + CardTemplate in CombatPrototype.unity."""

from __future__ import annotations

import re
import sys
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets/FracturedChorus/Scenes/CombatPrototype.unity"
CARD_ROOT_RECT = 1830421415
COMBAT_CANVAS_RECT = 706293081
SCENE_ROOTS = "--- !u!1660057539 &9223372036854775807"

CARD_TREE_IDS = [
    1830421414, 1830421415, 1830421416, 1830421417,
    275708780, 275708781, 275708782, 275708783,
    566313951, 566313952, 566313953, 566313954,
    194011093, 194011094, 194011095, 194011096,
    1528091572, 1528091573, 1528091574, 1528091575,
    1791716009, 1791716010, 1791716011, 1791716012,
    1153346078, 1153346079, 1153346080, 1153346081,
]

# Safe 32-bit fileIDs (above current scene max ~1.92e9).
BASE = 1_923_000_000
ENEMY_GO = BASE + 100
ENEMY_RECT = BASE + 101
ENEMY_UI = BASE + 102
CARDS_ROW_GO = BASE + 103
CARDS_ROW_RECT = BASE + 104
CARDS_ROW_HLG = BASE + 105
ENEMY_CARD_VIEW = BASE + 3  # maps from 1830421416

ENEMY_STATUS_BAR_SCRIPT = "2477ef845eb8e1f4596711e381cc4e35"
HLG_SCRIPT = "30649d3a9faa99c48a7b1166b86bf2a0"

BROKEN_ID_PREFIXES = (
    "9100100",
    "9294011",
    "9375708",
    "9666313",
    "1025334",
    "1093042",
    "1103346",
)


def build_id_map() -> dict[int, int]:
    mapping = {old: BASE + index for index, old in enumerate(CARD_TREE_IDS, start=1)}
    return mapping


def remove_blocks(text: str, file_ids: set[int]) -> str:
    for fid in sorted(file_ids, reverse=True):
        pattern = rf"--- !u!\d+ &{fid}\n(?:.*?\n)(?=--- !u!|\Z)"
        text = re.sub(pattern, "", text, flags=re.DOTALL)
    return text


def collect_broken_ids(text: str) -> set[int]:
    ids: set[int] = set()
    for match in re.finditer(r"--- !u!\d+ &(\d+)", text):
        value = match.group(1)
        if any(value.startswith(prefix) for prefix in BROKEN_ID_PREFIXES):
            ids.add(int(value))
    return ids


def extract_blocks(text: str, file_ids: list[int]) -> str:
    blocks: list[str] = []
    for fid in file_ids:
        pattern = rf"(--- !u!\d+ &{fid}\n(?:.*?\n)(?=--- !u!))"
        match = re.search(pattern, text, re.DOTALL)
        if not match:
            raise RuntimeError(f"Missing YAML block for fileID {fid}")
        blocks.append(match.group(1).rstrip())
    return "\n".join(blocks)


def remap_card_yaml(yaml: str, id_map: dict[int, int]) -> str:
    for old in sorted(id_map, reverse=True):
        new = id_map[old]
        yaml = yaml.replace(f"&{old}", f"&{new}")
        yaml = yaml.replace(f"{{fileID: {old}}}", f"{{fileID: {new}}}")
    yaml = yaml.replace(f"m_Father: {{fileID: {1326051293}}}", f"m_Father: {{fileID: {ENEMY_RECT}}}")
    return yaml


def build_enemy_shell() -> str:
    return f"""--- !u!1 &{ENEMY_GO}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {ENEMY_RECT}}}
  - component: {{fileID: {ENEMY_UI}}}
  m_Layer: 0
  m_Name: EnemyStatusBarUI
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{ENEMY_RECT}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ENEMY_GO}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {CARDS_ROW_RECT}}}
  - {{fileID: {id_map_rect()}}}
  m_Father: {{fileID: {COMBAT_CANVAS_RECT}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 1, y: 1}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: -12, y: -12}}
  m_SizeDelta: {{x: 713, y: 167}}
  m_Pivot: {{x: 1, y: 1}}
--- !u!114 &{ENEMY_UI}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ENEMY_GO}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ENEMY_STATUS_BAR_SCRIPT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.UI.EnemyStatusBarUIView
  cardsRow: {{fileID: {CARDS_ROW_RECT}}}
  cardTemplate: {{fileID: {ENEMY_CARD_VIEW}}}
  cardSpacing: 2
--- !u!1 &{CARDS_ROW_GO}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {CARDS_ROW_RECT}}}
  - component: {{fileID: {CARDS_ROW_HLG}}}
  m_Layer: 0
  m_Name: CardsRow
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{CARDS_ROW_RECT}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {CARDS_ROW_GO}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {ENEMY_RECT}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}
--- !u!114 &{CARDS_ROW_HLG}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {CARDS_ROW_GO}}}
  m_Enabled: 0
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {HLG_SCRIPT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup
  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: 2
  m_Spacing: 2
  m_ChildForceExpandWidth: 0
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 0
  m_ChildControlHeight: 0
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0"""


def id_map_rect() -> int:
    return build_id_map()[CARD_ROOT_RECT]


def ensure_canvas_child(text: str) -> str:
    child_line = f"  - {{fileID: {ENEMY_RECT}}}"
    if child_line in text:
        return text
    text = re.sub(
        rf"(  - {{fileID: {1326051293}}}\n)",
        rf"\1{child_line}\n",
        text,
        count=1,
    )
    return text


def wire_bootstrap(text: str) -> str:
    return re.sub(
        r"enemyStatusBarView: \{fileID: \d+\}",
        f"enemyStatusBarView: {{fileID: {ENEMY_UI}}}",
        text,
        count=1,
    )


def main() -> None:
    if not SCENE.exists():
        print(f"Scene not found: {SCENE}", file=sys.stderr)
        sys.exit(1)

    text = SCENE.read_text(encoding="utf-8")
    broken = collect_broken_ids(text)
    if broken:
        print(f"Removing {len(broken)} broken enemy-card blocks…")
        text = remove_blocks(text, broken)
        text = re.sub(r"\n  - \{fileID: 9100100002\}\n", "\n", text)
        text = re.sub(r"  - \{fileID: 1326051293\}  - \{fileID: \d+\}", "  - {fileID: 1326051293}", text)

    if f"m_Name: EnemyStatusBarUI" in text and f"&{ENEMY_UI}" in text:
        print("EnemyStatusBarUI already present with valid IDs.")
        SCENE.write_text(text, encoding="utf-8", newline="\n")
        return

    id_map = build_id_map()
    card_yaml = extract_blocks(text, CARD_TREE_IDS)
    card_yaml = remap_card_yaml(card_yaml, id_map)
    shell = build_enemy_shell()
    insertion = f"\n{shell}\n{card_yaml}\n"

    roots_idx = text.index(SCENE_ROOTS)
    text = text[:roots_idx].rstrip() + insertion + "\n" + text[roots_idx:]
    text = ensure_canvas_child(text)
    text = wire_bootstrap(text)

    SCENE.write_text(text, encoding="utf-8", newline="\n")
    print(f"Patched {SCENE.name}: EnemyStatusBarUI + CardTemplate @ Hierarchy (cardTemplate fileID {ENEMY_CARD_VIEW}).")


if __name__ == "__main__":
    main()
