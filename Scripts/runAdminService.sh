#!/bin/sh

export RUN_ENV_FILE=.env.production
cd ~/AdminService/
. ~/setEnvVars.sh
dotnet AdminService.dll
