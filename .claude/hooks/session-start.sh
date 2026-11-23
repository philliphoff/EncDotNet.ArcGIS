#!/bin/bash
# Installs the .NET SDK and restores NuGet packages in Claude Code cloud
# sessions. The cloud network policy blocks the dotnet-install script's
# download host, so the SDK comes from the Ubuntu package feed instead.
#
# Cloud sessions run on Linux: `dotnet build` and `dotnet format` work there
# (Directory.Build.props sets EnableWindowsTargeting), but the tests need the
# Windows desktop runtime, packaging the .esriAddinX needs Visual Studio's
# MSBuild, and running the add-in needs ArcGIS Pro.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks | grep -q '^10\.'; then
  export DEBIAN_FRONTEND=noninteractive
  apt-get update -qq
  apt-get install -y -qq dotnet-sdk-10.0
fi

cd "$CLAUDE_PROJECT_DIR"
dotnet restore EncDotNet.ArcGIS.slnx --verbosity quiet
