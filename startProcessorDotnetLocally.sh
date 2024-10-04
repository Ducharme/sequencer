#!/bin/sh

export RUN_ENV_FILE=.env.local

FOLDER=$(pwd)


. ./setConfigValues.sh
. ./setEnvFileValues.sh
. ./getContainerIP.sh
setDependenciesEndpoints


# ProcessorService

echo "cd $PROCESSOR_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0"
cd $PROCESSOR_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
rm -f app-ps-*.log

echo "dotnet $PROCESSOR_ASSEMBLY_FILE"
dotnet $PROCESSOR_ASSEMBLY_FILE

echo "DONE RUNNING!"
