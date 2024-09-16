#!/bin/sh

NB_PROCESSORS=3
NB_SEQUENCERS=2
ADMIN_WAIT_TIME_SEC=5
SEQUENCER_WAIT_TIME_SEC=2
PROCESSOR_WAIT_TIME_SEC=5
BUFFER_WAIT_TIME_SEC=3
#CONFIG=Debug

NB_MESSAGES=300
CREATION_DELAY_MS=50
PROCESSING_DELAY_MS=500
export RUN_ENV_FILE=.env.local
DEFAULT_CONTAINER_HOST=localhost

FOLDER=$(pwd)

extractPortsAwk() {
    app_dll=$1
    ports=$(ps -eo command | grep -v "grep" | grep "dotnet $app_dll" | grep "urls" | awk -F: '{print $NF}')
    echo "$ports"
}

sh haltAllLocally.sh
sleep 5

sh startPostgresqlLocally.sh
sh startRedisLocally.sh
sleep 10


TIME_GEN=$(( $CREATION_DELAY_MS * $NB_MESSAGES / 1000 + 1 ))
TIME_PROC=$(( ( $NB_MESSAGES / $NB_PROCESSORS ) * $PROCESSING_DELAY_MS / 1000 + 1 ))
RUNNING_WAIT_TIME_SEC=$(( $TIME_GEN + $TIME_PROC + $BUFFER_WAIT_TIME_SEC ))
echo "Running time will be $RUNNING_WAIT_TIME_SEC seconds (Gen $TIME_GEN + Proc $TIME_PROC + Buffer $BUFFER_WAIT_TIME_SEC)"


. ./setConfigValues.sh
. ./setEnvFileValues.sh
. ./getContainerIP.sh
. ./getAvailablePorts.sh
export PGSQL_ENDPOINT=$(getContainerIpFromImageName "local-postgres") && echo "PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-postgres is not running, exiting" && exit 1; fi
export REDIS_ENDPOINT=$(getContainerIpFromImageName "local-redis") && echo "REDIS_ENDPOINT=$REDIS_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-redis is not running, exiting" && exit 1; fi


# AdminService

cd $ADMIN_WEB_PORTAL_PROJECT_FOLDER/bin/$CONFIG/net8.0
rm -f app-awp-*.log
AWP_PORTS=$(launchDotnetAppOnAvailablePorts 5710 1 $ADMIN_WEB_ASSEMBLY_FILE)
sleep $ADMIN_WAIT_TIME_SEC
ADMIN_CONTAINER_PORT=$(echo "$AWP_PORTS" | tr ',' '\n' | head -n 1)
echo -n "curl -s -L -X GET http://$DEFAULT_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/healthz -> " && curl -s -L -X GET "http://$DEFAULT_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/healthz" && echo ""
echo -n "curl -X DELETE http://$DEFAULT_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/messages?name=$GROUP_NAME ->" && curl -X DELETE "http://$DEFAULT_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/messages?name=$GROUP_NAME" && echo ""
echo ""


# SequencerService

cd $SEQUENCER_WEB_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
rm -f app-ss-*.log
SWS=$(launchDotnetAppOnAvailablePorts 5720 $NB_SEQUENCERS $SEQUENCER_WEB_ASSEMBLY_FILE)
sleep $SEQUENCER_WAIT_TIME_SEC


# ProcessorService

cd $PROCESSOR_WEB_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
rm -f app-ps-*.log
PWS=$(launchDotnetAppOnAvailablePorts 5750 $NB_PROCESSORS $PROCESSOR_WEB_ASSEMBLY_FILE)
sleep $PROCESSOR_WAIT_TIME_SEC


# Initialize data

echo curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"$GROUP_NAME\", \"numberOfMessages\": $NB_MESSAGES, \"creationDelay\": $CREATION_DELAY_MS, \"processingTime\": $PROCESSING_DELAY_MS}" "http://$DEFAULT_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/messages"
curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"$GROUP_NAME\", \"numberOfMessages\": $NB_MESSAGES, \"creationDelay\": $CREATION_DELAY_MS, \"processingTime\": $PROCESSING_DELAY_MS}" "http://$DEFAULT_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/messages"
echo ""
sleep $RUNNING_WAIT_TIME_SEC


start=1
increment=100
end=$NB_MESSAGES
while [ "$start" -lt "$end" ]; do
  echo "Stats from $((start)) to $((start+increment-1))"
  curl -X GET "http://$DEFAULT_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/list/stats?name=$GROUP_NAME&start=$start&count=$increment"
  echo ""
  start=$((start + increment))
done

#echo "Stats from redis servers"
#curl -X GET "http://$DEFAULT_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/servers/stats"

cd $FOLDER
sh killDotnetServicesLocally.sh

echo "DONE RUNNING!"
