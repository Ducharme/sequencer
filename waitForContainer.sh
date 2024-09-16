#!/bin/sh

waitForContainer() {
    local waitTimeInSec=$1
    local containerHost=$2
    local containerPort=$3

    for i in $(seq 1 $waitTimeInSec); do
        
        response=$(getHealthStatus $containerHost $containerPort)
        echo "Status code: $response"
        if [ "$response" = "200" ]; then
            echo "http://$containerHost:$containerPort/healthz $i -> $response, exiting wait loop"
            return 0  # Return 0 (success) which equates to "OK"
        else
            echo "http://$containerHost:$containerPort/healthz $i -> $response, continue waiting"
            sleep 1
        fi
    done

    return 1  # Return 1 (failure) which equates to "KO"
}

waitForContainers() {
    local waitTimeInSec=$1
    local containerHosts=$2
    local containerPort=$3

    for i in $(seq 1 $waitTimeInSec); do
        local allReady="TRUE"
        echo "$containerHosts" | tr ',' '\n' | while read -r containerHost; do
            response=$(getHealthStatus $containerHost $containerPort)
            if [ "$response" != "200" ]; then
                echo "Container $containerHost return status code $response, continue waiting"
                allReady="FALSE"
                sleep 1
                break
            fi
        done
        if [ "$allReady" = "TRUE" ]; then
            echo "Received 200 OK for all containers, exiting wait loop."
            return 0  # Return 0 (success) which equates to "OK"
        fi
    done

    echo "Did not received 200 OK for all. Exiting loop."
    return 1  # Return 1 (failure) which equates to "KO"
}

getHealthStatus() {
    local containerHost=$1
    local containerPort=$2
    response=$(curl -s -o /dev/null -w "%{http_code}" "http://$containerHost:$containerPort/healthz")
    echo $response
}