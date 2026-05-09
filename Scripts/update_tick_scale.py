#!/usr/bin/env python3

from __future__ import annotations

import argparse
import re
from pathlib import Path


DEFAULT_FIELDS = [
    "BreedCooldown",
    "AttackCooldown",
    "MaturityAge",
    "MaxAge",
    "RestMin",
    "RestMax",
]


def format_number(value: float) -> str:
    if value.is_integer():
        return str(int(value))
    text = f"{value:.6f}".rstrip("0").rstrip(".")
    return text


def scale_file(path: Path, factor: float, fields: list[str], dry_run: bool) -> int:
    original = path.read_text()
    updated = original
    changes = 0

    for field in fields:
        pattern = re.compile(rf"(\b{re.escape(field)}\s*=\s*)(\d+)")

        def repl(match: re.Match[str]) -> str:
            nonlocal changes
            old_value = int(match.group(2))
            new_value = round(old_value * factor)
            changes += 1
            return f"{match.group(1)}{new_value}"

        updated = pattern.sub(repl, updated)

    if changes == 0:
        print(f"{path}: no matching tick fields found")
        return 0

    if dry_run:
        print(f"{path}: would update {changes} values with factor {format_number(factor)}")
        return changes

    if updated != original:
        path.write_text(updated)
        print(f"{path}: updated {changes} values with factor {format_number(factor)}")
    else:
        print(f"{path}: no content change")

    return changes


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Scale tick-based entity registry values after changing the simulation tick length."
    )
    parser.add_argument(
        "paths",
        nargs="*",
        default=["Core/EntityRegistry.cs"],
        help="C# files to update. Defaults to Core/EntityRegistry.cs.",
    )
    parser.add_argument(
        "--from-ticks-per-day",
        type=float,
        default=720.0,
        help="Original ticks per day used when the registry values were authored.",
    )
    parser.add_argument(
        "--to-ticks-per-day",
        type=float,
        default=1440.0,
        help="New ticks per day after the scale change.",
    )
    parser.add_argument(
        "--fields",
        nargs="*",
        default=DEFAULT_FIELDS,
        help="Field names to scale. Defaults to the common tick-duration fields.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show the planned updates without writing files.",
    )
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    if args.from_ticks_per_day <= 0 or args.to_ticks_per_day <= 0:
        raise SystemExit("ticks per day must be positive")

    factor = args.to_ticks_per_day / args.from_ticks_per_day

    total_changes = 0
    for raw_path in args.paths:
        total_changes += scale_file(Path(raw_path), factor, args.fields, args.dry_run)

    return 0 if total_changes > 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())