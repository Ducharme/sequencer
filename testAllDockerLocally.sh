#!/bin/sh

NB_PROCESSORS=10
NB_SEQUENCERS=3
ADMIN_WAIT_TIME_SEC=2
SEQUENCER_WAIT_TIME_SEC=2
PROCESSOR_WAIT_TIME_SEC=5
BUFFER_WAIT_TIME_SEC=3

NB_MESSAGES=300
CREATION_DELAY_MS=100
PROCESSING_DELAY_MS=500
export RUN_ENV_FILE=.env.local


FOLDER=$(pwd)

if ! docker images | grep -q "processorservice"; then
    echo "Docker image 'processorservice' does not exist. Please build the image and try again."
    exit 1
elif ! docker images | grep -q "sequencerservice"; then
    echo "Docker image 'sequencerservice' does not exist. Please build the image and try again."
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
LOGGING="--log-driver json-file"
ADMIN_CONTAINER_PORT=8080


# AdminService

AP_NAME=adminwebportal

echo "docker run --name $AP_NAME -d -p $ADMIN_CONTAINER_PORT:$ADMIN_CONTAINER_PORT $LOGGING $EP_ENVS $AP_NAME" && docker run --name $AP_NAME -d -p $ADMIN_CONTAINER_PORT:$ADMIN_CONTAINER_PORT $LOGGING $EP_ENVS $AP_NAME
sleep $ADMIN_WAIT_TIME_SEC

export ADMIN_CONTAINER_HOST=$(getContainerIP "$AP_NAME")
echo "ADMIN_CONTAINER_HOST=$ADMIN_CONTAINER_HOST"
echo "ADMIN_CONTAINER_PORT=$ADMIN_CONTAINER_PORT"

curl -X DELETE "http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/messages?name=$GROUP_NAME"
echo ""


# SequencerService

for i in $(seq 1 "$NB_SEQUENCERS"); do
    echo "docker run $LOGGING -d $EP_ENVS sequencerservice (#$i) " && docker run $LOGGING -d $EP_ENVS sequencerservice
done
sleep $SEQUENCER_WAIT_TIME_SEC


# ProcessorService

for i in $(seq 1 "$NB_PROCESSORS"); do
    echo "docker run $LOGGING -d $EP_ENVS processorservice (#$i) " && docker run $LOGGING -d $EP_ENVS processorservice 
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

cd $FOLDER
sh killDockerServicesLocally.sh

echo "DONE RUNNING!"
