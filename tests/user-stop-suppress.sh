#!/usr/bin/env bash
# Regression test for a user-stopped recording being restarted by auto detection (#280).
# Needs Xvfb, xclock and xdotool; uses a fake recorder only (no libobs, no real capture).
set -euo pipefail
assembly=$(realpath "${1:?Pass built RePlays.dll path}")
probe=$(mktemp -d)
trap 'kill ${clock_pid:-} ${xvfb_pid:-} 2>/dev/null || true; rm -rf "$probe"' EXIT
root=$(cd "$(dirname "$0")/.." && pwd)
cat > "$probe/Probe.csproj" <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="RePlays"><HintPath>$(RePlaysAssembly)</HintPath></Reference></ItemGroup></Project>
PROJECT
cp "$root/tests/user-stop-suppress/Program.cs.txt" "$probe/Program.cs"
touch "$probe/Probe.sln"
dotnet build "$probe/Probe.csproj" -p:RePlaysAssembly="$assembly" --nologo
Xvfb :77 -screen 0 800x600x24 >/dev/null 2>&1 & xvfb_pid=$!
sleep 1
export DISPLAY=:77
xclock >/dev/null 2>&1 & clock_pid=$!
sleep 1
export WID=$(xdotool search --pid "$clock_pid" | head -1)
timeout --kill-after=1s 40s dotnet "$probe/bin/Debug/net8.0/Probe.dll"
