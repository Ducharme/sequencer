#!/bin/sh

export RUN_ENV_FILE=.env.local

FOLDER=$(pwd)


. ./setConfigValues.sh
. ./setEnvFileValues.sh
. ./getContainerIP.sh
export PGSQL_ENDPOINT=$(getContainerIpFromImageName "local-postgres") && echo "PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-postgres is not running, exiting" && exit 1; fi
export REDIS_ENDPOINT=$(getContainerIpFromImageName "local-redis") && echo "REDIS_ENDPOINT=$REDIS_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-redis is not running, exiting" && exit 1; fi


# ProcessorService

echo "cd $PROCESSOR_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0"
cd $PROCESSOR_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
rm -f app-ps-*.log

echo "dotnet $PROCESSOR_ASSEMBLY_FILE"
dotnet $PROCESSOR_ASSEMBLY_FILE

echo "DONE RUNNING!"
