#!/usr/bin/env python3
"""Patch RunMapPrototype.unity legend: layout, round dots, colors, fonts."""

from __future__ import annotations

import re
import sys
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets/FracturedChorus/Scenes/RunMapPrototype.unity"

GUID_VLG = "59f8146938fff824cb5fd77236b75775"
GUID_HLG = "30649d3a9faa99c48a7b1166b86bf2a0"
GUID_LE = "306cc8c2b49d7114eaa3623786fc2126"
GUID_ARF = "86710e43de46f6f4bac7c8e50813a599"
GUID_IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc"
GUID_LEGEND_VIEW = "6a77eceb8db184542b3c71cc2918c187"
GUID_SCROLL = "4060a8beceb11474ab125ee53b1fe822"
CIRCLE_SPRITE = "{fileID: 2075944761}"
MAP_SCROLL_GO = 991252778
SCROLL_RECT_ID = 991252779

DOT = 34.0
INSET = round(DOT * (3.0 / 36.0), 5)
V_SPACING = 14.5
H_SPACING = 18.5
ROW_H = 62.0

STROKES = {
    "Battle": (0.6666667, 0.30588236, 0.28627452, 1),
    "Event": (0.50980395, 0.7019608, 0.4, 1),
    "Elite": (0.4745098, 0.37254903, 0.5254902, 1),
    "Camp": (0.8392157, 0.7137255, 0.34117648, 1),
    "Relay": (0.8431373, 0.60784316, 0.0, 1),
    "Treasure": (0.4392157, 0.5686275, 0.7529412, 1),
    "Boss": (0.7529412, 0.27450982, 0.24313726, 1),
}

FILLS = {
    "Battle": (0.8392157, 0.654902, 0.6431373, 1),
    "Event": (0.754902, 0.8509804, 0.69803923, 1),
    "Elite": (0.7372549, 0.69803923, 0.7372549, 1),
    "Camp": (0.9196079, 0.8627451, 0.67058825, 1),
    "Relay": (0.92156863, 0.8039216, 0.5, 1),
    "Treasure": (0.71960784, 0.7921569, 0.8784314, 1),
    "Boss": (0.12, 0.12, 0.2, 1),
}


def next_id(used: set[int], start: int = 920_000_000) -> int:
    while start in used:
        start += 1
    used.add(start)
    return start


def parse_go_names(content: str) -> dict[int, str]:
    names: dict[int, str] = {}
    for m in re.finditer(r"--- !u!1 &(\d+)\nGameObject:.*?m_Name: ([^\n]+)", content, re.S):
        names[int(m.group(1))] = m.group(1).strip() if False else m.group(2).strip()
    return names


def parse_rect_fathers(content: str) -> dict[int, int]:
    """rect fileID -> parent rect fileID"""
    fathers: dict[int, int] = {}
    for m in re.finditer(r"--- !u!224 &(\d+)\nRectTransform:.*?m_GameObject: \{fileID: (\d+)\}.*?m_Father: \{fileID: (\d+)\}", content, re.S):
        fathers[int(m.group(1))] = int(m.group(3))
    return fathers


def parse_go_components(content: str) -> dict[int, list[int]]:
    comps: dict[int, list[int]] = {}
    for m in re.finditer(r"--- !u!1 &(\d+)\nGameObject:.*?m_Component:\n((?:  - component: \{fileID: \d+\}\n)+)", content, re.S):
        go_id = int(m.group(1))
        comps[go_id] = [int(x) for x in re.findall(r"fileID: (\d+)", m.group(2))]
    return comps


def rect_for_go(content: str, go_id: int) -> int | None:
    """RectTransform fileID for a GameObject — scoped to one YAML block (no cross-block lazy match)."""
    for m in re.finditer(r"--- !u!224 &(\d+)\nRectTransform:(.*?)(?=\n--- !u!|\Z)", content, re.S):
        if re.search(rf"m_GameObject: {{fileID: {go_id}}}\n", m.group(2)):
            return int(m.group(1))
    return None


def append_component_refs(go_block: str, comp_ids: list[int]) -> str:
    """Append component fileIDs after existing m_Component entries."""
    lines = go_block.split("\n")
    insert_at = None
    for i, line in enumerate(lines):
        if line.startswith("  - component:"):
            insert_at = i + 1
    if insert_at is None:
        return go_block
    new_lines = lines[:insert_at] + [f"  - component: {{fileID: {cid}}}" for cid in comp_ids] + lines[insert_at:]
    return "\n".join(new_lines)


def rect_block(content: str, rect_id: int) -> str | None:
    m = re.search(rf"--- !u!224 &{rect_id}\nRectTransform:(.*?)(?=\n--- !u!|\Z)", content, re.S)
    return m.group(0) if m else None


def patch_rect_field(content: str, rect_id: int, field: str, value: str) -> str:
    block = rect_block(content, rect_id)
    if not block:
        return content
    new_block = re.sub(
        rf"({field}: ){{x: [^,]+, y: [^}}]+}}",
        rf"\1{{x: {value}}}" if "," not in value else rf"\1{{{value}}}",
        block,
        count=1,
    )
    if field == "m_SizeDelta":
        new_block = re.sub(
            rf"(m_SizeDelta: ){{x: [^,]+, y: [^}}]+}}",
            rf"\1{{x: {value}}}",
            new_block,
            count=1,
        )
    return content.replace(block, new_block, 1)


def image_block(comp_id: int, cr_id: int, go_id: int, rect_id: int, father_rect: int, color, name: str, offset_min=(0, 0), offset_max=(0, 0)) -> str:
    r, g, b, a = color
    ox0, oy0 = offset_min
    ox1, oy1 = offset_max
    return f"""--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {rect_id}}}
  - component: {{fileID: {cr_id}}}
  - component: {{fileID: {comp_id}}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{rect_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {father_rect}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}
  m_OffsetMin: {{x: {ox0}, y: {oy0}}}
  m_OffsetMax: {{x: {ox1}, y: {oy1}}}
--- !u!222 &{cr_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_CullTransparentMesh: 1
--- !u!114 &{comp_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_IMAGE}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {{r: {r}, g: {g}, b: {b}, a: {a}}}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {CIRCLE_SPRITE}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
"""


def patch_scroll_driver(content: str, used: set[int]) -> tuple[str, str]:
    if "RunMapScrollDriver" in content:
        return content, ""
    driver_id = next_id(used)
    append = f"""
--- !u!114 &{driver_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {MAP_SCROLL_GO}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_SCROLL}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.RunMap.UI.RunMapScrollDriver
  scrollRect: {{fileID: {SCROLL_RECT_ID}}}
  scrollSpeedScale: 0.5
  scrollSensitivity: 42
  decelerationRate: 0.038
  elasticity: 0.04
  smoothTime: 0.36
  initialScrollSmoothTime: 0.28
"""
    scroll_go_block = re.search(rf"--- !u!1 &{MAP_SCROLL_GO}\nGameObject:.*?(?=\n--- !u!)", content, re.S)
    if scroll_go_block:
        new_block = append_component_refs(scroll_go_block.group(0), [driver_id])
        content = content.replace(scroll_go_block.group(0), new_block, 1)
    if "scrollDriver:" not in content:
        content = content.replace(
            f"  scrollRect: {{fileID: {SCROLL_RECT_ID}}}\n",
            f"  scrollRect: {{fileID: {SCROLL_RECT_ID}}}\n  scrollDriver: {{fileID: {driver_id}}}\n",
            1,
        )
    content = re.sub(
        r"(m_EditorClassIdentifier: UnityEngine\.UI::UnityEngine\.UI\.ScrollRect\n.*?m_ScrollSensitivity: )[\d.]+",
        r"\g<1>21",
        content,
        count=1,
        flags=re.S,
    )
    return content, append


def ensure_dot_stroke_fill(content: str, dot_go: int, dot_rect: int, stroke, fill, used: set[int]) -> tuple[str, str]:
    dot_block = rect_block(content, dot_rect)
    if not dot_block:
        return content, ""
    # Already has Stroke child under this dot rect
    if re.search(r"m_Father: \{fileID: " + str(dot_rect) + r"\}", content):
        for m in re.finditer(r"--- !u!224 &(\d+)\nRectTransform:.*?m_Father: \{fileID: " + str(dot_rect) + r"\}", content, re.S):
            child_go = re.search(rf"--- !u!224 &{m.group(1)}\nRectTransform:.*?m_GameObject: {{fileID: (\d+)}}", content, re.S)
            if child_go and parse_go_names(content).get(int(child_go.group(1))) == "Stroke":
                return content, ""
    stroke_go = next_id(used)
    stroke_rect = next_id(used)
    stroke_cr = next_id(used)
    stroke_img = next_id(used)
    fill_go = next_id(used)
    fill_rect = next_id(used)
    fill_cr = next_id(used)
    fill_img = next_id(used)
    append = image_block(stroke_img, stroke_cr, stroke_go, stroke_rect, dot_rect, stroke, "Stroke")
    append += image_block(fill_img, fill_cr, fill_go, fill_rect, dot_rect, fill, "Fill", (INSET, INSET), (-INSET, -INSET))
    new_dot_block = re.sub(
        r"m_Children:\n(?:  - \{fileID: \d+\}\n)*",
        f"m_Children:\n  - {{fileID: {stroke_rect}}}\n  - {{fileID: {fill_rect}}}\n",
        dot_block,
        count=1,
    )
    content = content.replace(dot_block, new_dot_block, 1)
    content = re.sub(
        rf"(--- !u!114 &\d+\nMonoBehaviour:.*?m_GameObject: {{fileID: {dot_go}}}.*?m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image.*?m_Enabled: )1",
        r"\g<1>0",
        content,
        count=1,
        flags=re.S,
    )
    return content, append


def reorder_legend_panel_components(content: str, legend_panel_go: int) -> str:
    block = re.search(rf"--- !u!1 &{legend_panel_go}\nGameObject:.*?(?=\n--- !u!)", content, re.S)
    if not block:
        return content
    old = block.group(0)
    ids = [int(x) for x in re.findall(r"component: \{fileID: (\d+)\}", old)]
    extras = [i for i in ids if i in (920000000, 920000001)]
    base = [i for i in ids if i not in extras]
    if not extras or base + extras == ids:
        return content
    ordered = base + extras
    new_lines = []
    comp_iter = iter(ordered)
    for line in old.split("\n"):
        if line.startswith("  - component:"):
            new_lines.append(f"  - component: {{fileID: {next(comp_iter)}}}")
        else:
            new_lines.append(line)
    return content.replace(old, "\n".join(new_lines), 1)


def block_rect(rect_id: int, go_id: int, father_id: int, anchor_min, anchor_max, pivot, pos, size, children=None) -> str:
    child_lines = ""
    if children:
        child_lines = "".join(f"  - {{fileID: {c}}}\n" for c in children)
    return f"""--- !u!224 &{rect_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{child_lines}  m_Father: {{fileID: {father_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {anchor_min[0]}, y: {anchor_min[1]}}}
  m_AnchorMax: {{x: {anchor_max[0]}, y: {anchor_max[1]}}}
  m_AnchoredPosition: {{x: {pos[0]}, y: {pos[1]}}}
  m_SizeDelta: {{x: {size[0]}, y: {size[1]}}}
  m_Pivot: {{x: {pivot[0]}, y: {pivot[1]}}}
"""


def block_image(comp_id: int, cr_id: int, go_id: int, color) -> str:
    r, g, b, a = color
    return f"""--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {comp_id - 2}}}
  - component: {{fileID: {cr_id}}}
  - component: {{fileID: {comp_id}}}
  m_Layer: 0
  m_Name: PLACEHOLDER
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""


def main() -> int:
    content = SCENE.read_text(encoding="utf-8")
    used = {int(x) for x in re.findall(r"--- !u!\d+ &(\d+)", content)}
    names = parse_go_names(content)

    legend_panel_go = next((gid for gid, n in names.items() if n == "LegendPanel"), None)
    if legend_panel_go is None:
        print("LegendPanel not found", file=sys.stderr)
        return 1

    panel_rect = rect_for_go(content, legend_panel_go)
    if panel_rect is None:
        print("LegendPanel RectTransform not found", file=sys.stderr)
        return 1

    # LegendPanel: add VLG + RunMapLegendPanelView if missing
    panel_comps = parse_go_components(content).get(legend_panel_go, [])
    append_blocks = ""

    if not re.search(r"VerticalLayoutGroup", content[content.find(f"&{legend_panel_go}\n"):content.find(f"&{legend_panel_go}\n") + 800] if False else content):
        pass

    has_vlg = "VerticalLayoutGroup" in content and f"m_GameObject: {{fileID: {legend_panel_go}}}" in content
    # simpler: always inject if not on panel go block
    panel_go_block = re.search(rf"--- !u!1 &{legend_panel_go}\nGameObject:.*?(?=\n--- !u!)", content, re.S)
    has_legend_view = re.search(
        rf"m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.RunMap.UI.RunMapLegendPanelView[\s\S]*?m_GameObject: {{fileID: {legend_panel_go}}}",
        content,
    )
    if panel_go_block and not has_legend_view:
        vlg_id = next_id(used)
        legend_view_id = next_id(used)
        # update component list on panel GO
        old = panel_go_block.group(0)
        new = append_component_refs(old, [vlg_id, legend_view_id])
        content = content.replace(old, new, 1)
        append_blocks += f"""
--- !u!114 &{vlg_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {legend_panel_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_VLG}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup
  m_Padding:
    m_Left: 22
    m_Right: 22
    m_Top: 28
    m_Bottom: 22
  m_ChildAlignment: 0
  m_Spacing: {V_SPACING}
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 1
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
--- !u!114 &{legend_view_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {legend_panel_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_LEGEND_VIEW}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.RunMap.UI.RunMapLegendPanelView
"""

    # Legend rows
    row_types = []
    for go_id, name in names.items():
        if name.startswith("Legend_") and name not in ("LegendTitle", "LegendSpacer"):
            row_types.append((go_id, name.replace("Legend_", "")))

    for row_go, type_name in row_types:
        row_rect = rect_for_go(content, row_go)
        if row_rect is None:
            continue

        # row rect layout
        content = re.sub(
            rf"(--- !u!224 &{row_rect}\nRectTransform:.*?m_AnchorMin: ){{x: [^,]+, y: [^}}]+}}",
            rf"\1{{x: 0, y: 1}}",
            content,
            count=1,
            flags=re.S,
        )
        content = re.sub(
            rf"(--- !u!224 &{row_rect}\nRectTransform:.*?m_AnchorMax: ){{x: [^,]+, y: [^}}]+}}",
            rf"\1{{x: 1, y: 1}}",
            content,
            count=1,
            flags=re.S,
        )
        content = re.sub(
            rf"(--- !u!224 &{row_rect}\nRectTransform:.*?m_Pivot: ){{x: [^,]+, y: [^}}]+}}",
            rf"\1{{x: 0.5, y: 1}}",
            content,
            count=1,
            flags=re.S,
        )
        content = re.sub(
            rf"(--- !u!224 &{row_rect}\nRectTransform:.*?m_AnchoredPosition: ){{x: [^,]+, y: [^}}]+}}",
            rf"\1{{x: 0, y: 0}}",
            content,
            count=1,
            flags=re.S,
        )
        content = re.sub(
            rf"(--- !u!224 &{row_rect}\nRectTransform:.*?m_SizeDelta: ){{x: [^,]+, y: [^}}]+}}",
            rf"\1{{x: 0, y: {ROW_H}}}",
            content,
            count=1,
            flags=re.S,
        )

        row_block = re.search(rf"--- !u!1 &{row_go}\nGameObject:.*?(?=\n--- !u!)", content, re.S)
        if row_block and "HorizontalLayoutGroup" not in row_block.group(0):
            hlg_id = next_id(used)
            le_id = next_id(used)
            old = row_block.group(0)
            new = append_component_refs(old, [hlg_id, le_id])
            content = content.replace(old, new, 1)
            append_blocks += f"""
--- !u!114 &{hlg_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {row_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_HLG}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup
  m_Padding:
    m_Left: 6
    m_Right: 6
    m_Top: 4
    m_Bottom: 4
  m_ChildAlignment: 3
  m_Spacing: {H_SPACING}
  m_ChildForceExpandWidth: 0
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 1
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
--- !u!114 &{le_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {row_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_LE}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.LayoutElement
  m_IgnoreLayout: 0
  m_MinWidth: -1
  m_MinHeight: {ROW_H}
  m_PreferredWidth: -1
  m_PreferredHeight: -1
  m_FlexibleWidth: -1
  m_FlexibleHeight: 1
  m_LayoutPriority: 1
"""

        # find Dot child of row
        dot_go = next((gid for gid, n in names.items() if n == "Dot" and rect_for_go(content, gid) and _is_child(content, row_rect, rect_for_go(content, gid))), None)
        if dot_go is None:
            # find dot by father = row_rect
            for m in re.finditer(r"--- !u!224 &(\d+)\nRectTransform:.*?m_GameObject: \{fileID: (\d+)\}.*?m_Father: \{fileID: " + str(row_rect) + r"\}", content, re.S):
                go = int(m.group(2))
                if names.get(go) == "Dot":
                    dot_go = go
                    break
        if dot_go is None:
            continue

        dot_rect = rect_for_go(content, dot_go)
        stroke = STROKES.get(type_name, (0.5, 0.5, 0.5, 1))
        fill = FILLS.get(type_name, (0.8, 0.8, 0.8, 1))

        # fix dot rect square
        content = re.sub(
            rf"(--- !u!224 &{dot_rect}\nRectTransform:.*?m_SizeDelta: ){{x: [^,]+, y: [^}}]+}}",
            rf"\1{{x: {DOT}, y: {DOT}}}",
            content,
            count=1,
            flags=re.S,
        )

        dot_block = re.search(rf"--- !u!1 &{dot_go}\nGameObject:.*?(?=\n--- !u!)", content, re.S)
        if dot_block:
            old = dot_block.group(0)
            if "LayoutElement" not in old:
                le_dot = next_id(used)
                arf_dot = next_id(used)
                new = append_component_refs(old, [le_dot, arf_dot])
                content = content.replace(old, new, 1)
                append_blocks += f"""
--- !u!114 &{le_dot}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {dot_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_LE}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.LayoutElement
  m_IgnoreLayout: 0
  m_MinWidth: {DOT}
  m_MinHeight: {DOT}
  m_PreferredWidth: {DOT}
  m_PreferredHeight: {DOT}
  m_FlexibleWidth: 0
  m_FlexibleHeight: 0
  m_LayoutPriority: 1
--- !u!114 &{arf_dot}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {dot_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_ARF}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.AspectRatioFitter
  m_AspectMode: 1
  m_AspectRatio: 1
"""

            # disable legacy root image on dot
            content = re.sub(
                rf"(--- !u!114 &(\d+)\nMonoBehaviour:.*?m_GameObject: {{fileID: {dot_go}}}.*?m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image.*?m_Enabled: )1",
                r"\g<1>0",
                content,
                count=1,
                flags=re.S,
            )

        content, stroke_append = ensure_dot_stroke_fill(content, dot_go, dot_rect, stroke, fill, used)
        append_blocks += stroke_append

        # Desc font 20
        for m in re.finditer(rf"--- !u!224 &(\d+)\nRectTransform:.*?m_GameObject: {{fileID: (\d+)}}.*?m_Father: {{fileID: {row_rect}}}", content, re.S):
            desc_go = int(m.group(2))
            if names.get(desc_go) != "Desc":
                continue
            content = re.sub(
                rf"(--- !u!114 &\d+\nMonoBehaviour:.*?m_GameObject: {{fileID: {desc_go}}}.*?m_FontSize: )\d+",
                r"\g<1>20",
                content,
                count=1,
                flags=re.S,
            )

    # Title + Hint fonts
    for go_id, name in names.items():
        if name == "LegendTitle":
            content = re.sub(
                rf"(--- !u!114 &\d+\nMonoBehaviour:.*?m_GameObject: {{fileID: {go_id}}}.*?m_FontSize: )\d+",
                r"\g<1>24",
                content,
                count=1,
                flags=re.S,
            )
        if name == "Hint":
            content = re.sub(
                rf"(--- !u!114 &\d+\nMonoBehaviour:.*?m_GameObject: {{fileID: {go_id}}}.*?m_FontSize: )\d+",
                r"\g<1>18",
                content,
                count=1,
                flags=re.S,
            )

    content, scroll_append = patch_scroll_driver(content, used)
    append_blocks += scroll_append
    content = reorder_legend_panel_components(content, legend_panel_go)

    if append_blocks:
        content = content.rstrip() + "\n" + append_blocks

    SCENE.write_text(content, encoding="utf-8", newline="\n")
    print(f"Patched {SCENE} — reload scene in Unity if open.")
    return 0


def _is_child(content: str, parent_rect: int, child_rect: int | None) -> bool:
    if child_rect is None:
        return False
    m = re.search(rf"--- !u!224 &{child_rect}\nRectTransform:.*?m_Father: {{fileID: (\d+)}}", content, re.S)
    return m is not None and int(m.group(1)) == parent_rect


if __name__ == "__main__":
    raise SystemExit(main())
