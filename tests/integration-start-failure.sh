#!/usr/bin/env bash
# CS2 config path on Linux, and an integration that fails to start must not crash the app. Synthetic session only.
set -euo pipefail
assembly=$(realpath "${1:?Pass built RePlays.dll path}")
probe=$(mktemp -d)
trap 'rm -rf "$probe"' EXIT
root=$(cd "$(dirname "$0")/.." && pwd)
cat > "$probe/Probe.csproj" <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="RePlays"><HintPath>$(RePlaysAssembly)</HintPath></Reference></ItemGroup></Project>
PROJECT
cp "$root/tests/integration-start-failure/Program.cs.txt" "$probe/Program.cs"
touch "$probe/Probe.sln"
dotnet build "$probe/Probe.csproj" -p:RePlaysAssembly="$assembly" --nologo
timeout --kill-after=1s 40s dotnet "$probe/bin/Debug/net8.0/Probe.dll"
