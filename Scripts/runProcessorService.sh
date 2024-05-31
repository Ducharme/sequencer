#!/bin/sh

export RUN_ENV_FILE=.env.production
cd ~/ProcessorService/
. ~/setEnvVars.sh
dotnet ProcessorService.dll
