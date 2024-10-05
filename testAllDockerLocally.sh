#!/bin/sh

NB_PROCESSORS=10
NB_SEQUENCERS=3
ADMIN_WAIT_TIME_SEC=10
SEQUENCER_WAIT_TIME_SEC=10
PROCESSOR_WAIT_TIME_SEC=10
DOCKER_START_DELAY=3

NB_MESSAGES=300
CREATION_DELAY_MS=0
PROCESSING_DELAY_MS=0
export RUN_ENV_FILE=.env.local

AWP_NAME=adminwebportal
PWS_NAME=processorservice
SWS_NAME=sequencerservice

. ./sharedRunTest.sh
. ./sharedAppContainersTest.sh
check_images_exist $AWP_NAME $PWS_NAME $SWS_NAME 
halt_all_locally
start_dependencies_containers
start_app_containers $AWP_NAME $PWS_NAME $SWS_NAME
run_test $AWP_CONTAINER_HOST $AWP_CONTAINER_PORT
sh killDockerServicesLocally.sh

echo "DONE RUNNING!"
