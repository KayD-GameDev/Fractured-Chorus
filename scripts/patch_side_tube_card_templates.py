#!/usr/bin/env python3
"""Patch CombatPrototype party/enemy CardTemplates to compact side-tube layout."""
from __future__ import annotations

from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets/FracturedChorus/Scenes/CombatPrototype.unity"
GUID_TRACK = "7e4a1c9b2f8d4e6a9b0c1d2e3f4a5b6c"
GUID_FILL = "8f5b2d0c3e9a4f7b8c1d2e3f4a5b6c7d"
GREEN = "{r: 0.18, g: 0.92, b: 0.28, a: 1}"
MAGENTA = "{r: 1, g: 0.23921569, b: 0.6509804, a: 1}"


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError(f"Missing snippet for {label}")
    return text.replace(old, new, 1)


def image_block(
    go_id: int,
    name: str,
    father: int,
    sprite_guid: str,
    color: str,
    img_type: int,
    fill_method: int,
    children: str,
) -> str:
    rect_id = go_id + 1
    img_id = go_id + 2
    cr_id = go_id + 3
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
  - component: {{fileID: {img_id}}}
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
  m_Children:{children}
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}
--- !u!114 &{img_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {color}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 21300000, guid: {sprite_guid}, type: 3}}
  m_Type: {img_type}
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: {fill_method}
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!222 &{cr_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_CullTransparentMesh: 1
"""


def tick_block(go_id: int, name: str, father: int, y_anchor: float) -> str:
    rect_id = go_id + 1
    img_id = go_id + 2
    cr_id = go_id + 3
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
  - component: {{fileID: {img_id}}}
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
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0.18, y: {y_anchor}}}
  m_AnchorMax: {{x: 0.82, y: {y_anchor}}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 1.5}}
  m_Pivot: {{x: 0.5, y: 0.5}}
--- !u!114 &{img_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 0.35}}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 0}}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!222 &{cr_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_CullTransparentMesh: 1
"""


def main() -> None:
    text = SCENE.read_text(encoding="utf-8")

    text = replace_once(
        text,
        "  m_AnchoredPosition: {x: -1.3, y: 0}\n  m_SizeDelta: {x: 214.71, y: 105.56}\n  m_Pivot: {x: 0, y: 1}",
        "  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 160, y: 118}\n  m_Pivot: {x: 0, y: 1}",
        "party CardTemplate size",
    )
    text = replace_once(
        text,
        "  m_SizeDelta: {x: 240, y: 118}\n  m_Pivot: {x: 1, y: 1}",
        "  m_SizeDelta: {x: 160, y: 118}\n  m_Pivot: {x: 1, y: 1}",
        "enemy CardTemplate size",
    )
    text = text.replace("  m_PreferredWidth: 240\n  m_PreferredHeight: 118", "  m_PreferredWidth: 160\n  m_PreferredHeight: 118")

    text = replace_once(
        text,
        "  m_AnchorMin: {x: 0, y: 0.5}\n  m_AnchorMax: {x: 0, y: 0.5}\n  m_AnchoredPosition: {x: 46, y: 6}\n  m_SizeDelta: {x: 80, y: 102}",
        "  m_AnchorMin: {x: 0, y: 0.5}\n  m_AnchorMax: {x: 0, y: 0.5}\n  m_AnchoredPosition: {x: 56, y: 8}\n  m_SizeDelta: {x: 96, y: 96}",
        "party avatar",
    )
    text = replace_once(
        text,
        "  m_AnchorMin: {x: 1, y: 0.5}\n  m_AnchorMax: {x: 1, y: 0.5}\n  m_AnchoredPosition: {x: -46, y: 6}\n  m_SizeDelta: {x: 80, y: 102}",
        "  m_AnchorMin: {x: 0, y: 0.5}\n  m_AnchorMax: {x: 0, y: 0.5}\n  m_AnchoredPosition: {x: 56, y: 8}\n  m_SizeDelta: {x: 96, y: 96}",
        "enemy avatar",
    )

    text = replace_once(
        text,
        "  m_LocalScale: {x: -1, y: 1, z: 1}",
        "  m_LocalScale: {x: 1, y: 1, z: 1}",
        "enemy CardBg unflip",
    )

    text = replace_once(
        text,
        "  m_AnchorMin: {x: 1, y: 0.5}\n  m_AnchorMax: {x: 1, y: 0.5}\n  m_AnchoredPosition: {x: -14, y: 2}\n  m_SizeDelta: {x: 128, y: 84}\n  m_Pivot: {x: 1, y: 0.5}",
        "  m_AnchorMin: {x: 0, y: 0.5}\n  m_AnchorMax: {x: 0, y: 0.5}\n  m_AnchoredPosition: {x: 108, y: 8}\n  m_SizeDelta: {x: 31, y: 100}\n  m_Pivot: {x: 0, y: 0.5}",
        "party BarStack",
    )
    text = replace_once(
        text,
        "  m_AnchorMin: {x: 0, y: 0.5}\n  m_AnchorMax: {x: 0, y: 0.5}\n  m_AnchoredPosition: {x: 14, y: 2}\n  m_SizeDelta: {x: 128, y: 84}\n  m_Pivot: {x: 0, y: 0.5}",
        "  m_AnchorMin: {x: 0, y: 0.5}\n  m_AnchorMax: {x: 0, y: 0.5}\n  m_AnchoredPosition: {x: 108, y: 8}\n  m_SizeDelta: {x: 31, y: 100}\n  m_Pivot: {x: 0, y: 0.5}",
        "enemy BarStack",
    )

    # Party health/gauge slots
    text = replace_once(
        text,
        "  m_Father: {fileID: 979236147}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0.46}\n  m_AnchorMax: {x: 1, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0.625}\n  m_SizeDelta: {x: 0, y: -1.25}\n  m_Pivot: {x: 0.5, y: 0.5}",
        "  m_Father: {fileID: 979236147}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 14, y: 0}\n  m_Pivot: {x: 0, y: 0.5}",
        "party HealthSlot",
    )
    text = replace_once(
        text,
        "  m_Children:\n  - {fileID: 1931001071}\n  - {fileID: 1931001081}\n  - {fileID: 1372547369}\n  m_Father: {fileID: 979236147}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 0.46}\n  m_AnchoredPosition: {x: 0, y: -0.625}\n  m_SizeDelta: {x: 0, y: -1.25}\n  m_Pivot: {x: 0.5, y: 0.5}",
        "  m_Children:\n  - {fileID: 1933000011}\n  - {fileID: 1933000031}\n  - {fileID: 1933000041}\n  - {fileID: 1931001071}\n  - {fileID: 1931001081}\n  - {fileID: 1372547369}\n  m_Father: {fileID: 979236147}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 1}\n  m_AnchoredPosition: {x: 17, y: 0}\n  m_SizeDelta: {x: 14, y: 0}\n  m_Pivot: {x: 0, y: 0.5}",
        "party GaugeSlot",
    )

    # Enemy health/gauge slots
    text = replace_once(
        text,
        "  m_Father: {fileID: 1727946078}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0.46}\n  m_AnchorMax: {x: 1, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0.625}\n  m_SizeDelta: {x: 0, y: -1.25}\n  m_Pivot: {x: 0.5, y: 0.5}",
        "  m_Father: {fileID: 1727946078}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 14, y: 0}\n  m_Pivot: {x: 0, y: 0.5}",
        "enemy HealthSlot",
    )
    text = replace_once(
        text,
        "  m_Children:\n  - {fileID: 1932001071}\n  - {fileID: 1932001081}\n  - {fileID: 1314180597}\n  m_Father: {fileID: 1727946078}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 0.46}\n  m_AnchoredPosition: {x: 0, y: -0.625}",
        "  m_Children:\n  - {fileID: 1933001011}\n  - {fileID: 1933001031}\n  - {fileID: 1933001041}\n  - {fileID: 1932001071}\n  - {fileID: 1932001081}\n  - {fileID: 1314180597}\n  m_Father: {fileID: 1727946078}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 1}\n  m_AnchoredPosition: {x: 17, y: 0}",
        "enemy GaugeSlot children",
    )
    text = replace_once(
        text,
        "  m_SizeDelta: {x: 0, y: -1.25}\n  m_Pivot: {x: 0.5, y: 0.5}\n--- !u!1 &272797784",
        "  m_SizeDelta: {x: 14, y: 0}\n  m_Pivot: {x: 0, y: 0.5}\n--- !u!1 &272797784",
        "enemy GaugeSlot size",
    )

    # HealthBarBg stretch + tube sprite (party)
    text = replace_once(
        text,
        "  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 0}\n  m_AnchoredPosition: {x: 0, y: 2}\n  m_SizeDelta: {x: -4, y: 8}\n  m_Pivot: {x: 0.5, y: 0}",
        "  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}",
        "party HealthBarBg rect",
    )
    text = replace_once(
        text,
        "  m_Sprite: {fileID: 21300000, guid: 43bd1ac016de4b6daf019efffb675be5, type: 3}\n  m_Type: 0\n  m_PreserveAspect: 0",
        f"  m_Sprite: {{fileID: 21300000, guid: {GUID_TRACK}, type: 3}}\n  m_Type: 0\n  m_PreserveAspect: 0",
        "party HealthBarBg sprite",
    )

    # Enemy HealthBarBg
    text = replace_once(
        text,
        "  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 0}\n  m_AnchoredPosition: {x: 0.069999695, y: 2.619999}\n  m_SizeDelta: {x: -4, y: 14.23}\n  m_Pivot: {x: 0.5, y: 0}",
        "  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}",
        "enemy HealthBarBg rect",
    )
    text = replace_once(
        text,
        "  m_Sprite: {fileID: 21300000, guid: 43bd1ac016de4b6daf019efffb675be5, type: 3}\n  m_Type: 0\n  m_PreserveAspect: 0",
        f"  m_Sprite: {{fileID: 21300000, guid: {GUID_TRACK}, type: 3}}\n  m_Type: 0\n  m_PreserveAspect: 0",
        "enemy HealthBarBg sprite",
    )

    # Party fill
    text = replace_once(
        text,
        "  m_AnchoredPosition: {x: 0.00011444092, y: 0.0001296997}\n  m_SizeDelta: {x: 0, y: 0}\n  m_Pivot: {x: -0.0013160927, y: 0.46482036}",
        "  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}",
        "party HealthBarFill rect",
    )
    text = replace_once(
        text,
        "  m_Color: {r: 0.54901963, g: 0.9529412, b: 1, a: 1}\n  m_RaycastTarget: 0\n  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: {fileID: 126735929}\n  m_Type: 0\n  m_PreserveAspect: 0\n  m_FillCenter: 1\n  m_FillMethod: 0",
        f"  m_Color: {GREEN}\n  m_RaycastTarget: 0\n  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}\n  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: {{fileID: 21300000, guid: {GUID_FILL}, type: 3}}\n  m_Type: 3\n  m_PreserveAspect: 0\n  m_FillCenter: 1\n  m_FillMethod: 1",
        "party HealthBarFill image",
    )

    # Enemy fill
    text = replace_once(
        text,
        "  m_AnchoredPosition: {x: 3, y: 0}\n  m_SizeDelta: {x: -6, y: -4}\n  m_Pivot: {x: 0, y: 0.5}",
        "  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}",
        "enemy HealthBarFill rect",
    )

    text = replace_once(
        text,
        "  cardSpacing: 7.75",
        "  cardSpacing: 2.75",
        "enemy cardSpacing",
    )

    text = replace_once(
        text,
        "  gaugeSlot: {fileID: 2060846931}\n  characterCardPresets:",
        "  gaugeSlot: {fileID: 2060846931}\n  gaugeBarBg: {fileID: 1933000012}\n  gaugeBarFill: {fileID: 1933000022}\n  characterCardPresets:",
        "party gauge refs",
    )
    text = replace_once(
        text,
        "  gaugeSlot: {fileID: 268656867}\n  characterCardPresets:",
        "  gaugeSlot: {fileID: 268656867}\n  gaugeBarBg: {fileID: 1933001012}\n  gaugeBarFill: {fileID: 1933001022}\n  characterCardPresets:",
        "enemy gauge refs",
    )

    # Hide resource labels / pips by name
    for name in ("HpLabel", "HpValue", "PrepLabel", "PrepValue"):
        text = text.replace(
            f"  m_Name: {name}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1",
            f"  m_Name: {name}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 0",
        )
    text = text.replace(
        "  m_Name: PrepPips\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1",
        "  m_Name: PrepPips\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 0",
    )

    # Name labels
    text = replace_once(
        text,
        "  m_AnchoredPosition: {x: 3, y: 2.9000015}\n  m_SizeDelta: {x: 88, y: 18}\n  m_Pivot: {x: 0, y: 0}",
        "  m_AnchoredPosition: {x: 8, y: 4}\n  m_SizeDelta: {x: 96, y: 16}\n  m_Pivot: {x: 0, y: 0}",
        "party NameLabel",
    )
    text = replace_once(
        text,
        "  m_AnchorMin: {x: 1, y: 0}\n  m_AnchorMax: {x: 1, y: 0}\n  m_AnchoredPosition: {x: -5.7, y: 5.300003}\n  m_SizeDelta: {x: 88, y: 18}\n  m_Pivot: {x: 1, y: 0}",
        "  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 0}\n  m_AnchoredPosition: {x: 8, y: 4}\n  m_SizeDelta: {x: 96, y: 16}\n  m_Pivot: {x: 0, y: 0}",
        "enemy NameLabel",
    )

    party_blocks = (
        image_block(1933000010, "GaugeBarBg", 2060846931, GUID_TRACK, "{r: 1, g: 1, b: 1, a: 1}", 0, 4, "\n  - {fileID: 1933000021}")
        + image_block(1933000020, "GaugeBarFill", 1933000011, GUID_FILL, MAGENTA, 3, 1, " []")
        + tick_block(1933000030, "GaugeTick_0", 2060846931, 0.33333334)
        + tick_block(1933000040, "GaugeTick_1", 2060846931, 0.6666667)
    )
    enemy_blocks = (
        image_block(1933001010, "GaugeBarBg", 268656867, GUID_TRACK, "{r: 1, g: 1, b: 1, a: 1}", 0, 4, "\n  - {fileID: 1933001021}")
        + image_block(1933001020, "GaugeBarFill", 1933001011, GUID_FILL, MAGENTA, 3, 1, " []")
        + tick_block(1933001030, "GaugeTick_0", 268656867, 0.33333334)
        + tick_block(1933001040, "GaugeTick_1", 268656867, 0.6666667)
    )

    text = replace_once(
        text,
        "  m_Color: {r: 1, g: 0.2784314, b: 0.34117648, a: 1}\n  m_RaycastTarget: 0\n  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: {fileID: 126735929}\n  m_Type: 0\n  m_PreserveAspect: 0\n  m_FillCenter: 1\n  m_FillMethod: 0",
        f"  m_Color: {GREEN}\n  m_RaycastTarget: 0\n  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}\n  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: {{fileID: 21300000, guid: {GUID_FILL}, type: 3}}\n  m_Type: 3\n  m_PreserveAspect: 0\n  m_FillCenter: 1\n  m_FillMethod: 1",
        "enemy HealthBarFill image",
    )

    insert_at = text.rfind("\n")
    text = text.rstrip() + "\n" + party_blocks + enemy_blocks
    SCENE.write_text(text, encoding="utf-8")
    print(f"Patched {SCENE}")


if __name__ == "__main__":
    main()
