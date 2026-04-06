#!/usr/bin/env bash
# Checks that every Localizer["..."] key used in .cshtml views
# has a corresponding msgid in every .po file under Localization/.
#
# Exit code 0 = all keys present, 1 = missing translations found.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
VIEWS_DIR="$SCRIPT_DIR/PortalCalendarServer/Views"
LOCALIZATION_DIR="$SCRIPT_DIR/PortalCalendarServer/Localization"

# ── 1. Extract Localizer keys from .cshtml files ─────────────

extract_keys() {
    find "$VIEWS_DIR" -name '*.cshtml' -print0 \
        | xargs -0 perl -ne '
            while (/Localizer\["((?:[^"\\]|\\.)*)"/g) {
                print "$1\n";
            }
        ' \
        | sort -u
}

# ── 2. Extract msgid values from a .po file ──────────────────

extract_msgids() {
    local po_file="$1"
    # Match msgid "..." lines, skip the empty header msgid.
    perl -ne '
        if (/^msgid "((?:[^"\\]|\\.)+)"/) {
            print "$1\n";
        }
    ' "$po_file" \
        | sort -u
}

# ── 3. Compare ───────────────────────────────────────────────

found_missing=0

keys_file=$(mktemp)
extract_keys > "$keys_file"
key_count=$(wc -l < "$keys_file")
echo "Found $key_count unique Localizer keys in .cshtml files."

for po_file in "$LOCALIZATION_DIR"/*.po; do
    lang=$(basename "$po_file")
    msgids_file=$(mktemp)
    extract_msgids "$po_file" > "$msgids_file"
    msgid_count=$(wc -l < "$msgids_file")
    echo "Found $msgid_count msgids in $lang."

    # Keys in views but not in the .po file
    missing=$(comm -23 "$keys_file" "$msgids_file")
    if [[ -n "$missing" ]]; then
        found_missing=1
        count=$(echo "$missing" | wc -l)
        echo ""
        echo "::error::$lang is missing $count translation(s):"
        echo "$missing" | while IFS= read -r key; do
            echo "  - \"$key\""
        done
    fi

    rm -f "$msgids_file"
done

rm -f "$keys_file"

echo ""
if [[ "$found_missing" -eq 1 ]]; then
    echo "FAIL: Some Localizer keys are missing from .po files."
    exit 1
else
    echo "OK: All Localizer keys are present in every .po file."
    exit 0
fi
