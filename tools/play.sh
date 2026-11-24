#!/usr/bin/env bash
# Playtest AETHVIRSE on Linux without building on this machine.
#
#   tools/play.sh                 newest successful CI build (any branch)
#   tools/play.sh <branch>        newest successful CI build of that branch
#   tools/play.sh --offline       run the last downloaded build again
#
# Downloads the native Linux build (artifact AETHVIRSE-StandaloneLinux64) from GitHub Actions
# into ~/Unduhan/game/AETHVIRSE/linux/<run id>/ once, keeps the two newest, and starts it.
# Needs `gh` logged in. The game log goes to player.log next to the game.
set -euo pipefail

REPO="Kodok-Kejepit/aethvirse"
ARTIFACT="AETHVIRSE-StandaloneLinux64"
ROOT="${AETHVIRSE_PLAY_DIR:-$HOME/Unduhan/game/AETHVIRSE/linux}"
KEEP=2
mkdir -p "$ROOT"

latest_local() { ls -1d "$ROOT"/*/ 2>/dev/null | sort -V | tail -1; }

if [[ "${1:-}" == "--offline" ]]; then
	DIR="$(latest_local)"
	[[ -n "$DIR" ]] || { echo "No downloaded build yet. Run without --offline first." >&2; exit 1; }
else
	BRANCH_ARGS=()
	[[ -n "${1:-}" ]] && BRANCH_ARGS=(--branch "$1")
	RUN_ID="$(gh run list -R "$REPO" --workflow build.yml --status success "${BRANCH_ARGS[@]}" -L 1 \
		--json databaseId --jq '.[0].databaseId // empty')"
	[[ -n "$RUN_ID" ]] || { echo "No successful build found${1:+ for branch $1}." >&2; exit 1; }
	DIR="$ROOT/$RUN_ID/"
	if [[ ! -x "$DIR/AETHVIRSE.x86_64" ]]; then
		echo "Downloading build from run $RUN_ID ..."
		rm -rf "$DIR.part"
		if ! gh run download "$RUN_ID" -R "$REPO" -n "$ARTIFACT" -D "$DIR.part"; then
			rm -rf "$DIR.part"
			echo "Run $RUN_ID has no Linux build (older run?). Try: tools/play.sh --offline" >&2
			exit 1
		fi
		rm -rf "$DIR" && mv "$DIR.part" "$DIR"
		chmod +x "$DIR/AETHVIRSE.x86_64"
	fi
	# Keep only the newest builds (by run id).
	ls -1d "$ROOT"/*/ | sort -V | head -n -"$KEEP" | xargs -r rm -rf
fi

echo "Starting $DIR"
cd "$DIR"
exec ./AETHVIRSE.x86_64 -logFile "$DIR/player.log"
