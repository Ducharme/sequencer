#!/bin/sh

NB_PROCESSORS=10
NB_SEQUENCERS=3
ADMIN_WAIT_TIME_SEC=2
SEQUENCER_WAIT_TIME_SEC=2
PROCESSOR_WAIT_TIME_SEC=5
BUFFER_WAIT_TIME_SEC=3
#CONFIG=Debug

NB_MESSAGES=300
CREATION_DELAY_MS=50
PROCESSING_DELAY_MS=500
export RUN_ENV_FILE=.env.local

FOLDER=$(pwd)

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
export PGSQL_ENDPOINT=$(getContainerIpFromImageName "local-postgres") && echo "PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-postgres is not running, exiting" && exit 1; fi
export REDIS_ENDPOINT=$(getContainerIpFromImageName "local-redis") && echo "REDIS_ENDPOINT=$REDIS_ENDPOINT"
if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-redis is not running, exiting" && exit 1; fi

ADMIN_CONTAINER_HOST=localhost
ADMIN_CONTAINER_PORT=5002
export ASPNETCORE_URLS="http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT"
#export DOTNET_URLS="http://0.0.0.0:$ADMIN_CONTAINER_PORT"

# AdminService

cd $ADMIN_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
rm -f app-awp-*.log
dotnet $ADMIN_ASSEMBLY_FILE &
echo "Started $ADMIN_SERVICE_NAME instance"
sleep $ADMIN_WAIT_TIME_SEC

curl -s -L -X GET "http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/healthz"
curl -X DELETE "http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/messages?name=$GROUP_NAME"
echo ""


# SequencerService

cd $SEQUENCER_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
rm -f app-ss-*.log
for i in $(seq 1 "$NB_SEQUENCERS"); do
    dotnet $SEQUENCER_ASSEMBLY_FILE &
    echo "Started $SEQUENCER_SERVICE_NAME instance number $i"
done
sleep $SEQUENCER_WAIT_TIME_SEC


# ProcessorService

cd $PROCESSOR_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
rm -f app-ps-*.log
for i in $(seq 1 "$NB_PROCESSORS"); do
    dotnet $PROCESSOR_ASSEMBLY_FILE &
    echo "Started $PROCESSOR_SERVICE_NAME instance number $i"
done
sleep $PROCESSOR_WAIT_TIME_SEC


# Initialize data

echo curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"$GROUP_NAME\", \"numberOfMessages\": $NB_MESSAGES, \"creationDelay\": $CREATION_DELAY_MS, \"processingTime\": $PROCESSING_DELAY_MS}" "http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/messages"
curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"$GROUP_NAME\", \"numberOfMessages\": $NB_MESSAGES, \"creationDelay\": $CREATION_DELAY_MS, \"processingTime\": $PROCESSING_DELAY_MS}" "http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/messages"
echo ""
sleep $RUNNING_WAIT_TIME_SEC


start=1
increment=100
end=$NB_MESSAGES
while [ "$start" -lt "$end" ]; do
  echo "Stats from $((start)) to $((start+increment-1))"
  curl -X GET "http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/list/stats?name=$GROUP_NAME&start=$start&count=$increment"
  echo ""
  start=$((start + increment))
done

#echo "Stats from redis servers"
#curl -X GET "http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/servers/stats"

cd $FOLDER
sh killDotnetServicesLocally.sh

echo "DONE RUNNING!"
