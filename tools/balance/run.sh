#!/usr/bin/env bash
# Runs the balance harness (real Sim + scripted commander) with Unity's bundled .NET 8. No Unity needed.
set -e
cd "$(dirname "$0")/../.."
ED="${UNITY_EDITOR_DATA:-/c/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data}"
DOTNET="$ED/NetCoreRuntime/dotnet.exe"
CSC="$(ls -d "$ED"/DotNetSdk/sdk/*/Roslyn/bincore/csc.dll | head -1)"
FW="$(ls -d "$ED"/NetCoreRuntime/shared/Microsoft.NETCore.App/* | head -1)"
OUT=Temp/balance
mkdir -p "$OUT"
REFS=()
for d in "$FW"/System*.dll "$FW"/netstandard.dll "$FW"/mscorlib.dll; do case "$d" in *Native*) continue;; esac; REFS+=("-r:$(cygpath -w "$d")"); done
"$DOTNET" exec "$CSC" -nologo -noconfig -nostdlib -langversion:10 -nowarn:0649,0414,0169 -out:"$OUT/Harness.dll" "${REFS[@]}" \
  $(find Assets/Scripts/Sim -name '*.cs') Assets/Tests/EditMode/ScriptedCommander.cs tools/balance/Harness.cs
VER=$(basename "$FW")
cat > "$OUT/Harness.runtimeconfig.json" <<JSON
{ "runtimeOptions": { "tfm": "net8.0", "framework": { "name": "Microsoft.NETCore.App", "version": "$VER" } } }
JSON
"$DOTNET" "$OUT/Harness.dll" "$@"
