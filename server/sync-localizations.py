#!/usr/bin/env python3
"""
Sync missing Localizer["..."] keys into all .po localization files.

Walks .cshtml and .cs files, extracts every Localizer["..."] key, compares
against each .po file, and appends any missing msgid/msgstr stubs so
translators only need to fill in the msgstr values.

Also warns about hardcoded ViewData["Title"] strings that bypass localization.

Usage:
    python sync-localizations.py            # dry-run (default)
    python sync-localizations.py --write    # actually modify .po files
"""

import argparse
import re
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
SERVER_DIR = SCRIPT_DIR / "PortalCalendarServer"
VIEWS_DIR = SERVER_DIR / "Views"
LOCALIZATION_DIR = SERVER_DIR / "Localization"

# Matches Localizer["..."] including escaped quotes inside the string
LOCALIZER_RE = re.compile(r'Localizer\["((?:[^"\\]|\\.)*)(?:"|$)')

# Matches msgid "..." lines (single-line only, matching the bash script behavior)
MSGID_RE = re.compile(r'^msgid "(.+)"$')

# Matches hardcoded ViewData["Title"] = "..." (not using Localizer)
HARDCODED_TITLE_RE = re.compile(
    r'ViewData\["Title"\]\s*=\s*(?:\$?"[^"]*"(?!\s*\.Value))\s*;'
)


def extract_localizer_keys(search_dir: Path, extensions: list[str]) -> set[str]:
    """Extract all unique Localizer["..."] keys from files with given extensions."""
    keys = set()
    for ext in extensions:
        for path in search_dir.rglob(f"*{ext}"):
            for line in path.read_text(encoding="utf-8").splitlines():
                stripped = line.lstrip()
                # Skip C# single-line and XML doc comments
                if stripped.startswith("//"):
                    continue
                # Skip Razor/HTML comments
                if stripped.startswith("@*") or stripped.startswith("*"):
                    continue
                for match in LOCALIZER_RE.finditer(line):
                    keys.add(match.group(1))
    return keys


def find_hardcoded_titles(search_dir: Path) -> list[tuple[Path, int, str]]:
    """Find ViewData['Title'] assignments that use hardcoded strings instead of Localizer."""
    results = []
    for ext in [".cshtml", ".cs"]:
        for path in search_dir.rglob(f"*{ext}"):
            for i, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
                if HARDCODED_TITLE_RE.search(line) and "Localizer[" not in line:
                    results.append((path, i, line.strip()))
    return results


def extract_msgids_from_po(po_path: Path) -> set[str]:
    """Extract all msgid values from a .po file, skipping the empty header msgid."""
    msgids = set()
    for line in po_path.read_text(encoding="utf-8").splitlines():
        m = MSGID_RE.match(line)
        if m:
            msgids.add(m.group(1))
    return msgids


def is_source_language(po_path: Path) -> bool:
    """Check if the .po file is for English (source language where msgstr == msgid)."""
    return po_path.stem == "en"


def append_missing_keys(po_path: Path, missing: list[str], *, write: bool) -> None:
    """Append missing msgid/msgstr entries to a .po file."""
    source = is_source_language(po_path)
    block = "\n# ── Auto-added missing keys ───────────────────────────\n"
    for key in missing:
        msgstr = key if source else ""
        block += f'\nmsgid "{key}"\nmsgstr "{msgstr}"\n'

    if write:
        with po_path.open("a", encoding="utf-8", newline="\n") as f:
            f.write(block)


def main() -> int:
    parser = argparse.ArgumentParser(description="Sync missing localizer keys into .po files.")
    parser.add_argument(
        "--write",
        action="store_true",
        help="Actually modify the .po files (default is dry-run).",
    )
    args = parser.parse_args()

    if not SERVER_DIR.is_dir():
        print(f"ERROR: Server directory not found: {SERVER_DIR}", file=sys.stderr)
        return 1
    if not LOCALIZATION_DIR.is_dir():
        print(f"ERROR: Localization directory not found: {LOCALIZATION_DIR}", file=sys.stderr)
        return 1

    # ── Extract Localizer keys from .cshtml and .cs files ────
    keys = extract_localizer_keys(SERVER_DIR, [".cshtml", ".cs"])
    print(f"Found {len(keys)} unique Localizer keys in .cshtml and .cs files.\n")

    # ── Check for hardcoded ViewData["Title"] ────────────────
    hardcoded = find_hardcoded_titles(SERVER_DIR)
    if hardcoded:
        print(f"WARNING: {len(hardcoded)} hardcoded ViewData[\"Title\"] assignment(s) found:")
        for path, lineno, line in hardcoded:
            rel = path.relative_to(SCRIPT_DIR)
            print(f"  {rel}:{lineno}")
            print(f"    {line}")
        print()
        print("  These bypass localization. Consider using Localizer[\"...\"] instead.")
        print()

    # ── Sync .po files ───────────────────────────────────────
    po_files = sorted(LOCALIZATION_DIR.glob("*.po"))
    if not po_files:
        print("No .po files found.", file=sys.stderr)
        return 1

    any_missing = False

    for po_path in po_files:
        lang = po_path.name
        msgids = extract_msgids_from_po(po_path)
        missing = sorted(keys - msgids)

        print(f"{lang}: {len(msgids)} existing msgids, {len(missing)} missing.")
        if missing:
            any_missing = True
            for key in missing:
                print(f'  + "{key}"')
            if args.write:
                append_missing_keys(po_path, missing, write=True)
                print(f"  -> Written to {lang}")
            else:
                print(f"  -> Dry run, use --write to add them.")

    print()
    if any_missing and not args.write:
        print("Dry run complete. Re-run with --write to apply changes.")
        return 1
    elif any_missing:
        print("Done. Missing keys have been appended.")
        print("English (en.po) entries are pre-filled; other languages need translation.")
        return 0
    else:
        if hardcoded:
            print("All Localizer keys are synced, but hardcoded titles remain (see above).")
            return 1
        print("All .po files are up to date.")
        return 0


if __name__ == "__main__":
    sys.exit(main())
