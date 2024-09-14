#!/bin/sh

NB_PROCESSORS=1
NB_SEQUENCERS=1
ADMIN_WAIT_TIME_SEC=2
SEQUENCER_WAIT_TIME_SEC=2
PROCESSOR_WAIT_TIME_SEC=5
BUFFER_WAIT_TIME_SEC=3

NB_MESSAGES=100
CREATION_DELAY_MS=100
PROCESSING_DELAY_MS=500
export RUN_ENV_FILE=.env.local

DEFAULT_CONTAINER_PORT=8080
AWP_CONTAINER_PORT=8081
PWS_CONTAINER_PORT=8082
SWS_CONTAINER_PORT=8083
CONTAINER_LOCALHOST=localhost

FOLDER=$(pwd)

if ! docker images | grep -q "processorwebservice"; then
    echo "Docker image 'processorwebservice' does not exist. Please build the image and try again."
    exit 1
elif ! docker images | grep -q "sequencerwebservice"; then
    echo "Docker image 'sequencerwebservice' does not exist. Please build the image and try again."
    exit 1
elif ! docker images | grep -q "adminwebportal"; then
    echo "Docker image 'adminwebportal' does not exist. Please build the image and try again."
    exit 1
fi


sh haltAllLocally.sh
sleep 5

sh startPostgresqlLocally.sh
sh startRedisLocally.sh
sleep 10


TIME_GEN=$(( $CREATION_DELAY_MS * $NB_MESSAGES / 1000 + 1 ))
TIME_PROC=$(( ( $NB_MESSAGES / $NB_PROCESSORS ) * $PROCESSING_DELAY_MS / 1000 + 1 ))
RUNNING_WAIT_TIME_SEC=$(( $TIME_GEN + $TIME_PROC + $BUFFER_WAIT_TIME_SEC ))
echo "Running time will be $RUNNING_WAIT_TIME_SEC seconds (Gen $TIME_GEN + Proc $TIME_PROC + Buffer $BUFFER_WAIT_TIME_SEC)"


. ./setEnvFileValues.sh
. ./getContainerIP.sh
export PGSQL_ENDPOINT=$(getContainerIP "local-postgres") && echo "PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-postgres is not running, exiting" && exit 1; fi
export REDIS_ENDPOINT=$(getContainerIP "local-redis") && echo "REDIS_ENDPOINT=$REDIS_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-redis is not running, exiting" && exit 1; fi

EP_ENVS="-e REDIS_ENDPOINT=$REDIS_ENDPOINT -e PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
DD_ENVS="-e DD_API_KEY=$DD_API_KEY -e CORECLR_ENABLE_PROFILING=1 -e CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8} -e CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so -e DD_DOTNET_TRACER_HOME=/opt/datadog -e DD_PROCESS_AGENT_ENABLED=true"
LOGGING="--log-driver json-file"


# AdminService

AWP_NAME=adminwebportal

echo "docker run --name $AWP_NAME -d -p $AWP_CONTAINER_PORT:$DEFAULT_CONTAINER_PORT $LOGGING $EP_ENVS <DatadogEnvVars> $AWP_NAME" && docker run --name $AWP_NAME -d -p $AWP_CONTAINER_PORT:$DEFAULT_CONTAINER_PORT $LOGGING $EP_ENVS $DD_ENVS $AWP_NAME
sleep $ADMIN_WAIT_TIME_SEC

export AWP_CONTAINER_HOST=$(getContainerIP "$AWP_NAME")
echo "AWP_CONTAINER_HOST=$AWP_CONTAINER_HOST and AWP_CONTAINER_PORT=$AWP_CONTAINER_PORT"

echo -n "curl -X DELETE http://$CONTAINER_LOCALHOST:$AWP_CONTAINER_PORT/messages?name=$GROUP_NAME -> " && curl -X DELETE "http://$CONTAINER_LOCALHOST:$AWP_CONTAINER_PORT/messages?name=$GROUP_NAME"
echo "\n"


# SequencerWebService

SWS_NAME=sequencerwebservice

for i in $(seq 1 "$NB_SEQUENCERS"); do
    echo -n "docker run $LOGGING -d -p $SWS_CONTAINER_PORT:$DEFAULT_CONTAINER_PORT $EP_ENVS <DatadogEnvVars> $SWS_NAME (#$i) -> " && docker run $LOGGING -d -p $SWS_CONTAINER_PORT:$DEFAULT_CONTAINER_PORT $EP_ENVS $DD_ENVS $SWS_NAME && echo ""
done
sleep $SEQUENCER_WAIT_TIME_SEC

export SWS_CONTAINER_HOST=$(getContainerIP "$SWS_NAME")
echo "SWS_CONTAINER_HOST=$SWS_CONTAINER_HOST and SWS_CONTAINER_PORT=$SWS_CONTAINER_PORT"


# ProcessorWebService

PWS_NAME=processorwebservice

for i in $(seq 1 "$NB_PROCESSORS"); do
    echo -n "docker run $LOGGING -d -p $PWS_CONTAINER_PORT:$DEFAULT_CONTAINER_PORT $EP_ENVS <DatadogEnvVars> $PWS_NAME (#$i) -> " && docker run $LOGGING -d -p $PWS_CONTAINER_PORT:$DEFAULT_CONTAINER_PORT $EP_ENVS $DD_ENVS $PWS_NAME && echo ""
done
sleep $PROCESSOR_WAIT_TIME_SEC

export PWS_CONTAINER_HOST=$(getContainerIP "$PWS_NAME")
echo "PWS_CONTAINER_HOST=$PWS_CONTAINER_HOST and PWS_CONTAINER_PORT=$PWS_CONTAINER_PORT"

# Check health

echo ""
echo -n "curl -s -L -X GET http://$CONTAINER_LOCALHOST:$AWP_CONTAINER_PORT/healthz -> " && curl -s -L -X GET "http://$CONTAINER_LOCALHOST:$AWP_CONTAINER_PORT/healthz" && echo ""
echo -n "curl -s -L -X GET http://$CONTAINER_LOCALHOST:$PWS_CONTAINER_PORT/healthz -> " && curl -s -L -X GET "http://$CONTAINER_LOCALHOST:$PWS_CONTAINER_PORT/healthz" && echo ""
echo -n "curl -s -L -X GET http://$CONTAINER_LOCALHOST:$SWS_CONTAINER_PORT/healthz -> " && curl -s -L -X GET "http://$CONTAINER_LOCALHOST:$SWS_CONTAINER_PORT/healthz" && echo ""

# Initialize data

echo curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"$GROUP_NAME\", \"numberOfMessages\": $NB_MESSAGES, \"creationDelay\": $CREATION_DELAY_MS, \"processingTime\": $PROCESSING_DELAY_MS}" "http://$CONTAINER_LOCALHOST:$AWP_CONTAINER_PORT/messages"
curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"$GROUP_NAME\", \"numberOfMessages\": $NB_MESSAGES, \"creationDelay\": $CREATION_DELAY_MS, \"processingTime\": $PROCESSING_DELAY_MS}" "http://$CONTAINER_LOCALHOST:$AWP_CONTAINER_PORT/messages"
echo ""
sleep $RUNNING_WAIT_TIME_SEC


start=1
increment=100
end=$NB_MESSAGES
while [ "$start" -lt "$end" ]; do
  echo "Stats from $((start)) to $((start+increment-1))"
  curl -X GET "http://$CONTAINER_LOCALHOST:$AWP_CONTAINER_PORT/list/stats?name=$GROUP_NAME&start=$start&count=$increment"
  echo ""
  start=$((start + increment))
done

cd $FOLDER
sh killDockerServicesLocally.sh

echo "DONE RUNNING!"
