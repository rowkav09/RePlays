#!/usr/bin/env bash
set -euo pipefail
assembly=$(realpath "${1:?Pass built RePlays.dll path}")
probe=$(mktemp -d)
trap 'rm -rf "$probe"' EXIT
cat > "$probe/Probe.csproj" <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="RePlays"><HintPath>$(RePlaysAssembly)</HintPath></Reference></ItemGroup></Project>
PROJECT
root=$(cd "$(dirname "$0")/.." && pwd)
cp "$root/tests/metadata-cache-case/Program.cs.txt" "$probe/Program.cs"
# Debug finds this solution marker; Release uses the copied assembly directory.
touch "$probe/Probe.sln"
mkdir -p "$probe/ClientApp/node_modules/ffmpeg-ffprobe-static"
cat > "$probe/ffprobe" <<'PYTHON'
#!/usr/bin/env python3
import sys,os
name=os.path.basename(sys.argv[sys.argv.index('-i')+1])
print('0' if name.startswith('Upper') else ('60/1' if 'stream=avg_frame_rate' in sys.argv else '42'))
PYTHON
printf '#!/bin/sh\nexit 0\n' > "$probe/ffmpeg"
chmod +x "$probe/ffprobe" "$probe/ffmpeg"
dotnet build "$probe/Probe.csproj" -p:RePlaysAssembly="$assembly" --nologo
cp "$probe/ffprobe" "$probe/ffmpeg" "$probe/bin/Debug/net8.0/"
cp "$probe/ffprobe" "$probe/ffmpeg" "$probe/ClientApp/node_modules/ffmpeg-ffprobe-static/"
timeout --kill-after=1s 15s dotnet "$probe/bin/Debug/net8.0/Probe.dll"
