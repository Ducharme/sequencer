#!/bin/sh

NB_PROCESSORS=1
NB_SEQUENCERS=1
ADMIN_WAIT_TIME_SEC=2
SEQUENCER_WAIT_TIME_SEC=2
PROCESSOR_WAIT_TIME_SEC=5
#CONFIG=Debug

NB_MESSAGES=300
CREATION_DELAY_MS=0
PROCESSING_DELAY_MS=0
export RUN_ENV_FILE=.env.local

FOLDER=$(pwd)

. ./sharedRunTest.sh
. ./sharedAppDotnetTest.sh
. ./sharedAppDotnetSvc.sh
. ./setConfigValues.sh
. ./setEnvFileValues.sh
. ./getContainerIP.sh

halt_all_locally
start_dependencies_containers
setDependenciesEndpoints

ADMIN_PROCESS_HOST=localhost
ADMIN_PROCESS_PORT=5002
start_dotnet_services

cd $FOLDER

run_test $ADMIN_PROCESS_HOST $ADMIN_PROCESS_PORT
sh killDotnetServicesLocally.sh

echo "DONE RUNNING!"
