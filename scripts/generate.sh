#!/usr/bin/env bash
# Regenerate src/Hanzo.AI/{Api,Client,Model} from the Hanzo Cloud API document.
#
# A call site, not a generator invocation: the invocation lives once, in
# hanzoai/openapi `generate.py`, and every C# knob is the `csharp` row of its
# `sdks.yaml`.
#
#   ./scripts/generate.sh            # regenerate in place
#   ./scripts/generate.sh --check    # non-zero if the committed client drifted
#
# $SPEC is the document, already fetched at the ref `.spec-lock` names and
# digest-checked; $OPENAPI is a checkout of hanzoai/openapi. hanzoai/ci's
# client lane sets both. Requires uv and java 11+.
set -euo pipefail
cd "$(dirname "$0")/.."

: "${OPENAPI:?the generator lives in hanzoai/openapi; the ci client lane sets OPENAPI, or point it at a checkout}"

if [ -n "${SPEC:-}" ]; then set -- --spec "$SPEC" "$@"; fi

exec uv run --with pyyaml python3 "$OPENAPI/generate.py" csharp --repo "$PWD" "$@"
