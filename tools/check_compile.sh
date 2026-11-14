#!/usr/bin/env bash
# Fast compile check without opening Unity (uses the editor's bundled Roslyn).
# Needs the project imported once so Library/ScriptAssemblies exists.
# Usage: bash tools/check_compile.sh
set -e
cd "$(dirname "$0")/.."
ED="${UNITY_EDITOR_DATA:-/c/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data}"
DOTNET="$ED/NetCoreRuntime/dotnet.exe"
CSC="$(ls -d "$ED"/DotNetSdk/sdk/*/Roslyn/bincore/csc.dll | head -1)"
OUT=Temp/check
mkdir -p "$OUT"
NETSTD="$ED/NetStandard/ref/2.1.0/netstandard.dll"
ENGINE="$ED/Managed/UnityEngine"
SA=Library/ScriptAssemblies

csc() { "$DOTNET" exec "$CSC" -nologo -noconfig -nostdlib -langversion:9 -nowarn:1701,1702,0649,0414,0169 "$@"; }

win() { cygpath -w "$1" 2>/dev/null || echo "$1"; }

echo "== Sim"
csc -target:library -out:"$OUT/OutpostGale.Sim.dll" -r:"$(win "$NETSTD")" $(find Assets/Scripts/Sim -name '*.cs')

ENGINE_REFS=()
for d in "$ENGINE"/UnityEngine*.dll; do ENGINE_REFS+=("-r:$(win "$d")"); done

echo "== Game"
csc -target:library -out:"$OUT/OutpostGale.Game.dll" -r:"$(win "$NETSTD")" -r:"$OUT/OutpostGale.Sim.dll" "${ENGINE_REFS[@]}" \
  -r:"$SA/Unity.InputSystem.dll" -r:"$SA/UnityEngine.UI.dll" -r:"$SA/Unity.RenderPipelines.Universal.Runtime.dll" -r:"$SA/Unity.RenderPipelines.Universal.2D.Runtime.dll" -r:"$SA/Unity.RenderPipelines.Core.Runtime.dll" \
  $(find Assets/Scripts/Game -name '*.cs')

NUNIT=$(find Library/PackageCache -path '*net472/unity-custom/nunit.framework.dll' | head -1)
echo "== Tests"
csc -target:library -out:"$OUT/OutpostGale.Tests.dll" -r:"$(win "$NETSTD")" -r:"$OUT/OutpostGale.Sim.dll" "${ENGINE_REFS[@]}" -r:"$NUNIT" \
  -r:"$(win "$ED/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll")" -r:"$SA/UnityEngine.TestRunner.dll" $(find Assets/Tests/EditMode -name '*.cs')
echo "OK"
