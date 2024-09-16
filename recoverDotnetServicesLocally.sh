#!/bin/sh

# Called by disruptor_close_tcp.sh and disruptor_kill_proc

. ./setConfigValues.sh
. ./setEnvFileValues.sh
. ./getContainerIP.sh
export PGSQL_ENDPOINT=$(getContainerIpFromImageName "local-postgres") && echo "PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-postgres is not running, exiting" && exit 1; fi
export REDIS_ENDPOINT=$(getContainerIpFromImageName "local-redis") && echo "REDIS_ENDPOINT=$REDIS_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-redis is not running, exiting" && exit 1; fi


# AdminService

ADMIN_CONTAINER_HOST=localhost
ADMIN_CONTAINER_PORT=5002
export ASPNETCORE_URLS="http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT"


# SequencerService

cd $SEQUENCER_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
for i in $(seq 1 "$NB_SEQUENCERS"); do
    dotnet $SEQUENCER_ASSEMBLY_FILE &
    pid1=$!
    pid2=$(ps -eo pid,cmd --sort=start_time | grep "$SEQUENCER_ASSEMBLY_FILE" | grep -v "grep" | awk 'END{print $1}')
    echo "Started $SEQUENCER_SERVICE_NAME instance number $i"
    echo "The PID of the command is: $pid1 and $pid2"
done


# ProcessorService

cd $PROCESSOR_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
for i in $(seq 1 "$NB_PROCESSORS"); do
    dotnet $PROCESSOR_ASSEMBLY_FILE &
    pid1=$!
    pid2=$(ps -eo pid,cmd --sort=start_time | grep "$SEQUENCER_ASSEMBLY_FILE" | grep -v "grep" | awk 'END{print $1}')
    echo "Started $PROCESSOR_SERVICE_NAME instance number $i"
    echo "The PID of the command is: $pid1 and $pid2"
done
