#!/usr/bin/env bash
# Compressing a clip must never lose the original, whatever its extension or whether ffmpeg fails.
# Synthetic fake ffmpeg/ffprobe only, no real encoding.
set -euo pipefail
assembly=$(realpath "${1:?Pass built RePlays.dll path}")
probe=$(mktemp -d)
trap 'rm -rf "$probe"' EXIT
root=$(cd "$(dirname "$0")/.." && pwd)
cat > "$probe/Probe.csproj" <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="RePlays"><HintPath>$(RePlaysAssembly)</HintPath></Reference></ItemGroup></Project>
PROJECT
cp "$root/tests/compress-keeps-original/Program.cs.txt" "$probe/Program.cs"
touch "$probe/Probe.sln"
mkdir -p "$probe/ClientApp/node_modules/ffmpeg-ffprobe-static"
cat > "$probe/ffmpeg" <<'PYTHON'
#!/usr/bin/env python3
import sys
a=sys.argv[1:]
src=a[a.index('-i')+1]; out=a[-1]
if 'failclip' in src: sys.exit(1)
if out==src:
    print("Output same as Input #0 - exiting.", file=sys.stderr); sys.exit(1)
open(out,'wb').write(b'x'*500)
if 'partialclip' in src: sys.exit(1)
PYTHON
cat > "$probe/ffprobe" <<'PYTHON'
#!/usr/bin/env python3
import sys
a=sys.argv[1:]; src=a[a.index('-i')+1]
if 'probebadclip' in src: sys.exit(1)
if 'probeerrclip' in src: print("[mov] moov atom not found", file=sys.stderr)
PYTHON
chmod +x "$probe/ffmpeg" "$probe/ffprobe"
dotnet build "$probe/Probe.csproj" -p:RePlaysAssembly="$assembly" --nologo
cp "$probe/ffmpeg" "$probe/ffprobe" "$probe/bin/Debug/net8.0/"
cp "$probe/ffmpeg" "$probe/ffprobe" "$probe/ClientApp/node_modules/ffmpeg-ffprobe-static/"
timeout --kill-after=1s 40s dotnet "$probe/bin/Debug/net8.0/Probe.dll"
