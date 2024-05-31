#!/bin/sh

PGSQL_NAME=local-postgres

PGSQL_CONTAINER_ID=$(docker ps -a | grep "$PGSQL_NAME" | awk '{print $1}')
if [ ! -z "$PGSQL_CONTAINER_ID" ]; then echo "docker stop $PGSQL_NAME $PGSQL_CONTAINER_ID" && docker stop $PGSQL_CONTAINER_ID > /dev/null; fi
if [ ! -z "$PGSQL_CONTAINER_ID" ]; then echo "docker rm $PGSQL_NAME $PGSQL_CONTAINER_ID" && docker rm $PGSQL_CONTAINER_ID > /dev/null; fi
