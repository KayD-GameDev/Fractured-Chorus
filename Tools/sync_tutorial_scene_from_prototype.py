"""Copy authored CombatPrototype layout objects into CombatTutorial.unity.

Does not copy boss-only TV objects (BuffAstraTv, BuffReduceS2, AstraStageTv,
Beat_1). NoteSimulator is copied: its knob offsets pin the note belly.
"""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(r"F:\Unity_Project\Fractured Chorus")
PROTO = ROOT / "Assets/FracturedChorus/Scenes/CombatPrototype.unity"
TUT = ROOT / "Assets/FracturedChorus/Scenes/CombatTutorial.unity"

SKIP_NAMES = {
    "BuffAstraTv",
    "BuffReduceS2",
    "AstraStageTv",
    "Beat_1",
}

LAYOUT_NAMES = {
    "BeatTimelineUI",
    "Viewport",
    "ScrollContent",
    "LaneFootprint",
    "LaneMarkers",
    "LaneLines",
    "LaneAvatarGutter",
    "CardTemplate",
    "BarStack",
    "HealthSlot",
    "GaugeSlot",
    "HealthBarBg",
    "HealthBarFill",
    "PartyStatusBarUI",
    "EnemyStatusBarUI",
}

RECHILD_NAMES = LAYOUT_NAMES | {
    "BossTrackFrame",
    "BossNoteClusterLayer",
}

RECT_KEYS = (
    "m_LocalRotation",
    "m_LocalPosition",
    "m_LocalScale",
    "m_ConstrainProportionsScale",
    "m_AnchorMin",
    "m_AnchorMax",
    "m_AnchoredPosition",
    "m_SizeDelta",
    "m_Pivot",
    "m_LocalEulerAnglesHint",
)

VIEW_KEYS = (
    "bossTrackFrame",
    "bossNoteRailAnchoredY",
    "avatarSlotSize",
    "NoteDisplaySize",
    "NoteDisplayWidth",
    "NoteDisplayHeight",
)

MIMI_SPRITE = (
    "{fileID: -1833356304334501433, guid: 38bb3315d198a054192d86fca7cc6e09, type: 3}"
)


def load(path: Path):
    raw = path.read_bytes()
    text = raw.decode("utf-8")
    crlf = "\r\n" in text[:800]
    text = text.replace("\r\n", "\n")
    parts = re.split(r"\n--- !u!", text)
    header = parts[0]
    docs: dict[int, tuple[int, str]] = {}
    order: list[int] = []
    for part in parts[1:]:
        match = re.match(r"(\d+) &(\d+)\n", part)
        if not match:
            continue
        typ, fid = int(match.group(1)), int(match.group(2))
        docs[fid] = (typ, part)
        order.append(fid)
    return header, docs, order, crlf


def save(path: Path, header: str, docs: dict, order: list[int], crlf: bool) -> None:
    chunks = [header]
    for fid in order:
        if fid not in docs:
            continue
        typ, body = docs[fid]
        if not body.startswith(f"{typ} &{fid}\n"):
            body = re.sub(r"^\d+ &\d+\n", f"{typ} &{fid}\n", body, count=1)
        chunks.append("\n--- !u!" + body)
    text = "".join(chunks)
    if not text.endswith("\n"):
        text += "\n"
    if crlf:
        text = text.replace("\n", "\r\n")
    path.write_bytes(text.encode("utf-8"))


def doc_name(body: str) -> str:
    match = re.search(r"\n  m_Name: (.*)", body)
    return match.group(1).strip() if match else ""


def components_of(body: str) -> list[int]:
    return [int(x) for x in re.findall(r"- component: \{fileID: (\d+)\}", body)]


def children_of(body: str) -> list[int]:
    if "m_Children:" not in body:
        return []
    block = body.split("m_Children:", 1)[1].split("m_Father:", 1)[0]
    return [int(x) for x in re.findall(r"\{fileID: (\d+)\}", block)]


def transform_id(docs, go_id: int) -> int | None:
    for comp in components_of(docs[go_id][1]):
        if comp in docs and docs[comp][0] in (4, 224):
            return comp
    return None


def game_object_of_transform(docs, transform: int) -> int | None:
    match = re.search(r"m_GameObject: \{fileID: (\d+)\}", docs[transform][1])
    return int(match.group(1)) if match else None


def child_game_objects(docs, go_id: int) -> list[int]:
    rect = transform_id(docs, go_id)
    if rect is None:
        return []
    result = []
    for child in children_of(docs[rect][1]):
        if child not in docs:
            continue
        go = game_object_of_transform(docs, child)
        if go is not None:
            result.append(go)
    return result


def find_gos(docs, object_name: str) -> list[int]:
    return [fid for fid, (typ, body) in docs.items() if typ == 1 and doc_name(body) == object_name]


def copy_line(src: str, dst: str, key: str) -> str:
    match = re.search(rf"^  {re.escape(key)}: .*$", src, re.M)
    if not match or not re.search(rf"^  {re.escape(key)}: ", dst, re.M):
        return dst
    return re.sub(rf"^  {re.escape(key)}: .*$", match.group(0), dst, count=1, flags=re.M)


def set_children(body: str, ids: list[int]) -> str:
    if ids:
        block = "  m_Children:\n" + "".join(f"  - {{fileID: {i}}}\n" for i in ids)
    else:
        block = "  m_Children: []\n"
    return re.sub(r"  m_Children:.*?(?=  m_Father:)", block, body, count=1, flags=re.S)


def collect_import(src, dst, go_id: int, acc: set[int]) -> None:
    if go_id not in src:
        return
    name = doc_name(src[go_id][1])
    if name in SKIP_NAMES:
        return
    if go_id not in dst:
        acc.add(go_id)
        for comp in components_of(src[go_id][1]):
            if comp not in dst:
                acc.add(comp)
    for child in child_game_objects(src, go_id):
        collect_import(src, dst, child, acc)


def main() -> None:
    header, proto, _, _ = load(PROTO)
    tut_header, tut, tut_order, crlf = load(TUT)

    seeds = []
    for name in (
        "BeatTimelineUI",
        "CardTemplate",
        "PartyStatusBarUI",
        "EnemyStatusBarUI",
    ):
        seeds.extend(find_gos(proto, name))

    importing: set[int] = set()
    for go in seeds:
        collect_import(proto, tut, go, importing)

    for fid in sorted(importing):
        tut[fid] = proto[fid]
        tut_order.append(fid)

    existing_after = set(tut)

    for go in list(find_gos(tut, n) for n in LAYOUT_NAMES):
        pass

    layout_gos = []
    for name in LAYOUT_NAMES:
        for go in find_gos(proto, name):
            if go in tut and tut[go][0] == 1:
                layout_gos.append(go)

    for go in layout_gos:
        src_rect = transform_id(proto, go)
        dst_rect = transform_id(tut, go)
        if src_rect is None or dst_rect is None or src_rect != dst_rect:
            continue
        src_body = proto[src_rect][1]
        dst_body = tut[dst_rect][1]
        for key in RECT_KEYS:
            dst_body = copy_line(src_body, dst_body, key)
        tut[dst_rect] = (tut[dst_rect][0], dst_body)

    for go in find_gos(proto, "BeatTimelineUI") + find_gos(proto, "Viewport") + find_gos(proto, "ScrollContent") + find_gos(proto, "LaneLines") + find_gos(proto, "LaneAvatarGutter") + find_gos(proto, "CardTemplate") + find_gos(proto, "BarStack") + find_gos(proto, "HealthSlot") + find_gos(proto, "GaugeSlot") + find_gos(proto, "BossTrackFrame") + find_gos(proto, "BossNoteClusterLayer"):
        if go not in proto or go not in tut:
            continue
        src_rect = transform_id(proto, go)
        dst_rect = transform_id(tut, go)
        if src_rect is None or dst_rect is None:
            continue
        wanted = []
        for child_go in child_game_objects(proto, go):
            child_name = doc_name(proto[child_go][1])
            if child_name in SKIP_NAMES:
                continue
            child_rect = transform_id(proto, child_go)
            if child_rect is None:
                continue
            if child_rect in tut or child_rect in importing:
                wanted.append(child_rect)
        body = set_children(tut[dst_rect][1], wanted)
        tut[dst_rect] = (tut[dst_rect][0], body)

    # Timeline view fields live on the shared component.
    view_id = 1236853536
    if view_id in proto and view_id in tut:
        src = proto[view_id][1]
        dst = tut[view_id][1]
        for key in VIEW_KEYS:
            dst = copy_line(src, dst, key)
        tut[view_id] = (tut[view_id][0], dst)

    image_id = 1236853534
    if image_id in proto and image_id in tut:
        dst = copy_line(proto[image_id][1], tut[image_id][1], "m_Color")
        tut[image_id] = (tut[image_id][0], dst)

    for bar_name in ("PartyStatusBarUI", "EnemyStatusBarUI"):
        for go in find_gos(proto, bar_name):
            if go not in tut:
                continue
            for comp in components_of(proto[go][1]):
                if comp in tut and "cardSpacing:" in proto[comp][1]:
                    dst = copy_line(proto[comp][1], tut[comp][1], "cardSpacing")
                    tut[comp] = (tut[comp][0], dst)

    for fill_name in ("HealthBarBg", "HealthBarFill", "GaugeBarBg", "GaugeBarFill"):
        for go in find_gos(proto, fill_name):
            if go not in tut:
                continue
            for comp in components_of(proto[go][1]):
                if comp not in tut or comp not in proto:
                    continue
                if proto[comp][0] == 114 and "m_Sprite:" in proto[comp][1]:
                    tut[comp] = proto[comp]

    enemy_avatar = 1932001030
    if enemy_avatar in tut:
        for comp in components_of(tut[enemy_avatar][1]):
            if comp in tut and "m_Sprite:" in tut[comp][1]:
                body = re.sub(
                    r"^  m_Sprite: .*$",
                    f"  m_Sprite: {MIMI_SPRITE}",
                    tut[comp][1],
                    count=1,
                    flags=re.M,
                )
                body = copy_line("  m_PreserveAspect: 1\n", body, "m_PreserveAspect") if False else body
                body = re.sub(r"^  m_PreserveAspect: .*$", "  m_PreserveAspect: 1", body, count=1, flags=re.M)
                tut[comp] = (tut[comp][0], body)

    remove: set[int] = set()
    for name in ("BuffAstraTv", "BuffReduceS2", "AstraStageTv"):
        for go in find_gos(tut, name):
            remove.add(go)
            for comp in components_of(tut[go][1]):
                remove.add(comp)

    for fid in remove:
        tut.pop(fid, None)

    for fid, (typ, body) in list(tut.items()):
        if fid in remove:
            continue
        updated = body
        for gone in remove:
            updated = updated.replace(f"{{fileID: {gone}}}", "{fileID: 0}")
        if updated != body:
            tut[fid] = (typ, updated)

    # Drop removed ids and keep newly appended ids that survived.
    tut_order = [fid for fid in tut_order if fid in tut]
    save(TUT, tut_header, tut, tut_order, crlf)
    print(f"imported {len(importing)} documents, removed {len(remove)} tv documents")
    print("tutorial now has BossTrackFrame", 749800628 in tut, "Avatar enemy", enemy_avatar in tut)
    print("BuffAstraTv left", len(find_gos(tut, "BuffAstraTv")))


if __name__ == "__main__":
    main()
