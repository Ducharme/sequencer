#!/bin/sh

export RUN_ENV_FILE=.env.production
cd ~/AdminWebPortal/
. ~/setEnvVars.sh
dotnet AdminWebPortal.dll
