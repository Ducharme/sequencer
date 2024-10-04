#!/bin/sh

convert_to_comma_separated() {
    printf '%s' "$1" | tr '\n' ',' | sed 's/,$//'
}

get_first_item() {
    echo "$1" | tr ',' '\n' | head -n 1
}

getContainerIpFromImageName() {
    IMAGE_NAME=$1
    CONTAINER_ID=$(docker ps --format "table {{.ID}}\t{{.Names}}\t{{.Image}}\t{{.Status}}" | grep $IMAGE_NAME | grep -v "Exited" | awk '{print $1}')
    if [ -z "$CONTAINER_ID" ]; then
        echo ""
    else
        CONTAINER_IP=$(getContainerIpFromContainerId "$CONTAINER_ID")
        echo "$CONTAINER_IP"
    fi
}

getContainerIpsFromImageName() {
    IMAGE_NAME=$1
    CONTAINER_IDS=$(docker ps --format "table {{.ID}}\t{{.Names}}\t{{.Image}}\t{{.Status}}" | grep "$IMAGE_NAME" | grep -v "Exited" | awk '{ids = ids $1 " "} END {print ids}' | sed 's/ $//')
    if [ -z "$CONTAINER_IDS" ]; then
        echo ""
    else
        echo "$CONTAINER_IDS" | tr ' ' '\n' | while read -r CONTAINER_ID; do
            CONTAINER_IP=$(getContainerIpFromContainerId "$CONTAINER_ID")
            if [ -n "$CONTAINER_IP" ]; then
                echo "$CONTAINER_IP"
            fi
        done
    fi
}

getContainerIpFromContainerId() {
    CONTAINER_ID=$1
    CONTAINER_IP=$(docker inspect $CONTAINER_ID | grep -w '"IPAddress"' | head -n 1 | sed -E 's/.*"IPAddress": "([0-9.]+)".*/\1/')
    echo $CONTAINER_IP
}

setDependenciesEndpoints() {
    export PGSQL_ENDPOINT=$(getContainerIpFromImageName "local-postgres") && echo "PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
    if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-postgres is not running, exiting" && exit 1; fi
    export REDIS_ENDPOINT=$(getContainerIpFromImageName "local-redis") && echo "REDIS_ENDPOINT=$REDIS_ENDPOINT"
    if [ -z "$PGSQL_ENDPOINT" ]; then echo "Container local-redis is not running, exiting" && exit 1; fi
}
