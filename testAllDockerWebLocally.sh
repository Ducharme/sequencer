#!/bin/sh

NB_PROCESSORS=10
NB_SEQUENCERS=3
ADMIN_WAIT_TIME_SEC=10
SEQUENCER_WAIT_TIME_SEC=10
PROCESSOR_WAIT_TIME_SEC=10
BUFFER_WAIT_TIME_SEC=3
DOCKER_START_DELAY=3

NB_MESSAGES=300
CREATION_DELAY_MS=100
PROCESSING_DELAY_MS=500
export RUN_ENV_FILE=.env.local

DEFAULT_CONTAINER_PORT=8080
AWP_CONTAINER_PORT=$DEFAULT_CONTAINER_PORT
PWS_CONTAINER_PORT=$DEFAULT_CONTAINER_PORT
SWS_CONTAINER_PORT=$DEFAULT_CONTAINER_PORT
CONTAINER_LOCALHOST=localhost

AWP_NAME=adminwebportal
PWS_NAME=processorwebservice
SWS_NAME=sequencerwebservice

FOLDER=$(pwd)

convert_to_comma_separated() {
    printf '%s' "$1" | tr '\n' ',' | sed 's/,$//'
}

get_first_item() {
    echo "$1" | tr ',' '\n' | head -n 1
}

if ! docker images | grep -q "$PWS_NAME"; then
    echo "Docker image \"$PWS_NAME\" does not exist. Please build the image and try again."
    exit 1
elif ! docker images | grep -q "$SWS_NAME"; then
    echo "Docker image \"$SWS_NAME\" does not exist. Please build the image and try again."
    exit 1
elif ! docker images | grep -q "$AWP_NAME"; then
    echo "Docker image \"$AWP_NAME\" does not exist. Please build the image and try again."
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
export PGSQL_ENDPOINT=$(getContainerIpFromImageName "local-postgres") && echo "PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-postgres is not running, exiting" && exit 1; fi
export REDIS_ENDPOINT=$(getContainerIpFromImageName "local-redis") && echo "REDIS_ENDPOINT=$REDIS_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-redis is not running, exiting" && exit 1; fi

EP_ENVS="-e REDIS_ENDPOINT=$REDIS_ENDPOINT -e PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
DD_ENVS="-e DD_API_KEY=$DD_API_KEY -e CORECLR_ENABLE_PROFILING=1 -e CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8} -e CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so -e DD_DOTNET_TRACER_HOME=/opt/datadog -e DD_PROCESS_AGENT_ENABLED=true"
LOGGING="--log-driver json-file"
. ./waitForContainer.sh

# AdminWebPortal

echo "docker run --name $AWP_NAME -d $LOGGING $EP_ENVS <DatadogEnvVars> $AWP_NAME" && docker run --name $AWP_NAME -d $LOGGING $EP_ENVS $DD_ENVS $AWP_NAME
sleep $DOCKER_START_DELAY
export AWP_CONTAINER_HOST=$(getContainerIpFromImageName "$AWP_NAME")
echo "AWP_CONTAINER_HOST=$AWP_CONTAINER_HOST and AWP_CONTAINER_PORT=$AWP_CONTAINER_PORT"
waitForContainer $ADMIN_WAIT_TIME_SEC $AWP_CONTAINER_HOST $AWP_CONTAINER_PORT

echo -n "curl -X DELETE http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/messages?name=$GROUP_NAME -> " && curl -X DELETE "http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/messages?name=$GROUP_NAME"
echo "\n"


# SequencerWebService

for i in $(seq 1 "$NB_SEQUENCERS"); do
    echo -n "docker run $LOGGING -d $EP_ENVS <DatadogEnvVars> $SWS_NAME (#$i) -> " && docker run $LOGGING -d $EP_ENVS $DD_ENVS $SWS_NAME && echo ""
done
sleep $DOCKER_START_DELAY
export SWS_CONTAINER_HOSTS=$(getContainerIpsFromImageName "$SWS_NAME")
SWS_CONTAINER_HOSTS_INLINE=$(convert_to_comma_separated "$SWS_CONTAINER_HOSTS")
echo "SWS_CONTAINER_HOSTS=$SWS_CONTAINER_HOSTS_INLINE and SWS_CONTAINER_PORT=$SWS_CONTAINER_PORT"
waitForContainers $ADMIN_WAIT_TIME_SEC $SWS_CONTAINER_HOSTS\$SWS_CONTAINER_PORT


# ProcessorWebService

for i in $(seq 1 "$NB_PROCESSORS"); do
    echo -n "docker run $LOGGING -d <DatadogEnvVars> $PWS_NAME (#$i) -> " && docker run $LOGGING -d $EP_ENVS $DD_ENVS $PWS_NAME && echo ""
done
sleep $DOCKER_START_DELAY
export PWS_CONTAINER_HOSTS=$(getContainerIpsFromImageName "$PWS_NAME")
PWS_CONTAINER_HOSTS_INLINE=$(convert_to_comma_separated "$PWS_CONTAINER_HOSTS")
echo "PWS_CONTAINER_HOSTS=$PWS_CONTAINER_HOSTS_INLINE and PWS_CONTAINER_PORT=$PWS_CONTAINER_PORT"
waitForContainers $ADMIN_WAIT_TIME_SEC $PWS_CONTAINER_HOSTS $PWS_CONTAINER_PORT


# Check health

FIRST_PWS_HOST=$(get_first_item "$PWS_CONTAINER_HOSTS_INLINE")
FIRST_SWS_HOST=$(get_first_item "$SWS_CONTAINER_HOSTS_INLINE")
echo ""
echo -n "curl -s -L -X GET http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/healthz ($AWP_NAME) -> " && curl -s -L -X GET "http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/healthz" && echo ""
echo -n "curl -s -L -X GET http://$FIRST_PWS_HOST:$PWS_CONTAINER_PORT/healthz ($PWS_NAME) -> " && curl -s -L -X GET "http://$FIRST_PWS_HOST:$PWS_CONTAINER_PORT/healthz" && echo ""
echo -n "curl -s -L -X GET http://$FIRST_SWS_HOST:$SWS_CONTAINER_PORT/healthz ($PWS_NAME) -> " && curl -s -L -X GET "http://$FIRST_SWS_HOST:$SWS_CONTAINER_PORT/healthz" && echo ""

# Initialize data

echo curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"$GROUP_NAME\", \"numberOfMessages\": $NB_MESSAGES, \"creationDelay\": $CREATION_DELAY_MS, \"processingTime\": $PROCESSING_DELAY_MS}" "http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/messages"
curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"$GROUP_NAME\", \"numberOfMessages\": $NB_MESSAGES, \"creationDelay\": $CREATION_DELAY_MS, \"processingTime\": $PROCESSING_DELAY_MS}" "http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/messages"
echo ""
sleep $RUNNING_WAIT_TIME_SEC


start=1
increment=100
end=$NB_MESSAGES
while [ "$start" -lt "$end" ]; do
  echo "Stats from $((start)) to $((start+increment-1))"
  curl -X GET "http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/list/stats?name=$GROUP_NAME&start=$start&count=$increment"
  echo ""
  start=$((start + increment))
done

cd $FOLDER
sh killDockerServicesLocally.sh

echo "DONE RUNNING!"
