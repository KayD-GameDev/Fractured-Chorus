#!/usr/bin/env python3
"""Upgrade CombatPrototype party CardTemplate to modular P5 layers + preview wiring."""

from __future__ import annotations

import math
import re
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets/FracturedChorus/Scenes/CombatPrototype.unity"

# New objects — above current scene max (~1923000105) and prior enemy-patch base.
BASE = 1_931_001_000
CARD_BG = BASE + 10
ACCENT = BASE + 20
AVATAR = BASE + 30
NAME = BASE + 40
HP_LABEL = BASE + 50
HP_VALUE = BASE + 60
PREP_LABEL = BASE + 70
PREP_VALUE = BASE + 80

GUID_BG = "d5b4cd06d40f4c9e936a91a49b44b0cd"
GUID_DIAMOND = "c0dab81da2764ee78a2f1aeb7ebd24a3"
GUID_TRACK = "43bd1ac016de4b6daf019efffb675be5"
GUID_REN = "49c43eca443e44feb00fcc6da6283366"
GUID_FONT = "d4e5f6a7b8c94091a2b3c4d5e6f70891"
GUID_PRESET_REN = "08098499270ddf345a42369c72d140bd"
GUID_PRESET_CODA = "ba6b3d7fe0fdc344088d19d78edf4905"
GUID_PRESET_CHARLOTTE = "0e2d62403b670bb42b8b48d5eaa62268"

CYAN = "{r: 0.54901963, g: 0.9529412, b: 1, a: 1}"
MAGENTA = "{r: 1, g: 0.23921569, b: 0.6509804, a: 1}"
TEXT_PRIMARY = "{r: 0.91764706, g: 0.9843137, b: 1, a: 1}"
ACCENT_COLOR = "{r: 0.18, g: 0.43, b: 1, a: 1}"


def quat_z(degrees: float) -> str:
    half = math.radians(degrees) * 0.5
    z = math.sin(half)
    w = math.cos(half)
    return f"{{x: 0, y: 0, z: {z:.8f}, w: {w:.8f}}}"


def image_block(
    go_id: int,
    name: str,
    father: int,
    sprite_guid: str | None,
    color: str,
    anchors: tuple[str, str],
    pos: str,
    size: str,
    pivot: str,
    rotation: str,
    euler_z: str,
    preserve_aspect: int,
    extra_rect: str = "",
) -> str:
    rect_id = go_id + 1
    img_id = go_id + 2
    cr_id = go_id + 3
    sprite = (
        f"{{fileID: 21300000, guid: {sprite_guid}, type: 3}}"
        if sprite_guid
        else "{fileID: 0}"
    )
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
  m_LocalRotation: {rotation}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: {euler_z}}}
  m_AnchorMin: {anchors[0]}
  m_AnchorMax: {anchors[1]}
  m_AnchoredPosition: {pos}
  m_SizeDelta: {size}
  m_Pivot: {pivot}{extra_rect}
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
  m_Sprite: {sprite}
  m_Type: 0
  m_PreserveAspect: {preserve_aspect}
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


def text_block(
    go_id: int,
    name: str,
    father: int,
    text: str,
    font_size: int,
    color: str,
    anchors: tuple[str, str],
    pos: str,
    size: str,
    pivot: str,
    rotation: str,
    euler_z: str,
) -> str:
    rect_id = go_id + 1
    txt_id = go_id + 2
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
  - component: {{fileID: {txt_id}}}
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
  m_LocalRotation: {rotation}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: {euler_z}}}
  m_AnchorMin: {anchors[0]}
  m_AnchorMax: {anchors[1]}
  m_AnchoredPosition: {pos}
  m_SizeDelta: {size}
  m_Pivot: {pivot}
--- !u!114 &{txt_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Text
  m_Material: {{fileID: 0}}
  m_Color: {color}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {{fileID: 12800000, guid: {GUID_FONT}, type: 3}}
    m_FontSize: {font_size}
    m_FontStyle: 3
    m_BestFit: 0
    m_MinSize: 1
    m_MaxSize: 40
    m_Alignment: 3
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 1
    m_VerticalOverflow: 1
    m_LineSpacing: 1
  m_Text: {text}
--- !u!222 &{cr_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_CullTransparentMesh: 1
"""


def build_new_objects() -> str:
    card = 1830421415
    health = 777863442
    gauge = 2060846931
    ident = "{x: 0, y: 0, z: 0, w: 1}"
    parts = [
        image_block(
            CARD_BG,
            "CardBg",
            card,
            GUID_BG,
            "{r: 1, g: 1, b: 1, a: 1}",
            ("{x: 0, y: 0}", "{x: 1, y: 1}"),
            "{x: 0, y: 1}",
            "{x: -8, y: -10}",
            "{x: 0.5, y: 0.5}",
            ident,
            "0",
            1,
        ),
        image_block(
            ACCENT,
            "AccentShard",
            card,
            GUID_DIAMOND,
            ACCENT_COLOR,
            ("{x: 0, y: 0.5}", "{x: 0, y: 0.5}"),
            "{x: 40, y: 8}",
            "{x: 72, y: 72}",
            "{x: 0.5, y: 0.5}",
            quat_z(12),
            "12",
            1,
        ),
        image_block(
            AVATAR,
            "Avatar",
            card,
            GUID_REN,
            "{r: 1, g: 1, b: 1, a: 1}",
            ("{x: 0, y: 0.5}", "{x: 0, y: 0.5}"),
            "{x: 52, y: 4}",
            "{x: 92, y: 112}",
            "{x: 0.5, y: 0.5}",
            ident,
            "0",
            1,
        ),
        text_block(
            NAME,
            "NameLabel",
            card,
            "Ren",
            18,
            TEXT_PRIMARY,
            ("{x: 0, y: 0}", "{x: 0, y: 0}"),
            "{x: 8, y: 6}",
            "{x: 110, y: 22}",
            "{x: 0, y: 0}",
            quat_z(8),
            "8",
        ),
        text_block(
            HP_LABEL,
            "HpLabel",
            health,
            "HP",
            10,
            CYAN,
            ("{x: 0, y: 1}", "{x: 0, y: 1}"),
            "{x: 2, y: 0}",
            "{x: 28, y: 14}",
            "{x: 0, y: 1}",
            ident,
            "0",
        ),
        text_block(
            HP_VALUE,
            "HpValue",
            health,
            "80",
            22,
            CYAN,
            ("{x: 0, y: 1}", "{x: 1, y: 1}"),
            "{x: 18, y: 2}",
            "{x: 0, y: 28}",
            "{x: 0, y: 1}",
            ident,
            "0",
        ),
        text_block(
            PREP_LABEL,
            "PrepLabel",
            gauge,
            "PREP",
            10,
            MAGENTA,
            ("{x: 0, y: 1}", "{x: 0, y: 1}"),
            "{x: 2, y: 0}",
            "{x: 36, y: 14}",
            "{x: 0, y: 1}",
            ident,
            "0",
        ),
        text_block(
            PREP_VALUE,
            "PrepValue",
            gauge,
            "0",
            18,
            MAGENTA,
            ("{x: 0, y: 1}", "{x: 1, y: 1}"),
            "{x: 18, y: 2}",
            "{x: 0, y: 22}",
            "{x: 0, y: 1}",
            ident,
            "0",
        ),
    ]
    return "".join(parts)


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError(f"Missing snippet for {label}")
    return text.replace(old, new, 1)


def patch_existing(text: str) -> str:
    text = replace_once(
        text,
        """  m_Children:
  - {fileID: 979236147}
  - {fileID: 1910074876}
  - {fileID: 1413621199}
  - {fileID: 1791716010}
  m_Father: {fileID: 1326051293}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 1.5700073, y: -5.23999}
  m_SizeDelta: {x: 180.7, y: 180}
  m_Pivot: {x: 0.5, y: 0.5}""",
        f"""  m_Children:
  - {{fileID: {CARD_BG + 1}}}
  - {{fileID: {ACCENT + 1}}}
  - {{fileID: {AVATAR + 1}}}
  - {{fileID: 979236147}}
  - {{fileID: 1910074876}}
  - {{fileID: 1413621199}}
  - {{fileID: {NAME + 1}}}
  - {{fileID: 1791716010}}
  m_Father: {{fileID: 1326051293}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: -8}}
  m_AnchorMin: {{x: 0, y: 1}}
  m_AnchorMax: {{x: 0, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 240, y: 120}}
  m_Pivot: {{x: 0, y: 1}}""",
        "CardTemplate rect",
    )

    # Rotation on the CardTemplate RectTransform just above children — set localRotation.
    text = replace_once(
        text,
        """--- !u!224 &1830421415
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1830421414}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}""",
        f"""--- !u!224 &1830421415
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1830421414}}
  m_LocalRotation: {quat_z(-8)}""",
        "CardTemplate rotation",
    )

    text = replace_once(
        text,
        """  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.UI.PartyMemberCardView
  healthBarBg: {fileID: 194011095}
  healthBarFill: {fileID: 1528091574}
  healthBarFillRect: {fileID: 1528091573}
  elementBadgeRing: {fileID: 1791716011}
  elementIcon: {fileID: 1153346080}
  cardArtImage: {fileID: 1910074877}
  barStack: {fileID: 979236147}
  healthSlot: {fileID: 777863442}
  gaugeSlot: {fileID: 2060846931}""",
        f"""  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.UI.PartyMemberCardView
  healthBarBg: {{fileID: 194011095}}
  healthBarFill: {{fileID: 1528091574}}
  healthBarFillRect: {{fileID: 1528091573}}
  elementBadgeRing: {{fileID: 1791716011}}
  elementIcon: {{fileID: 1153346080}}
  cardArtImage: {{fileID: 1910074877}}
  cardBg: {{fileID: {CARD_BG + 2}}}
  accentShard: {{fileID: {ACCENT + 2}}}
  avatarImage: {{fileID: {AVATAR + 2}}}
  nameLabel: {{fileID: {NAME + 2}}}
  hpLabel: {{fileID: {HP_LABEL + 2}}}
  hpValue: {{fileID: {HP_VALUE + 2}}}
  prepLabel: {{fileID: {PREP_LABEL + 2}}}
  prepValue: {{fileID: {PREP_VALUE + 2}}}
  barStack: {{fileID: 979236147}}
  healthSlot: {{fileID: 777863442}}
  gaugeSlot: {{fileID: 2060846931}}
  characterCardPresets:
  - {{fileID: 11400000, guid: {GUID_PRESET_REN}, type: 2}}
  - {{fileID: 11400000, guid: {GUID_PRESET_CODA}, type: 2}}
  - {{fileID: 11400000, guid: {GUID_PRESET_CHARLOTTE}, type: 2}}
  previewCharacterIndex: 0""",
        "PartyMemberCardView fields",
    )

    text = replace_once(
        text,
        """  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.LayoutElement
  m_IgnoreLayout: 0
  m_MinWidth: -1
  m_MinHeight: -1
  m_PreferredWidth: 180.7
  m_PreferredHeight: 180
  m_FlexibleWidth: -1
  m_FlexibleHeight: -1
  m_LayoutPriority: 1
--- !u!1 &1849548838""",
        """  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.LayoutElement
  m_IgnoreLayout: 0
  m_MinWidth: -1
  m_MinHeight: -1
  m_PreferredWidth: 240
  m_PreferredHeight: 120
  m_FlexibleWidth: -1
  m_FlexibleHeight: -1
  m_LayoutPriority: 1
--- !u!1 &1849548838""",
        "LayoutElement size",
    )

    text = replace_once(
        text,
        """  m_Father: {fileID: 706293081}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 12, y: -12}
  m_SizeDelta: {x: 720, y: 180}
  m_Pivot: {x: 0, y: 1}""",
        """  m_Father: {fileID: 706293081}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 12, y: -12}
  m_SizeDelta: {x: 980, y: 128}
  m_Pivot: {x: 0, y: 1}""",
        "PartyStatusBar size",
    )

    text = replace_once(
        text,
        """  m_GameObject: {fileID: 979236146}
  m_LocalRotation: {x: -0, y: -0, z: -0.10955118, w: 0.9939812}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 777863442}
  - {fileID: 2060846931}
  m_Father: {fileID: 1830421415}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: -12.579}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 75, y: 42}
  m_SizeDelta: {x: 121.76, y: 26.58}
  m_Pivot: {x: 0.5, y: 0.5}""",
        f"""  m_GameObject: {{fileID: 979236146}}
  m_LocalRotation: {quat_z(-8)}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: 777863442}}
  - {{fileID: 2060846931}}
  m_Father: {{fileID: 1830421415}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: -8}}
  m_AnchorMin: {{x: 1, y: 0.5}}
  m_AnchorMax: {{x: 1, y: 0.5}}
  m_AnchoredPosition: {{x: -10, y: -4}}
  m_SizeDelta: {{x: 118, y: 64}}
  m_Pivot: {{x: 1, y: 0.5}}""",
        "BarStack rect",
    )

    text = replace_once(
        text,
        """  m_Children:
  - {fileID: 194011094}
  m_Father: {fileID: 979236147}""",
        f"""  m_Children:
  - {{fileID: {HP_LABEL + 1}}}
  - {{fileID: {HP_VALUE + 1}}}
  - {{fileID: 194011094}}
  m_Father: {{fileID: 979236147}}""",
        "HealthSlot children",
    )

    text = replace_once(
        text,
        """  m_Children:
  - {fileID: 1372547369}
  m_Father: {fileID: 979236147}""",
        f"""  m_Children:
  - {{fileID: {PREP_LABEL + 1}}}
  - {{fileID: {PREP_VALUE + 1}}}
  - {{fileID: 1372547369}}
  m_Father: {{fileID: 979236147}}""",
        "GaugeSlot children",
    )

    text = replace_once(
        text,
        """  m_Name: CardArt
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &1910074876""",
        """  m_Name: CardArt
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!224 &1910074876""",
        "Hide CardArt",
    )

    text = replace_once(
        text,
        """  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: -0.6800003, y: -1.8800001}
  m_SizeDelta: {x: 0.48, y: 1.59}
  m_Pivot: {x: 0.5, y: 0}""",
        """  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 0}
  m_AnchoredPosition: {x: 0, y: 2}
  m_SizeDelta: {x: 0, y: 10}
  m_Pivot: {x: 0.5, y: 0}""",
        "HealthBarBg rect",
    )

    text = replace_once(
        text,
        """  m_Color: {r: 0.08, g: 0.08, b: 0.1, a: 0.95}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 126735929}
  m_Type: 0
  m_PreserveAspect: 0""",
        f"""  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 21300000, guid: {GUID_TRACK}, type: 3}}
  m_Type: 0
  m_PreserveAspect: 0""",
        "HealthBarBg sprite",
    )

    text = replace_once(
        text,
        """  m_Color: {r: 0.18, g: 0.92, b: 0.28, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 126735929}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 0""",
        f"""  m_Color: {CYAN}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 126735929}}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 0""",
        "HealthBarFill color",
    )

    text = replace_once(
        text,
        """  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 25, y: -25}
  m_SizeDelta: {x: 35, y: 35}
  m_Pivot: {x: 0.5, y: 0.5}""",
        """  m_AnchorMin: {x: 1, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: -18, y: -14}
  m_SizeDelta: {x: 35, y: 35}
  m_Pivot: {x: 0.5, y: 0.5}""",
        "ElementBadge rect",
    )
    return text


def main() -> None:
    text = SCENE.read_text(encoding="utf-8")
    if f"&{CARD_BG}" in text:
        print("Party CardTemplate already patched; skip.")
        return

    if "m_Name: CardTemplate" not in text:
        raise RuntimeError("CardTemplate missing from scene")

    text = patch_existing(text)
    insert_at = text.find("--- !u!1 &1849548838")
    if insert_at < 0:
        raise RuntimeError("Insert point LaneLines not found")
    text = text[:insert_at] + build_new_objects() + text[insert_at:]
    SCENE.write_text(text, encoding="utf-8")
    print(f"Patched {SCENE}")


if __name__ == "__main__":
    main()
