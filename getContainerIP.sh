#!/bin/sh

getContainerIP() {
    CONTAINER_NAME=$1
    CONTAINER_ID=$(docker ps -a | grep $CONTAINER_NAME | grep -v "Exited" | awk '{print $1}')
    if [ -z "$CONTAINER_ID" ]; then
        echo ""
    else
        CONTAINER_IP=$(docker inspect $CONTAINER_ID | grep -w '"IPAddress"' | head -n 1 | sed -E 's/.*"IPAddress": "([0-9.]+)".*/\1/')
        echo $CONTAINER_IP
    fi
}
