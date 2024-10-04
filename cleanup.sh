#!/bin/sh

dotnet clean

echo "Deleting bin and obj folders"

find "." -maxdepth 3 -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} +

echo "DONE CLEANING!"
