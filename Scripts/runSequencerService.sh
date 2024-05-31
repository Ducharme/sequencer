#!/bin/sh

export RUN_ENV_FILE=.env.production
cd ~/SequencerService/
. ~/setEnvVars.sh
dotnet SequencerService.dll
