#!/usr/bin/env python3
"""Tighten CombatPrototype party CardTemplate for a clearer P5 HUD read."""

from __future__ import annotations

import math
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets/FracturedChorus/Scenes/CombatPrototype.unity"


def quat_z(degrees: float) -> str:
    half = math.radians(degrees) * 0.5
    return f"{{x: 0, y: 0, z: {math.sin(half):.8f}, w: {math.cos(half):.8f}}}"


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError(f"Missing snippet for {label}")
    return text.replace(old, new, 1)


def main() -> None:
    text = SCENE.read_text(encoding="utf-8")

    text = replace_once(
        text,
        """  m_GameObject: {fileID: 1830421414}
  m_LocalRotation: {x: 0, y: 0, z: -0.06975647, w: 0.9975641}""",
        f"""  m_GameObject: {{fileID: 1830421414}}
  m_LocalRotation: {quat_z(-6)}""",
        "CardTemplate rotation",
    )
    text = replace_once(
        text,
        """  m_LocalEulerAnglesHint: {x: 0, y: 0, z: -8}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: -1.49, y: 0}
  m_SizeDelta: {x: 218.54, y: 119.08}
  m_Pivot: {x: 0, y: 1}""",
        """  m_LocalEulerAnglesHint: {x: 0, y: 0, z: -6}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 240, y: 118}
  m_Pivot: {x: 0, y: 1}""",
        "CardTemplate size",
    )
    text = replace_once(
        text,
        """  m_PreferredWidth: 240
  m_PreferredHeight: 120""",
        """  m_PreferredWidth: 240
  m_PreferredHeight: 118""",
        "LayoutElement height",
    )

    text = replace_once(
        text,
        """  m_AnchoredPosition: {x: 0, y: 1}
  m_SizeDelta: {x: -8, y: -10}
  m_Pivot: {x: 0.5, y: 0.5}""",
        """  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}""",
        "CardBg rect",
    )
    text = replace_once(
        text,
        """  m_Sprite: {fileID: 21300000, guid: d5b4cd06d40f4c9e936a91a49b44b0cd, type: 3}
  m_Type: 0
  m_PreserveAspect: 1""",
        """  m_Sprite: {fileID: 21300000, guid: d5b4cd06d40f4c9e936a91a49b44b0cd, type: 3}
  m_Type: 0
  m_PreserveAspect: 0""",
        "CardBg fill",
    )

    text = replace_once(
        text,
        """  m_AnchoredPosition: {x: 40, y: 8}
  m_SizeDelta: {x: 72, y: 72}""",
        """  m_AnchoredPosition: {x: 42, y: 12}
  m_SizeDelta: {x: 44, y: 44}""",
        "AccentShard rect",
    )
    text = replace_once(
        text,
        """  m_Color: {r: 0.18, g: 0.43, b: 1, a: 1}""",
        """  m_Color: {r: 0.18, g: 0.43, b: 1, a: 0.72}""",
        "AccentShard alpha",
    )

    text = replace_once(
        text,
        """  m_AnchoredPosition: {x: 52, y: 4}
  m_SizeDelta: {x: 92, y: 112}""",
        """  m_AnchoredPosition: {x: 46, y: 6}
  m_SizeDelta: {x: 80, y: 102}""",
        "Avatar rect",
    )

    text = replace_once(
        text,
        """  m_GameObject: {fileID: 1931001040}
  m_LocalRotation: {x: 0, y: 0, z: 0.06975647, w: 0.9975641}""",
        """  m_GameObject: {fileID: 1931001040}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}""",
        "NameLabel rotation",
    )
    text = replace_once(
        text,
        """  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 8}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 8, y: 6}
  m_SizeDelta: {x: 110, y: 22}""",
        """  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 12, y: 10}
  m_SizeDelta: {x: 88, y: 18}""",
        "NameLabel rect",
    )
    text = replace_once(
        text,
        """    m_FontSize: 18
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 1
    m_MaxSize: 40
    m_Alignment: 3
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 1
    m_VerticalOverflow: 1
    m_LineSpacing: 1
  m_Text: Ren""",
        """    m_FontSize: 16
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 1
    m_MaxSize: 40
    m_Alignment: 3
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 1
    m_VerticalOverflow: 1
    m_LineSpacing: 1
  m_Text: Ren""",
        "NameLabel font",
    )

    text = replace_once(
        text,
        """  m_GameObject: {fileID: 979236146}
  m_LocalRotation: {x: 0, y: 0, z: -0.06975647, w: 0.9975641}""",
        """  m_GameObject: {fileID: 979236146}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}""",
        "BarStack rotation",
    )
    text = replace_once(
        text,
        """  m_LocalEulerAnglesHint: {x: 0, y: 0, z: -8}
  m_AnchorMin: {x: 1, y: 0.5}
  m_AnchorMax: {x: 1, y: 0.5}
  m_AnchoredPosition: {x: -10, y: -3.9999962}
  m_SizeDelta: {x: 118, y: 64}
  m_Pivot: {x: 1, y: 0.5}""",
        """  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 1, y: 0.5}
  m_AnchorMax: {x: 1, y: 0.5}
  m_AnchoredPosition: {x: -14, y: 2}
  m_SizeDelta: {x: 128, y: 84}
  m_Pivot: {x: 1, y: 0.5}""",
        "BarStack rect",
    )

    text = replace_once(
        text,
        """  m_Father: {fileID: 979236147}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0.5}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0.75}
  m_SizeDelta: {x: 0, y: -1.5}""",
        """  m_Father: {fileID: 979236147}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0.46}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0.625}
  m_SizeDelta: {x: 0, y: -1.25}""",
        "HealthSlot split",
    )
    text = replace_once(
        text,
        """  m_Father: {fileID: 979236147}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 0.5}
  m_AnchoredPosition: {x: 0, y: -0.75}
  m_SizeDelta: {x: 0, y: -1.5}""",
        """  m_Father: {fileID: 979236147}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 0.46}
  m_AnchoredPosition: {x: 0, y: -0.625}
  m_SizeDelta: {x: 0, y: -1.25}""",
        "GaugeSlot split",
    )

    text = replace_once(
        text,
        """  m_AnchoredPosition: {x: 2, y: 0}
  m_SizeDelta: {x: 28, y: 14}
  m_Pivot: {x: 0, y: 1}""",
        """  m_AnchoredPosition: {x: 2, y: -1}
  m_SizeDelta: {x: 24, y: 14}
  m_Pivot: {x: 0, y: 1}""",
        "HpLabel rect",
    )
    text = replace_once(
        text,
        """  m_GameObject: {fileID: 1931001060}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 777863442}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 18, y: 2}
  m_SizeDelta: {x: 0, y: 28}""",
        """  m_GameObject: {fileID: 1931001060}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 777863442}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 26, y: -1}
  m_SizeDelta: {x: 98, y: 22}""",
        "HpValue rect",
    )
    text = replace_once(
        text,
        """    m_FontSize: 22
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 1
    m_MaxSize: 40
    m_Alignment: 3
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 1
    m_VerticalOverflow: 1
    m_LineSpacing: 1
  m_Text: 114""",
        """    m_FontSize: 20
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 1
    m_MaxSize: 40
    m_Alignment: 3
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 1
    m_VerticalOverflow: 1
    m_LineSpacing: 1
  m_Text: 114""",
        "HpValue font",
    )

    text = replace_once(
        text,
        """  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 0}
  m_AnchoredPosition: {x: 0, y: 2}
  m_SizeDelta: {x: 0, y: 10}
  m_Pivot: {x: 0.5, y: 0}""",
        """  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 0}
  m_AnchoredPosition: {x: 0, y: 2}
  m_SizeDelta: {x: -4, y: 8}
  m_Pivot: {x: 0.5, y: 0}""",
        "HealthBarBg rect",
    )

    text = replace_once(
        text,
        """  m_AnchoredPosition: {x: 2, y: 0}
  m_SizeDelta: {x: 36, y: 14}
  m_Pivot: {x: 0, y: 1}""",
        """  m_AnchoredPosition: {x: 2, y: -1}
  m_SizeDelta: {x: 36, y: 12}
  m_Pivot: {x: 0, y: 1}""",
        "PrepLabel rect",
    )
    text = replace_once(
        text,
        """  m_GameObject: {fileID: 1931001080}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 2060846931}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 18, y: 2}
  m_SizeDelta: {x: 0, y: 22}""",
        """  m_GameObject: {fileID: 1931001080}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 2060846931}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 38, y: -1}
  m_SizeDelta: {x: 86, y: 16}""",
        "PrepValue rect",
    )

    text = replace_once(
        text,
        """  m_Father: {fileID: 2060846931}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0, y: 1}""",
        """  m_Father: {fileID: 2060846931}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 0}
  m_AnchoredPosition: {x: 0, y: 2}
  m_SizeDelta: {x: -4, y: 11}
  m_Pivot: {x: 0.5, y: 0}""",
        "PrepPips strip",
    )

    text = replace_once(
        text,
        """  m_AnchorMin: {x: 1, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: -18, y: -14}
  m_SizeDelta: {x: 35, y: 35}
  m_Pivot: {x: 0.5, y: 0.5}""",
        """  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 16, y: -12}
  m_SizeDelta: {x: 22, y: 22}
  m_Pivot: {x: 0.5, y: 0.5}""",
        "ElementBadge rect",
    )

    text = replace_once(
        text,
        """  m_Name: BuffReduceS2
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1""",
        """  m_Name: BuffReduceS2
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0""",
        "Hide BuffReduceS2",
    )
    text = replace_once(
        text,
        """  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 9.7, y: -25.8}
  m_SizeDelta: {x: 32, y: 32}
  m_Pivot: {x: 0, y: 0}""",
        """  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 86, y: -8}
  m_SizeDelta: {x: 18, y: 18}
  m_Pivot: {x: 1, y: 1}""",
        "BuffReduceS2 rect",
    )

    SCENE.write_text(text, encoding="utf-8")
    print(f"Relayout patched {SCENE}")


if __name__ == "__main__":
    main()
