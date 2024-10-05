#!/bin/sh

NB_PROCESSORS=10
NB_SEQUENCERS=3
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
. ./sharedAppDotnetWebSvc.sh
. ./setConfigValues.sh
. ./setEnvFileValues.sh
. ./getContainerIP.sh

halt_all_locally
start_dependencies_containers
setDependenciesEndpoints

ADMIN_PROCESS_HOST=localhost
start_dotnet_web_services

cd $FOLDER

run_test $ADMIN_PROCESS_HOST $ADMIN_PROCESS_PORT
sh killDotnetServicesLocally.sh

echo "DONE RUNNING!"
