#!/usr/bin/env bash
# Playtest AETHVIRSE on Linux without building on this machine.
#
#   tools/play.sh                 newest successful CI build (any branch)
#   tools/play.sh <branch>        newest successful CI build of that branch
#   tools/play.sh --offline       run the last downloaded build again
#   tools/play.sh [...] -- ARGS   pass ARGS to the game, e.g. `tools/play.sh -- -touch-ui`
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

# Split our arguments from the game's (everything after --).
OURS=()
GAME_ARGS=()
while [[ $# -gt 0 ]]; do
	if [[ "$1" == "--" ]]; then shift; GAME_ARGS=("$@"); break; fi
	OURS+=("$1"); shift
done
set -- "${OURS[@]}"

# The player binary is AETHVIRSE (GameCI) or AETHVIRSE.x86_64 (Unity default naming).
game_binary() { for f in "$1/AETHVIRSE" "$1/AETHVIRSE.x86_64"; do [[ -f "$f" ]] && { echo "$f"; return; }; done; }

latest_local() { ls -1d "$ROOT"/*/ 2>/dev/null | sort -V | tail -1 | sed "s:/$::"; }

if [[ "${1:-}" == "--offline" ]]; then
	DIR="$(latest_local)"
	[[ -n "$DIR" ]] || { echo "No downloaded build yet. Run without --offline first." >&2; exit 1; }
else
	BRANCH_ARGS=()
	[[ -n "${1:-}" ]] && BRANCH_ARGS=(--branch "$1")
	RUN_ID="$(gh run list -R "$REPO" --workflow build.yml --status success "${BRANCH_ARGS[@]}" -L 1 \
		--json databaseId --jq '.[0].databaseId // empty')"
	[[ -n "$RUN_ID" ]] || { echo "No successful build found${1:+ for branch $1}." >&2; exit 1; }
	DIR="$ROOT/$RUN_ID"
	if [[ -z "$(game_binary "$DIR")" ]]; then
		echo "Downloading build from run $RUN_ID ..."
		rm -rf "$DIR.part"
		if ! gh run download "$RUN_ID" -R "$REPO" -n "$ARTIFACT" -D "$DIR.part"; then
			rm -rf "$DIR.part"
			echo "Run $RUN_ID has no Linux build (older run?). Try: tools/play.sh --offline" >&2
			exit 1
		fi
		rm -rf "$DIR" && mv "$DIR.part" "$DIR"
	fi
	# Keep only the newest builds (by run id).
	ls -1d "$ROOT"/*/ | sort -V | head -n -"$KEEP" | xargs -r rm -rf
fi

GAME="$(game_binary "$DIR")"
[[ -n "$GAME" ]] || { echo "No game binary in $DIR" >&2; exit 1; }
chmod +x "$GAME"
echo "Starting $GAME"
cd "$DIR"
# Fullscreen at the monitor's (scaled) size from the first frame. Without this, Hyprland's first tile size
# (e.g. 860x836) became the game resolution, stretched over the screen and saved for next time.
SCREEN_ARGS=()
if command -v hyprctl >/dev/null && [[ " ${GAME_ARGS[*]} " != *" -screen-width "* && " ${GAME_ARGS[*]} " != *" -windowed "* ]]; then
	read -r W H < <(hyprctl monitors -j 2>/dev/null | python3 -c 'import json,sys
m=[x for x in json.load(sys.stdin) if x.get("focused")] or [None]
m=m[0]
print(round(m["width"]/m["scale"]), round(m["height"]/m["scale"])) if m else print("")' 2>/dev/null)
	[[ -n "${W:-}" && -n "${H:-}" ]] && SCREEN_ARGS=(-screen-fullscreen 1 -screen-width "$W" -screen-height "$H")
fi
exec "$GAME" -logFile "$DIR/player.log" "${SCREEN_ARGS[@]}" "${GAME_ARGS[@]}"
