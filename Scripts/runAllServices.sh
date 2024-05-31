#!/bin/sh

export RUN_ENV_FILE=.env.production

cd ~/AdminService/
. ~/setEnvVars.sh
dotnet AdminService.dll

cd ~/ProcessorService/
. ~/setEnvVars.sh
dotnet ProcessorService.dll

cd ~/SequencerService/
. ~/setEnvVars.sh
dotnet SequencerService.dll
