#!/bin/sh

REDIS_NAME=local-redis

REDIS_CONTAINER_ID=$(docker ps -a | grep "$REDIS_NAME" | awk '{print $1}')
if [ ! -z "$REDIS_CONTAINER_ID" ]; then echo "docker stop $REDIS_NAME $REDIS_CONTAINER_ID" && docker stop $REDIS_CONTAINER_ID > /dev/null; fi
if [ ! -z "$REDIS_CONTAINER_ID" ]; then echo "docker rm $REDIS_NAME $REDIS_CONTAINER_ID" && docker rm $REDIS_CONTAINER_ID > /dev/null; fi
