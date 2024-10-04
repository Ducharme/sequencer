#!/bin/sh

halt_all_locally() {
    sh haltAllLocally.sh
    sleep 5
}

start_dependencies_containers() {
    sh startPostgresqlLocally.sh
    sh startRedisLocally.sh
    sleep 10
}

run_test() {
    awp_host=$1
    awp_port=$2

    # Initialize data

    echo ""
    echo curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"$GROUP_NAME\", \"numberOfMessages\": $NB_MESSAGES, \"creationDelay\": $CREATION_DELAY_MS, \"processingTime\": $PROCESSING_DELAY_MS}" "http://$awp_host:$awp_port/messages"
    curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"$GROUP_NAME\", \"numberOfMessages\": $NB_MESSAGES, \"creationDelay\": $CREATION_DELAY_MS, \"processingTime\": $PROCESSING_DELAY_MS}" "http://$awp_host:$awp_port/messages"
    echo ""

    # Waiting for completion
    . ./waitForCompletion.sh
    waitForCompletion $NB_MESSAGES $awp_host $awp_port $GROUP_NAME

    # Display statistics
    echo "The current time is $(date +"%H:%M:%S"), displaying stats"
    # Determine the increment based on NB_MESSAGES
    if [ "$NB_MESSAGES" -le 1000 ]; then
        increment=100
    elif [ "$NB_MESSAGES" -le 10000 ]; then
        increment=1000
    elif [ "$NB_MESSAGES" -le 100000 ]; then
        increment=10000
    else
        increment=10000  # Default for larger numbers
    fi
    start=1
    end=$NB_MESSAGES
    while [ "$start" -lt "$end" ]; do
        echo "Stats from $((start)) to $((start+increment-1))"
        curl -X GET "http://$awp_host:$awp_port/list/stats?name=$GROUP_NAME&start=$start&count=$increment"
        echo ""
        start=$((start + increment))
    done

    echo ""
    start=1
    end=$NB_MESSAGES
    echo "ALL -- Stats from $start to $end"
    curl -s -X GET "http://$awp_host:$awp_port/list/stats?name=$GROUP_NAME&start=$start&count=$end"
    echo ""
    echo "ALL -- perfs from $start to $end"
    curl -s -X GET "http://$awp_host:$awp_port/list/perfs?name=$GROUP_NAME&start=$start&count=$end"
    echo ""
}
