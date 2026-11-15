#!/usr/bin/env bash
# Interface thread without a tray library must not crash and must still show the window. Needs Xvfb and GTK.
set -euo pipefail
assembly=$(realpath "${1:?Pass built RePlays.dll path}")
probe=$(mktemp -d)
trap 'rm -rf "$probe"' EXIT
root=$(cd "$(dirname "$0")/.." && pwd)
cat > "$probe/Probe.csproj" <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /><Reference Include="RePlays"><HintPath>$(RePlaysAssembly)</HintPath></Reference></ItemGroup></Project>
PROJECT
cp "$root/tests/linux-no-tray/Program.cs.txt" "$probe/Program.cs"
touch "$probe/Probe.sln"
dotnet build "$probe/Probe.csproj" -p:RePlaysAssembly="$assembly" --nologo
xvfb-run -a timeout --kill-after=1s 60s dotnet "$probe/bin/Debug/net8.0/Probe.dll"
