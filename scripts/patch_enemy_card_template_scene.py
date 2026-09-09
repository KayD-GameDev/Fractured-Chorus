#!/usr/bin/env python3
"""Upgrade CombatPrototype enemy CardTemplate to modular P5 layers (mirrored)."""

from __future__ import annotations

import math
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets/FracturedChorus/Scenes/CombatPrototype.unity"

BASE = 1_932_001_000
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
GUID_ASTRA = "8e2c1a9f4b6d47c0a3e5f7192d8b40c1"
GUID_FONT = "d4e5f6a7b8c94091a2b3c4d5e6f70891"
GUID_PRESET_ASTRA = "5c0e567a2709efc4cbe4f79ffc656a35"
GUID_PRESET_KIKI = "a6b7c8d9e0fb4c3405060718293a4b5c"
GUID_PRESET_MIC = "9ab3039d897d3944c8123ff76fd14d15"
GUID_PRESET_EYE = "e23ab770461550f4599cc0df7d6fa5b0"

RED = "{r: 1, g: 0.2784314, b: 0.34117648, a: 1}"
MAGENTA = "{r: 1, g: 0.23921569, b: 0.6509804, a: 1}"
TEXT_PRIMARY = "{r: 0.91764706, g: 0.9843137, b: 1, a: 1}"
ACCENT_COLOR = "{r: 0.85, g: 0.12, b: 0.22, a: 0.82}"


def quat_z(degrees: float) -> str:
    half = math.radians(degrees) * 0.5
    return f"{{x: 0, y: 0, z: {math.sin(half):.8f}, w: {math.cos(half):.8f}}}"


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
    scale: str = "{x: 1, y: 1, z: 1}",
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
  m_LocalScale: {scale}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: {euler_z}}}
  m_AnchorMin: {anchors[0]}
  m_AnchorMax: {anchors[1]}
  m_AnchoredPosition: {pos}
  m_SizeDelta: {size}
  m_Pivot: {pivot}
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
    alignment: int = 3,
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
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 1
    m_MaxSize: 40
    m_Alignment: {alignment}
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
    card = 1923000002
    health = 254508704
    gauge = 268656867
    ident = "{x: 0, y: 0, z: 0, w: 1}"
    parts = [
        image_block(
            CARD_BG,
            "CardBg",
            card,
            GUID_BG,
            "{r: 1, g: 1, b: 1, a: 1}",
            ("{x: 0, y: 0}", "{x: 1, y: 1}"),
            "{x: 0, y: 0}",
            "{x: 0, y: 0}",
            "{x: 0.5, y: 0.5}",
            ident,
            "0",
            0,
            scale="{x: -1, y: 1, z: 1}",
        ),
        image_block(
            ACCENT,
            "AccentShard",
            card,
            GUID_DIAMOND,
            ACCENT_COLOR,
            ("{x: 1, y: 0.5}", "{x: 1, y: 0.5}"),
            "{x: -42, y: 12}",
            "{x: 44, y: 44}",
            "{x: 0.5, y: 0.5}",
            quat_z(-12),
            "-12",
            1,
        ),
        image_block(
            AVATAR,
            "Avatar",
            card,
            GUID_ASTRA,
            "{r: 1, g: 1, b: 1, a: 1}",
            ("{x: 1, y: 0.5}", "{x: 1, y: 0.5}"),
            "{x: -46, y: 6}",
            "{x: 80, y: 102}",
            "{x: 0.5, y: 0.5}",
            ident,
            "0",
            1,
        ),
        text_block(
            NAME,
            "NameLabel",
            card,
            "Astra",
            16,
            TEXT_PRIMARY,
            ("{x: 1, y: 0}", "{x: 1, y: 0}"),
            "{x: -12, y: 10}",
            "{x: 88, y: 18}",
            "{x: 1, y: 0}",
            ident,
            "0",
            alignment=5,
        ),
        text_block(
            HP_LABEL,
            "HpLabel",
            health,
            "HP",
            10,
            RED,
            ("{x: 0, y: 1}", "{x: 0, y: 1}"),
            "{x: 2, y: -1}",
            "{x: 24, y: 14}",
            "{x: 0, y: 1}",
            ident,
            "0",
        ),
        text_block(
            HP_VALUE,
            "HpValue",
            health,
            "80",
            20,
            RED,
            ("{x: 0, y: 1}", "{x: 0, y: 1}"),
            "{x: 26, y: -1}",
            "{x: 98, y: 22}",
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
            "{x: 2, y: -1}",
            "{x: 36, y: 12}",
            "{x: 0, y: 1}",
            ident,
            "0",
        ),
        text_block(
            PREP_VALUE,
            "PrepValue",
            gauge,
            "0",
            16,
            MAGENTA,
            ("{x: 0, y: 1}", "{x: 0, y: 1}"),
            "{x: 38, y: -1}",
            "{x: 86, y: 16}",
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
        """--- !u!224 &1923000002
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1923000001}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}""",
        f"""--- !u!224 &1923000002
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1923000001}}
  m_LocalRotation: {quat_z(6)}""",
        "Enemy CardTemplate rotation",
    )

    text = replace_once(
        text,
        """  m_Children:
  - {fileID: 1727946078}
  - {fileID: 1944951639}
  - {fileID: 1923000022}
  m_Father: {fileID: 1923000101}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 1.5700073, y: -0.000038146973}
  m_SizeDelta: {x: 180, y: 180}
  m_Pivot: {x: 0.5, y: 0.5}""",
        f"""  m_Children:
  - {{fileID: {CARD_BG + 1}}}
  - {{fileID: {ACCENT + 1}}}
  - {{fileID: {AVATAR + 1}}}
  - {{fileID: 1727946078}}
  - {{fileID: 1944951639}}
  - {{fileID: {NAME + 1}}}
  - {{fileID: 1923000022}}
  m_Father: {{fileID: 1923000101}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 6}}
  m_AnchorMin: {{x: 1, y: 1}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 240, y: 118}}
  m_Pivot: {{x: 1, y: 1}}""",
        "Enemy CardTemplate rect",
    )

    text = replace_once(
        text,
        """  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.UI.PartyMemberCardView
  healthBarBg: {fileID: 1923000015}
  healthBarFill: {fileID: 1923000019}
  healthBarFillRect: {fileID: 1923000018}
  elementBadgeRing: {fileID: 1923000023}
  elementIcon: {fileID: 1923000027}
  cardArtImage: {fileID: 1944951640}
  cardBg: {fileID: 0}
  accentShard: {fileID: 0}
  avatarImage: {fileID: 0}
  nameLabel: {fileID: 0}
  hpLabel: {fileID: 0}
  hpValue: {fileID: 0}
  prepLabel: {fileID: 0}
  prepValue: {fileID: 0}
  barStack: {fileID: 1727946078}
  healthSlot: {fileID: 254508704}
  gaugeSlot: {fileID: 268656867}
  characterCardPresets: []
  previewCharacterIndex: 0""",
        f"""  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.UI.PartyMemberCardView
  healthBarBg: {{fileID: 1923000015}}
  healthBarFill: {{fileID: 1923000019}}
  healthBarFillRect: {{fileID: 1923000018}}
  elementBadgeRing: {{fileID: 1923000023}}
  elementIcon: {{fileID: 1923000027}}
  cardArtImage: {{fileID: 1944951640}}
  cardBg: {{fileID: {CARD_BG + 2}}}
  accentShard: {{fileID: {ACCENT + 2}}}
  avatarImage: {{fileID: {AVATAR + 2}}}
  nameLabel: {{fileID: {NAME + 2}}}
  hpLabel: {{fileID: {HP_LABEL + 2}}}
  hpValue: {{fileID: {HP_VALUE + 2}}}
  prepLabel: {{fileID: {PREP_LABEL + 2}}}
  prepValue: {{fileID: {PREP_VALUE + 2}}}
  barStack: {{fileID: 1727946078}}
  healthSlot: {{fileID: 254508704}}
  gaugeSlot: {{fileID: 268656867}}
  characterCardPresets:
  - {{fileID: 11400000, guid: {GUID_PRESET_ASTRA}, type: 2}}
  - {{fileID: 11400000, guid: {GUID_PRESET_KIKI}, type: 2}}
  - {{fileID: 11400000, guid: {GUID_PRESET_MIC}, type: 2}}
  - {{fileID: 11400000, guid: {GUID_PRESET_EYE}, type: 2}}
  previewCharacterIndex: 0""",
        "Enemy PartyMemberCardView fields",
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
--- !u!1 &1923000013""",
        """  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.LayoutElement
  m_IgnoreLayout: 0
  m_MinWidth: -1
  m_MinHeight: -1
  m_PreferredWidth: 240
  m_PreferredHeight: 118
  m_FlexibleWidth: -1
  m_FlexibleHeight: -1
  m_LayoutPriority: 1
--- !u!1 &1923000013""",
        "Enemy LayoutElement size",
    )

    text = replace_once(
        text,
        """  m_Father: {fileID: 706293081}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 1, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: -12, y: -12}
  m_SizeDelta: {x: 720, y: 180}
  m_Pivot: {x: 1, y: 1}""",
        """  m_Father: {fileID: 706293081}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 1, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: -12, y: -12}
  m_SizeDelta: {x: 980, y: 128}
  m_Pivot: {x: 1, y: 1}""",
        "EnemyStatusBar size",
    )

    text = replace_once(
        text,
        """  m_GameObject: {fileID: 1727946077}
  m_LocalRotation: {x: 0, y: 0, z: -0.08838145, w: 0.9960867}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 254508704}
  - {fileID: 268656867}
  m_Father: {fileID: 1923000002}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 73.41, y: 27.8}
  m_SizeDelta: {x: 124.53, y: 32.98}
  m_Pivot: {x: 0.5, y: 0.5}""",
        """  m_GameObject: {fileID: 1727946077}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 254508704}
  - {fileID: 268656867}
  m_Father: {fileID: 1923000002}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0.5}
  m_AnchorMax: {x: 0, y: 0.5}
  m_AnchoredPosition: {x: 14, y: 2}
  m_SizeDelta: {x: 128, y: 84}
  m_Pivot: {x: 0, y: 0.5}""",
        "Enemy BarStack rect",
    )

    text = replace_once(
        text,
        """  m_Children:
  - {fileID: 1923000014}
  m_Father: {fileID: 1727946078}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0.5}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0.75}
  m_SizeDelta: {x: 0, y: -1.5}""",
        f"""  m_Children:
  - {{fileID: {HP_LABEL + 1}}}
  - {{fileID: {HP_VALUE + 1}}}
  - {{fileID: 1923000014}}
  m_Father: {{fileID: 1727946078}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0.46}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0.625}}
  m_SizeDelta: {{x: 0, y: -1.25}}""",
        "Enemy HealthSlot",
    )

    text = replace_once(
        text,
        """  m_Children:
  - {fileID: 1314180597}
  m_Father: {fileID: 1727946078}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 0.5}
  m_AnchoredPosition: {x: 0, y: -0.75}
  m_SizeDelta: {x: 0, y: -1.5}""",
        f"""  m_Children:
  - {{fileID: {PREP_LABEL + 1}}}
  - {{fileID: {PREP_VALUE + 1}}}
  - {{fileID: 1314180597}}
  m_Father: {{fileID: 1727946078}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 1, y: 0.46}}
  m_AnchoredPosition: {{x: 0, y: -0.625}}
  m_SizeDelta: {{x: 0, y: -1.25}}""",
        "Enemy GaugeSlot",
    )

    text = replace_once(
        text,
        """  m_Children:
  - {fileID: 1622410171}
  - {fileID: 1645217133}
  - {fileID: 1028127785}
  m_Father: {fileID: 268656867}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0.25999832, y: 0.05000019}
  m_SizeDelta: {x: -1.65, y: -4.08}
  m_Pivot: {x: 0, y: 1}""",
        """  m_Children:
  - {fileID: 1622410171}
  - {fileID: 1645217133}
  - {fileID: 1028127785}
  m_Father: {fileID: 268656867}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 0}
  m_AnchoredPosition: {x: 0, y: 2}
  m_SizeDelta: {x: -4, y: 11}
  m_Pivot: {x: 0.5, y: 0}""",
        "Enemy PrepPips strip",
    )

    text = replace_once(
        text,
        """  m_Children:
  - {fileID: 1923000018}
  m_Father: {fileID: 254508704}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0}""",
        """  m_Children:
  - {fileID: 1923000018}
  m_Father: {fileID: 254508704}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 0}
  m_AnchoredPosition: {x: 0, y: 2}
  m_SizeDelta: {x: -4, y: 8}
  m_Pivot: {x: 0.5, y: 0}""",
        "Enemy HealthBarBg rect",
    )

    text = replace_once(
        text,
        """  m_GameObject: {fileID: 1923000013}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 0.08, g: 0.08, b: 0.1, a: 0.95}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 126735929}
  m_Type: 0
  m_PreserveAspect: 0""",
        f"""  m_GameObject: {{fileID: 1923000013}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 21300000, guid: {GUID_TRACK}, type: 3}}
  m_Type: 0
  m_PreserveAspect: 0""",
        "Enemy HealthBarBg sprite",
    )

    text = replace_once(
        text,
        """  m_GameObject: {fileID: 1923000017}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 1923000014}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0.5699997, y: -3.1400003}
  m_SizeDelta: {x: 0.88, y: -0.75}
  m_Pivot: {x: 0, y: 0.5}""",
        """  m_GameObject: {fileID: 1923000017}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 1923000014}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 3, y: 0}
  m_SizeDelta: {x: -6, y: -4}
  m_Pivot: {x: 0, y: 0.5}""",
        "Enemy HealthBarFill rect",
    )

    text = replace_once(
        text,
        """  m_GameObject: {fileID: 1923000017}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 0.18, g: 0.92, b: 0.28, a: 1}""",
        f"""  m_GameObject: {{fileID: 1923000017}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {RED}""",
        "Enemy HealthBarFill color",
    )

    text = replace_once(
        text,
        """  m_Father: {fileID: 1923000002}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 1, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: -18, y: -18}
  m_SizeDelta: {x: 35, y: 35}
  m_Pivot: {x: 0.5, y: 0.5}""",
        """  m_Father: {fileID: 1923000002}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 1, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: -16, y: -12}
  m_SizeDelta: {x: 22, y: 22}
  m_Pivot: {x: 0.5, y: 0.5}""",
        "Enemy ElementBadge rect",
    )

    text = replace_once(
        text,
        """  m_Name: CardArt
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &1944951639""",
        """  m_Name: CardArt
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!224 &1944951639""",
        "Hide enemy CardArt",
    )
    return text


def main() -> None:
    text = SCENE.read_text(encoding="utf-8")
    if f"&{CARD_BG}" in text:
        print("Enemy CardTemplate already patched; skip.")
        return

    if "--- !u!1 &1923000001" not in text:
        raise RuntimeError("Enemy CardTemplate missing from scene")

    text = patch_existing(text)
    insert_at = text.find("--- !u!1 &1923000013")
    if insert_at < 0:
        raise RuntimeError("Insert point HealthBarBg not found")
    text = text[:insert_at] + build_new_objects() + text[insert_at:]
    SCENE.write_text(text, encoding="utf-8")
    print(f"Patched {SCENE}")


if __name__ == "__main__":
    main()
