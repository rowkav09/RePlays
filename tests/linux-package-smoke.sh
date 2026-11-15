#!/usr/bin/env bash
set -euo pipefail

archive=$(realpath "${1:?Pass the RePlays Linux tarball}")
start_minimized=${2:-false}
scratch=$(mktemp -d)
app_pid=
launcher_pid=
cleanup() {
  if [[ -n "$app_pid" ]]; then kill -KILL "$app_pid" 2>/dev/null || true; fi
  if [[ -n "$launcher_pid" ]]; then wait "$launcher_pid" 2>/dev/null || true; fi
  rm -rf "$scratch"
}
trap cleanup EXIT

# Extract where characters in the install path have to be escaped in the WebView's file URL.
install_dir="$scratch/RePlays #1"
mkdir -p "$install_dir"
tar -xzf "$archive" -C "$install_dir" --strip-components=1

export HOME="$scratch/home" XDG_VIDEOS_DIR="$scratch/home/Videos"
export REPLAYS_WEB_PORT=38417
mkdir -p "$XDG_VIDEOS_DIR" "$scratch/cfg"
printf '{"generalSettings":{"startMinimized":%s}}\n' "$start_minimized" > "$scratch/cfg/userSettings.json"

cd "$install_dir"
log="$scratch/app.log"
start=$SECONDS
xvfb-run -a bash -c 'echo "$$" > "$1"; exec ./RePlays --debug' _ "$scratch/app.pid" > "$log" 2>&1 &
launcher_pid=$!

ready=no
for ((i = 0; i < 120; i++)); do
  if [[ -s "$scratch/app.pid" ]]; then read -r app_pid < "$scratch/app.pid"; fi
  if grep -q 'Initialize ::: .*RePlays/WebView' "$log"; then ready=yes; break; fi
  if ! kill -0 "$launcher_pid" 2>/dev/null; then break; fi
  sleep 0.5
done
elapsed=$((SECONDS - start))
cat "$log"
if [[ "$ready" != yes || -z "$app_pid" ]] || ! kill -0 "$app_pid" 2>/dev/null; then
  echo "The packaged WebView did not connect to the app." >&2
  exit 1
fi
if [[ "$start_minimized" == true ]] && ! grep -q 'No system tray, opening the interface instead' "$log"; then
  echo "The app did not take the no-tray fallback path." >&2
  exit 1
fi

curl --fail --silent --show-error --max-time 5 --noproxy '*' \
  "http://localhost:$REPLAYS_WEB_PORT/" -o "$scratch/served.html"
cmp ClientApp/build/index.html "$scratch/served.html"

sleep 5
rss_kb=$(awk '/VmRSS/ {print $2}' "/proc/$app_pid/status")
cpu_start=$(awk '{print $14 + $15}' "/proc/$app_pid/stat")
sleep 10
cpu_end=$(awk '{print $14 + $15}' "/proc/$app_pid/stat")
cpu_pct=$(awk -v ticks="$((cpu_end - cpu_start))" -v rate="$(getconf CLK_TCK)" \
  'BEGIN {printf "%.1f", 10 * ticks / rate}')

# Report shutdown behavior without imposing a new requirement on the app.
kill -TERM "$app_pid"
stopped=no
for ((i = 0; i < 20; i++)); do
  if ! kill -0 "$app_pid" 2>/dev/null; then app_pid=; stopped=yes; break; fi
  sleep 0.5
done
{
  echo "- Packaged interface connected after ${elapsed}s"
  echo "- Local server returned the packaged index"
  echo "- RSS: $((rss_kb / 1024)) MB"
  echo "- Idle CPU over 10s: $cpu_pct% of one core"
  echo "- Exited within 10s of SIGTERM: $stopped"
} | tee -a "${GITHUB_STEP_SUMMARY:-/dev/null}"
