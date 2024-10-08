#!/bin/sh

NB_PROCESSORS=10
NB_SEQUENCERS=3
SEQUENCER_WAIT_TIME_SEC=2
PROCESSOR_WAIT_TIME_SEC=5
BUFFER_WAIT_TIME_SEC=3
#CONFIG=Debug

NB_MESSAGES=300
CREATION_DELAY_MS=100
PROCESSING_DELAY_MS=500
export RUN_ENV_FILE=.env.production

FOLDER=$(pwd)

TIME_GEN=$(( $CREATION_DELAY_MS * $NB_MESSAGES / 1000 + 1 ))
TIME_PROC=$(( ( $NB_MESSAGES / $NB_PROCESSORS ) * $PROCESSING_DELAY_MS / 1000 + 1 ))
RUNNING_WAIT_TIME_SEC=$(( $TIME_GEN + $TIME_PROC + $BUFFER_WAIT_TIME_SEC ))
echo "Running time will be $RUNNING_WAIT_TIME_SEC seconds (Gen $TIME_GEN + Proc $TIME_PROC + Buffer $BUFFER_WAIT_TIME_SEC)"


ADMIN_SERVICE_NAME=AdminWebPortal
SEQUENCER_SERVICE_NAME=SequencerService
PROCESSOR_SERVICE_NAME=ProcessorService

ADMIN_ASSEMBLY_FILE=$ADMIN_SERVICE_NAME.dll
SEQUENCER_ASSEMBLY_FILE=$SEQUENCER_SERVICE_NAME.dll
PROCESSOR_ASSEMBLY_FILE=$PROCESSOR_SERVICE_NAME.dll


sh killDotnetServicesLocally.sh
sleep 5

ADMIN_CONTAINER_HOST=localhost
ADMIN_CONTAINER_PORT=5002
export ASPNETCORE_URLS="http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT"

# AdminService

cd ~/AdminWebPortal/
rm -f app-awp-*.log
. ~/setEnvVars.sh
dotnet $ADMIN_ASSEMBLY_FILE &
echo "Started $ADMIN_SERVICE_NAME instance"

# Function to check health status
check_health() {
  curl -s -X GET http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/healthz
}

# Loop until the health check returns "Healthy"
while true; do
  RESPONSE=$(check_health)
  if [ "$RESPONSE" = "Healthy" ]; then
    break
  fi
  echo "Waiting for AdminWebPortal service to become healthy..."
  sleep 1
done
sleep 1

curl -X DELETE "http://$ADMIN_CONTAINER_HOST:$ADMIN_CONTAINER_PORT/messages?name=$GROUP_NAME"
echo ""


# SequencerService

cd ~/SequencerService/
rm -f app-ss-*.log
. ~/setEnvVars.sh
for i in $(seq 1 "$NB_SEQUENCERS"); do
    dotnet $SEQUENCER_ASSEMBLY_FILE &
    echo "Started $SEQUENCER_SERVICE_NAME instance number $i"
    sleep 1
done
sleep $SEQUENCER_WAIT_TIME_SEC


# ProcessorService

cd ~/ProcessorService/
rm -f app-ps-*.log
. ~/setEnvVars.sh
for i in $(seq 1 "$NB_PROCESSORS"); do
    dotnet $PROCESSOR_ASSEMBLY_FILE &
    echo "Started $PROCESSOR_SERVICE_NAME instance number $i"
    sleep 1
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
