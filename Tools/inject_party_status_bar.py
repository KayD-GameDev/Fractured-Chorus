from __future__ import annotations

import re
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets/FracturedChorus/Scenes/CombatPrototype.unity"

SCRIPT_PARTY_BAR = "8d9ae2e87cdbd43439668cba80ee80d0"
SCRIPT_PARTY_CARD = "49500af8a17d81c49ab6d91fb9e02c06"
SCRIPT_IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc"
SCRIPT_HLG = "30649d3a9faa99c48a7b1166b86bf2a0"
SCRIPT_LAYOUT = "306cc8c2b49d7114eaa3623786fc2126"

CANVAS_RECT = 706293081
BOOTSTRAP_BEHAVIOUR = 707156460

CARD_W = 78
CARD_H = 98
SPACING = 8
ROOT_W = CARD_W * 3 + SPACING * 2 + 16
ROOT_H = CARD_H


def uid(n: int) -> int:
    return 1_810_000_000 + n


def go_block(go_id: int, name: str, components: list[int]) -> str:
    comp_lines = "\n".join(f"  - component: {{fileID: {c}}}" for c in components)
    return f"""--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comp_lines}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""


def rect_block(
    rect_id: int,
    go_id: int,
    father: int,
    children: list[int],
    anchor_min: tuple,
    anchor_max: tuple,
    pivot: tuple,
    anchored: tuple,
    size: tuple,
) -> str:
    child_lines = "\n".join(f"  - {{fileID: {c}}}" for c in children) if children else ""
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
{child_lines}
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {anchor_min[0]}, y: {anchor_min[1]}}}
  m_AnchorMax: {{x: {anchor_max[0]}, y: {anchor_max[1]}}}
  m_AnchoredPosition: {{x: {anchored[0]}, y: {anchored[1]}}}
  m_SizeDelta: {{x: {size[0]}, y: {size[1]}}}
  m_Pivot: {{x: {pivot[0]}, y: {pivot[1]}}}
"""


def canvas_renderer_block(cr_id: int, go_id: int) -> str:
    return f"""--- !u!222 &{cr_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_CullTransparentMesh: 1
"""


def image_block(
    img_id: int,
    go_id: int,
    color: tuple,
    raycast: int = 0,
    preserve_aspect: int = 0,
    enabled: int = 1,
    filled: bool = False,
    fill_amount: float = 1.0,
) -> str:
    img_type = 3 if filled else 0
    fill_method = 0 if filled else 4
    return f"""--- !u!114 &{img_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: {enabled}
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_IMAGE}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {{r: {color[0]}, g: {color[1]}, b: {color[2]}, a: {color[3]}}}
  m_RaycastTarget: {raycast}
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 0}}
  m_Type: {img_type}
  m_PreserveAspect: {preserve_aspect}
  m_FillCenter: 1
  m_FillMethod: {fill_method}
  m_FillAmount: {fill_amount}
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
"""


def build_card(index: int, father_rect: int) -> tuple[str, int, int]:
    base = 100 + index * 100
    go = uid(base + 1)
    rect = uid(base + 2)
    layout = uid(base + 3)
    card_view = uid(base + 4)

    frame_go = uid(base + 11)
    frame_rect = uid(base + 12)
    frame_img = uid(base + 13)
    frame_cr = uid(base + 14)

    av_root_go = uid(base + 21)
    av_root_rect = uid(base + 22)

    av_go = uid(base + 31)
    av_rect = uid(base + 32)
    av_img = uid(base + 33)
    av_cr = uid(base + 34)

    el_go = uid(base + 41)
    el_rect = uid(base + 42)
    el_img = uid(base + 43)
    el_cr = uid(base + 44)

    hp_go = uid(base + 51)
    hp_rect = uid(base + 52)
    hp_img = uid(base + 53)
    hp_cr = uid(base + 54)

    fill_go = uid(base + 61)
    fill_rect = uid(base + 62)
    fill_img = uid(base + 63)
    fill_cr = uid(base + 64)

    parts = [
        go_block(go, f"PartyCard_{index}", [rect, layout, card_view]),
        rect_block(
            rect,
            go,
            father_rect,
            [frame_rect, av_root_rect, hp_rect],
            (0, 1),
            (0, 1),
            (0.5, 1),
            (0, 0),
            (CARD_W, CARD_H),
        ),
        f"""--- !u!114 &{layout}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_LAYOUT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.LayoutElement
  m_IgnoreLayout: 0
  m_MinWidth: -1
  m_MinHeight: -1
  m_PreferredWidth: {CARD_W}
  m_PreferredHeight: {CARD_H}
  m_FlexibleWidth: 0
  m_FlexibleHeight: 0
  m_LayoutPriority: 1
""",
        f"""--- !u!114 &{card_view}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_PARTY_CARD}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.UI.PartyUnitCardView
  frame: {{fileID: {frame_img}}}
  avatar: {{fileID: {av_img}}}
  elementIcon: {{fileID: {el_img}}}
  hpFill: {{fileID: {fill_img}}}
  hpBackground: {{fileID: {hp_img}}}
  preserveSceneImages: 1
""",
        go_block(frame_go, "Frame", [frame_rect, frame_img, frame_cr]),
        rect_block(
            frame_rect,
            frame_go,
            rect,
            [],
            (0, 0),
            (1, 1),
            (0.5, 0.5),
            (0, 0),
            (0, 0),
        ),
        image_block(frame_img, frame_go, (0.08, 0.12, 0.28, 0.94)),
        canvas_renderer_block(frame_cr, frame_go),
        go_block(av_root_go, "AvatarRoot", [av_root_rect]),
        rect_block(
            av_root_rect,
            av_root_go,
            rect,
            [av_rect, el_rect],
            (0.5, 1),
            (0.5, 1),
            (0.5, 1),
            (0, -6),
            (68, 68),
        ),
        go_block(av_go, "Avatar", [av_rect, av_img, av_cr]),
        rect_block(
            av_rect,
            av_go,
            av_root_rect,
            [],
            (0, 0),
            (1, 1),
            (0.5, 0.5),
            (0, 0),
            (0, 0),
        ),
        image_block(av_img, av_go, (0.25, 0.28, 0.35, 1), preserve_aspect=1),
        canvas_renderer_block(av_cr, av_go),
        go_block(el_go, "ElementIcon", [el_rect, el_img, el_cr]),
        rect_block(
            el_rect,
            el_go,
            av_root_rect,
            [],
            (1, 1),
            (1, 1),
            (1, 1),
            (4, 4),
            (24, 24),
        ),
        image_block(el_img, el_go, (0.95, 0.82, 0.25, 1), enabled=0),
        canvas_renderer_block(el_cr, el_go),
        go_block(hp_go, "HpBar", [hp_rect, hp_img, hp_cr]),
        rect_block(
            hp_rect,
            hp_go,
            rect,
            [fill_rect],
            (0, 0),
            (1, 0),
            (0.5, 0),
            (0, 6),
            (-12, 10),
        ),
        image_block(hp_img, hp_go, (0.04, 0.06, 0.1, 0.95)),
        canvas_renderer_block(hp_cr, hp_go),
        go_block(fill_go, "Fill", [fill_rect, fill_img, fill_cr]),
        rect_block(
            fill_rect,
            fill_go,
            hp_rect,
            [],
            (0, 0),
            (1, 1),
            (0.5, 0.5),
            (0, 0),
            (0, 0),
        ),
        image_block(fill_img, fill_go, (0.25, 0.88, 0.35, 1), filled=True),
        canvas_renderer_block(fill_cr, fill_go),
    ]

    return "\n".join(parts), rect, card_view


def build_all() -> str:
    bar_go = uid(1)
    bar_rect = uid(2)
    bar_view = uid(3)

    row_go = uid(11)
    row_rect = uid(12)
    row_hlg = uid(13)

    card_rects = []
    card_views = []
    card_yaml_parts = []
    for i in range(3):
        yaml_part, card_rect, card_view = build_card(i, row_rect)
        card_yaml_parts.append(yaml_part)
        card_rects.append(card_rect)
        card_views.append(card_view)

    card_lines = "\n".join(f"  - {{fileID: {r}}}" for r in card_rects)

    parts = [
        go_block(bar_go, "PartyStatusBarUI", [bar_rect, bar_view]),
        f"""--- !u!224 &{bar_rect}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {bar_go}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {row_rect}}}
  m_Father: {{fileID: {CANVAS_RECT}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 1}}
  m_AnchorMax: {{x: 0, y: 1}}
  m_AnchoredPosition: {{x: 16, y: -16}}
  m_SizeDelta: {{x: {ROOT_W}, y: {ROOT_H}}}
  m_Pivot: {{x: 0, y: 1}}
""",
        f"""--- !u!114 &{bar_view}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {bar_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_PARTY_BAR}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.UI.PartyStatusBarUIView
  cardsRow: {{fileID: {row_rect}}}
  cardTemplate: {{fileID: {card_views[0]}}}
  sceneCards:
  - {{fileID: {card_views[0]}}}
  - {{fileID: {card_views[1]}}}
  - {{fileID: {card_views[2]}}}
  preserveSceneLayout: 1
  useSceneHierarchyOnly: 1
""",
        go_block(row_go, "CardsRow", [row_rect, row_hlg]),
        f"""--- !u!224 &{row_rect}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {row_go}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{card_lines}
  m_Father: {{fileID: {bar_rect}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}
""",
        f"""--- !u!114 &{row_hlg}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {row_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_HLG}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup
  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: 0
  m_Spacing: {SPACING}
  m_ChildForceExpandWidth: 0
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 0
  m_ChildControlHeight: 0
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
""",
        *card_yaml_parts,
    ]

    return "\n".join(parts), bar_view


def main() -> None:
    text = SCENE.read_text(encoding="utf-8")

    if "PartyStatusBarUI" in text:
        text = re.sub(
            r"--- !u!1 &1810000001\nGameObject:[\s\S]*?(?=--- !u!1660057539)",
            "",
            text,
            count=1,
        )
        text = text.replace("  - {fileID: 1810000002}\n", "")

    yaml, bar_view = build_all()

    canvas_children = """  m_Children:
  - {fileID: 1236853533}
  - {fileID: 463385829}
  - {fileID: 2120607401}"""
    canvas_children_new = canvas_children + f"\n  - {{fileID: {uid(2)}}}"
    if canvas_children_new not in text:
        text = text.replace(canvas_children, canvas_children_new, 1)

    text = text.replace(
        "  partyStatusBar: {fileID: 0}",
        f"  partyStatusBar: {{fileID: {bar_view}}}",
        1,
    )

    marker = "--- !u!1660057539 &9223372036854775807"
    if yaml.strip() not in text:
        text = text.replace(marker, yaml + "\n" + marker, 1)

    SCENE.write_text(text, encoding="utf-8", newline="\n")
    print(f"Injected PartyStatusBarUI into {SCENE}")
    print(f"Bootstrap partyStatusBar -> {bar_view}")


if __name__ == "__main__":
    main()
