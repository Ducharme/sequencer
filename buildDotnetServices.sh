#!/bin/sh

configuration=${1:-Debug}

if [ "$configuration" != "Debug" ] && [ "$configuration" != "Release" ]; then
    echo "Invalid configuration: $configuration. Configuration should be Debug or Release."
    exit 1
fi

echo "Building as $configuration..."

dotnet restore Sequencer.sln
dotnet build Sequencer.sln --configuration $configuration

echo "DONE BUILDING!"
