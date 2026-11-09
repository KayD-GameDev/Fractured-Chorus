#!/usr/bin/env python3
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image


EXPECTED_W = 864
EXPECTED_H = 1152
REQUIRED_PARTS = ("Documentation", "Art_Pipeline", "Review", "Pilot", "Ren_CombatIdle")
ALLOWED_F1_NAME = "ren_combat_battle_idle_semichibi_v2_full.png"


def normalize_path(path: Path) -> Path:
    return path.resolve()


def is_under_pilot_dir(path: Path) -> bool:
    resolved = normalize_path(path)
    parts = [p.lower() for p in resolved.parts]
    needle = [p.lower() for p in REQUIRED_PARTS]
    for i in range(len(parts) - len(needle) + 1):
        if parts[i : i + len(needle)] == needle:
            return True
    return False


def friendly_exit(message: str, code: int = 1) -> None:
    print(message, file=sys.stderr)
    raise SystemExit(code)


def parse_size(text: str) -> tuple[int, int]:
    try:
        w_s, h_s = text.split(",", 1)
        return int(w_s.strip()), int(h_s.strip())
    except ValueError as exc:
        friendly_exit(f"expected-size không hợp lệ: {text!r} ({exc})")


def load_mask_raw(mask_path: Path) -> Image.Image:
    if not mask_path.is_file():
        friendly_exit(f"Không tìm thấy mask: {mask_path}")
    try:
        with Image.open(mask_path) as im:
            im.load()
            return im.copy()
    except OSError as exc:
        friendly_exit(f"Không đọc được mask: {exc}")


def load_f1_raw(f1_path: Path) -> Image.Image:
    if not f1_path.is_file():
        friendly_exit(f"Không tìm thấy F1: {f1_path}")
    try:
        with Image.open(f1_path) as im:
            im.load()
            return im.copy()
    except OSError as exc:
        friendly_exit(f"Không đọc được F1: {exc}")


def check_mask_mode_and_values(mask: Image.Image) -> tuple[int, int, int]:
    if mask.size != (EXPECTED_W, EXPECTED_H):
        friendly_exit(
            f"Mask size {mask.size[0]}×{mask.size[1]} ≠ {EXPECTED_W}×{EXPECTED_H}"
        )

    mode = mask.mode
    if mode != "L":
        friendly_exit(
            f"Mask mode phải là L (grayscale). PNG gốc: {mode!r}. Không convert tự động."
        )

    pixels = list(mask.getdata())
    white = 0
    black = 0
    non_binary = 0
    for p in pixels:
        if p == 0:
            black += 1
        elif p == 255:
            white += 1
        else:
            non_binary += 1

    if non_binary > 0:
        friendly_exit(
            f"Mask không nhị phân: {non_binary} pixel không phải 0 hoặc 255 (không sửa file)."
        )

    if white == 0:
        friendly_exit("Mask toàn 0 — không có vùng 255.")
    if black == 0:
        friendly_exit("Mask toàn 255 — không an toàn cho pilot.")

    return white, black, non_binary


def check_f1(f1: Image.Image, expected: tuple[int, int]) -> None:
    if f1.size != expected:
        friendly_exit(f"F1 size {f1.size[0]}×{f1.size[1]} ≠ mask {expected[0]}×{expected[1]}")
    if f1.mode != "RGBA":
        friendly_exit(f"F1 mode phải là RGBA. Hiện tại: {f1.mode!r}")


def check_alpha_vs_mask(f1: Image.Image, mask: Image.Image) -> int:
    alpha = f1.getchannel("A")
    violations = 0
    for i, (a, m) in enumerate(zip(alpha.getdata(), mask.getdata())):
        if a == 0 and m != 0:
            violations += 1
            if violations >= 10:
                break
    if violations > 0:
        friendly_exit(
            f"Có ít nhất {violations} pixel: F1 alpha=0 nhưng mask≠0 (vùng trong suốt)."
        )
    return violations


def mask_bbox_stats(mask: Image.Image) -> tuple[int, int, int, int, int]:
    w, h = mask.size
    min_x, min_y, max_x, max_y = w, h, -1, -1
    count = 0
    for y in range(h):
        for x in range(w):
            if mask.getpixel((x, y)) == 255:
                count += 1
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)
    return count, min_x, min_y, max_x, max_y


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Kiểm tra kỹ thuật mask pilot (không đánh giá giải phẫu)."
    )
    parser.add_argument("--mask", type=Path, required=True, help="PNG mask grayscale")
    parser.add_argument("--f1", type=Path, required=True, help="F1/SoT RGBA (read-only)")
    parser.add_argument(
        "--expected-size",
        default=f"{EXPECTED_W},{EXPECTED_H}",
        help="width,height mặc định 864,1152",
    )
    args = parser.parse_args()

    expected = parse_size(args.expected_size)

    mask_path = normalize_path(args.mask)
    f1_path = normalize_path(args.f1)

    if not is_under_pilot_dir(mask_path):
        friendly_exit(
            "Mask phải nằm trong Documentation/Art_Pipeline/Review/Pilot/Ren_CombatIdle/"
        )

    if f1_path.name.lower() != ALLOWED_F1_NAME:
        friendly_exit(
            f"F1 filename phải là {ALLOWED_F1_NAME!r} (read-only SoT)."
        )

    try:
        mask = load_mask_raw(mask_path)
        f1 = load_f1_raw(f1_path)

        white, black, _ = check_mask_mode_and_values(mask)
        check_f1(f1, expected)
        check_alpha_vs_mask(f1, mask)

        total = mask.size[0] * mask.size[1]
        count_255, bx0, by0, bx1, by1 = mask_bbox_stats(mask)
        pct = (count_255 / total) * 100.0

        print("PASS — kiểm tra kỹ thuật mask (không kết luận vùng giải phẫu).")
        print(f"  mask: {mask_path}")
        print(f"  f1:   {f1_path} (read-only)")
        print(f"  mode: L | pixels 0: {black} | 255: {count_255} ({pct:.4f}%)")
        if count_255 > 0:
            print(
                f"  bbox 255 (chỉ số): x={bx0}..{bx1}, y={by0}..{by1} "
                "(spot-check bằng mắt — không QC tự động hình dáng)."
            )
        print(f"  alpha0∩mask255 violations: 0")
        raise SystemExit(0)
    except SystemExit:
        raise
    except Exception as exc:
        print(f"Lỗi không mong đợi: {exc}", file=sys.stderr)
        raise SystemExit(1)


if __name__ == "__main__":
    main()
