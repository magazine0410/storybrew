#!/usr/bin/env bash
# Builds packaging/appimage/out/storybrew-<version>-x86_64.AppImage
#
# Needs the .NET 10 SDK and appimagetool, from https://github.com/AppImage/appimagetool/releases:
# set APPIMAGETOOL to its path if it isn't in PATH, and RUNTIME_FILE to a type2 runtime
# (https://github.com/AppImage/type2-runtime/releases) to avoid appimagetool downloading it.
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
appimagetool="${APPIMAGETOOL:-appimagetool}"

version="$(sed -n 's|.*<AssemblyVersion>\(.*\)</AssemblyVersion>.*|\1|p' "$root/editor/editor.csproj")"
out="$here/out"
appdir="$out/storybrew.AppDir"
lib="$appdir/usr/lib/storybrew"

rm -rf "$appdir"
mkdir -p "$lib"

# Self-contained, with the reference assemblies scripts are compiled against (refs/)
dotnet publish "$root/editor/editor.csproj" -c Release -r linux-x64 --self-contained true \
    -p:PreserveCompilationReferences=true -o "$lib"

# Common scripts, copied to the user's data folder on startup
mkdir -p "$lib/scripts"
cp "$root"/scripts/*.cs "$lib/scripts/"

cp "$here/AppRun" "$here/storybrew.desktop" "$here/storybrew.png" "$appdir/"
ln -sf storybrew.png "$appdir/.DirIcon"

runtime_args=()
if [ -n "${RUNTIME_FILE:-}" ]; then
    runtime_args=(--runtime-file "$RUNTIME_FILE")
fi

ARCH=x86_64 "$appimagetool" "${runtime_args[@]}" "$appdir" "$out/storybrew-$version-x86_64.AppImage"
