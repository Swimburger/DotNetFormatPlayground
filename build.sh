#!/bin/sh
set -eu

if ! dotnet workload list | grep -q '^wasm-tools[[:space:]]'; then
  dotnet workload install wasm-tools
fi

dotnet publish DotNetFormatPlayground.csproj --configuration Release --output dist --disable-build-servers -p:UseSharedCompilation=false
